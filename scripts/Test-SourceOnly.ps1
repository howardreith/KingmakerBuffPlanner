[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

$root = Get-KbpRepositoryRoot
& (Join-Path $PSScriptRoot 'Validate-Source.ps1')
$msbuild = Get-KbpMsBuild
& $msbuild (Join-Path $root 'tests\KingmakerBuffPlanner.Tests\KingmakerBuffPlanner.Tests.csproj') /t:Rebuild /p:Configuration=Release /m /nologo /v:minimal
if ($LASTEXITCODE -ne 0) { throw 'Source-only test build failed.' }
$testExe = Join-Path $root 'artifacts\tests\KingmakerBuffPlanner.Tests.exe'
$prior = $env:KBP_TEST_GAME_PATH
try {
    $env:KBP_TEST_GAME_PATH = Get-KbpGamePath
    & $testExe
    if ($LASTEXITCODE -ne 0) { throw "Source-only test runner failed with exit code $LASTEXITCODE." }
    & (Join-Path $PSScriptRoot 'Test-RuntimeHarness.ps1')
    & (Join-Path $PSScriptRoot 'Test-DeploymentWhatIf.ps1')
}
finally {
    $env:KBP_TEST_GAME_PATH = $prior
}
Write-Host 'Source-only suite: PASS=1 FAIL=0'
