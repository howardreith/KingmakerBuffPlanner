[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')

$repo = Get-KbpRepositoryRoot
$package = Join-Path $repo ('artifacts\local-runtime\' + (Get-KbpVersion) + '\KingmakerBuffPlanner-' + (Get-KbpVersion) + '-local-runtime.zip')
if (-not (Test-Path -LiteralPath $package -PathType Leaf)) { throw 'Build the validated local package before runtime harness tests.' }
$root = Join-Path $repo ('artifacts\runtime-harness-tests\' + [Guid]::NewGuid().ToString('N'))
$stateRoot = Join-Path $root 'state'
$stagingRoot = Join-Path $root 'staging'
$backupRoot = Join-Path $root 'backups'
foreach ($path in @($stateRoot, $stagingRoot, $backupRoot)) { New-Item -ItemType Directory -Path $path -Force | Out-Null }
$passed = 0
try {
    Assert-KbpNotRunning -KnownProcessIds $null
    $passed++

    $game = Join-Path $root 'game-existing'
    New-Item -ItemType Directory -Path (Join-Path $game 'Mods\Existing\settings') -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $game 'Kingmaker.exe') -Value 'fixture' -Encoding Ascii
    Set-Content -LiteralPath (Join-Path $game 'Mods\Existing\Info.json') -Value '{"Id":"Existing"}' -Encoding Ascii
    Set-Content -LiteralPath (Join-Path $game 'Mods\Existing\settings\state.txt') -Value 'preserve' -Encoding Ascii
    $before = @(Get-KbpDirectoryManifest (Join-Path $game 'Mods'))
    $statePath = Enter-KbpRuntimeTransaction -PackagePath $package -KingmakerInstallDir $game `
        -StateRoot $stateRoot -StagingRoot $stagingRoot -BackupRoot $backupRoot `
        -RunId 'success' -FixtureMode -KnownKingmakerProcessIds @()
    if (-not (Test-Path -LiteralPath (Join-Path $game 'Mods\KingmakerBuffPlanner\Info.json'))) { throw 'Product was not staged.' }
    $restored = Restore-KbpRuntimeTransaction -RunId 'success' -StateRoot $stateRoot -FixtureMode -KnownKingmakerProcessIds @()
    if (-not $restored.restorationVerified -or
        -not (Test-KbpManifestEqual $before @(Get-KbpDirectoryManifest (Join-Path $game 'Mods')))) {
        throw 'Successful transaction did not restore the exact original manifest.'
    }
    $passed++

    $game = Join-Path $root 'game-pre-activation-interruption'
    $mods = Join-Path $game 'Mods'
    New-Item -ItemType Directory -Path (Join-Path $mods 'Existing') -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $game 'Kingmaker.exe') -Value 'fixture' -Encoding Ascii
    Set-Content -LiteralPath (Join-Path $mods 'Existing\Info.json') -Value '{"Id":"Existing"}' -Encoding Ascii
    $runId = 'pre-activation-interruption'
    $token = [Guid]::NewGuid().ToString('N')
    $lockPath = Join-Path $stateRoot 'deployment.lock'
    $transactionRoot = Join-Path $stateRoot ('transactions\' + $runId)
    $stagingRunRoot = Join-Path $stagingRoot $runId
    $backupRunRoot = Join-Path $backupRoot $runId
    New-Item -ItemType Directory -Path $transactionRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $stagingRunRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $backupRunRoot -Force | Out-Null
    New-KbpOwnedLock $lockPath $runId $token
    $before = @(Get-KbpDirectoryManifest $mods)
    $statePath = Join-Path $transactionRoot 'transaction.json'
    Write-KbpJsonAtomic $statePath ([ordered]@{
        schemaVersion = 1; runId = $runId; token = $token; status = 'Preparing'
        modsPath = $mods; originalExisted = $true; originalManifest = $before
        originalBackup = Join-Path $backupRunRoot 'Mods.original'
        stagedQuarantine = Join-Path $backupRunRoot 'Mods.staged'
        stagingRunRoot = $stagingRunRoot; lockPath = $lockPath
        restorationVerified = $false; stagedMutationObserved = $false
        observedStagedManifest = @(); restoredAtUtc = $null; restorationFailure = $null
    })
    $restored = Restore-KbpRuntimeTransaction -RunId $runId -StateRoot $stateRoot `
        -FixtureMode -KnownKingmakerProcessIds @()
    if (-not $restored.restorationVerified -or
        -not (Test-KbpManifestEqual $before @(Get-KbpDirectoryManifest $mods)) -or
        (Test-Path -LiteralPath $stagingRunRoot) -or (Test-Path -LiteralPath $lockPath)) {
        throw 'Pre-activation interruption did not recover as an exact no-op.'
    }
    $passed++

    $game = Join-Path $root 'game-mutation'
    New-Item -ItemType Directory -Path (Join-Path $game 'Mods\Existing') -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $game 'Kingmaker.exe') -Value 'fixture' -Encoding Ascii
    Set-Content -LiteralPath (Join-Path $game 'Mods\Existing\Info.json') -Value '{"Id":"Existing"}' -Encoding Ascii
    $before = @(Get-KbpDirectoryManifest (Join-Path $game 'Mods'))
    Enter-KbpRuntimeTransaction -PackagePath $package -KingmakerInstallDir $game `
        -StateRoot $stateRoot -StagingRoot $stagingRoot -BackupRoot $backupRoot `
        -RunId 'mutation' -FixtureMode -KnownKingmakerProcessIds @() | Out-Null
    Set-Content -LiteralPath (Join-Path $game 'Mods\unexpected.txt') -Value 'runtime mutation' -Encoding Ascii
    $restored = Restore-KbpRuntimeTransaction -RunId 'mutation' -StateRoot $stateRoot -FixtureMode -KnownKingmakerProcessIds @()
    if (-not $restored.stagedMutationObserved -or
        -not (Test-KbpManifestEqual $before @(Get-KbpDirectoryManifest (Join-Path $game 'Mods')))) {
        throw 'Staged mutation was not recorded while restoring the original.'
    }
    $passed++

    $game = Join-Path $root 'game-absent'
    New-Item -ItemType Directory -Path $game -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $game 'Kingmaker.exe') -Value 'fixture' -Encoding Ascii
    Enter-KbpRuntimeTransaction -PackagePath $package -KingmakerInstallDir $game `
        -StateRoot $stateRoot -StagingRoot $stagingRoot -BackupRoot $backupRoot `
        -RunId 'absent' -FixtureMode -KnownKingmakerProcessIds @() | Out-Null
    $restored = Restore-KbpRuntimeTransaction -RunId 'absent' -StateRoot $stateRoot -FixtureMode -KnownKingmakerProcessIds @()
    if (-not $restored.restorationVerified -or (Test-Path -LiteralPath (Join-Path $game 'Mods'))) {
        throw 'Original Mods absence was not restored.'
    }
    $passed++

    $game = Join-Path $root 'game-running'
    New-Item -ItemType Directory -Path (Join-Path $game 'Mods') -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $game 'Kingmaker.exe') -Value 'fixture' -Encoding Ascii
    try {
        Enter-KbpRuntimeTransaction -PackagePath $package -KingmakerInstallDir $game `
            -StateRoot $stateRoot -StagingRoot $stagingRoot -BackupRoot $backupRoot `
            -RunId 'running' -FixtureMode -KnownKingmakerProcessIds @(4242) | Out-Null
        throw 'Running-process preflight did not fail.'
    }
    catch {
        if ($_.Exception.Message -notlike '*Kingmaker is running*') { throw }
    }
    if (-not (Test-Path -LiteralPath (Join-Path $game 'Mods'))) { throw 'Running preflight mutated Mods.' }
    $passed++

    $game = Join-Path $root 'game-lock'
    New-Item -ItemType Directory -Path (Join-Path $game 'Mods') -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $game 'Kingmaker.exe') -Value 'fixture' -Encoding Ascii
    Set-Content -LiteralPath (Join-Path $stateRoot 'deployment.lock') -Value '{"foreign":true}' -Encoding Ascii
    try {
        Enter-KbpRuntimeTransaction -PackagePath $package -KingmakerInstallDir $game `
            -StateRoot $stateRoot -StagingRoot $stagingRoot -BackupRoot $backupRoot `
            -RunId 'lock' -FixtureMode -KnownKingmakerProcessIds @() | Out-Null
        throw 'Foreign lock preflight did not fail.'
    }
    catch {
        if ($_.Exception.Message -notlike '*Unresolved runtime deployment lock*') { throw }
    }
    Remove-Item -LiteralPath (Join-Path $stateRoot 'deployment.lock') -Force
    $passed++

    $game = Join-Path $root 'game-compatibility'
    $fixtureMod = Join-Path $game 'Mods\FixtureOptional'
    New-Item -ItemType Directory -Path (Join-Path $fixtureMod 'data') -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $game 'Kingmaker.exe') -Value 'fixture' -Encoding Ascii
    Set-Content -LiteralPath (Join-Path $fixtureMod 'info.json') `
        -Value '{"Id":"FixtureOptional","Version":"1.0"}' -Encoding Ascii
    Set-Content -LiteralPath (Join-Path $fixtureMod 'FixtureOptional.dll') -Value 'assembly' -Encoding Ascii
    Set-Content -LiteralPath (Join-Path $fixtureMod 'data\value.txt') -Value 'exact' -Encoding Ascii
    $identity = Get-KbpDirectoryContentIdentity $fixtureMod
    $profile = [pscustomobject]@{
        profileId = 'fixture-compatibility'
        mods = @([pscustomobject]@{
            ummId = 'FixtureOptional'; directoryName = 'FixtureOptional'; version = '1.0'
            assemblyName = 'FixtureOptional.dll'
            infoSha256 = Get-KbpSha256 (Join-Path $fixtureMod 'info.json')
            assemblySha256 = Get-KbpSha256 (Join-Path $fixtureMod 'FixtureOptional.dll')
            directoryManifestSha256 = $identity.directoryManifestSha256
            fileCount = $identity.fileCount; totalBytes = $identity.totalBytes
        })
    }
    $before = @(Get-KbpDirectoryManifest (Join-Path $game 'Mods'))
    $statePath = Enter-KbpRuntimeTransaction -PackagePath $package -KingmakerInstallDir $game `
        -StateRoot $stateRoot -StagingRoot $stagingRoot -BackupRoot $backupRoot `
        -RunId 'compatibility' -FixtureMode -KnownKingmakerProcessIds @() `
        -CompatibilityProfile $profile
    $active = Read-KbpJson $statePath
    if (-not (Test-Path -LiteralPath (Join-Path $game 'Mods\FixtureOptional\data\value.txt')) -or
        $active.compatibilityProfileId -cne 'fixture-compatibility' -or
        @($active.compatibilityMods).Count -ne 1) {
        throw 'Exact optional fixture was not staged or recorded.'
    }
    $restored = Restore-KbpRuntimeTransaction -RunId 'compatibility' -StateRoot $stateRoot `
        -FixtureMode -KnownKingmakerProcessIds @()
    if (-not $restored.restorationVerified -or
        -not (Test-KbpManifestEqual $before @(Get-KbpDirectoryManifest (Join-Path $game 'Mods')))) {
        throw 'Compatibility transaction did not restore the exact original manifest.'
    }
    $passed++

    # --- Guarded automation-fixture bootstrap (production script) ---
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    function New-TestSaveArchive {
        param([string]$Path, [string]$Name, [string]$GameName = 'Disposable Automation Campaign', [string]$GameId = '11111111-2222-3333-4444-555555555555')
        $archive = [IO.Compression.ZipFile]::Open($Path,
            [IO.Compression.ZipArchiveMode]::Create, [Text.Encoding]::UTF8)
        try {
            $headerEntry = $archive.CreateEntry('header.json')
            $writer = [IO.StreamWriter]::new($headerEntry.Open(), [Text.Encoding]::UTF8)
            try {
                $writer.Write('{"Name":"' + $Name + '","GameName":"' + $GameName +
                    '","GameId":"' + $GameId + '","Area":"Test Area"}')
            } finally { $writer.Dispose() }
            $partyEntry = $archive.CreateEntry('party.json')
            $writer = [IO.StreamWriter]::new($partyEntry.Open(), [Text.Encoding]::UTF8)
            try { $writer.Write('{"members":[]}') } finally { $writer.Dispose() }
        } finally { $archive.Dispose() }
    }
    function Read-TestHeaderName {
        param([string]$Path)
        $archive = [IO.Compression.ZipFile]::OpenRead($Path)
        try {
            $reader = [IO.StreamReader]::new($archive.GetEntry('header.json').Open())
            try { return ((($reader.ReadToEnd()) | ConvertFrom-Json).Name) } finally { $reader.Dispose() }
        } finally { $archive.Dispose() }
    }
    function New-FixtureSeedRoot {
        param([string]$Name)
        $saveRoot = Join-Path $root $Name
        New-Item -ItemType Directory -Path $saveRoot | Out-Null
        New-TestSaveArchive -Path (Join-Path $saveRoot 'Manual_400_PlayerCampaign.zks') -Name 'PlayerCampaign' -GameName 'Valued Campaign'
        New-TestSaveArchive -Path (Join-Path $saveRoot 'Manual_401_KBP_AUTOMATION_SEED.zks') -Name 'KBP_AUTOMATION_SEED'
        return $saveRoot
    }
    $bootstrapScript = Join-Path $repo 'scripts\New-KbpAutomationFixture.ps1'
    $stateRoot = Join-Path $root 'fixture-state'

    # Reproduce the reviewed bypass: an explicitly supplied null id list must
    # NOT see a real Kingmaker-named process...
    $probeDir = Join-Path $root 'guard-probe'
    New-Item -ItemType Directory -Path $probeDir | Out-Null
    Copy-Item (Join-Path $env:SystemRoot 'System32\cmd.exe') (Join-Path $probeDir 'Kingmaker.exe')
    $probe = Start-Process -FilePath (Join-Path $probeDir 'Kingmaker.exe') `
        -ArgumentList '/c', 'timeout', '/t', '25', '/nobreak' -PassThru -WindowStyle Hidden
    try {
        $guardSawProcess = $false
        try { Assert-KbpNotRunning } catch { $guardSawProcess = $true }
        if (-not $guardSawProcess) { throw 'Real process discovery failed to see the probe.' }
        $bypassed = $false
        try { Assert-KbpNotRunning -KnownProcessIds $null } catch { $bypassed = $true }
        if ($bypassed) { throw 'Reproduction failed: the null-list bypass should pass (documenting the defect).' }
        # ...and the REPAIRED production script must refuse while it runs.
        $guardRoot = New-FixtureSeedRoot 'saves-guard'
        $refused = $false
        try {
            & $bootstrapScript -RunId 'guard-refused' -SaveRoot $guardRoot `
                -StateRoot $stateRoot -ArchiveRoot (Join-Path $root 'archive-guard') -Confirm:$false | Out-Null
        } catch { $refused = $true }
        if (-not $refused) { throw 'Repaired bootstrap did not refuse while Kingmaker was running.' }
        if (@(Get-ChildItem -LiteralPath $guardRoot -Filter '*KBP_AUTOMATION_BASELINE*').Count -ne 0 -or
            @(Get-ChildItem -LiteralPath $guardRoot -Filter '*KBP_AUTOMATION_WORKING*').Count -ne 0) {
            throw 'Refused bootstrap still wrote fixture saves.'
        }
    }
    finally {
        if (-not $probe.HasExited) {
            try { Stop-Process -Id $probe.Id -Force } catch { }
        }
    }
    $passed++

    # WhatIf purity: no state directories, no saves, no lock.
    $whatIfRoot = New-FixtureSeedRoot 'saves-whatif'
    $WhatIfPreference = $true
    try {
        & $bootstrapScript -RunId 'whatif' -SaveRoot $whatIfRoot `
            -StateRoot (Join-Path $stateRoot 'whatif') -ArchiveRoot (Join-Path $root 'archive-whatif') | Out-Null
    }
    finally { $WhatIfPreference = $false }
    if ((Test-Path -LiteralPath (Join-Path $stateRoot 'whatif')) -or
        (Test-Path -LiteralPath (Join-Path $root 'archive-whatif')) -or
        @(Get-ChildItem -LiteralPath $whatIfRoot -Filter '*.zks').Count -ne 2) {
        throw 'Fixture bootstrap WhatIf was not pure.'
    }
    $passed++

    # Happy path: staged, archived, manifest, published pair; everything
    # else byte-identical; transaction completed; lock released.
    $saveRoot = New-FixtureSeedRoot 'saves-happy'
    $playerHash = Get-KbpSha256 (Join-Path $saveRoot 'Manual_400_PlayerCampaign.zks')
    $seedHash = Get-KbpSha256 (Join-Path $saveRoot 'Manual_401_KBP_AUTOMATION_SEED.zks')
    $archive = Join-Path $root 'archive-happy'
    & $bootstrapScript -RunId 'happy' -SaveRoot $saveRoot -StateRoot $stateRoot -ArchiveRoot $archive -Confirm:$false | Out-Null
    $baselinePath = Join-Path $saveRoot 'Manual_402_KBP_AUTOMATION_BASELINE.zks'
    $workingPath = Join-Path $saveRoot 'Manual_403_KBP_AUTOMATION_WORKING.zks'
    $manifest = Read-KbpJson (Join-Path $archive 'fixture-manifest.json')
    if (-not (Test-Path -LiteralPath $baselinePath) -or -not (Test-Path -LiteralPath $workingPath) -or
        (Read-TestHeaderName $baselinePath) -cne 'KBP_AUTOMATION_BASELINE' -or
        (Read-TestHeaderName $workingPath) -cne 'KBP_AUTOMATION_WORKING' -or
        $manifest.baseline.sha256 -cne (Get-KbpSha256 $baselinePath) -or
        -not (Test-Path -LiteralPath (Join-Path $archive 'Manual_401_KBP_AUTOMATION_SEED.zks'))) {
        throw 'Happy-path bootstrap did not produce the verified sealed pair and archive.'
    }
    if ((Get-KbpSha256 (Join-Path $saveRoot 'Manual_400_PlayerCampaign.zks')) -cne $playerHash -or
        (Get-KbpSha256 (Join-Path $saveRoot 'Manual_401_KBP_AUTOMATION_SEED.zks')) -cne $seedHash) {
        throw 'Happy-path bootstrap mutated the seed or an ordinary save.'
    }
    $transaction = Read-KbpJson (Join-Path $stateRoot 'happy\transaction.json')
    if ($transaction.status -cne 'Completed' -or
        (Test-Path -LiteralPath (Join-Path $stateRoot 'fixture.lock'))) {
        throw 'Happy-path transaction did not complete or the lock leaked.'
    }
    $passed++

    # Repeated operation refuses (existing pair + existing run paths).
    $refused = $false
    try {
        & $bootstrapScript -RunId 'happy-again' -SaveRoot $saveRoot -StateRoot $stateRoot `
            -ArchiveRoot (Join-Path $root 'archive-happy-2') -Confirm:$false | Out-Null
    } catch { $refused = $true }
    if (-not $refused) { throw 'A second bootstrap over an existing pair did not refuse.' }
    $refused = $false
    try {
        & $bootstrapScript -RunId 'happy' -SaveRoot (New-FixtureSeedRoot 'saves-rerun') `
            -StateRoot $stateRoot -ArchiveRoot (Join-Path $root 'archive-rerun') -Confirm:$false | Out-Null
    } catch { $refused = $true }
    if (-not $refused) { throw 'Reusing a run id did not refuse.' }
    $passed++

    # Lock coordination refuses a concurrent operation.
    New-Item -ItemType Directory -Path $stateRoot -Force | Out-Null
    $lockToken = [Guid]::NewGuid().ToString('N')
    New-KbpOwnedLock (Join-Path $stateRoot 'fixture.lock') 'concurrent' $lockToken
    $refused = $false
    try {
        & $bootstrapScript -RunId 'locked' -SaveRoot (New-FixtureSeedRoot 'saves-locked') `
            -StateRoot $stateRoot -ArchiveRoot (Join-Path $root 'archive-locked') -Confirm:$false | Out-Null
    } catch { $refused = $true }
    Remove-KbpOwnedLock (Join-Path $stateRoot 'fixture.lock') 'concurrent' $lockToken
    if (-not $refused) { throw 'A concurrent locked bootstrap did not refuse.' }
    $passed++

    # Injected failures roll back exactly the owned partial state and leave
    # the seed and ordinary saves intact; retry then succeeds.
    foreach ($stage in @('Staged', 'ManifestWritten', 'Published')) {
        $failRoot = New-FixtureSeedRoot ('saves-fail-' + $stage)
        $playerHash = Get-KbpSha256 (Join-Path $failRoot 'Manual_400_PlayerCampaign.zks')
        $seedHash = Get-KbpSha256 (Join-Path $failRoot 'Manual_401_KBP_AUTOMATION_SEED.zks')
        $failed = $false
        try {
            & $bootstrapScript -RunId ('fail-' + $stage) -SaveRoot $failRoot -StateRoot $stateRoot `
                -ArchiveRoot (Join-Path $root ('archive-fail-' + $stage)) -FailAfterStage $stage -Confirm:$false | Out-Null
        } catch { $failed = $true }
        if (-not $failed) { throw "Injected failure stage $stage did not fail." }
        if (@(Get-ChildItem -LiteralPath $failRoot -Filter '*KBP_AUTOMATION_BASELINE*').Count -ne 0 -or
            @(Get-ChildItem -LiteralPath $failRoot -Filter '*KBP_AUTOMATION_WORKING*').Count -ne 0 -or
            (Test-Path -LiteralPath (Join-Path $stateRoot ('fail-' + $stage + '\staging'))) -or
            -not (Test-Path -LiteralPath (Join-Path $stateRoot ('fail-' + $stage + '\transaction.json')))) {
            throw "Failure stage $stage did not roll back owned artifacts while keeping the recovery record."
        }
        $record = Read-KbpJson (Join-Path $stateRoot ('fail-' + $stage + '\transaction.json'))
        $lockLeaked = Test-Path -LiteralPath (Join-Path $stateRoot 'fixture.lock')
        if ($record.status -cne 'RolledBack' -or $lockLeaked) {
            throw "Failure stage $stage did not record RolledBack (=$($record.status)) or leaked the lock (=$lockLeaked)."
        }
        if ((Get-KbpSha256 (Join-Path $failRoot 'Manual_400_PlayerCampaign.zks')) -cne $playerHash -or
            (Get-KbpSha256 (Join-Path $failRoot 'Manual_401_KBP_AUTOMATION_SEED.zks')) -cne $seedHash) {
            throw "Failure stage $stage mutated a protected save."
        }
        # Safe retry with the same run id works after rollback.
        & $bootstrapScript -RunId ('fail-' + $stage) -SaveRoot $failRoot -StateRoot $stateRoot `
            -ArchiveRoot (Join-Path $root ('archive-fail-' + $stage + '-retry')) -Confirm:$false | Out-Null
        if (-not (Test-Path -LiteralPath (Join-Path $failRoot 'Manual_402_KBP_AUTOMATION_BASELINE.zks'))) {
            throw "Retry after rollback at stage $stage did not succeed."
        }
    }
    $passed++

    # Interrupted-process recovery: an orphaned Published transaction and
    # lock are recovered through -Recover using the ownership record only.
    $recoverRoot = New-FixtureSeedRoot 'saves-recover'
    $orphanRun = 'orphan'
    $orphanState = Join-Path $stateRoot $orphanRun
    New-Item -ItemType Directory -Path (Join-Path $orphanState 'staging') | Out-Null
    New-TestSaveArchive -Path (Join-Path $recoverRoot 'Manual_402_KBP_AUTOMATION_BASELINE.zks') -Name 'KBP_AUTOMATION_BASELINE'
    Write-KbpJsonAtomic (Join-Path $orphanState 'transaction.json') ([ordered]@{
        schemaVersion = 1; runId = $orphanRun; token = 'tok'; status = 'Published'
        saveRoot = $recoverRoot; seedPath = ''
        baselineName = 'Manual_402_KBP_AUTOMATION_BASELINE.zks'; workingName = 'Manual_403_KBP_AUTOMATION_WORKING.zks'
        publishedPaths = @((Join-Path $recoverRoot 'Manual_402_KBP_AUTOMATION_BASELINE.zks'))
        stagingRoot = (Join-Path $orphanState 'staging'); archiveRoot = ''
    })
    New-KbpOwnedLock (Join-Path $stateRoot 'fixture.lock') $orphanRun 'tok'
    $refused = $false
    try {
        & $bootstrapScript -RunId 'blocked-by-orphan' -SaveRoot (New-FixtureSeedRoot 'saves-orphan-block') `
            -StateRoot $stateRoot -ArchiveRoot (Join-Path $root 'archive-orphan') -Confirm:$false | Out-Null
    } catch { $refused = $true }
    if (-not $refused) { throw 'An unresolved transaction did not block a new bootstrap.' }
    & $bootstrapScript -RunId $orphanRun -StateRoot $stateRoot -Recover -Confirm:$false | Out-Null
    if ((Test-Path -LiteralPath (Join-Path $recoverRoot 'Manual_402_KBP_AUTOMATION_BASELINE.zks')) -or
        (Test-Path -LiteralPath (Join-Path $orphanState 'staging')) -or
        (Test-Path -LiteralPath (Join-Path $stateRoot 'fixture.lock'))) {
        throw 'Recover did not remove the owned partial artifacts or release the lock.'
    }
    $passed++

    # Manifest-bound teardown: happy path, then refusal modes.
    & $bootstrapScript -RunId 'happy' -SaveRoot $saveRoot -StateRoot $stateRoot -ArchiveRoot $archive -Teardown -Confirm:$false | Out-Null
    if ((Test-Path -LiteralPath $baselinePath) -or (Test-Path -LiteralPath $workingPath) -or
        -not (Test-Path -LiteralPath (Join-Path $saveRoot 'Manual_401_KBP_AUTOMATION_SEED.zks')) -or
        (Get-KbpSha256 (Join-Path $saveRoot 'Manual_400_PlayerCampaign.zks')) -cne $playerHash -or
        -not (Test-Path -LiteralPath (Join-Path $archive 'fixture-manifest.json'))) {
        throw 'Teardown did not remove exactly the manifest-owned pair while preserving seed/archive/ordinary saves.'
    }
    $refused = $false
    try {
        & $bootstrapScript -RunId 'teardown-nomanifest' -SaveRoot $saveRoot -StateRoot $stateRoot `
            -ArchiveRoot (Join-Path $root 'archive-missing') -Teardown -Confirm:$false | Out-Null
    } catch { $refused = $true }
    if (-not $refused) { throw 'Teardown without a manifest did not refuse.' }
    $passed++

    # Tampered sealed baseline hash refuses deletion.
    $tamperRoot = New-FixtureSeedRoot 'saves-tamper'
    $tamperArchive = Join-Path $root 'archive-tamper'
    & $bootstrapScript -RunId 'tamper' -SaveRoot $tamperRoot -StateRoot $stateRoot -ArchiveRoot $tamperArchive -Confirm:$false | Out-Null
    Remove-Item -LiteralPath (Join-Path $tamperRoot 'Manual_402_KBP_AUTOMATION_BASELINE.zks') -Force
    New-TestSaveArchive -Path (Join-Path $tamperRoot 'Manual_402_KBP_AUTOMATION_BASELINE.zks') -Name 'KBP_AUTOMATION_BASELINE' -GameName 'Tampered'
    $refused = $false
    try {
        & $bootstrapScript -RunId 'teardown-tamper' -SaveRoot $tamperRoot -StateRoot $stateRoot `
            -ArchiveRoot $tamperArchive -Teardown -Confirm:$false | Out-Null
    } catch { $refused = $true }
    if (-not $refused -or -not (Test-Path -LiteralPath (Join-Path $tamperRoot 'Manual_402_KBP_AUTOMATION_BASELINE.zks'))) {
        throw 'Teardown with a drifted sealed baseline did not refuse before deletion.'
    }
    # An unrecorded matching-name artifact is never deleted by teardown.
    # Restore the sealed baseline from its archived copy first so the pair is
    # again exactly manifest-owned before adding an unrecorded lookalike.
    Copy-Item (Join-Path $tamperArchive 'Manual_402_KBP_AUTOMATION_BASELINE.zks') `
        (Join-Path $tamperRoot 'Manual_402_KBP_AUTOMATION_BASELINE.zks') -Force
    New-TestSaveArchive -Path (Join-Path $tamperRoot 'Manual_404_KBP_AUTOMATION_BASELINE.zks') -Name 'KBP_AUTOMATION_BASELINE'
    & $bootstrapScript -RunId 'teardown-selective' -SaveRoot $tamperRoot -StateRoot $stateRoot `
        -ArchiveRoot $tamperArchive -Teardown -Confirm:$false | Out-Null
    if (-not (Test-Path -LiteralPath (Join-Path $tamperRoot 'Manual_404_KBP_AUTOMATION_BASELINE.zks')) -or
        (Test-Path -LiteralPath (Join-Path $tamperRoot 'Manual_402_KBP_AUTOMATION_BASELINE.zks'))) {
        throw 'Teardown did not delete exactly the manifest-owned files.'
    }
    $passed++

    # Missing seed refuses; nothing created.
    $emptyRoot = Join-Path $root 'saves-empty'
    New-Item -ItemType Directory -Path $emptyRoot | Out-Null
    $refused = $false
    try { & $bootstrapScript -RunId 'harness-no-seed' -SaveRoot $emptyRoot -Confirm:$false | Out-Null }
    catch { $refused = $true }
    if (-not $refused) { throw 'Bootstrap without a seed did not refuse.' }
    $passed++
}
finally {
    if (Test-Path -LiteralPath $root) {
        $resolved = [IO.Path]::GetFullPath($root)
        [void](Assert-KbpPathWithin -Path $resolved -Root (Join-Path $repo 'artifacts\runtime-harness-tests'))
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
Write-Host "Runtime harness filesystem tests: PASS=$passed FAIL=0"
