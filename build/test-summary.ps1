<#
.SYNOPSIS
    Turns .trx test results into Markdown for the GitHub Actions job summary: totals, failed
    tests with their messages, and the result of every test grouped by test class.
.EXAMPLE
    ./build/test-summary.ps1 -Path dist/test-results >> $env:GITHUB_STEP_SUMMARY
#>
param(
    [Parameter(Mandatory = $true)] [string] $Path
)

$ErrorActionPreference = "Stop"

function Format-Duration([TimeSpan] $duration) {
    $culture = [System.Globalization.CultureInfo]::InvariantCulture
    if ($duration.TotalSeconds -ge 1) { return $duration.TotalSeconds.ToString("0.00 s", $culture) }
    return $duration.TotalMilliseconds.ToString("0.# ms", $culture)
}

function Format-Cell([string] $text) { return $text.Replace("|", "\|").Replace("`r", "").Replace("`n", " ") }

function Get-Icon([string] $outcome) {
    switch ($outcome) {
        "Passed" { return ":white_check_mark:" }
        "Failed" { return ":x:" }
        default { return ":fast_forward:" }
    }
}

$files = @(Get-ChildItem -Path $Path -Filter *.trx -Recurse)
if ($files.Count -eq 0) {
    "## Tests`n`n:warning: No test results found in ``$Path``."
    return
}

$results = foreach ($file in $files) {
    [xml] $run = Get-Content -Raw -LiteralPath $file.FullName
    $classes = @{}
    foreach ($definition in $run.TestRun.TestDefinitions.UnitTest) {
        $classes[$definition.id] = $definition.TestMethod.className
    }

    foreach ($result in $run.TestRun.Results.UnitTestResult) {
        $class = $classes[$result.testId]
        [pscustomobject]@{
            Class    = ($class -split "\.")[-1]
            Name     = $result.testName.Substring($class.Length + 1)
            Outcome  = $result.outcome
            Duration = [TimeSpan]::Parse($result.duration)
            Message  = $result.Output.ErrorInfo.Message
            Stack    = $result.Output.ErrorInfo.StackTrace
        }
    }
}

$passed = @($results | Where-Object Outcome -eq "Passed").Count
$failed = @($results | Where-Object Outcome -eq "Failed").Count
$skipped = $results.Count - $passed - $failed
$total = [TimeSpan]::FromTicks(($results | Measure-Object -Property { $_.Duration.Ticks } -Sum).Sum)

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add("## $(if ($failed -gt 0) { ':x:' } else { ':white_check_mark:' }) Tests")
$lines.Add("")
$lines.Add("| Total | Passed | Failed | Skipped | Duration |")
$lines.Add("| ---: | ---: | ---: | ---: | ---: |")
$lines.Add("| $($results.Count) | $passed | $failed | $skipped | $(Format-Duration $total) |")
$lines.Add("")

foreach ($failure in $results | Where-Object Outcome -eq "Failed") {
    $lines.Add("### :x: $($failure.Class).$(Format-Cell $failure.Name)")
    $lines.Add("")
    $lines.Add('```')
    $lines.Add(($failure.Message + "`n" + $failure.Stack).Trim())
    $lines.Add('```')
    $lines.Add("")
}

foreach ($group in $results | Group-Object Class | Sort-Object Name) {
    $groupFailed = @($group.Group | Where-Object Outcome -eq "Failed").Count
    $groupPassed = @($group.Group | Where-Object Outcome -eq "Passed").Count
    $open = if ($groupFailed -gt 0) { " open" } else { "" }
    $lines.Add("<details$open><summary>$(if ($groupFailed -gt 0) { ':x:' } else { ':white_check_mark:' }) <b>$($group.Name)</b>: $groupPassed/$($group.Count) passed</summary>")
    $lines.Add("")
    $lines.Add("| | Test | Duration |")
    $lines.Add("| --- | --- | ---: |")
    foreach ($test in $group.Group | Sort-Object Name) {
        $lines.Add("| $(Get-Icon $test.Outcome) | $(Format-Cell $test.Name) | $(Format-Duration $test.Duration) |")
    }

    $lines.Add("")
    $lines.Add("</details>")
    $lines.Add("")
}

$lines -join "`n"
