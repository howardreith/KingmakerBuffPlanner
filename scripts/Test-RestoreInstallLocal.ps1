[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')

# Isolated-state tests for Restore-InstallLocal.ps1: a fake lab (state,
# backup, staging, evidence roots) and a fake game Mods tree exercise the
# rollback record contract, profile preservation, WhatIf purity, and
# refusal rules without touching the real machine or any game file.
$boundary = Join-Path ([IO.Path]::GetTempPath()) ('KbpRestoreInstallTest-' + [Guid]::NewGuid().ToString('N'))
$passed = 0
function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "RestoreInstallLocal test failed: $Message" }
}

Add-Type -AssemblyName Microsoft.CSharp
function New-FakeAssembly([string]$Path, [string]$AssemblyVersion) {
    $provider = [Microsoft.CSharp.CSharpCodeProvider]::new()
    $parameters = [System.CodeDom.Compiler.CompilerParameters]::new()
    $parameters.GenerateExecutable = $false
    $parameters.OutputAssembly = $Path
    $source = '[assembly: System.Reflection.AssemblyVersion("' + $AssemblyVersion + '")] public class Marker { }'
    $compile = $provider.CompileAssemblyFromSource($parameters, $source)
    if ($compile.Errors.Count -ne 0) { throw "Marker assembly failed to compile: $($compile.Errors[0])" }
}

function New-FakePlanner([string]$Path, [string]$Version, [string]$AssemblyVersion) {
    New-Item -ItemType Directory -Path (Join-Path $Path 'UserSettings') | Out-Null
    @{ Id = 'KingmakerBuffPlanner'; DisplayName = 'Kingmaker Buff Planner'; Author = 'Test'
       Version = $Version; ManagerVersion = '0.28.2'; GameVersion = '2.1.7'
       AssemblyName = 'KingmakerBuffPlanner.dll'; EntryMethod = 'KingmakerBuffPlanner.Main.Load'
       Requirements = @() } | ConvertTo-Json |
        Set-Content -LiteralPath (Join-Path $Path 'Info.json')
    New-FakeAssembly (Join-Path $Path 'KingmakerBuffPlanner.dll') $AssemblyVersion
    Set-Content -LiteralPath (Join-Path $Path 'THIRD-PARTY-NOTICES.md') -Value 'notices'
}

try {
    # ---------- fixture: installed 9.9.9-rc1 over prior 9.9.8 ----------
    $game = Join-Path $boundary 'game'
    $mods = Join-Path $game 'Mods'
    $state = Join-Path $boundary 'state'
    $backup = Join-Path $boundary 'backup'
    $staging = Join-Path $boundary 'staging'
    $evidence = Join-Path $boundary 'evidence'
    foreach ($path in @((Join-Path $mods 'OtherMod'), $state, $backup, $staging, $evidence)) {
        New-Item -ItemType Directory -Path $path | Out-Null
    }
    Set-Content -LiteralPath (Join-Path (Join-Path $mods 'OtherMod') 'other.txt') -Value 'other'
    $planner = Join-Path $mods 'KingmakerBuffPlanner'
    New-FakePlanner $planner '9.9.9-rc1' '9.9.9.0'
    $prior = Join-Path $backup 'KingmakerBuffPlanner.prior'
    New-FakePlanner $prior '9.9.8' '9.9.8.0'
    # A profile edited AFTER installation must survive the rollback.
    Set-Content -LiteralPath (Join-Path $planner 'UserSettings\kingmaker-buff-planner-new.json') `
        -Value '{"newer":true}'
    Set-Content -LiteralPath (Join-Path $prior 'UserSettings\kingmaker-buff-planner-old.json') `
        -Value '{"older":true}'
    # A casting-first (schema-6) candidate the prior binary cannot read.
    Set-Content -LiteralPath (Join-Path $planner 'UserSettings\kingmaker-buff-planner-casting-abc.json') `
        -Value '{"schemaVersion":6}'

    $installDir = Join-Path $state 'installations\test-install'
    New-Item -ItemType Directory -Path $installDir | Out-Null
    $manifest = @(Get-KbpDirectoryManifest $mods | Where-Object {
        $_.path -cne 'KingmakerBuffPlanner' -and
        -not ($_.path).StartsWith('KingmakerBuffPlanner\', [StringComparison]::OrdinalIgnoreCase) })
    [ordered]@{
        schemaVersion = 1; installId = 'test-install'; token = 'tok'; status = 'Installed'
        version = '9.9.9-rc1'; expectedPriorVersion = '9.9.8'
        modsPath = $mods; backupPlanner = $prior
        beforeOtherModsManifest = $manifest
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $installDir 'install.json')

    $restoreScript = Join-Path $PSScriptRoot 'Restore-InstallLocal.ps1'
    $arguments = @{ InstallId = 'test-install'; GameRoot = $game; StateRoot = $state
                    BackupRoot = $backup; StagingRoot = $staging; EvidenceRoot = $evidence
                    Confirm = $false }

    # WhatIf purity: nothing moves.
    & $restoreScript @arguments -WhatIf | Out-Null
    Assert-True (Test-Path -LiteralPath (Join-Path $planner 'Info.json')) 'WhatIf removed the installed planner.'
    Assert-True (@(Get-ChildItem -LiteralPath $staging).Count -eq 0) 'WhatIf created staging state.'
    $passed++

    # Real rollback: prior restored, newer profile preserved, record updated.
    & $restoreScript @arguments | Out-Null
    Assert-True ((Read-KbpJson (Join-Path $planner 'Info.json')).Version -ceq '9.9.8') `
        'Rollback did not restore the prior planner version.'
    Assert-True (Test-Path -LiteralPath (Join-Path $planner 'UserSettings\kingmaker-buff-planner-new.json')) `
        'Newer post-install profile was not preserved.'
    Assert-True (Test-Path -LiteralPath (Join-Path $planner 'UserSettings\kingmaker-buff-planner-old.json')) `
        'Prior profile was lost.'
    Assert-True (Test-Path -LiteralPath (Join-Path $evidence 'rollback-test-install\rolled-back-planner\Info.json')) `
        'Rolled-back build was not archived as evidence.'
    $record = Read-KbpJson (Join-Path $installDir 'install.json')
    Assert-True ([string]$record.status -ceq 'RolledBack') 'Record status was not RolledBack.'
    Assert-True (@($record.rollbackPreservedProfiles).Count -eq 1) 'Preserved-profile list was not recorded.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $planner 'UserSettings\kingmaker-buff-planner-casting-abc.json'))) `
        'A schema-6 candidate profile was left active beside the rolled-back build.'
    Assert-True (Test-Path -LiteralPath (Join-Path $evidence 'rollback-test-install\deactivated-candidate-profiles\kingmaker-buff-planner-casting-abc.json')) `
        'The deactivated schema-6 candidate was not archived with the rollback evidence.'
    Assert-True (@($record.rollbackDeactivatedCandidateProfiles).Count -eq 1) `
        'Deactivated candidate profiles were not recorded.'
    Assert-True (Test-Path -LiteralPath (Join-Path (Join-Path $mods 'OtherMod') 'other.txt')) `
        'Unrelated mod was touched.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $state 'deployment.lock'))) 'Lock was not released.'
    $passed++

    # Second rollback of the same record refuses.
    $refused = $false
    try { & $restoreScript @arguments | Out-Null }
    catch { $refused = $true }
    Assert-True $refused 'A non-Installed record was rolled back again.'
    $passed++

    # Unknown install id refuses.
    $refused = $false
    try { & $restoreScript -InstallId 'absent' -GameRoot $game -StateRoot $state `
            -BackupRoot $backup -StagingRoot $staging -EvidenceRoot $evidence -Confirm:$false | Out-Null }
    catch { $refused = $true }
    Assert-True $refused 'An unknown install id was accepted.'
    $passed++

    Write-Host "Restore-InstallLocal tests: PASS=$passed FAIL=0"
}
finally {
    if (Test-Path -LiteralPath $boundary) { Remove-Item -LiteralPath $boundary -Recurse -Force }
}
