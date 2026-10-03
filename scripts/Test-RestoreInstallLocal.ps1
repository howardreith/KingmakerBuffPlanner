[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')

# Isolated-state tests for Restore-InstallLocal.ps1: a fake lab (state,
# backup, staging, evidence roots) and a fake game Mods tree exercise the
# rollback record contract, profile preservation, WhatIf purity, refusal
# rules, injected failures in every phase including both swap moves and
# their reversal (reviews K6/L4), and candidate-format compatibility read
# from the restored binary (review L5), without touching the real machine
# or any game file.
$boundary = Join-Path ([IO.Path]::GetTempPath()) ('KbpRestoreInstallTest-' + [Guid]::NewGuid().ToString('N'))
$passed = 0
function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "RestoreInstallLocal test failed: $Message" }
}

# A fake planner assembly. $CandidateFormat declares the candidate-profile
# format the binary reads, exactly as the real AssemblyInfo does; '' means
# no declaration (a build that predates the contract, e.g. 258a1d0 or
# 475d2b9, which contain the candidate file-name literal but declare
# nothing). The literal is always compiled in, so a file-name scan would
# (wrongly) call every fixture compatible.
Add-Type -AssemblyName Microsoft.CSharp
function New-FakeAssembly([string]$Path, [string]$AssemblyVersion, [string]$CandidateFormat) {
    $provider = [Microsoft.CSharp.CSharpCodeProvider]::new()
    $parameters = [System.CodeDom.Compiler.CompilerParameters]::new()
    $parameters.GenerateExecutable = $false
    $parameters.OutputAssembly = $Path
    $metadata = if ($CandidateFormat) {
        '[assembly: System.Reflection.AssemblyMetadata("KingmakerBuffPlanner.CandidateProfileFormat", "' + $CandidateFormat + '")] '
    } else { '' }
    $source = '[assembly: System.Reflection.AssemblyVersion("' + $AssemblyVersion + '")] ' + $metadata +
        'public class Marker { public static string Prefix() { return "kingmaker-buff-planner-casting-"; } }'
    $compile = $provider.CompileAssemblyFromSource($parameters, $source)
    if ($compile.Errors.Count -ne 0) { throw "Marker assembly failed to compile: $($compile.Errors[0])" }
}

function New-FakePlanner([string]$Path, [string]$Version, [string]$AssemblyVersion, [string]$CandidateFormat) {
    New-Item -ItemType Directory -Path (Join-Path $Path 'UserSettings') | Out-Null
    @{ Id = 'KingmakerBuffPlanner'; DisplayName = 'Kingmaker Buff Planner'; Author = 'Test'
       Version = $Version; ManagerVersion = '0.28.2'; GameVersion = '2.1.7'
       AssemblyName = 'KingmakerBuffPlanner.dll'; EntryMethod = 'KingmakerBuffPlanner.Main.Load'
       Requirements = @() } | ConvertTo-Json |
        Set-Content -LiteralPath (Join-Path $Path 'Info.json')
    New-FakeAssembly (Join-Path $Path 'KingmakerBuffPlanner.dll') $AssemblyVersion $CandidateFormat
    Set-Content -LiteralPath (Join-Path $Path 'THIRD-PARTY-NOTICES.md') -Value 'notices'
}

# Realistic schema-6 candidate documents (members of the real profile model).
$candidateBase = '"schemaVersion":6,"campaignId":"campaign-a","routines":[{"routineId":"long","name":"Long"}],' +
    '"castings":[{"castingId":"c1","routineId":"long","order":0,"sourceId":"s","ability":{"baseAbilityGuid":"g",' +
    '"variantGuid":"","metamagicMask":0,"sourceKind":"Spellbook","specialSourceId":""},"casterUnitId":"u1",' +
    '"spellbookGuid":null,"targetMode":"DirectTarget","directTargetUnitId":"u2","origin":null,' +
    '"requiredCoverageUnitIds":[],"targetingModifiers":[],"enhancements":[],"existingEffectPolicy":"SkipAlreadyActive",' +
    '"ignoredPresenceMarkers":[],"state":"Ready","provenance":null}],"ui":{"scale":1.0,"hotkey":"Ctrl+Shift+B"},' +
    '"execution":{"mode":"hybrid","allowAnimatedFallback":true,"outOfCombatOnly":true,"recastExisting":false}'
$candidates = [ordered]@{
    'kingmaker-buff-planner-casting-rev1.json' = '{' + $candidateBase + '}'
    'kingmaker-buff-planner-casting-rev2.json' = '{' + $candidateBase + ',"importNotices":["legacy-provider-preference:x"]}'
    'kingmaker-buff-planner-casting-rev3.json' = '{' + $candidateBase + ',"importNotices":["n"],"acknowledgedImportNotices":["n"],"formatRevision":3}'
    'kingmaker-buff-planner-casting-unknown-member.json' = '{' + $candidateBase + ',"futureField":1}'
    'kingmaker-buff-planner-casting-rev4.json' = '{' + $candidateBase + ',"formatRevision":4}'
    'kingmaker-buff-planner-casting-malformed.json' = '{not json'
}

# Fresh isolated fixture: installed 9.9.9-rc1 over a recorded prior 9.9.8
# whose binary declares $PriorFormat. Candidates sit on BOTH sides.
function New-Fixture([string]$Name, [string]$PriorFormat) {
    $root = Join-Path $boundary $Name
    $f = [ordered]@{}
    $f.game = Join-Path $root 'game'
    $f.mods = Join-Path $f.game 'Mods'
    $f.state = Join-Path $root 'state'
    $f.backup = Join-Path $root 'backup'
    $f.staging = Join-Path $root 'staging'
    $f.evidence = Join-Path $root 'evidence'
    foreach ($path in @((Join-Path $f.mods 'OtherMod'), $f.state, $f.backup, $f.staging, $f.evidence)) {
        New-Item -ItemType Directory -Path $path | Out-Null
    }
    Set-Content -LiteralPath (Join-Path (Join-Path $f.mods 'OtherMod') 'other.txt') -Value 'other'
    $f.planner = Join-Path $f.mods 'KingmakerBuffPlanner'
    New-FakePlanner $f.planner '9.9.9-rc1' '9.9.9.0' '6.3'
    $f.prior = Join-Path $f.backup 'KingmakerBuffPlanner.prior'
    New-FakePlanner $f.prior '9.9.8' '9.9.8.0' $PriorFormat
    # A profile edited AFTER installation must survive the rollback.
    Set-Content -LiteralPath (Join-Path $f.planner 'UserSettings\kingmaker-buff-planner-new.json') `
        -Value '{"newer":true}'
    Set-Content -LiteralPath (Join-Path $f.prior 'UserSettings\kingmaker-buff-planner-old.json') `
        -Value '{"older":true}'
    # Casting-first player settings written after installation: the saved
    # planner mode and a persisted review digest. Neither is a candidate
    # plan; both must survive any rollback byte-for-byte (older builds
    # ignore them).
    [IO.File]::WriteAllText((Join-Path $f.planner 'UserSettings\planner-mode.json'),
        '{"schemaVersion":1,"mode":"casting-first"}')
    [IO.File]::WriteAllText((Join-Path $f.planner 'UserSettings\kingmaker-buff-planner-review-0123456789abcdef01234567.json'),
        ('{"schemaVersion":1,"campaignId":"c","accepted":{"long":"' + ('a' * 64) + '"}}'))
    foreach ($name in $candidates.Keys) {
        [IO.File]::WriteAllText((Join-Path $f.planner ('UserSettings\' + $name)), $candidates[$name])
    }
    # A backup-side candidate (e.g. a .bak rotation) that came back with the prior build.
    [IO.File]::WriteAllText((Join-Path $f.prior 'UserSettings\kingmaker-buff-planner-casting-prior.json.bak.1'),
        $candidates['kingmaker-buff-planner-casting-rev2.json'])

    $f.installDir = Join-Path $f.state 'installations\test-install'
    New-Item -ItemType Directory -Path $f.installDir | Out-Null
    $manifest = @(Get-KbpDirectoryManifest $f.mods | Where-Object {
        $_.path -cne 'KingmakerBuffPlanner' -and
        -not ($_.path).StartsWith('KingmakerBuffPlanner\', [StringComparison]::OrdinalIgnoreCase) })
    [ordered]@{
        schemaVersion = 1; installId = 'test-install'; token = 'tok'; status = 'Installed'
        version = '9.9.9-rc1'; expectedPriorVersion = '9.9.8'
        modsPath = $f.mods; backupPlanner = $f.prior
        beforeOtherModsManifest = $manifest
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $f.installDir 'install.json')
    $f.arguments = @{ InstallId = 'test-install'; GameRoot = $f.game; StateRoot = $f.state
                      BackupRoot = $f.backup; StagingRoot = $f.staging; EvidenceRoot = $f.evidence
                      Confirm = $false }
    $f.installedIdentity = (Get-KbpDirectoryContentIdentity $f.planner).directoryManifestSha256
    $f.priorIdentity = (Get-KbpDirectoryContentIdentity $f.prior).directoryManifestSha256
    return $f
}

function Read-Record($f) { return Read-KbpJson (Join-Path $f.installDir 'install.json') }
function Get-Identity([string]$Path) { return (Get-KbpDirectoryContentIdentity $Path).directoryManifestSha256 }
function Get-ActiveCandidates([string]$Planner) {
    return @(Get-ChildItem -LiteralPath (Join-Path $Planner 'UserSettings') -File |
        Where-Object { $_.Name -like 'kingmaker-buff-planner-casting-*' } | ForEach-Object Name | Sort-Object)
}

$restoreScript = Join-Path $PSScriptRoot 'Restore-InstallLocal.ps1'

try {
    # ---------- WhatIf purity ----------
    $f = New-Fixture 'whatif' ''
    $arguments = $f.arguments
    & $restoreScript @arguments -WhatIf | Out-Null
    Assert-True (Test-Path -LiteralPath (Join-Path $f.planner 'Info.json')) 'WhatIf removed the installed planner.'
    Assert-True (@(Get-ChildItem -LiteralPath $f.staging).Count -eq 0) 'WhatIf created staging state.'
    Assert-True ([string](Read-Record $f).status -ceq 'Installed') 'WhatIf changed the record.'
    $passed++

    # ---------- L5: target declares no candidate format (pre-contract build) ----------
    $f = New-Fixture 'no-declaration' ''
    $arguments = $f.arguments
    & $restoreScript @arguments | Out-Null
    $settings = Join-Path $f.planner 'UserSettings'
    Assert-True ((Read-KbpJson (Join-Path $f.planner 'Info.json')).Version -ceq '9.9.8') `
        'Rollback did not restore the prior planner version.'
    Assert-True (Test-Path -LiteralPath (Join-Path $settings 'kingmaker-buff-planner-new.json')) `
        'Newer post-install profile was not preserved.'
    Assert-True (Test-Path -LiteralPath (Join-Path $settings 'kingmaker-buff-planner-old.json')) 'Prior profile was lost.'
    Assert-True ([IO.File]::ReadAllText((Join-Path $settings 'planner-mode.json')) -ceq
        '{"schemaVersion":1,"mode":"casting-first"}') 'The saved planner mode did not survive rollback.'
    Assert-True ([IO.File]::ReadAllText((Join-Path $settings 'kingmaker-buff-planner-review-0123456789abcdef01234567.json')) -ceq
        ('{"schemaVersion":1,"campaignId":"c","accepted":{"long":"' + ('a' * 64) + '"}}')) `
        'The persisted review state did not survive rollback.'
    $rollEvidence = Join-Path $f.evidence 'rollback-test-install'
    Assert-True (Test-Path -LiteralPath (Join-Path $rollEvidence 'rolled-back-planner\Info.json')) `
        'Rolled-back build was not archived as evidence.'
    $record = Read-Record $f
    Assert-True ([string]$record.status -ceq 'RolledBack') 'Record status was not RolledBack.'
    Assert-True ([string]$record.rollbackPhase -ceq 'complete') 'Record phase was not complete.'
    Assert-True ([string]$record.rollbackTargetCandidateFormat -ceq 'none') 'Undeclared format was misrecorded.'
    Assert-True (@(Get-ActiveCandidates $f.planner).Count -eq 0) `
        'A candidate was left active beside a binary that declares no candidate format.'
    Assert-True (@($record.rollbackDeactivatedCandidateProfiles).Count -eq ($candidates.Count + 1)) `
        'Not every candidate (both sides) was deactivated.'
    foreach ($name in $candidates.Keys) {
        $archived = Join-Path $rollEvidence ('deactivated-candidate-profiles\installed\' + $name)
        Assert-True ((Test-Path -LiteralPath $archived) -and
            [IO.File]::ReadAllText($archived) -ceq $candidates[$name]) "Candidate $name was not archived byte-exact."
    }
    Assert-True (Test-Path -LiteralPath (Join-Path $rollEvidence 'deactivated-candidate-profiles\prior\kingmaker-buff-planner-casting-prior.json.bak.1')) `
        'The backup-side candidate was not archived.'
    Assert-True ((Get-Identity $f.prior) -ceq $f.priorIdentity) 'The recorded prior backup was modified.'
    Assert-True (Test-Path -LiteralPath (Join-Path (Join-Path $f.mods 'OtherMod') 'other.txt')) 'Unrelated mod was touched.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $f.state 'deployment.lock'))) 'Lock was not released.'
    $passed++

    # Second rollback of the same record refuses.
    $refused = $false
    try { & $restoreScript @arguments | Out-Null } catch { $refused = $true }
    Assert-True $refused 'A non-Installed record was rolled back again.'
    $passed++

    # Unknown install id refuses.
    $refused = $false
    try { & $restoreScript -InstallId 'absent' -GameRoot $f.game -StateRoot $f.state `
            -BackupRoot $f.backup -StagingRoot $f.staging -EvidenceRoot $f.evidence -Confirm:$false | Out-Null }
    catch { $refused = $true }
    Assert-True $refused 'An unknown install id was accepted.'
    $passed++

    # ---------- L5: older format revision with the same schema and prefix ----------
    $f = New-Fixture 'older-revision' '6.1'
    $arguments = $f.arguments
    & $restoreScript @arguments | Out-Null
    $active = @(Get-ActiveCandidates $f.planner)
    Assert-True (($active -join ',') -ceq 'kingmaker-buff-planner-casting-rev1.json') `
        "A 6.1 reader kept candidates it cannot read: $($active -join ',')"
    Assert-True ([string](Read-Record $f).rollbackTargetCandidateFormat -ceq '6.1') 'Declared format was not recorded.'
    $passed++

    # ---------- L5: current reader keeps exactly what it can read ----------
    $f = New-Fixture 'current-revision' '6.3'
    $arguments = $f.arguments
    & $restoreScript @arguments | Out-Null
    $active = @(Get-ActiveCandidates $f.planner)
    $expected = @('kingmaker-buff-planner-casting-prior.json.bak.1', 'kingmaker-buff-planner-casting-rev1.json',
        'kingmaker-buff-planner-casting-rev2.json', 'kingmaker-buff-planner-casting-rev3.json') | Sort-Object
    Assert-True (($active -join ',') -ceq ($expected -join ',')) `
        "A 6.3 reader kept the wrong set: $($active -join ',')"
    foreach ($name in @('kingmaker-buff-planner-casting-unknown-member.json',
            'kingmaker-buff-planner-casting-rev4.json', 'kingmaker-buff-planner-casting-malformed.json')) {
        Assert-True (Test-Path -LiteralPath (Join-Path $f.evidence ('rollback-test-install\deactivated-candidate-profiles\installed\' + $name))) `
            "Unreadable candidate $name was not archived."
    }
    Assert-True ([IO.File]::ReadAllText((Join-Path $f.planner 'UserSettings\kingmaker-buff-planner-casting-rev3.json')) -ceq
        $candidates['kingmaker-buff-planner-casting-rev3.json']) 'A kept candidate was modified.'
    $passed++

    # ---------- pre-swap failures: nothing applied, record truthful, retry works ----------
    foreach ($phase in @('candidate-archive', 'settings-merge', 'identity', 'move-installed')) {
        $f = New-Fixture ('pre-' + $phase) ''
        $arguments = $f.arguments
        $threw = $false
        try { & $restoreScript @arguments -InjectFailureAt $phase | Out-Null }
        catch { $threw = $_.Exception.Message -like "*injected rollback failure at $phase*" }
        Assert-True $threw "Injected $phase failure did not surface."
        $record = Read-Record $f
        Assert-True ([string]$record.status -ceq 'Installed') "After a $phase failure the record is not Installed."
        Assert-True ([string]$record.rollbackPhase -ceq 'not-applied') "After a $phase failure the phase is wrong."
        Assert-True ([string]$record.failure -like '*injected*') "The $phase failure was not recorded."
        Assert-True ((Get-Identity $f.planner) -ceq $f.installedIdentity) "A $phase failure changed the installed planner."
        Assert-True ((Get-Identity $f.prior) -ceq $f.priorIdentity) "A $phase failure changed the recorded prior backup."
        Assert-True (-not (Test-Path -LiteralPath (Join-Path $f.state 'deployment.lock'))) "Lock kept after a $phase failure."
        & $restoreScript @arguments | Out-Null
        $record = Read-Record $f
        Assert-True ([string]$record.status -ceq 'RolledBack') "Retry after a $phase failure did not complete."
        Assert-True ([string]$record.rollbackEvidenceRoot -like '*rollback-test-install-attempt2') `
            "Retry after a $phase failure reused the failed attempt's evidence."
        $passed++
    }

    # ---------- L4: second move fails, immediate reversal succeeds ----------
    $f = New-Fixture 'move-prior' ''
    $arguments = $f.arguments
    $threw = $false
    try { & $restoreScript @arguments -InjectFailureAt 'move-prior' | Out-Null }
    catch { $threw = $_.Exception.Message -like '*injected rollback failure at move-prior*' }
    Assert-True $threw 'Injected move-prior failure did not surface.'
    $record = Read-Record $f
    Assert-True ([string]$record.status -ceq 'Installed' -and [string]$record.rollbackPhase -ceq 'not-applied') `
        "A reversed partial swap was recorded as $($record.status)/$($record.rollbackPhase)."
    Assert-True ((Get-Identity $f.planner) -ceq $f.installedIdentity) 'The reversed installed planner is not byte-identical.'
    Assert-True ((Get-Identity $f.prior) -ceq $f.priorIdentity) 'The prior backup changed in a partial swap.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $f.state 'deployment.lock'))) 'Lock kept after a verified reversal.'
    $passed++

    # ---------- L4: second move fails AND its reversal fails ----------
    $f = New-Fixture 'move-prior-and-reverse' ''
    $arguments = $f.arguments
    $threw = $false
    try { & $restoreScript @arguments -InjectFailureAt 'move-prior-and-reverse' | Out-Null } catch { $threw = $true }
    Assert-True $threw 'The unrecoverable partial swap did not surface.'
    $record = Read-Record $f
    Assert-True ([string]$record.status -ceq 'RollbackRecoveryNeeded') `
        "A displaced, unrestored install was recorded as $($record.status)."
    Assert-True (Test-Path -LiteralPath (Join-Path $f.state 'deployment.lock')) 'The lock was released over a displaced install.'
    Assert-True (-not (Test-Path -LiteralPath $f.planner)) 'Fixture precondition: the live planner should be displaced.'
    $displaced = [string]$record.rollbackInstalledDisplacedPath
    Assert-True ((Get-Identity $displaced) -ceq $f.installedIdentity) 'The displaced installed build lost bytes.'
    Assert-True ([string]$record.rollbackInstalledIdentity -ceq $f.installedIdentity) 'The recovery identity was not recorded.'
    Assert-True ([string]$record.failure -like ('*' + $displaced + '*')) 'The recovery location was not recorded.'
    Assert-True ((Get-Identity $f.prior) -ceq $f.priorIdentity) 'The prior backup changed.'
    $refused = $false
    try { & $restoreScript @arguments | Out-Null } catch { $refused = $true }
    Assert-True $refused 'A rollback ran over an unrecovered partial swap.'
    $refused = $false
    try { Assert-KbpNoUnresolvedTransaction $f.state } catch { $refused = $true }
    Assert-True $refused 'A deployment could start over an unrecovered partial swap.'
    Remove-Item -LiteralPath (Join-Path $f.state 'deployment.lock') -Force
    $refused = $false
    try { Assert-KbpNoUnresolvedTransaction $f.state } catch { $refused = $_.Exception.Message -like '*Unresolved install rollback*' }
    Assert-True $refused 'The RollbackRecoveryNeeded record alone did not block deployment.'
    $passed++

    # ---------- post-swap failures: the swap is reversed ----------
    foreach ($phase in @('verify', 'record')) {
        $f = New-Fixture ('post-' + $phase) ''
        $arguments = $f.arguments
        $threw = $false
        try { & $restoreScript @arguments -InjectFailureAt $phase | Out-Null }
        catch { $threw = $_.Exception.Message -like "*injected rollback failure at $phase*" }
        Assert-True $threw "Injected $phase failure did not surface."
        $record = Read-Record $f
        Assert-True ([string]$record.status -ceq 'Installed') "After a $phase failure the record is not Installed."
        Assert-True ([string]$record.rollbackPhase -ceq 'reversed') "After a $phase failure the swap was not reversed."
        Assert-True ((Get-Identity $f.planner) -ceq $f.installedIdentity) `
            "After a $phase failure the installed planner is not byte-identical."
        Assert-True ((Get-Identity $f.prior) -ceq $f.priorIdentity) "After a $phase failure the prior backup changed."
        Assert-True (-not (Test-Path -LiteralPath (Join-Path $f.state 'deployment.lock'))) "Lock kept after a reversed $phase failure."
        $passed++
    }

    # ---------- reversal after a complete swap fails: recovery needed ----------
    $f = New-Fixture 'reverse-failed' ''
    $arguments = $f.arguments
    $threw = $false
    try { & $restoreScript @arguments -InjectFailureAt 'record-and-reverse' | Out-Null } catch { $threw = $true }
    Assert-True $threw 'The unrecoverable failure did not surface.'
    $record = Read-Record $f
    Assert-True ([string]$record.status -ceq 'RollbackRecoveryNeeded') 'An unreversed swap was not recorded as needing recovery.'
    Assert-True (Test-Path -LiteralPath (Join-Path $f.state 'deployment.lock')) 'The lock was released over an ambiguous Mods tree.'
    Assert-True ((Get-Identity ([string]$record.rollbackInstalledDisplacedPath)) -ceq $f.installedIdentity) `
        'The displaced installed build lost bytes.'
    $passed++

    # ---------- injection refuses without fully isolated roots ----------
    $refused = $false
    try { & $restoreScript -InstallId 'test-install' -StateRoot $f.state -InjectFailureAt 'record' -Confirm:$false | Out-Null }
    catch { $refused = $_.Exception.Message -like '*only permitted with fully isolated root overrides*' }
    Assert-True $refused 'Failure injection was accepted without isolated roots.'
    $passed++

    Write-Host "Restore-InstallLocal tests: PASS=$passed FAIL=0"
}
finally {
    if (Test-Path -LiteralPath $boundary) { Remove-Item -LiteralPath $boundary -Recurse -Force }
}
