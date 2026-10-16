<#
.SYNOPSIS
    Live end-to-end test on the simulated camera: starts the application with --simulator, drives
    it like a user (held arrow and zoom keys, the "New" preset button, preset number keys, the
    numeric keypad while another application is in front) and checks where the simulated camera
    ended up, from the "Simulator at pan P, tilt T, zoom Z." lines of the application's log.
    Overwrites %APPDATA%\TenveoPTZ settings and presets: do not run it on a PC whose settings matter.
.PARAMETER Quirks
    Quirks of the simulated camera (see SimulatedCameraQuirks), e.g. "LaggingReadback".
#>
param(
    [Parameter(Mandatory = $true)] [string] $Exe,
    [string] $ScreenshotDir,
    [string] $Quirks = "LaggingReadback",
    [int] $TimeoutSeconds = 20
)

$ErrorActionPreference = "Stop"
if ($ScreenshotDir) {
    $ScreenshotDir = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ScreenshotDir)
    New-Item -ItemType Directory -Force $ScreenshotDir | Out-Null
}

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing
Add-Type -Namespace Win32 -Name Input -MemberDefinition @"
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, int dx, int dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window, out Rect rect);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr window, IntPtr dc, uint flags);
    [DllImport("user32.dll")] public static extern short GetKeyState(int key);
"@
[void][Win32.Input]::SetProcessDPIAware()
$automation = [System.Windows.Automation.AutomationElement]

$VK_LEFT = 0x25; $VK_UP = 0x26; $VK_RIGHT = 0x27; $VK_DOWN = 0x28
$VK_ADD = 0x6B; $VK_SUBTRACT = 0x6D; $VK_RETURN = 0x0D; $VK_NUMLOCK = 0x90
$KEYEVENTF_KEYUP = 0x2; $KEYEVENTF_EXTENDEDKEY = 0x1

$dataFolder = Join-Path $env:APPDATA "TenveoPTZ"
$logFolder = Join-Path $dataFolder "logs"

function Save-WindowImage([IntPtr] $Window, [string] $Name) {
    if (-not $ScreenshotDir) { return }
    $rect = New-Object Win32.Input+Rect
    [void][Win32.Input]::GetWindowRect($Window, [ref] $rect)
    $bitmap = New-Object System.Drawing.Bitmap ($rect.Right - $rect.Left), ($rect.Bottom - $rect.Top)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $dc = $graphics.GetHdc()
        [void][Win32.Input]::PrintWindow($Window, $dc, 2)
        $graphics.ReleaseHdc($dc)
        $bitmap.Save((Join-Path $ScreenshotDir "$Name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

function Get-LogLines {
    $log = Get-ChildItem $logFolder -Filter "tenveo-*.log" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime | Select-Object -Last 1
    if (-not $log) { return @() }
    return @(Get-Content $log.FullName)
}

function Wait-For([scriptblock] $Condition, [string] $What) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $result = & $Condition
        if ($result) { return $result }
        Start-Sleep -Milliseconds 250
    }
    Get-LogLines | Select-Object -Last 30 | Write-Host
    throw "Timed out after $TimeoutSeconds seconds waiting for $What (the app's log is above)."
}

# Position the simulated camera reported when it last stopped, after log line $After.
function Get-Position([int] $After = 0) {
    $lines = Get-LogLines
    for ($i = $lines.Count - 1; $i -ge $After; $i--) {
        if ($lines[$i] -match "Simulator at pan (-?\d+), tilt (-?\d+), zoom (-?\d+)\.") {
            return [pscustomobject]@{ Pan = [int]$Matches[1]; Tilt = [int]$Matches[2]; Zoom = [int]$Matches[3] }
        }
    }
    return $null
}

# Runs an action, then waits for the camera to report it stopped.
function Invoke-AndSettle([scriptblock] $Action, [string] $What) {
    $mark = (Get-LogLines).Count
    & $Action
    $position = Wait-For { Get-Position $mark } "the camera to stop after $What"
    Write-Host ("{0,-28} -> pan {1,4}  tilt {2,4}  zoom {3,4}" -f $What, $position.Pan, $position.Tilt, $position.Zoom)
    return $position
}

function Hold-Key([int] $Key, [int] $Milliseconds, [switch] $Extended) {
    $flags = if ($Extended) { $KEYEVENTF_EXTENDEDKEY } else { 0 }
    [Win32.Input]::keybd_event([byte]$Key, 0, $flags, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds $Milliseconds
    [Win32.Input]::keybd_event([byte]$Key, 0, $flags -bor $KEYEVENTF_KEYUP, [UIntPtr]::Zero)
}

function Press-Key([int] $Key) { Hold-Key $Key 50 }

function Assert-That([bool] $Condition, [string] $Message) {
    if (-not $Condition) { throw "FAILED: $Message" }
    Write-Host "ok: $Message"
}

function Assert-Same($Expected, $Actual, [string] $What) {
    Assert-That ($Expected.Pan -eq $Actual.Pan -and $Expected.Tilt -eq $Actual.Tilt -and $Expected.Zoom -eq $Actual.Zoom) `
        "$What is back at pan $($Expected.Pan), tilt $($Expected.Tilt), zoom $($Expected.Zoom) (got $($Actual.Pan), $($Actual.Tilt), $($Actual.Zoom))"
}

function Focus-App {
    [void][Win32.Input]::SetForegroundWindow($process.MainWindowHandle)
    Wait-For { [Win32.Input]::GetForegroundWindow() -eq $process.MainWindowHandle } "the application to come to the front" | Out-Null
}

function Click-Button([string] $Name) {
    # Not named $condition: Wait-For's $Condition parameter would shadow it inside the script block.
    $byName = New-Object System.Windows.Automation.AndCondition @(
        (New-Object System.Windows.Automation.PropertyCondition $automation::NameProperty, $Name),
        (New-Object System.Windows.Automation.PropertyCondition $automation::ProcessIdProperty, $process.Id))
    $button = Wait-For { $automation::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $byName) } "the '$Name' button"
    $bounds = $button.Current.BoundingRectangle
    [void][Win32.Input]::SetCursorPos([int]($bounds.X + $bounds.Width / 2), [int]($bounds.Y + $bounds.Height / 2))
    [Win32.Input]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    [Win32.Input]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
}

function Save-Preset([int] $Number) {
    Focus-App
    $mark = (Get-LogLines).Count
    Click-Button "New"
    Start-Sleep -Milliseconds 700 # The name prompt opens with "Preset n" selected.
    Press-Key $VK_RETURN
    Wait-For { (Get-LogLines | Select-Object -Skip $mark) -match "Saved preset" } "preset $Number to be saved" | Out-Null
    Write-Host "Saved preset $Number."
}

# Simulated camera, preview on, continuous moves off (the default), keypad active in the background.
New-Item -ItemType Directory -Force $dataFolder | Out-Null
Remove-Item (Join-Path $logFolder "*") -ErrorAction SilentlyContinue
@"
<?xml version="1.0" encoding="utf-8"?>
<AppSettings>
  <Connection><VideoDeviceName>PTZ Simulator</VideoDeviceName><Mode>Uvc</Mode><ShowPreview>true</ShowPreview></Connection>
  <AutoConnect>true</AutoConnect>
  <Speed>0.5</Speed>
  <BackgroundNumpad>true</BackgroundNumpad>
  <CheckForUpdates>false</CheckForUpdates>
</AppSettings>
"@ | Set-Content -Encoding UTF8 (Join-Path $dataFolder "settings.xml")
"<?xml version=`"1.0`" encoding=`"utf-8`"?><Presets />" | Set-Content -Encoding UTF8 (Join-Path $dataFolder "presets.xml")

$process = Start-Process -FilePath (Resolve-Path $Exe) -ArgumentList "--simulator=$Quirks", "--theme=light" -PassThru
$notepad = $null
try {
    Wait-For { $process.Refresh(); $process.MainWindowHandle -ne 0 } "the main window" | Out-Null
    Wait-For { (Get-LogLines) -match "Connected: PTZ Simulator" } "the connection to the simulator" | Out-Null
    $start = Wait-For { Get-Position } "the simulator's first position"
    Write-Host "Connected to the simulator (quirks: $Quirks) at pan $($start.Pan), tilt $($start.Tilt), zoom $($start.Zoom)."
    Focus-App

    # Arrow keys and zoom keys, held. Tenveo convention: right decreases pan, up decreases tilt.
    $p = Invoke-AndSettle { Hold-Key $VK_RIGHT 1500 -Extended } "Right arrow held 1.5 s"
    Assert-That ($p.Pan -le $start.Pan - 3) "the Right arrow turns the camera right"
    $q = Invoke-AndSettle { Hold-Key $VK_UP 1000 -Extended } "Up arrow held 1 s"
    Assert-That ($q.Tilt -le $p.Tilt - 3) "the Up arrow tilts the camera up"
    $r = Invoke-AndSettle { Hold-Key $VK_ADD 1000 } "+ held 1 s"
    Assert-That ($r.Zoom -ge $q.Zoom + 10) "+ zooms in"
    $s = Invoke-AndSettle { Hold-Key $VK_LEFT 800 -Extended } "Left arrow held 0.8 s"
    Assert-That ($s.Pan -ge $r.Pan + 2) "the Left arrow turns the camera left"
    Save-WindowImage $process.MainWindowHandle "screenshot-simulator"

    # Presets: save two positions, then go back and forth between them.
    Save-Preset 1
    $first = Get-Position
    $second = Invoke-AndSettle { Hold-Key $VK_DOWN 1200 -Extended; Hold-Key $VK_SUBTRACT 500 } "Down then - held"
    Save-Preset 2
    Assert-That ($first.Tilt -ne $second.Tilt) "the two presets are different positions"
    Focus-App
    foreach ($round in 1..2) {
        Assert-Same $first (Invoke-AndSettle { Press-Key 0x31 } "key 1 (preset 1)") "preset 1"
        Assert-Same $second (Invoke-AndSettle { Press-Key 0x32 } "key 2 (preset 2)") "preset 2"
    }

    # Numeric keypad while another application is in front (Num Lock off: keypad 4 = Left).
    if ([Win32.Input]::GetKeyState($VK_NUMLOCK) -band 1) { Press-Key $VK_NUMLOCK }
    $notepad = Start-Process notepad -PassThru
    Wait-For { $notepad.Refresh(); $notepad.MainWindowHandle -ne 0 -and [Win32.Input]::GetForegroundWindow() -eq $notepad.MainWindowHandle } "Notepad in front" | Out-Null
    $before = Get-Position
    $t = Invoke-AndSettle { Hold-Key $VK_LEFT 1000 } "keypad 4 held 1 s in Notepad"
    Assert-That ($t.Pan -ge $before.Pan + 2) "the keypad turns the camera while Notepad is in front"
    Assert-That ([Win32.Input]::GetForegroundWindow() -eq $notepad.MainWindowHandle) "Notepad stayed in front"

    Write-Host "All simulator checks passed."
}
catch {
    if ($process -and -not $process.HasExited) { Save-WindowImage $process.MainWindowHandle "simulator-failure" }
    throw
}
finally {
    if ($notepad -and -not $notepad.HasExited) { $notepad.Kill() }
    if ($process -and -not $process.HasExited) {
        if (-not $process.CloseMainWindow() -or -not $process.WaitForExit(10000)) { $process.Kill() }
    }
}
