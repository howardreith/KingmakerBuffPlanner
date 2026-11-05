[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeAutomation.Common.ps1')
$root = Get-KbpRepositoryRoot
$head = (& git -C $root rev-parse HEAD).Trim()
$fixture = Join-Path $root ('artifacts\heroism-allowance-tests-' + [Guid]::NewGuid().ToString('N'))
$approvals = Join-Path $fixture 'approvals'
$evidence = Join-Path $fixture 'evidence'
$backups = Join-Path $fixture 'backups'
$freeze = Join-Path $backups "rc-frozen\$head"
$selection = Join-Path $evidence 'selection'
foreach ($directory in @($approvals, $freeze, $selection)) { [void](New-Item -ItemType Directory -Path $directory -Force) }
function Save-Fixture([string]$Path, $Value) { Write-KbpJsonAtomic $Path $Value }
$manifest = [ordered]@{ schemaVersion = 1; commit = $head; packageSha256 = ('a' * 64); dllSha256 = ('b' * 64)
    assemblyMvid = '11111111-2222-3333-4444-555555555555'; version = '0.0.0-test' }
Save-Fixture (Join-Path $freeze 'FREEZE.json') $manifest
Save-Fixture (Join-Path $selection 'runtime-result.json') @{ status = 'PASS' }
Save-Fixture (Join-Path $selection 'run-completion.json') @{ complete = $true; profileId = 'full-user'; compatibilityIdentity = ('e' * 64)
    fixture = @{ workingSha256 = ('9' * 64); gameId = 'game' } }
Save-Fixture (Join-Path $selection 'runtime-request.json') @{ scenario = 'live-cast-qual-select'; expectedCommit = $head
    expectedPackageSha256 = ('a' * 64); expectedDllSha256 = ('b' * 64); profileId = 'full-user'
    parameters = @{ expectedGameId = 'game'; workingSha256 = ('9' * 64); executionMode = 'instant'; qualificationRecipe = 'heroism-successor' } }
$context = @{ heroismAbilityGuid = '5ab0d42fb68c9e34abae4921822b9d63'; successorAbilityGuid = '3e4ab69ada402d145a5e0ad3ad4b8564'
    successorName = 'Mirror Image'; casterUnitId = 'caster'; primaryRecipientUnitId = 'recipient'; observationUnitIds = @('caster', 'recipient', 'nearby')
    casterFacts = @{ '1a4bd53d5b8848d9a717084d7f9dfa0c' = $true }; discoveredExpression = @{ expressionType = 'sequence' }
    boundExpression = @{ expressionType = 'sequence' } }
$outcome = @{ castingScenario = $false; violations = @(); selection = @{ selected = $true; recipe = 'heroism-successor'
        castings = @('qual-cast-1', 'qual-cast-2'); effectContext = $context }
    forecast = @(
        @{ name = 'heroism-first'; projectionId = ('1' * 64); canonicalContract = '{}'; castingIds = @('qual-cast-1', 'qual-cast-2') },
        @{ name = 'heroism-recast'; projectionId = ('2' * 64); canonicalContract = '{}'; castingIds = @('qual-cast-1', 'qual-cast-2') },
        @{ name = 'heroism-skip'; projectionId = ('3' * 64); canonicalContract = '{}'; castingIds = @('qual-cast-2') }) }
$outcomePath = Join-Path $selection 'qual-outcome.json'
Save-Fixture $outcomePath $outcome
$original = [IO.File]::ReadAllText($outcomePath)
$common = @{ Kind = 'qualification'; SelectionRunId = 'selection'; ExpectedCommit = $head; FreezeKind = 'rc-frozen'
    ApprovedBy = 'Howie'; Authority = 'bounded synthetic writer test, never a live authorization'
    ApprovalsRoot = $approvals; EvidenceRoot = $evidence; BackupRoot = $backups }
$passed = 0
Assert-KbpHeroismQualificationSelection ($original | ConvertFrom-Json)
$passed++
# Exercise the exact metadata shape emitted by the native dictionary writer,
# for both conditional branches, through the existing allowance writer below.
$metadataOutcome = $original | ConvertFrom-Json
foreach ($factPresent in @($false, $true)) {
    $metadataOutcome.selection.effectContext.casterFacts = [pscustomobject]@{
        '$id' = '1'; '1a4bd53d5b8848d9a717084d7f9dfa0c' = $factPresent }
    Assert-KbpHeroismQualificationSelection $metadataOutcome
    $passed++
}
Save-Fixture $outcomePath $metadataOutcome
foreach ($mode in @('instant', 'animated')) {
    $path = & (Join-Path $PSScriptRoot 'New-KbpRunAllowance.ps1') @common -RunId "good-$mode" -ExecutionMode $mode | Select-Object -Last 1
    $json = [IO.File]::ReadAllText($path)
    $allowance = $json | ConvertFrom-Json
    if ([int]$allowance.maximumNativeSubmissions -ne 5 -or
        $null -ne (Get-KbpQualificationAllowanceBuildRefusal -AllowanceJson $json -RunId "good-$mode" -BuildManifest ([pscustomobject]$manifest) `
            -Recipe 'heroism-successor' -ExecutionMode $mode) -or
        $null -ne (Get-KbpAllowanceFixtureBindingRefusal -AllowanceJson $json -ProfileId 'full-user' -CompatibilityIdentity ('e' * 64) `
            -WorkingSaveSha256 ('9' * 64) -FixtureGameId 'game')) { throw "Written $mode allowance failed its real launcher readers." }
    $passed++
}
$changes = @(
    { param($o) $o.selection.effectContext.successorAbilityGuid = $o.selection.effectContext.heroismAbilityGuid },
    { param($o) $o.selection.effectContext.primaryRecipientUnitId = 'caster' },
    { param($o) $o.selection.effectContext.casterFacts = [pscustomobject]@{} },
    { param($o) $o.selection.effectContext.casterFacts = [pscustomobject]@{ '$id' = '1' } },
    { param($o) $o.selection.effectContext.casterFacts = [pscustomobject]@{ '$id' = 'bad'; '1a4bd53d5b8848d9a717084d7f9dfa0c' = $true } },
    { param($o) $o.selection.effectContext.casterFacts = [pscustomobject]@{ '$id' = 1; '1a4bd53d5b8848d9a717084d7f9dfa0c' = $true } },
    { param($o) $o.selection.effectContext.casterFacts = [pscustomobject]@{ '$ref' = '1'; '1a4bd53d5b8848d9a717084d7f9dfa0c' = $true } },
    { param($o) $o.selection.effectContext.casterFacts = [pscustomobject]@{ '1a4bd53d5b8848d9a717084d7f9dfa0c' = 'false' } },
    { param($o) $o.forecast[2].castingIds = @('qual-cast-1', 'qual-cast-2') },
    { param($o) $o.forecast[1].name = 'other' },
    { param($o) $o.selection.effectContext.observationUnitIds = @('caster') })
$index = 0
foreach ($change in $changes) {
    $bad = $original | ConvertFrom-Json
    & $change $bad
    Save-Fixture $outcomePath $bad
    $id = 'bad-' + (++$index)
    $refused = $false
    try { & (Join-Path $PSScriptRoot 'New-KbpRunAllowance.ps1') @common -RunId $id -ExecutionMode instant | Out-Null }
    catch { $refused = $true }
    if (-not $refused -or (Test-Path -LiteralPath (Join-Path $approvals "$id.json"))) { throw "$id was not refused before writing." }
    $passed++
}
Write-Host "Heroism qualification evidence/writer: PASS=$passed FAIL=0 (synthetic offline fixtures)."
