[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

# Exact-product reader correction, NOT a new publication/staging/launch route.
# Alpha4's aggregate stopped on a source-adjacency test that also fails on
# Alpha3. The corrected test checks compiled reset-before-close ordering,
# including missing/late/wrong-value negatives. All other source and safety
# checks still run. Product and test-harness identities remain separate.
$harnessRoot = Get-KbpRepositoryRoot
if (@(& git -C $harnessRoot status --porcelain).Count -ne 0 -or $LASTEXITCODE -ne 0) {
    throw 'Corrected harness must have a clean, reviewable source identity.'
}
$frozen = Join-Path $harnessRoot 'private\worktrees\KBP-CONTROLLER-FULL04'
$product = '69a48a4d4c6484abf84549ab8bb062195cedb380'
if ((& git -C $frozen rev-parse HEAD).Trim() -cne $product -or $LASTEXITCODE -ne 0) {
    throw 'This reader is bound only to frozen Alpha4 product69a48a4.'
}
if (@(& git -C $frozen status --porcelain).Count -ne 0 -or $LASTEXITCODE -ne 0) {
    throw 'Frozen Alpha4 worktree must remain clean.'
}
& git -C $harnessRoot diff --quiet $product HEAD -- src Version.props tests/KingmakerBuffPlanner.Tests/KingmakerBuffPlanner.Tests.csproj
if ($LASTEXITCODE -ne 0) { throw 'Harness linked production inputs differ from Alpha4; do not apply its evidence to changed bytes.' }
$testChanges = @(& git -C $harnessRoot diff --name-only $product HEAD -- tests)
if ($LASTEXITCODE -ne 0 -or $testChanges.Count -ne 1 -or
    $testChanges[0] -cne 'tests/KingmakerBuffPlanner.Tests/ProductionExecutionTests.cs') {
    throw 'Only the reviewed Classic cleanup reader correction is allowed in this harness.'
}
$dll = Join-Path $frozen 'artifacts\build\Release\KingmakerBuffPlanner.dll'
$package = Join-Path $frozen 'artifacts\local-runtime\0.5.4-controller.4\KingmakerBuffPlanner-0.5.4-controller.4-local-runtime.zip'
if ((Get-KbpSha256 $dll) -cne '3d2199e8b736bbc63034d15d571a8cddf0788e884512e631b1213cf145b8fe2d' -or
    (Get-KbpSha256 $package) -cne 'b307b18950f9254b4df68b13a7aa028abf0d012fa3352d347132c3de6b3b0f58') {
    throw 'Frozen Alpha4 DLL/package identity changed.'
}
$started = [Diagnostics.Stopwatch]::StartNew()
$hadGamePath = Test-Path Env:\KBP_TEST_GAME_PATH
$oldGamePath = $env:KBP_TEST_GAME_PATH
$oldDefaults = $PSDefaultParameterValues
try {
    $env:KBP_TEST_GAME_PATH = Get-KbpGamePath
    $msbuild = Get-KbpMsBuild
    & $msbuild (Join-Path $harnessRoot 'tests\KingmakerBuffPlanner.Tests\KingmakerBuffPlanner.Tests.csproj') /t:Rebuild /p:Configuration=Release /m /nologo /v:minimal
    if ($LASTEXITCODE -ne 0) { throw 'Corrected reader build failed.' }
    $reader = Join-Path $harnessRoot 'artifacts\tests\KingmakerBuffPlanner.Tests.exe'
    Write-Host ('ALPHA4 corrected-reader harnessHead=' + (& git -C $harnessRoot rev-parse HEAD).Trim() + '; readerSha256=' + (Get-KbpSha256 $reader))
    Push-Location $frozen
    try {
        & (Join-Path $frozen 'scripts\Test-RuntimeLauncherArguments.ps1')
        & (Join-Path $frozen 'scripts\Validate-Source.ps1')
        & (Join-Path $frozen 'scripts\validate-package.ps1') -PackagePath $package
        & $reader
        if ($LASTEXITCODE -ne 0) { throw 'Corrected-reader full C# suite failed; no later safety stage selected.' }
        foreach ($stage in @('Test-RuntimeHarness.ps1', 'Test-PhysicalSearchEvidence.ps1',
            'Test-ProblemNavigationEvidence.ps1', 'Test-SpellbookEntryEvidence.ps1',
            'Test-DeploymentWhatIf.ps1', 'Test-RuntimeLauncherFileWhatIf.ps1',
            'Test-FixtureInventoryEvidence.ps1', 'Test-RestoreInstallLocal.ps1', 'Test-PublishReleaseGate.ps1')) {
            Write-Host ('ALPHA4 safety-stage=' + $stage + '; elapsedSeconds=' + [Math]::Round($started.Elapsed.TotalSeconds, 1))
            try {
                # Only the real full-history comparisons use a bounded attempt
                # count. The fixture harness must retain its normal defaults:
                # it deliberately exercises foreign-overlap retry behavior.
                if ($stage -cin @('Test-DeploymentWhatIf.ps1', 'Test-RuntimeLauncherFileWhatIf.ps1')) {
                    $PSDefaultParameterValues = @{}
                    foreach ($key in $oldDefaults.Keys) { $PSDefaultParameterValues[$key] = $oldDefaults[$key] }
                    $PSDefaultParameterValues['Invoke-KbpLivePurityWindow:Attempts'] = 1
                    $PSDefaultParameterValues['Invoke-KbpLivePurityWindow:LeaseWaitSeconds'] = 120
                }
                & (Join-Path $frozen ('scripts\' + $stage))
            }
            finally { $PSDefaultParameterValues = $oldDefaults }
        }
    }
    finally { Pop-Location }
    Write-Host ('ALPHA4 corrected-reader source/safety sequence PASS; seconds=' + [Math]::Round($started.Elapsed.TotalSeconds, 1) + '; no native game qualification.')
}
finally {
    $PSDefaultParameterValues = $oldDefaults
    if ($hadGamePath) { $env:KBP_TEST_GAME_PATH = $oldGamePath }
    else { Remove-Item Env:\KBP_TEST_GAME_PATH -ErrorAction SilentlyContinue }
}
