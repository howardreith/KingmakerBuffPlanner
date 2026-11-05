[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeAutomation.Common.ps1')
$fixture = Join-Path (Get-KbpRepositoryRoot) ('artifacts\heroism-fixture-tests-' + [Guid]::NewGuid().ToString('N'))
$saves = Join-Path $fixture 'saves'
$evidence = Join-Path $fixture 'evidence'
foreach ($directory in @($saves, $evidence)) { [void](New-Item -ItemType Directory -Path $directory -Force) }
$working = Join-Path $saves 'Manual_305_KBP_AUTOMATION_WORKING.zks'
$baseline = Join-Path $saves 'Manual_304_KBP_AUTOMATION_BASELINE.zks'
$other = Join-Path $saves 'Manual_1_Owner.zks'
[IO.File]::WriteAllText($working, 'working-original')
[IO.File]::WriteAllText($baseline, 'immutable-baseline')
[IO.File]::WriteAllText($other, 'protected-owner-save')
$before = Get-KbpSaveFolderSnapshot $saves
$original = Get-KbpSha256 $working
$script:passed = 0
function Refused([scriptblock]$Action) {
    $refused = $false
    try { & $Action | Out-Null } catch { $refused = $true }
    if (-not $refused) { throw 'An invalid fixture preparation operation was accepted.' }
    $script:passed++
}
Refused { Save-KbpHeroismWorkingBackup $baseline $evidence (Get-KbpSha256 $baseline) 'test' }
Refused { Save-KbpHeroismWorkingBackup $working $evidence ('a' * 64) 'test' }
Save-KbpHeroismWorkingBackup $working $evidence $original 'test'
if ((Get-KbpSha256 (Join-Path $evidence 'heroism-working-before.zks')) -cne $original) { throw 'Backup bytes differ.' }
$script:passed++
Refused { Save-KbpHeroismWorkingBackup $working $evidence $original 'test' }
[IO.File]::WriteAllText($working, 'failed-partial-preparation')
$failed = Get-KbpSha256 $working
Refused { Restore-KbpHeroismWorkingBackup $working $evidence $original 'another-run' }
if ((Get-KbpSha256 $working) -cne $failed) { throw 'A wrong run changed the working save.' }
Restore-KbpHeroismWorkingBackup $working $evidence $original 'test'
$differences = Compare-KbpSaveFolderSnapshot $before (Get-KbpSaveFolderSnapshot $saves)
if ((Get-KbpSha256 $working) -cne $original -or
    @($differences).Count -ne 0) { throw 'Rollback or protected saves differ.' }
$script:passed++
$backup = Join-Path $evidence 'heroism-working-before.zks'
Remove-Item -LiteralPath $working
Restore-KbpHeroismWorkingBackup $working $evidence $original 'test'
if ((Get-KbpSha256 $working) -cne $original) { throw 'An interrupted native replacement could not restore its missing working file.' }
$script:passed++
[IO.File]::WriteAllText($backup, 'tampered-backup')
Refused { Restore-KbpHeroismWorkingBackup $working $evidence $original 'test' }
[IO.File]::WriteAllText($backup, 'working-original')
$policy = Get-KbpProtectedSavePolicy 'live-heroism-fixture-prepare' 'Automation' (Split-Path -Leaf $working)
if (@($policy.allowedChanged).Count -ne 1 -or @($policy.allowedChanged)[0] -cne (Split-Path -Leaf $working) -or
    -not $policy.newFilesBlocking) { throw 'Preparation changed the protected-save policy beyond the exact working copy.' }
$script:passed++
$record = [ordered]@{ schemaVersion=1; runId='test'; prepared=$true; failure=$null; castsSubmitted=0
    workingSha256Before=$original; workingSha256After=('b'*64); spellbookCasterLevel=10; classProgressionChanged=$false
    levelTwoCapacity=5; levelTwoRemaining=5; infectiousCharmsPresent=$true
    heroism='5ab0d42fb68c9e34abae4921822b9d63'; successor='3e4ab69ada402d145a5e0ad3ad4b8564' }
$request = [pscustomobject]@{ runId='test'; scenario='live-heroism-fixture-prepare'; evidenceDirectory=$evidence
    parameters=[pscustomobject]@{ workingSha256=$original; heroismCondition='present' } }
$recordPath = Join-Path $evidence 'heroism-fixture-preparation.json'
Write-KbpJsonAtomic $recordPath $record
Assert-KbpScenarioOutcome $request
$script:passed++
$goodJson = [IO.File]::ReadAllText($recordPath)
foreach ($change in @(
    {param($r) $r.prepared=$false}, {param($r) $r.castsSubmitted=1}, {param($r) $r.workingSha256Before='wrong'},
    {param($r) $r.failure='save failed'}, {param($r) $r.spellbookCasterLevel=1},
    {param($r) $r.classProgressionChanged=$true}, {param($r) $r.levelTwoCapacity=4},
    {param($r) $r.levelTwoRemaining=4}, {param($r) $r.infectiousCharmsPresent=$false},
    {param($r) $r.successor=$r.heroism})) {
    $bad = $goodJson | ConvertFrom-Json
    & $change $bad
    Write-KbpJsonAtomic $recordPath $bad
    Refused { Assert-KbpScenarioOutcome $request }
}
Write-KbpJsonAtomic $recordPath ([ordered]@{ schemaVersion=1; runId='test'; workingSha256Before=$original
    nativeTemporaryFileName='Manual_306_KBP_AUTOMATION_WORKING.zks' })
$nativeTemporary = Join-Path $saves 'Manual_306_KBP_AUTOMATION_WORKING.zks'
[IO.File]::WriteAllText($nativeTemporary, 'failed-native-temporary')
Restore-KbpHeroismWorkingBackup $working $evidence $original 'test' $before
if (Test-Path -LiteralPath $nativeTemporary) { throw 'The predeclared native temporary save survived rollback.' }
$script:passed++
Write-KbpJsonAtomic $recordPath ([ordered]@{ schemaVersion=1; runId='test'; workingSha256Before=$original
    nativeTemporaryFileName=(Split-Path -Leaf $baseline) })
Refused { Restore-KbpHeroismWorkingBackup $working $evidence $original 'test' $before }
Write-KbpJsonAtomic $recordPath ([ordered]@{ schemaVersion=1; runId='test'; workingSha256Before=$original
    nativeTemporaryFileName=(Split-Path -Leaf $working) })
Refused { Restore-KbpHeroismWorkingBackup $working $evidence $original 'test' $before }
Write-Host "Heroism fixture backup/rollback/policy/evidence: PASS=$script:passed FAIL=0 (isolated offline files; no native qualification)."
