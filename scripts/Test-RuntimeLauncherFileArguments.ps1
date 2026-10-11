[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeLauncherRefusalTest.Common.ps1')
$launcher = Join-Path $PSScriptRoot 'Invoke-KingmakerRuntimeTest.ps1'
$cases = @(Get-KbpRuntimeLauncherAdvancedFamilyCases)
$native = @($cases | Where-Object { $_.Name -ceq 'advanced-native-ui' })
if ($native.Count -ne 1 -or $cases.Count -ne 7) { throw 'Missing or ambiguous Advanced family cases.' }
$native = $native[0]
$passed = 0
# The broad gate's exact case, process invocation and assertion, without its
# earlier live-purity windows. This guard returns before any external import.
$result = Invoke-KbpRuntimeLauncherFileRefusal -Launcher $launcher -Case $native
Write-Host "PASS $($result.Name): child exit $($result.ExitCode)"
Write-Host ($result.Output -join [Environment]::NewLine)
$passed++
foreach ($case in @(
    @{ Name = 'advanced-native-qualify-casing'; Expect = $native.Expect; Args = @('-Scenario', 'LIVE-WORKSPACE-NATIVE-UI', '-NativeUiStage', 'QUALIFY', '-FixtureFamily', 'ADVANCED', '-TimeoutSeconds', '600', '-WhatIf') },
    @{ Name = 'advanced-native-profile'; Expect = $native.Expect; Args = @('-Scenario', 'live-workspace-native-ui', '-NativeUiStage', 'facts', '-CompatibilityProfileId', 'advanced-gunslinger-0136', '-TimeoutSeconds', '600', '-WhatIf') }
)) {
    $result = Invoke-KbpRuntimeLauncherFileRefusal -Launcher $launcher -Case $case
    Write-Host "PASS $($result.Name): child exit $($result.ExitCode)"
    $passed++
}
# A different real refusal must not satisfy the fixture assertion.
$missingStage = @{ Name = 'native-missing-stage-is-not-fixture-refusal'; Expect = $native.Expect
    Args = @('-Scenario', 'live-workspace-native-ui', '-FixtureFamily', 'Advanced', '-TimeoutSeconds', '600', '-WhatIf') }
$rejected = $false
try { Invoke-KbpRuntimeLauncherFileRefusal -Launcher $launcher -Case $missingStage | Out-Null }
catch {
    if ($_.Exception.Message -notlike '*was not refused as expected*' -or
        $_.Exception.Message -notlike '*KBP_NATIVE_UI_STAGE_REQUIRED*') { throw }
    $rejected = $true
}
if (-not $rejected) { throw 'A different launcher refusal passed the fixture assertion.' }
Write-Host "PASS $($missingStage.Name)"
$passed++

# Consumer sentinels use real -File child output, but are not launcher guards
# or live-purity evidence. They preserve the legacy matcher and prove that
# neither a wrong marker nor a zero exit is accepted by the shared assertion.
$sentinel = Join-Path ([IO.Path]::GetTempPath()) ('kbp-file-refusal-' + [Guid]::NewGuid().ToString('N') + '.ps1')
@('param([string]$Marker, [int]$ExitCode)', 'Write-Output $Marker', 'exit $ExitCode') |
    Set-Content -LiteralPath $sentinel -Encoding ASCII
try {
    $legacy = $cases[0].Expect
    foreach ($case in @(
        @{ Name = 'legacy-refusal-retained'; Expect = $legacy; Marker = 'may only be loaded by the non-casting scenarios'; ExitCode = 1; Accept = $true },
        @{ Name = 'legacy-rejects-native-marker'; Expect = $legacy; Marker = 'KBP_NATIVE_UI_FIXTURE'; ExitCode = 1; Accept = $false },
        @{ Name = 'native-rejects-legacy-prose'; Expect = $native.Expect; Marker = 'may only be loaded by the non-casting scenarios'; ExitCode = 1; Accept = $false },
        @{ Name = 'native-rejects-success-exit'; Expect = $native.Expect; Marker = 'KBP_NATIVE_UI_FIXTURE'; ExitCode = 0; Accept = $false }
    )) {
        $childCase = @{ Name = $case.Name; Expect = $case.Expect; Args = @('-Marker', $case.Marker, '-ExitCode', [string]$case.ExitCode) }
        $accepted = $false
        try {
            Invoke-KbpRuntimeLauncherFileRefusal -Launcher $sentinel -Case $childCase | Out-Null
            $accepted = $true
        }
        catch { if ($_.Exception.Message -notlike '*was not refused as expected*') { throw } }
        if ($accepted -ne $case.Accept) { throw "Child-process refusal assertion failed for $($case.Name)." }
        Write-Host "PASS $($case.Name)"
        $passed++
    }
}
finally { Remove-Item -LiteralPath $sentinel -Force -ErrorAction SilentlyContinue }
Write-Host "Runtime launcher -File argument checks PASS: $passed; FAIL: 0. Focused checks; no live-purity proof."
