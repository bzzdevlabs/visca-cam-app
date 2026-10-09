<#
.SYNOPSIS
    Starts the application, checks that its main window appears without an error dialog, saves
    a screenshot of the window, then closes it. Runs once per theme. Used by CI; also handy on a
    test PC.
.PARAMETER SamplePresets
    Writes a few sample presets to %APPDATA%\TenveoPTZ first so the screenshots show a filled
    list. Do not use it on a PC whose presets matter.
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
Add-Type -TypeDefinition @"
using System;
using System.Drawing;
using System.Runtime.InteropServices;

public static class WindowCapture
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out Rect rect);
    [DllImport("user32.dll")] private static extern bool PrintWindow(IntPtr window, IntPtr dc, uint flags);
    [DllImport("user32.dll")] private static extern bool SetProcessDPIAware();

    // PW_RENDERFULLCONTENT captures the window even when other windows cover it.
    public static Bitmap Capture(IntPtr window)
    {
        SetProcessDPIAware();
        Rect rect;
        GetWindowRect(window, out rect);
        var bitmap = new Bitmap(rect.Right - rect.Left, rect.Bottom - rect.Top);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            var dc = graphics.GetHdc();
            PrintWindow(window, dc, 2);
            graphics.ReleaseHdc(dc);
        }
        return bitmap;
    }
}
"@ -ReferencedAssemblies System.Drawing

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

foreach ($theme in $Themes) {
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
            $bitmap = [WindowCapture]::Capture($process.MainWindowHandle)
            $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
            $bitmap.Dispose()
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
