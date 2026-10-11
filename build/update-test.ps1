<#
.SYNOPSIS
    End-to-end test of the automatic update: runs a copy of the application pretending to be
    version 0.0.0.1, accepts the "Update available" prompt, then checks that the files were
    replaced by the latest GitHub release and that the application started again. Needs
    network access to GitHub. Used by CI; also handy on a test PC (the copy runs in %TEMP%).
#>
param(
    [Parameter(Mandatory = $true)] [string] $Exe,
    [int] $TimeoutSeconds = 60
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Windows.Forms
$automation = [System.Windows.Automation.AutomationElement]

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
    # The custom-drawn button exposes no Invoke pattern: focus it and press Space, like a keyboard user.
    $button.SetFocus()
    Start-Sleep -Milliseconds 300
    [System.Windows.Forms.SendKeys]::SendWait(" ")

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
finally {
    foreach ($p in @($process, $restarted)) {
        if ($p -and -not $p.HasExited) { $p.Kill() }
    }
}
