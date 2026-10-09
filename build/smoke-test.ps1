<#
.SYNOPSIS
    Starts the application, checks that its main window appears without an error dialog,
    optionally saves a screenshot, then closes it. Used by CI; also handy on a test PC.
#>
param(
    [Parameter(Mandatory = $true)] [string] $Exe,
    [string] $Screenshot,
    [int] $TimeoutSeconds = 20
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Windows.Forms, System.Drawing

$process = Start-Process -FilePath (Resolve-Path $Exe) -PassThru
try {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        Start-Sleep -Milliseconds 500
        $process.Refresh()
        if ($process.HasExited) { throw "The application exited early with code $($process.ExitCode)." }
    } until ($process.MainWindowHandle -ne 0 -or (Get-Date) -gt $deadline)

    if ($process.MainWindowHandle -eq 0) { throw "No window appeared within $TimeoutSeconds seconds." }
    Start-Sleep -Seconds 2
    $process.Refresh()

    $title = $process.MainWindowTitle
    if ($title -ne "Tenveo PTZ") { throw "Unexpected window '$title' (an error dialog?)." }
    Write-Host "Main window is up: '$title'"

    if ($Screenshot) {
        New-Item -ItemType Directory -Force (Split-Path $Screenshot) | Out-Null
        $bounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
        $bitmap = New-Object System.Drawing.Bitmap $bounds.Width, $bounds.Height
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.CopyFromScreen($bounds.Location, [System.Drawing.Point]::Empty, $bounds.Size)
        $bitmap.Save($Screenshot, [System.Drawing.Imaging.ImageFormat]::Png)
        $graphics.Dispose()
        $bitmap.Dispose()
        Write-Host "Screenshot saved to $Screenshot"
    }

    if (-not $process.CloseMainWindow()) { throw "The window did not accept the close request." }
    if (-not $process.WaitForExit(10000)) { throw "The application did not exit after closing its window." }
    Write-Host "Closed cleanly (exit code $($process.ExitCode))."
}
finally {
    if (-not $process.HasExited) { $process.Kill() }
}
