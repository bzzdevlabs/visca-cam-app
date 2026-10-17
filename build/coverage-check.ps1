<#
.SYNOPSIS
    Reads the Cobertura coverage report of the unit tests, writes a summary (Markdown, for the
    run page) and fails when line coverage is below the minimum.
.PARAMETER Path
    Folder searched for coverage.cobertura.xml (the test results folder).
.PARAMETER Minimum
    Minimum line coverage, in percent.
#>
param(
    [Parameter(Mandatory)] [string] $Path,
    [double] $Minimum = 90
)

$ErrorActionPreference = "Stop"

$report = Get-ChildItem -Path $Path -Recurse -Filter coverage.cobertura.xml | Sort-Object LastWriteTime | Select-Object -Last 1
if (-not $report) { throw "No coverage report under $Path." }

[xml] $coverage = Get-Content -Raw -LiteralPath $report.FullName
$lines = [math]::Round([double]$coverage.coverage.'line-rate' * 100, 1)
$branches = [math]::Round([double]$coverage.coverage.'branch-rate' * 100, 1)

# Least covered files first, so the gaps are easy to find (a file may hold several classes).
$weakest = $coverage.coverage.packages.package.classes.class | Group-Object filename | ForEach-Object {
    $fileLines = @($_.Group | ForEach-Object { $_.lines.line })
    $covered = @($fileLines | Where-Object { [int]$_.hits -gt 0 }).Count
    [pscustomobject]@{ File = Split-Path $_.Name -Leaf; Rate = if ($fileLines.Count) { $covered / $fileLines.Count } else { 1 } }
} | Sort-Object Rate | Select-Object -First 5

$status = if ($lines -ge $Minimum) { ":white_check_mark:" } else { ":x:" }
Write-Output "## $status Unit test coverage (TenveoPtz.Core)"
Write-Output ""
Write-Output "**Lines:** $lines % (minimum $Minimum %) - **Branches:** $branches %"
Write-Output ""
Write-Output "| Least covered files | Lines |"
Write-Output "| --- | --- |"
foreach ($file in $weakest) {
    Write-Output "| $($file.File) | $([math]::Round($file.Rate * 100, 1)) % |"
}

if ($lines -lt $Minimum) {
    Write-Host "::error title=Coverage too low::Line coverage is $lines %, below the minimum of $Minimum %."
    exit 1
}
