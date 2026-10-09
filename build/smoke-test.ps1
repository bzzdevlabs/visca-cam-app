<#
.SYNOPSIS
    Starts the application, checks that its main window appears without an error dialog, saves
    a screenshot of the window, then closes it. Runs once per theme. Used by CI; also handy on a
    test PC.
.PARAMETER SamplePresets
    Writes sample presets and settings to %APPDATA%\TenveoPTZ first so the screenshots show a
    filled list; the light run uses USB (UVC) control and the dark run serial (VISCA) control,
    so both header layouts are covered. Do not use it on a PC whose settings matter.
#>
param(
    [Parameter(Mandatory = $true)] [string] $Exe,
    [string] $ScreenshotDir,
    [string[]] $Themes = @("light", "dark"),
    [switch] $SamplePresets,
    [int] $TimeoutSeconds = 20
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
# Win32 calls only: System.Drawing types are used from PowerShell so the snippet compiles on
# both Windows PowerShell 5.1 and PowerShell 7 (where Bitmap lives in System.Drawing.Common).
Add-Type -Namespace Win32 -Name Window -MemberDefinition @"
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window, out Rect rect);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr window, IntPtr dc, uint flags);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
"@

function Save-WindowImage([IntPtr] $Window, [string] $Path) {
    [void][Win32.Window]::SetProcessDPIAware()
    $rect = New-Object Win32.Window+Rect
    [void][Win32.Window]::GetWindowRect($Window, [ref] $rect)
    $bitmap = New-Object System.Drawing.Bitmap ($rect.Right - $rect.Left), ($rect.Bottom - $rect.Top)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $dc = $graphics.GetHdc()
        # PW_RENDERFULLCONTENT (2) captures the window even when other windows cover it.
        [void][Win32.Window]::PrintWindow($Window, $dc, 2)
        $graphics.ReleaseHdc($dc)
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

if ($SamplePresets) {
    $dataFolder = Join-Path $env:APPDATA "TenveoPTZ"
    New-Item -ItemType Directory -Force $dataFolder | Out-Null
    @"
<?xml version="1.0" encoding="utf-8"?>
<Presets>
  <Preset><Name>Wide shot</Name><Slot>1</Slot></Preset>
  <Preset><Name>Lectern</Name><Slot>2</Slot></Preset>
  <Preset><Name>Platform left</Name><Slot>3</Slot></Preset>
  <Preset><Name>Platform right</Name><Slot>4</Slot></Preset>
  <Preset><Name>Audience</Name><Slot>5</Slot></Preset>
</Presets>
"@ | Set-Content -Encoding UTF8 (Join-Path $dataFolder "presets.xml")
}

$sampleModes = @{ light = "Uvc"; dark = "Visca" }

foreach ($theme in $Themes) {
    if ($SamplePresets -and $sampleModes.ContainsKey($theme)) {
        @"
<?xml version="1.0" encoding="utf-8"?>
<AppSettings>
  <Connection><Mode>$($sampleModes[$theme])</Mode><BaudRate>9600</BaudRate><Address>1</Address></Connection>
  <Speed>0.5</Speed>
</AppSettings>
"@ | Set-Content -Encoding UTF8 (Join-Path $env:APPDATA "TenveoPTZ\settings.xml")
    }

    $process = Start-Process -FilePath (Resolve-Path $Exe) -ArgumentList "--theme=$theme" -PassThru
    try {
        $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
        do {
            Start-Sleep -Milliseconds 500
            $process.Refresh()
            if ($process.HasExited) { throw "[$theme] The application exited early with code $($process.ExitCode)." }
        } until ($process.MainWindowHandle -ne 0 -or (Get-Date) -gt $deadline)

        if ($process.MainWindowHandle -eq 0) { throw "[$theme] No window appeared within $TimeoutSeconds seconds." }
        Start-Sleep -Seconds 2
        $process.Refresh()

        $title = $process.MainWindowTitle
        if ($title -ne "Tenveo PTZ") { throw "[$theme] Unexpected window '$title' (an error dialog?)." }
        Write-Host "[$theme] Main window is up."

        if ($ScreenshotDir) {
            New-Item -ItemType Directory -Force $ScreenshotDir | Out-Null
            $path = Join-Path $ScreenshotDir "screenshot-$theme.png"
            Save-WindowImage $process.MainWindowHandle $path
            Write-Host "[$theme] Screenshot saved to $path"
        }

        if (-not $process.CloseMainWindow()) { throw "[$theme] The window did not accept the close request." }
        if (-not $process.WaitForExit(10000)) { throw "[$theme] The application did not exit after closing its window." }
        Write-Host "[$theme] Closed cleanly (exit code $($process.ExitCode))."
    }
    finally {
        if (-not $process.HasExited) { $process.Kill() }
    }
}
