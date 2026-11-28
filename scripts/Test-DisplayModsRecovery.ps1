[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeAutomation.Common.ps1')
. (Join-Path $PSScriptRoot 'RuntimeDisplayRecovery.ps1')

# Only OS input/state and interruption timing are supplied by the fixture.
# Actual manifests, receipt files, lock ownership, moves and admission are exercised.
$originalMove = (Get-Command Move-KbpRecoveryDirectory).ScriptBlock
$processCheck = (Get-Command Assert-KbpNotRunning).ScriptBlock
function Move-KbpRecoveryDirectory($State,$Source,$Destination) {
    if ($script:stopAt -ceq 'before-first') { throw 'injected interruption' }
    & $originalMove $State $Source $Destination
    if (($script:stopAt -ceq 'after-first' -and $Source -ceq $State.modsPath) -or
        ($script:stopAt -ceq 'after-second' -and $Source -ceq $State.originalBackup)) { throw 'injected interruption' }
}
function Assert-KbpNotRunning { & $processCheck -KnownProcessIds $script:processIds }
function Get-KbpGamePath { $game }
function Get-KbpRegistryValueSnapshot($KeyPath) { return [ordered]@{} }
function Get-KbpSessionDisplaySnapshot { return $script:currentDisplay }
function Assert-Result($Condition,$Message) { if (-not $Condition) { throw $Message } }
function Assert-Refuses([scriptblock]$Action,[string]$Pattern) {
    $failure = ''
    try { & $Action | Out-Null } catch { $failure = $_.Exception.Message }
    if ($failure -notlike $Pattern) { throw "Expected '$Pattern', got '$failure'." }
}
function Recover { Invoke-KbpDisplayRecovery $id $script:KbpRuntimeStateRoot $script:KbpRuntimeEvidenceRoot -ModsOnly }
function Finish { Invoke-KbpDisplayRecovery $id $script:KbpRuntimeStateRoot $script:KbpRuntimeEvidenceRoot }

# A disposable boundary inside this checkout's harness test root, with a
# private Kingmaker native-test reservation per case (inherited by any child
# process); the machine-wide reservation is never touched.
$temp = Join-Path (Get-KbpRepositoryRoot) 'artifacts\runtime-harness-tests'
New-Item -ItemType Directory -Path $temp -Force | Out-Null
$boundary = Join-Path $temp ('kbp-mods-recovery-' + [Guid]::NewGuid().ToString('N'))
$priorReservationRoot = $env:KBP_TEST_RESERVATION_ROOT
$original = [pscustomobject]@{ primarySize='1920x1080'; primaryWidth=1920; primaryHeight=1080
    virtualLeft=0; virtualTop=0; virtualWidth=1920; virtualHeight=1080; monitorCount=1 }
$mismatch = $original.PSObject.Copy()
$mismatch.virtualLeft = -20  # Same primary size; the whole snapshot matters.
$passed = 0
try {
    foreach ($case in @('partial','before-first','after-first','after-second','owner','sentinel','missing',
        'modified','running','foreign','pending','save-drift','registry','additional-failure','ordinary','whatif')) {
        $script:KbpLabRoot = Join-Path $boundary $case
        $script:KbpRuntimeStateRoot = Join-Path $script:KbpLabRoot 'state'
        $script:KbpRuntimeEvidenceRoot = Join-Path $script:KbpLabRoot 'evidence'
        $script:KbpRuntimeBackupRoot = Join-Path $script:KbpLabRoot 'backups'
        $script:KbpRuntimeStagingRoot = Join-Path $script:KbpLabRoot 'staging'
        $env:KBP_TEST_RESERVATION_ROOT = Join-Path $boundary ($case + '-reservation')
        $foreign = Join-Path $script:KbpLabRoot 'foreign.lock'
        $script:KbpForeignRuntimeLeases = @($foreign)
        $id = 'display-test'
        $token = [Guid]::NewGuid().ToString('N')
        $dir = Join-Path $script:KbpRuntimeStateRoot "transactions\$id"
        $ev = Join-Path $script:KbpRuntimeEvidenceRoot $id
        $backupRoot = Join-Path $script:KbpRuntimeBackupRoot $id
        $game = Join-Path $script:KbpLabRoot 'game'
        $live = Join-Path $game 'Mods'
        $backup = Join-Path $backupRoot 'Mods.original'
        $quarantine = Join-Path $backupRoot 'Mods.staged'
        $lock = Join-Path $script:KbpRuntimeStateRoot 'deployment.lock'
        $statePath = Join-Path $dir 'transaction.json'
        $stage = Join-Path $script:KbpRuntimeStagingRoot $id
        $physical = Join-Path $dir 'display-physical.json'
        $reg = Join-Path $dir 'display-registry.json'
        $base = Join-Path $dir 'protected-saves-before.json'
        $completion = Join-Path $ev 'run-completion.json'
        $receipt = Join-Path $ev 'mods-recovery.json'
        $saveRoot = Join-Path $script:KbpLabRoot 'saves'
        foreach ($path in @($dir,$ev,$backup,$live,$stage,$saveRoot)) { [void][IO.Directory]::CreateDirectory($path) }
        New-KbpOwnedLock $lock $id $token
        $lockHash = Get-KbpSha256 $lock
        $now = [DateTime]::UtcNow.ToString('o')
        [IO.File]::WriteAllText((Join-Path $backup 'original.dll'),'original')
        $sent = Join-Path $live '.kbp-runtime-sentinel.json'
        Write-KbpJsonAtomic $sent @{schemaVersion=1; runId=$id; token=$token; statePath=$statePath}
        Write-KbpJsonAtomic $statePath ([ordered]@{schemaVersion=1; runId=$id; token=$token; createdAtUtc=$now; activatedAtUtc=$now
            status='Active'; restorationVerified=$false; restoredAtUtc=$null; lockPath=$lock; gameRoot=$game; modsPath=$live
            originalExisted=$true; originalBackup=$backup; stagedQuarantine=$quarantine; stagingRunRoot=$stage
            originalManifest=@(Get-KbpDirectoryManifest $backup); stagedManifest=@(Get-KbpDirectoryManifest $live)
            stagedMutationObserved=$false; observedStagedManifest=@(); packageSha256=('a'*64) })
        [IO.File]::WriteAllText((Join-Path $live 'changed.cfg'),'staged mutation')
        $expectedStage = @(Get-KbpDirectoryManifest $live)
        $expectedOriginal = @(Get-KbpDirectoryManifest $backup)
        Save-KbpPhysicalDisplaySnapshotFile $physical $original $id '1280x720'
        $physicalHash = Get-KbpSha256 $physical
        $now = [DateTime]::UtcNow.ToString('o')
        Write-KbpJsonAtomic $reg @{schemaVersion=1; runId=$id; keyPath='HKCU:\fixture'; values=@{}; restored=$true; savedAtUtc=$now; restoredAtUtc=$now}
        Write-KbpJsonAtomic $base @{runId=$id; scenario='live-workspace-native-ui'; fixtureFamily='Automation'; workingFileName='working'
            saveRoot=$saveRoot; files=@(); compared=$true; blocking=@(); comparedAtUtc=$now}
        Write-KbpJsonAtomic (Join-Path $ev 'protected-saves.json') @{runId=$id; fixtureFamily='Automation'; violations=@(); blocking=@()}
        $detail = "Physical display state changed during run " + $id + ': virtualLeft:0->-20. Refusing to overwrite a potentially newer user change; the pending snapshot must be reviewed before another run.'
        $failure = "Mods restoration failed for $id (Restore-Local.ps1 -RunId $id recovers it once the cause is fixed): $detail | Display/game-settings restoration verification failed after the windowed run (Restore-Local.ps1 -RunId $id retries or preserves it for review): $detail"
        Write-KbpJsonAtomic $completion @{runId=$id; scenario='live-workspace-native-ui'; fixtureFamily='Automation'; fixture=@{workingFileName='working'}
            kingmakerExited=$true; complete=$false; restorationVerified=$false; protectedSavesApplicable=$true; protectedSavesCompared=$true
            protectedSavesClean=$true; restorationFailure=$failure; completedAtUtc=$now}
        $failedHash = Get-KbpSha256 $completion
        $script:currentDisplay = $mismatch
        $script:stopAt = ''
        $script:processIds = @()
        $rejection = $null
        switch ($case) {
            'owner' { $v=Read-KbpJson $lock; $v.token='foreign'; Write-KbpJsonAtomic $lock $v; $rejection='*ownership*' }
            'sentinel' { $v=Read-KbpJson $sent; $v.runId='foreign'; Write-KbpJsonAtomic $sent $v; $rejection='*sentinel*' }
            'missing' { [IO.Directory]::Move($backup, (Join-Path $backupRoot 'missing')); $rejection='*layout*' }
            'modified' { [IO.File]::AppendAllText((Join-Path $backup 'original.dll'),'altered'); $rejection='*manifest*' }
            'running' { $script:processIds=@(1234); $rejection='*is running*' }
            'foreign' { [IO.File]::WriteAllText($foreign,'fixture'); $rejection='*foreign.lock*' }
            'pending' { $v=Read-KbpJson $base; $v.compared=$false; Write-KbpJsonAtomic $base $v; $rejection='*protected-save*' }
            'registry' { $v=Read-KbpJson $reg; $v.restored=$false; Write-KbpJsonAtomic $reg $v; $rejection='*registry*' }
            'save-drift' { [IO.File]::WriteAllText((Join-Path $saveRoot 'new.zks'),'unexpected'); $rejection='*saves differ*' }
            'additional-failure' { $v=Read-KbpJson $completion; $v.restorationFailure+=' | missing dependency'; Write-KbpJsonAtomic $completion $v; $rejection='*isolated display*' }
        }
        if ($rejection) {
            $before = @(Get-KbpDirectoryManifest $script:KbpLabRoot)
            Assert-Refuses { Recover } $rejection
            Assert-Result (Test-KbpManifestEqual $before @(Get-KbpDirectoryManifest $script:KbpLabRoot)) 'Refusal changed fixture bytes.'
        } elseif ($case -ceq 'whatif') {
            $before = @(Get-KbpDirectoryManifest $script:KbpLabRoot)
            & (Join-Path $PSScriptRoot 'Restore-Local.ps1') -RunId $id -StateRoot $script:KbpRuntimeStateRoot -EvidenceRoot $script:KbpRuntimeEvidenceRoot -RestoreModsOnly -WhatIf
            Assert-Result (Test-KbpManifestEqual $before @(Get-KbpDirectoryManifest $script:KbpLabRoot)) 'WhatIf changed fixture bytes.'
        } elseif ($case -ceq 'ordinary') {
            $script:currentDisplay = $original
            [void](Confirm-KbpPhysicalDisplaySnapshotFile $physical $original -Reverify)
            $result = Restore-KbpRuntimeTransaction -RunId $id -StateRoot $script:KbpRuntimeStateRoot -FixtureMode
            Assert-Result ($result.restorationVerified -and -not (Test-Path $lock) -and
                (Test-KbpManifestEqual $expectedOriginal @(Get-KbpDirectoryManifest $live))) 'Ordinary restoration failed.'
        } else {
            if ($case -cne 'partial') {
                $script:stopAt = $case
                Assert-Refuses { Recover } '*injected interruption*'
                Assert-Result ((Read-KbpJson $statePath).status -ceq 'RecoveringMods' -and
                    (Get-KbpSha256 $lock) -ceq $lockHash) 'Lost interrupted recovery intent/lock.'
                $script:stopAt = ''
            }
            $result = Recover
            $receiptHash = Get-KbpSha256 $receipt
            $script:stopAt = 'before-first'  # Any additional rename is a failure.
            $result = Recover
            Assert-Refuses { Finish } '*Physical display state changed*'
            Assert-Refuses { Assert-KbpNoUnresolvedTransaction $script:KbpRuntimeStateRoot } '*deployment lock*'
            Assert-Result ($result.status -ceq 'ModsRestoredPendingDisplay' -and -not $result.restorationVerified -and
                (Get-KbpSha256 $lock) -ceq $lockHash -and (Get-KbpSha256 $physical) -ceq $physicalHash -and
                (Get-KbpSha256 $receipt) -ceq $receiptHash -and (Get-KbpSha256 $completion) -ceq $failedHash -and
                (Test-KbpManifestEqual $expectedOriginal @(Get-KbpDirectoryManifest $live)) -and
                (Test-KbpManifestEqual $expectedStage @(Get-KbpDirectoryManifest $quarantine))) 'Partial recovery invariant failed.'
            $script:currentDisplay = $original
            if ($case -ceq 'partial') {
                $extra = Join-Path $live 'unexpected'
                [IO.File]::WriteAllText($extra,'after recovery')
                Assert-Refuses { Finish } '*manifest/hash mismatch*'
                Assert-Result ((Get-KbpSha256 $lock) -ceq $lockHash) 'Finalization released an unverified tree.'
                [IO.File]::Delete($extra)
            }
            $result = Finish
            Assert-Result ($result.restorationVerified -and $result.status -ceq 'Restored' -and -not (Test-Path $lock) -and
                (Get-KbpSha256 $completion) -ceq $failedHash -and (Get-KbpSha256 $receipt) -ceq $receiptHash -and
                (Get-KbpSha256 (Join-Path $dir 'display-physical.original.json')) -ceq $physicalHash) 'Full finalization failed.'
            Assert-KbpNoUnresolvedTransaction $script:KbpRuntimeStateRoot
        }
        Write-Host "PASS mods-recovery-$case"
        $passed++
    }
    Write-Host "Display Mods recovery: PASS=$passed FAIL=0"
} finally {
    $env:KBP_TEST_RESERVATION_ROOT = $priorReservationRoot
    if ([IO.Path]::GetFullPath($boundary) -ine $boundary -or
        (Split-Path -Parent $boundary) -ine $temp.TrimEnd('\') -or
        (Split-Path -Leaf $boundary) -notmatch '^kbp-mods-recovery-[0-9a-f]{32}$') { throw 'Unsafe fixture cleanup path.' }
    if (Test-Path -LiteralPath $boundary) { Remove-Item -LiteralPath $boundary -Recurse -Force }
}
