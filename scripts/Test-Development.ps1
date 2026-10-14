[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string[]]$Only,
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [ValidateSet('RuntimeHarness', 'ProblemNavigationEvidence', 'SpellbookEntryEvidence', 'FixtureInventoryEvidence', 'PhysicalSearchEvidence', 'PolicyQualificationEvidence', 'DisplayTransaction')]
    [string[]]$Check = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

# Validate selection before compilation. -File accepts a comma-separated value.
$prefixes = @($Only | ForEach-Object { $_ -split '[,;]' } |
    ForEach-Object { $_.Trim() } | Where-Object { $_ } | Select-Object -Unique)
if ($prefixes.Count -eq 0) { throw 'Development validation requires a non-empty test prefix.' }
$root = Get-KbpRepositoryRoot
$started = [Diagnostics.Stopwatch]::StartNew()
$hadGamePath = Test-Path Env:\KBP_TEST_GAME_PATH
$priorGamePath = $env:KBP_TEST_GAME_PATH
try {
    $env:KBP_TEST_GAME_PATH = Get-KbpGamePath
    # The solution contains production only. Keep its source-revision stamping.
    & (Join-Path $PSScriptRoot 'Build.ps1') -Configuration $Configuration | Out-Host
    $msbuild = Get-KbpMsBuild
    & $msbuild (Join-Path $root 'tests\KingmakerBuffPlanner.Tests\KingmakerBuffPlanner.Tests.csproj') `
        /t:Rebuild "/p:Configuration=$Configuration" /m /nologo /v:minimal | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Development test build failed.' }
    $testExe = Join-Path $root 'artifacts\tests\KingmakerBuffPlanner.Tests.exe'
    if (-not (Test-Path -LiteralPath $testExe -PathType Leaf)) { throw 'Current test runner is missing.' }
    $testArguments = @('--only') + $prefixes
    $output = @(& $testExe @testArguments)
    $testExit = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }
    $summary = @($output | Where-Object { $_ -match '^Protocol tests: PASS=\d+ FAIL=\d+$' })
    if ($summary.Count -ne 1) { throw 'Development runner did not report exactly one test count.' }
    $counts = [regex]::Match($summary[0], 'PASS=(\d+) FAIL=(\d+)')
    $passed = [int]$counts.Groups[1].Value
    $failed = [int]$counts.Groups[2].Value
    if (($passed + $failed) -eq 0) { throw "No tests matched: $($prefixes -join ', ')." }
    if ($testExit -ne 0 -or $failed -ne 0) { throw "Development tests failed with exit code $testExit (PASS=$passed FAIL=$failed)." }
    foreach ($name in @($Check | Select-Object -Unique)) {
        # Existing bounded filesystem/reader checks only; no live/publisher path.
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "Test-$name.ps1")
        if ($LASTEXITCODE -ne 0) { throw "Development check $name failed with exit code $LASTEXITCODE." }
    }
    Write-Host ("DEVELOPMENT PASS=$passed FAIL=$failed prefixes=$($prefixes -join ',') seconds=$([Math]::Round($started.Elapsed.TotalSeconds, 1)); native/purity/release coverage deferred.")
}
finally {
    if ($hadGamePath) { $env:KBP_TEST_GAME_PATH = $priorGamePath }
    else { Remove-Item Env:\KBP_TEST_GAME_PATH -ErrorAction SilentlyContinue }
    $started.Stop()
}
