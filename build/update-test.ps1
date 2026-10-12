<#
.SYNOPSIS
    End-to-end test of the automatic update: runs a copy of the application pretending to be
    version 0.0.0.1, accepts the "Update available" prompt, then checks that the executable was
    replaced by the one of the latest GitHub release and that the application started again. Needs
    network access to GitHub. Used by CI; also handy on a test PC (the copy runs in %TEMP%).
#>
param(
    [Parameter(Mandatory = $true)] [string] $Exe,
    [string] $ScreenshotDir,
    [int] $TimeoutSeconds = 60
)

$ErrorActionPreference = "Stop"
if ($ScreenshotDir) {
    # .NET resolves relative paths against the process directory, not the PowerShell location.
    $ScreenshotDir = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ScreenshotDir)
}

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing
Add-Type -Namespace Win32 -Name Mouse -MemberDefinition @"
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, int dx, int dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern int GetSystemMetrics(int index);
"@
[void][Win32.Mouse]::SetProcessDPIAware() # Bounding rectangles and the screen are in physical pixels.
$automation = [System.Windows.Automation.AutomationElement]

# Whole primary screen: shows the prompt, or what is in the way when the test fails.
function Save-ScreenImage([string] $Name) {
    if (-not $ScreenshotDir) { return }
    New-Item -ItemType Directory -Force $ScreenshotDir | Out-Null
    $width = [Win32.Mouse]::GetSystemMetrics(0)
    $height = [Win32.Mouse]::GetSystemMetrics(1)
    $bitmap = New-Object System.Drawing.Bitmap $width, $height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen(0, 0, 0, 0, $bitmap.Size)
        $bitmap.Save((Join-Path $ScreenshotDir "$Name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

function Wait-For([scriptblock] $Condition, [string] $What) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $result = & $Condition
        if ($result) { return $result }
        Start-Sleep -Milliseconds 500
    }
    throw "Timed out after $TimeoutSeconds seconds waiting for $What."
}

function Find-Named($Root, [string] $Name, [int] $ProcessId) {
    $condition = New-Object System.Windows.Automation.AndCondition @(
        (New-Object System.Windows.Automation.PropertyCondition $automation::NameProperty, $Name),
        (New-Object System.Windows.Automation.PropertyCondition $automation::ProcessIdProperty, $ProcessId))
    return $Root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}

# The update installs the release's .exe: releases published before the app came as a single
# .exe (they held a .zip) cannot be updated to, so there is nothing to test until one exists.
# Authenticated when CI provides a token: anonymous calls share the runner IP's small rate limit.
$headers = @{ "User-Agent" = "TenveoPTZ-update-test" }
if ($env:GITHUB_TOKEN) { $headers["Authorization"] = "Bearer $env:GITHUB_TOKEN" }
$latest = Invoke-RestMethod "https://api.github.com/repos/bzzdevlabs/visca-cam-app/releases/latest" -Headers $headers
if (-not ($latest.assets | Where-Object { $_.name -like "*.exe" })) {
    Write-Host "::notice title=Update test skipped::The latest release ($($latest.tag_name)) has no .exe to update to."
    return
}

# Work on a copy: the update replaces the files in place.
$work = Join-Path ([IO.Path]::GetTempPath()) "TenveoPTZ-update-test"
if (Test-Path $work) { Remove-Item -Recurse -Force $work }
New-Item -ItemType Directory $work | Out-Null
Copy-Item (Join-Path (Split-Path (Resolve-Path $Exe)) "*") $work
$copy = Join-Path $work (Split-Path $Exe -Leaf)
$before = (Get-Item $copy).VersionInfo.FileVersion
Write-Host "Copied version $before to $work"

$process = Start-Process -FilePath $copy -ArgumentList "--simulate-version=0.0.0.1" -PassThru
$restarted = $null
try {
    $dialog = Wait-For { Find-Named $automation::RootElement "Update available" $process.Id } "the 'Update available' prompt"
    Write-Host "Prompt shown."
    $button = Find-Named $dialog "Update" $process.Id
    if (-not $button) { throw "The prompt has no 'Update' button." }
    # The custom-drawn button exposes no Invoke pattern and cannot be focused through UI
    # automation: click its centre with the mouse, like a user.
    Save-ScreenImage "update-prompt"
    $bounds = $button.Current.BoundingRectangle
    Write-Host "Clicking the 'Update' button at $bounds."
    [void][Win32.Mouse]::SetCursorPos([int]($bounds.X + $bounds.Width / 2), [int]($bounds.Y + $bounds.Height / 2))
    [Win32.Mouse]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero) # Left button down.
    [Win32.Mouse]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero) # Left button up.

    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) { throw "The application did not close to install the update." }
    Write-Host "Closed for the update (exit code $($process.ExitCode))."

    $restarted = Wait-For {
        Get-Process -Name ([IO.Path]::GetFileNameWithoutExtension($copy)) -ErrorAction SilentlyContinue |
            Where-Object { $_.Path -eq $copy -and $_.MainWindowHandle -ne 0 } | Select-Object -First 1
    } "the restarted application"

    $after = (Get-Item $copy).VersionInfo.FileVersion
    if ($after -eq $before) { throw "The executable was not replaced (still version $before)." }
    if ($restarted.MainWindowTitle -ne "Tenveo PTZ") { throw "Unexpected window '$($restarted.MainWindowTitle)' after the restart." }
    Write-Host "Updated from $before to $after and restarted."

    if (-not $restarted.CloseMainWindow() -or -not $restarted.WaitForExit(10000)) { throw "The updated application did not close." }
}
catch {
    Save-ScreenImage "update-failure"
    throw
}
finally {
    foreach ($p in @($process, $restarted)) {
        if ($p -and -not $p.HasExited) { $p.Kill() }
    }
}
