[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')

# Isolated-state tests for Restore-InstallLocal.ps1: a fake lab (state,
# backup, staging, evidence roots) and a fake game Mods tree exercise the
# rollback record contract, profile preservation, WhatIf purity, refusal
# rules, injected failures in every phase (review K6), and target-binary
# candidate compatibility, without touching the real machine or any game
# file.
$boundary = Join-Path ([IO.Path]::GetTempPath()) ('KbpRestoreInstallTest-' + [Guid]::NewGuid().ToString('N'))
$passed = 0
function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "RestoreInstallLocal test failed: $Message" }
}

Add-Type -AssemblyName Microsoft.CSharp
function New-FakeAssembly([string]$Path, [string]$AssemblyVersion, [bool]$ReadsCandidates) {
    $provider = [Microsoft.CSharp.CSharpCodeProvider]::new()
    $parameters = [System.CodeDom.Compiler.CompilerParameters]::new()
    $parameters.GenerateExecutable = $false
    $parameters.OutputAssembly = $Path
    $body = if ($ReadsCandidates) {
        'public static string Prefix() { return "kingmaker-buff-planner-casting-"; }'
    } else { '' }
    $source = '[assembly: System.Reflection.AssemblyVersion("' + $AssemblyVersion + '")] public class Marker { ' + $body + ' }'
    $compile = $provider.CompileAssemblyFromSource($parameters, $source)
    if ($compile.Errors.Count -ne 0) { throw "Marker assembly failed to compile: $($compile.Errors[0])" }
}

function New-FakePlanner([string]$Path, [string]$Version, [string]$AssemblyVersion, [bool]$ReadsCandidates) {
    New-Item -ItemType Directory -Path (Join-Path $Path 'UserSettings') | Out-Null
    @{ Id = 'KingmakerBuffPlanner'; DisplayName = 'Kingmaker Buff Planner'; Author = 'Test'
       Version = $Version; ManagerVersion = '0.28.2'; GameVersion = '2.1.7'
       AssemblyName = 'KingmakerBuffPlanner.dll'; EntryMethod = 'KingmakerBuffPlanner.Main.Load'
       Requirements = @() } | ConvertTo-Json |
        Set-Content -LiteralPath (Join-Path $Path 'Info.json')
    New-FakeAssembly (Join-Path $Path 'KingmakerBuffPlanner.dll') $AssemblyVersion $ReadsCandidates
    Set-Content -LiteralPath (Join-Path $Path 'THIRD-PARTY-NOTICES.md') -Value 'notices'
}

# Fresh isolated fixture: installed 9.9.9-rc1 over a recorded prior 9.9.8.
function New-Fixture([string]$Name, [bool]$PriorReadsCandidates) {
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
    New-FakePlanner $f.planner '9.9.9-rc1' '9.9.9.0' $true
    $f.prior = Join-Path $f.backup 'KingmakerBuffPlanner.prior'
    New-FakePlanner $f.prior '9.9.8' '9.9.8.0' $PriorReadsCandidates
    # A profile edited AFTER installation must survive the rollback.
    Set-Content -LiteralPath (Join-Path $f.planner 'UserSettings\kingmaker-buff-planner-new.json') `
        -Value '{"newer":true}'
    Set-Content -LiteralPath (Join-Path $f.prior 'UserSettings\kingmaker-buff-planner-old.json') `
        -Value '{"older":true}'
    # Casting-first candidates on BOTH sides: the newer install's, and one
    # that came back inside the prior backup.
    Set-Content -LiteralPath (Join-Path $f.planner 'UserSettings\kingmaker-buff-planner-casting-abc.json') `
        -Value '{"schemaVersion":6}'
    Set-Content -LiteralPath (Join-Path $f.prior 'UserSettings\kingmaker-buff-planner-casting-old.json') `
        -Value '{"schemaVersion":6,"older":true}'

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

$restoreScript = Join-Path $PSScriptRoot 'Restore-InstallLocal.ps1'

try {
    # ---------- WhatIf purity ----------
    $f = New-Fixture 'whatif' $false
    $arguments = $f.arguments
    & $restoreScript @arguments -WhatIf | Out-Null
    Assert-True (Test-Path -LiteralPath (Join-Path $f.planner 'Info.json')) 'WhatIf removed the installed planner.'
    Assert-True (@(Get-ChildItem -LiteralPath $f.staging).Count -eq 0) 'WhatIf created staging state.'
    Assert-True ([string](Read-Record $f).status -ceq 'Installed') 'WhatIf changed the record.'
    $passed++

    # ---------- successful rollback to a binary that cannot read candidates ----------
    $f = New-Fixture 'success' $false
    $arguments = $f.arguments
    & $restoreScript @arguments | Out-Null
    $settings = Join-Path $f.planner 'UserSettings'
    Assert-True ((Read-KbpJson (Join-Path $f.planner 'Info.json')).Version -ceq '9.9.8') `
        'Rollback did not restore the prior planner version.'
    Assert-True (Test-Path -LiteralPath (Join-Path $settings 'kingmaker-buff-planner-new.json')) `
        'Newer post-install profile was not preserved.'
    Assert-True (Test-Path -LiteralPath (Join-Path $settings 'kingmaker-buff-planner-old.json')) 'Prior profile was lost.'
    $rollEvidence = Join-Path $f.evidence 'rollback-test-install'
    Assert-True (Test-Path -LiteralPath (Join-Path $rollEvidence 'rolled-back-planner\Info.json')) `
        'Rolled-back build was not archived as evidence.'
    $record = Read-Record $f
    Assert-True ([string]$record.status -ceq 'RolledBack') 'Record status was not RolledBack.'
    Assert-True ([string]$record.rollbackPhase -ceq 'complete') 'Record phase was not complete.'
    Assert-True (@($record.rollbackPreservedProfiles).Count -eq 1) 'Preserved-profile list was not recorded.'
    Assert-True (@(Get-ChildItem -LiteralPath $settings -Filter 'kingmaker-buff-planner-casting-*').Count -eq 0) `
        'A casting-first candidate was left active beside a binary that cannot read it.'
    Assert-True (Test-Path -LiteralPath (Join-Path $rollEvidence 'deactivated-candidate-profiles\installed\kingmaker-buff-planner-casting-abc.json')) `
        'The installed-side candidate was not archived.'
    Assert-True (Test-Path -LiteralPath (Join-Path $rollEvidence 'deactivated-candidate-profiles\prior\kingmaker-buff-planner-casting-old.json')) `
        'The prior-side candidate was not archived.'
    Assert-True (@($record.rollbackDeactivatedCandidateProfiles).Count -eq 2) 'Deactivated candidates were not recorded.'
    Assert-True ($record.rollbackTargetReadsCandidateProfiles -eq $false) 'Target compatibility was misrecorded.'
    Assert-True ((Get-KbpDirectoryContentIdentity $f.prior).directoryManifestSha256 -ceq $f.priorIdentity) `
        'The recorded prior backup was modified by the rollback.'
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

    # ---------- target binary reads candidates: they stay active ----------
    $f = New-Fixture 'compatible' $true
    $arguments = $f.arguments
    & $restoreScript @arguments | Out-Null
    $settings = Join-Path $f.planner 'UserSettings'
    $record = Read-Record $f
    Assert-True ([string]$record.status -ceq 'RolledBack') 'Compatible rollback did not complete.'
    Assert-True ($record.rollbackTargetReadsCandidateProfiles -eq $true) 'Compatible target was not detected from its binary.'
    Assert-True (Test-Path -LiteralPath (Join-Path $settings 'kingmaker-buff-planner-casting-abc.json')) `
        'A candidate the restored binary reads was deactivated.'
    Assert-True (Test-Path -LiteralPath (Join-Path $settings 'kingmaker-buff-planner-casting-old.json')) `
        'The prior-side candidate was removed from a compatible target.'
    Assert-True (@($record.rollbackDeactivatedCandidateProfiles).Count -eq 0) 'Candidates were deactivated for a compatible target.'
    $passed++

    # ---------- pre-swap failures: nothing applied, record truthful, retry works ----------
    foreach ($phase in @('candidate-archive', 'settings-merge', 'identity')) {
        $f = New-Fixture ('pre-' + $phase) $false
        $arguments = $f.arguments
        $threw = $false
        try { & $restoreScript @arguments -InjectFailureAt $phase | Out-Null }
        catch { $threw = $_.Exception.Message -like "*injected rollback failure at $phase*" }
        Assert-True $threw "Injected $phase failure did not surface."
        $record = Read-Record $f
        Assert-True ([string]$record.status -ceq 'Installed') "After a $phase failure the record is not Installed."
        Assert-True ([string]$record.rollbackPhase -ceq 'not-applied') "After a $phase failure the phase is wrong."
        Assert-True ([string]$record.failure -like '*injected*') "The $phase failure was not recorded."
        Assert-True ((Get-KbpDirectoryContentIdentity $f.planner).directoryManifestSha256 -ceq $f.installedIdentity) `
            "A $phase failure changed the installed planner."
        Assert-True ((Get-KbpDirectoryContentIdentity $f.prior).directoryManifestSha256 -ceq $f.priorIdentity) `
            "A $phase failure changed the recorded prior backup."
        Assert-True (-not (Test-Path -LiteralPath (Join-Path $f.state 'deployment.lock'))) "Lock kept after a $phase failure."
        & $restoreScript @arguments | Out-Null
        $record = Read-Record $f
        Assert-True ([string]$record.status -ceq 'RolledBack') "Retry after a $phase failure did not complete."
        Assert-True ([string]$record.rollbackEvidenceRoot -like '*rollback-test-install-attempt2') `
            "Retry after a $phase failure reused the failed attempt's evidence."
        Assert-True ((Read-KbpJson (Join-Path $f.planner 'Info.json')).Version -ceq '9.9.8') `
            "Retry after a $phase failure did not restore the prior build."
        $passed++
    }

    # ---------- post-swap failures: the swap is reversed ----------
    foreach ($phase in @('verify', 'record')) {
        $f = New-Fixture ('post-' + $phase) $false
        $arguments = $f.arguments
        $threw = $false
        try { & $restoreScript @arguments -InjectFailureAt $phase | Out-Null }
        catch { $threw = $_.Exception.Message -like "*injected rollback failure at $phase*" }
        Assert-True $threw "Injected $phase failure did not surface."
        $record = Read-Record $f
        Assert-True ([string]$record.status -ceq 'Installed') "After a $phase failure the record is not Installed."
        Assert-True ([string]$record.rollbackPhase -ceq 'reversed') "After a $phase failure the swap was not reversed."
        Assert-True ((Read-KbpJson (Join-Path $f.planner 'Info.json')).Version -ceq '9.9.9-rc1') `
            "After a $phase failure Mods does not hold the installed version the record names."
        Assert-True ((Get-KbpDirectoryContentIdentity $f.planner).directoryManifestSha256 -ceq $f.installedIdentity) `
            "After a $phase failure the installed planner is not byte-identical."
        Assert-True ((Get-KbpDirectoryContentIdentity $f.prior).directoryManifestSha256 -ceq $f.priorIdentity) `
            "After a $phase failure the recorded prior backup changed."
        Assert-True (-not (Test-Path -LiteralPath (Join-Path $f.state 'deployment.lock'))) "Lock kept after a reversed $phase failure."
        $passed++
    }

    # ---------- reversal itself fails: recovery needed, lock kept ----------
    $f = New-Fixture 'reverse-failed' $false
    $arguments = $f.arguments
    $threw = $false
    try { & $restoreScript @arguments -InjectFailureAt 'record-and-reverse' | Out-Null } catch { $threw = $true }
    Assert-True $threw 'The unrecoverable failure did not surface.'
    $record = Read-Record $f
    Assert-True ([string]$record.status -ceq 'RollbackRecoveryNeeded') 'An unreversed swap was not recorded as needing recovery.'
    Assert-True ([string]$record.failure -like '*reverse*') 'The reversal failure was not recorded.'
    Assert-True (Test-Path -LiteralPath (Join-Path $f.state 'deployment.lock')) 'The lock was released over an ambiguous Mods tree.'
    $refused = $false
    try { Assert-KbpNoUnresolvedTransaction $f.state } catch { $refused = $true }
    Assert-True $refused 'An unrecoverable rollback did not block further transactions.'
    Remove-Item -LiteralPath (Join-Path $f.state 'deployment.lock') -Force
    $refused = $false
    try { Assert-KbpNoUnresolvedTransaction $f.state } catch { $refused = $_.Exception.Message -like '*Unresolved install rollback*' }
    Assert-True $refused 'A RollbackRecoveryNeeded record alone did not block further transactions.'
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
