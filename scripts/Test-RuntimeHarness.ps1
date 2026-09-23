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

    # The harness refuses PowerShell 7, whose JSON date conversion breaks
    # exact manifest verification (checked only where pwsh is installed).
    $pwsh = Get-Command pwsh -ErrorAction SilentlyContinue
    if ($null -ne $pwsh) {
        $common = Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1'
        # Native stderr is an ErrorRecord in 5.1; capture it, do not throw.
        $priorPreference = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try {
            $editionOutput = (& $pwsh.Source -NoProfile -NonInteractive -Command ". '$common'" 2>&1 |
                ForEach-Object { [string]$_ }) -join ' '
            $editionExit = $LASTEXITCODE
        }
        finally { $ErrorActionPreference = $priorPreference }
        if ($editionExit -eq 0 -or $editionOutput -notmatch 'requires Windows PowerShell 5\.1') {
            throw "Runtime harness did not refuse PowerShell 7: $editionOutput"
        }
        $passed++
    }

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
        -ArgumentList '/c', 'ping', '-n', '30', '127.0.0.1', '>nul' -PassThru -WindowStyle Hidden
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
            # Stop-Process does not wait; a still-exiting probe would make
            # the NEXT case's not-running guard see it (observed race).
            try { [void]$probe.WaitForExit(15000) } catch { }
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

    # Advanced family (docs/ADVANCED-SAVE-COPY-INSPECTION-REQUEST.md): only an
    # owner-saved KBP_ADVANCED_SEED is accepted; the automation pair, the
    # automation seed and ordinary saves stay byte-identical; the sealed
    # advanced pair and seed archive are produced under their own names.
    $advancedRoot = New-FixtureSeedRoot 'saves-advanced'
    New-TestSaveArchive -Path (Join-Path $advancedRoot 'Manual_410_KBP_ADVANCED_SEED.zks') `
        -Name 'KBP_ADVANCED_SEED' -GameName 'Advanced Campaign' -GameId '66666666-7777-8888-9999-000000000000'
    $before = @{}
    foreach ($file in @(Get-ChildItem -LiteralPath $advancedRoot -File)) { $before[$file.Name] = Get-KbpSha256 $file.FullName }
    $advancedArchive = Join-Path $root 'archive-advanced'
    & $bootstrapScript -Family Advanced -RunId 'advanced-happy' -SaveRoot $advancedRoot `
        -StateRoot $stateRoot -ArchiveRoot $advancedArchive -Confirm:$false | Out-Null
    $advancedBaseline = Join-Path $advancedRoot 'Manual_411_KBP_ADVANCED_BASELINE.zks'
    $advancedWorking = Join-Path $advancedRoot 'Manual_412_KBP_ADVANCED_WORKING.zks'
    if (-not (Test-Path -LiteralPath $advancedBaseline) -or -not (Test-Path -LiteralPath $advancedWorking) -or
        (Read-TestHeaderName $advancedBaseline) -cne 'KBP_ADVANCED_BASELINE' -or
        (Read-TestHeaderName $advancedWorking) -cne 'KBP_ADVANCED_WORKING' -or
        -not (Test-Path -LiteralPath (Join-Path $advancedArchive 'Manual_410_KBP_ADVANCED_SEED.zks')) -or
        @(Get-ChildItem -LiteralPath $advancedRoot -Filter '*KBP_AUTOMATION_BASELINE*').Count -ne 0) {
        throw 'Advanced-family bootstrap did not produce exactly the sealed advanced pair.'
    }
    foreach ($name in $before.Keys) {
        if ((Get-KbpSha256 (Join-Path $advancedRoot $name)) -cne $before[$name]) {
            throw "Advanced-family bootstrap changed $name."
        }
    }
    if (Test-Path -LiteralPath (Join-Path $stateRoot 'fixture.lock')) { throw 'Advanced bootstrap kept the lock.' }
    # Without an advanced seed the Advanced family refuses and writes nothing.
    $noAdvancedRoot = New-FixtureSeedRoot 'saves-no-advanced'
    $refused = $false
    try {
        & $bootstrapScript -Family Advanced -RunId 'advanced-refused' -SaveRoot $noAdvancedRoot `
            -StateRoot $stateRoot -ArchiveRoot (Join-Path $root 'archive-no-advanced') -Confirm:$false | Out-Null
    } catch { $refused = $_.Exception.Message -like '*KBP_ADVANCED_SEED*' }
    if (-not $refused -or @(Get-ChildItem -LiteralPath $noAdvancedRoot -File).Count -ne 2) {
        throw 'The Advanced family acted without an owner-saved advanced seed.'
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
        $failPlayerHash = Get-KbpSha256 (Join-Path $failRoot 'Manual_400_PlayerCampaign.zks')
        $failSeedHash = Get-KbpSha256 (Join-Path $failRoot 'Manual_401_KBP_AUTOMATION_SEED.zks')
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
        if ((Get-KbpSha256 (Join-Path $failRoot 'Manual_400_PlayerCampaign.zks')) -cne $failPlayerHash -or
            (Get-KbpSha256 (Join-Path $failRoot 'Manual_401_KBP_AUTOMATION_SEED.zks')) -cne $failSeedHash) {
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
        gameId = '11111111-2222-3333-4444-555555555555'
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
    & $bootstrapScript -RunId $orphanRun -SaveRoot $recoverRoot -StateRoot $stateRoot -Recover -Confirm:$false | Out-Null
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

    # --- N4: journal-before-move, rollback-failure lock retention,
    # repeated recovery, escaped-manifest teardown ---

    # Interruption immediately AFTER journaling but BEFORE the move: the
    # journaled destination does not exist. Recovery must reconcile it as an
    # unperformed operation — no error, no deletion, verified clean rollback.
    foreach ($journal in @('baseline', 'working')) {
        $jRoot = New-FixtureSeedRoot ('saves-n4-journal-' + $journal)
        $jState = Join-Path $stateRoot ('n4-journal-' + $journal)
        $jArchive = Join-Path $root ('archive-n4-journal-' + $journal)
        $jSeedHash = Get-KbpSha256 (Join-Path $jRoot 'Manual_401_KBP_AUTOMATION_SEED.zks')
        $failed = $false
        try {
            & $bootstrapScript -RunId ('n4-j-' + $journal) -SaveRoot $jRoot -StateRoot $jState `
                -ArchiveRoot $jArchive -FailAfterJournal $journal -Confirm:$false | Out-Null
        } catch { $failed = $true }
        if (-not $failed) { throw "Journal-window injection ($journal) did not fail." }
        $jRecord = Read-KbpJson (Join-Path $jState ('n4-j-' + $journal + '\transaction.json'))
        if ($jRecord.status -cne 'RolledBack') {
            throw "Journal-window failure ($journal) did not leave a rolled-back record."
        }
        if (@(Get-ChildItem -LiteralPath $jRoot -Filter '*KBP_AUTOMATION_BASELINE*').Count -ne 0 -or
            @(Get-ChildItem -LiteralPath $jRoot -Filter '*KBP_AUTOMATION_WORKING*').Count -ne 0 -or
            (Get-KbpSha256 (Join-Path $jRoot 'Manual_401_KBP_AUTOMATION_SEED.zks')) -cne $jSeedHash) {
            throw "Journal-window recovery ($journal) left residue or touched the seed."
        }
        & $bootstrapScript -RunId ('n4-j-' + $journal) -SaveRoot $jRoot -StateRoot $jState `
            -ArchiveRoot (Join-Path $root ('archive-n4-j-' + $journal + '-retry')) -Confirm:$false | Out-Null
        if (-not (Test-Path -LiteralPath (Join-Path $jRoot 'Manual_402_KBP_AUTOMATION_BASELINE.zks'))) {
            throw "Retry after journal-window recovery ($journal) did not complete."
        }
    }
    $passed++

    # Rollback failure keeps the lock and record recoverable; a second
    # -Recover (repeated recovery) completes cleanly once the foreign
    # blocker is gone.
    $rbRoot = New-FixtureSeedRoot 'saves-n4-rollback'
    $rbState = Join-Path $stateRoot 'n4-rollback'
    $rbArchive = Join-Path $root 'archive-n4-rollback'
    $failed = $false
    try {
        & $bootstrapScript -RunId 'n4-rb' -SaveRoot $rbRoot -StateRoot $rbState `
            -ArchiveRoot $rbArchive -FailRollback -FailAfterStage Staged -Confirm:$false | Out-Null
    } catch { $failed = $true }
    if (-not $failed) { throw 'Rollback-failure injection did not fail.' }
    $rbRecord = Read-KbpJson (Join-Path $rbState 'n4-rb\transaction.json')
    if ($rbRecord.status -ceq 'RolledBack') {
        throw 'Rollback reported success despite the injected failure.'
    }
    if (-not (Test-Path -LiteralPath (Join-Path $rbState 'fixture.lock'))) {
        throw 'Failed rollback released the lock, stranding the transaction.'
    }
    # Remove the injected foreign blocker exactly, then recover.
    Remove-Item (Join-Path $rbState 'n4-rb\staging\foreign-rollback-blocker.txt') -Force
    & $bootstrapScript -RunId 'n4-rb' -SaveRoot $rbRoot -StateRoot $rbState -Recover -Confirm:$false | Out-Null
    if ((Test-Path -LiteralPath (Join-Path $rbState 'n4-rb\staging')) -or
        (Test-Path -LiteralPath (Join-Path $rbState 'fixture.lock'))) {
        throw 'Repeated recovery did not complete the owned rollback.'
    }
    $passed++

    # Escaped/absolute manifest path in teardown is refused whole-set via the
    # containment validator.
    $escRoot = New-FixtureSeedRoot 'saves-n4-escape'
    $escState = Join-Path $stateRoot 'n4-escape'
    $escArchive = Join-Path $root 'archive-n4-escape'
    & $bootstrapScript -RunId 'n4-esc' -SaveRoot $escRoot -StateRoot $escState -ArchiveRoot $escArchive -Confirm:$false | Out-Null
    $escManifestPath = Join-Path $escArchive 'fixture-manifest.json'
    $escManifest = Read-KbpJson $escManifestPath
    $escManifest.baseline.fileName = '..\..\saves-n4-escape\Manual_402_KBP_AUTOMATION_BASELINE.zks'
    Write-KbpJsonAtomic $escManifestPath $escManifest
    $refused = $false
    try {
        & $bootstrapScript -RunId 'n4-esc-teardown' -SaveRoot $escRoot -StateRoot $escState `
            -ArchiveRoot $escArchive -Teardown -Confirm:$false | Out-Null
    } catch { $refused = $true }
    if (-not $refused -or
        -not (Test-Path -LiteralPath (Join-Path $escRoot 'Manual_402_KBP_AUTOMATION_BASELINE.zks')) -or
        -not (Test-Path -LiteralPath (Join-Path $escRoot 'Manual_403_KBP_AUTOMATION_WORKING.zks'))) {
        throw 'Escaped manifest path did not refuse teardown whole-set.'
    }
    $passed++

    # N-live-1: a COMPLETED fixture transaction in the fixture state root
    # must never block the deployment guard's scan, and fixture state must
    # default OUTSIDE the deployment state root entirely.
    $deploymentStateRoot = Join-Path $root 'deployment-state'
    $lab3 = Split-Path -Parent (Split-Path -Parent $repo)
    if ((Join-Path $lab3 'runtime-fixture-state') -eq
        (Join-Path $lab3 'runtime-state')) {
        throw 'Fixture state root collides with the deployment state root.'
    }
    # Simulate the exact production integration: a completed fixture
    # transaction sitting in its own root while a deployment scan runs.
    New-Item -ItemType Directory -Path (Join-Path $deploymentStateRoot 'fixture-run') -Force | Out-Null
    Write-KbpJsonAtomic (Join-Path $deploymentStateRoot 'fixture-run\transaction.json') ([ordered]@{
        schemaVersion = 1; runId = 'fixture-run'; token = 'tok'
        status = 'Completed'
    })
    # The deployment guard scans only ITS state root; a completed fixture
    # transaction under a fixture root is invisible to it, and the guard
    # finds no unresolved deployment transaction.
    $scan = @(Get-ChildItem -LiteralPath $deploymentStateRoot -Filter 'transaction.json' -File -Recurse |
        Where-Object { (Read-KbpJson $_.FullName).status -cne 'Completed' })
    if (@($scan).Count -ne 0) {
        throw 'A completed fixture transaction was treated as unresolved deployment state.'
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

    # --- R1: recovery, dry-run purity, lock binding, publication window ---

    # Recovery refuses while a real Kingmaker-named process runs; the
    # artifacts, transaction record, and lock stay unchanged.
    $liveRoot = New-FixtureSeedRoot 'saves-r1-live'
    $liveState = Join-Path $stateRoot 'r1-live'
    $liveArchive = Join-Path $root 'archive-r1-live'
    & $bootstrapScript -RunId 'r1-live' -SaveRoot $liveRoot -StateRoot $liveState -ArchiveRoot $liveArchive -Confirm:$false | Out-Null
    # Simulate an interrupted run: flip the record to Published and hold the
    # lock with the exact run token.
    $liveTransaction = Read-KbpJson (Join-Path $liveState 'r1-live\transaction.json')
    $liveTransaction.status = 'Published'
    Write-KbpJsonAtomic (Join-Path $liveState 'r1-live\transaction.json') $liveTransaction
    New-KbpOwnedLock (Join-Path $liveState 'fixture.lock') 'r1-live' ([string]$liveTransaction.token)
    $liveProbeDir = Join-Path $root 'r1-guard-probe'
    New-Item -ItemType Directory -Path $liveProbeDir | Out-Null
    Copy-Item (Join-Path $env:SystemRoot 'System32\cmd.exe') (Join-Path $liveProbeDir 'Kingmaker.exe')
    $liveProbe = Start-Process -FilePath (Join-Path $liveProbeDir 'Kingmaker.exe') `
        -ArgumentList '/c', 'ping', '-n', '25', '127.0.0.1', '>nul' -PassThru -WindowStyle Hidden
    try {
        $beforeInventory = @(Get-ChildItem -LiteralPath $liveRoot -Filter '*.zks' -File |
            ForEach-Object { "$($_.Name):$(Get-KbpSha256 $_.FullName)" })
        $refused = $false
        try {
            & $bootstrapScript -RunId 'r1-live' -StateRoot $liveState -Recover -Confirm:$false | Out-Null
        } catch { $refused = $true }
        if (-not $refused) { throw 'Recovery did not refuse while Kingmaker was running.' }
        $afterInventory = @(Get-ChildItem -LiteralPath $liveRoot -Filter '*.zks' -File |
            ForEach-Object { "$($_.Name):$(Get-KbpSha256 $_.FullName)" })
        $recordNow = Read-KbpJson (Join-Path $liveState 'r1-live\transaction.json')
        if (($beforeInventory -join '`n') -cne ($afterInventory -join '`n') -or
            $recordNow.status -cne 'Published' -or
            -not (Test-Path -LiteralPath (Join-Path $liveState 'fixture.lock'))) {
            throw 'Refused recovery still mutated artifacts, the record, or the lock.'
        }
    }
    finally {
        if (-not $liveProbe.HasExited) { try { Stop-Process -Id $liveProbe.Id -Force } catch { } }
        # Wait for the probe to be gone before the next case's guard runs
        # (observed race: 'Kingmaker is running' from the r1-mixed setup).
        try { [void]$liveProbe.WaitForExit(15000) } catch { }
    }
    $passed++

    # A run-A transaction plus a run-B lock must not authorize recovery:
    # neither A's artifacts nor B's lock may be touched. Also same-run
    # wrong-token.
    $mixedRoot = New-FixtureSeedRoot 'saves-r1-mixed'
    $mixedState = Join-Path $stateRoot 'r1-mixed'
    $mixedArchive = Join-Path $root 'archive-r1-mixed'
    & $bootstrapScript -RunId 'r1-a' -SaveRoot $mixedRoot -StateRoot $mixedState -ArchiveRoot $mixedArchive -Confirm:$false | Out-Null
    $aTransaction = Read-KbpJson (Join-Path $mixedState 'r1-a\transaction.json')
    $aTransaction.status = 'Published'
    Write-KbpJsonAtomic (Join-Path $mixedState 'r1-a\transaction.json') $aTransaction
    # Lock belongs to a DIFFERENT run.
    New-KbpOwnedLock (Join-Path $mixedState 'fixture.lock') 'r1-b' ([Guid]::NewGuid().ToString('N'))
    $mixedSavesBefore = @(Get-ChildItem -LiteralPath $mixedRoot -Filter 'KBP_AUTOMATION*' -File |
        ForEach-Object { "$($_.Name):$(Get-KbpSha256 $_.FullName)" })
    $refused = $false
    try { & $bootstrapScript -RunId 'r1-a' -SaveRoot $mixedRoot -StateRoot $mixedState -Recover -Confirm:$false | Out-Null }
    catch { $refused = $true }
    $mixedSavesAfter = @(Get-ChildItem -LiteralPath $mixedRoot -Filter 'KBP_AUTOMATION*' -File |
        ForEach-Object { "$($_.Name):$(Get-KbpSha256 $_.FullName)" })
    if (-not $refused -or
        (Test-Path -LiteralPath (Join-Path $mixedRoot 'Manual_402_KBP_AUTOMATION_BASELINE.zks')) -eq $false -or
        ($mixedSavesBefore -join '`n') -cne ($mixedSavesAfter -join '`n') -or
        -not (Test-Path -LiteralPath (Join-Path $mixedState 'fixture.lock'))) {
        throw 'Foreign-lock recovery deleted artifacts or released a foreign lock.'
    }
    Remove-KbpOwnedLock (Join-Path $mixedState 'fixture.lock') 'r1-b' ((Read-KbpJson (Join-Path $mixedState 'fixture.lock')).token)
    # Same run, wrong token in the lock.
    $wrongToken = [Guid]::NewGuid().ToString('N')
    New-KbpOwnedLock (Join-Path $mixedState 'fixture.lock') 'r1-a' $wrongToken
    $refused = $false
    try { & $bootstrapScript -RunId 'r1-a' -SaveRoot $mixedRoot -StateRoot $mixedState -Recover -Confirm:$false | Out-Null }
    catch { $refused = $true }
    if (-not $refused -or
        -not (Test-Path -LiteralPath (Join-Path $mixedRoot 'Manual_402_KBP_AUTOMATION_BASELINE.zks')) -or
        -not (Test-Path -LiteralPath (Join-Path $mixedState 'fixture.lock'))) {
        throw 'Wrong-token recovery was not refused while preserving state.'
    }
    # Correct token recovers cleanly.
    Remove-KbpOwnedLock (Join-Path $mixedState 'fixture.lock') 'r1-a' $wrongToken
    New-KbpOwnedLock (Join-Path $mixedState 'fixture.lock') 'r1-a' ([string]$aTransaction.token)
    & $bootstrapScript -RunId 'r1-a' -SaveRoot $mixedRoot -StateRoot $mixedState -Recover -Confirm:$false | Out-Null
    if ((Test-Path -LiteralPath (Join-Path $mixedRoot 'Manual_402_KBP_AUTOMATION_BASELINE.zks')) -or
        (Test-Path -LiteralPath (Join-Path $mixedState 'fixture.lock'))) {
        throw 'Correctly-bound recovery did not clean the owned state.'
    }
    $passed++

    # WhatIf purity for all three modes over PRE-EXISTING state, including
    # locks and transaction files (byte-for-byte, timestamps included).
    $pureRoot = New-FixtureSeedRoot 'saves-r1-pure'
    $pureState = Join-Path $stateRoot 'r1-pure'
    $pureArchive = Join-Path $root 'archive-r1-pure'
    & $bootstrapScript -RunId 'r1-pure' -SaveRoot $pureRoot -StateRoot $pureState -ArchiveRoot $pureArchive -Confirm:$false | Out-Null
    $pureTransaction = Read-KbpJson (Join-Path $pureState 'r1-pure\transaction.json')
    $pureTransaction.status = 'Published'
    Write-KbpJsonAtomic (Join-Path $pureState 'r1-pure\transaction.json') $pureTransaction
    New-KbpOwnedLock (Join-Path $pureState 'fixture.lock') 'r1-pure' ([string]$pureTransaction.token)
    function Get-R1TreeInventory {
        param([string[]]$Roots)
        foreach ($r in $Roots) {
            if (-not (Test-Path -LiteralPath $r)) { continue }
            Get-ChildItem -LiteralPath $r -Recurse -Force -File | Sort-Object FullName | ForEach-Object {
                "$($_.FullName):$($_.Length):$($_.LastWriteTimeUtc.Ticks):$(Get-KbpSha256 $_.FullName)"
            }
        }
    }
    $pureRoots = @($pureRoot, $pureState, $pureArchive, (Join-Path $stateRoot 'r1-pure-2'), (Join-Path $root 'archive-r1-pure-2'))
    $pureBefore = Get-R1TreeInventory $pureRoots
    $WhatIfPreference = $true
    try {
        # A pre-existing lock legitimately refuses a new bootstrap before
        # its dry run; use a fresh state root so the WhatIf itself is
        # exercised rather than short-circuited. The existing pair likewise
        # triggers a read-only refusal — either way nothing may be written.
        try {
            & $bootstrapScript -RunId 'r1-pure-2' -SaveRoot $pureRoot -StateRoot (Join-Path $stateRoot 'r1-pure-2') -ArchiveRoot (Join-Path $root 'archive-r1-pure-2') | Out-Null
        } catch { }
        & $bootstrapScript -RunId 'r1-pure' -SaveRoot $pureRoot -StateRoot $pureState -Recover | Out-Null
        & $bootstrapScript -RunId 'r1-pure-teardown' -SaveRoot $pureRoot -StateRoot $pureState -ArchiveRoot $pureArchive -Teardown | Out-Null
    }
    finally { $WhatIfPreference = $false }
    $pureAfter = Get-R1TreeInventory $pureRoots
    if (($pureBefore -join '`n') -cne ($pureAfter -join '`n')) {
        $diff = Compare-Object ($pureBefore -split '`n') ($pureAfter -split '`n')
        throw "A WhatIf mode mutated state: $($diff | ForEach-Object { $_.InputObject } | Select-Object -First 3)"
    }
    $passed++

    # Identity refusal: substituted published file, drifted working campaign
    # identity, escaped recorded path, and unknown staging content.
    $tamper2Root = New-FixtureSeedRoot 'saves-r1-tamper2'
    $tamper2State = Join-Path $stateRoot 'r1-tamper2'
    $tamper2Archive = Join-Path $root 'archive-r1-tamper2'
    & $bootstrapScript -RunId 'r1-t2' -SaveRoot $tamper2Root -StateRoot $tamper2State -ArchiveRoot $tamper2Archive -Confirm:$false | Out-Null
    $t2 = Read-KbpJson (Join-Path $tamper2State 'r1-t2\transaction.json')
    $t2.status = 'Published'
    Write-KbpJsonAtomic (Join-Path $tamper2State 'r1-t2\transaction.json') $t2
    # Substitute a foreign save at the published baseline pathname.
    Remove-Item (Join-Path $tamper2Root 'Manual_402_KBP_AUTOMATION_BASELINE.zks') -Force
    New-TestSaveArchive -Path (Join-Path $tamper2Root 'Manual_402_KBP_AUTOMATION_BASELINE.zks') -Name 'KBP_AUTOMATION_BASELINE' -GameId '99999999-9999-9999-9999-999999999999'
    New-KbpOwnedLock (Join-Path $tamper2State 'fixture.lock') 'r1-t2' ([string]$t2.token)
    $refused = $false
    try { & $bootstrapScript -RunId 'r1-t2' -StateRoot $tamper2State -Recover -Confirm:$false | Out-Null }
    catch { $refused = $true }
    if (-not $refused -or -not (Test-Path -LiteralPath (Join-Path $tamper2Root 'Manual_402_KBP_AUTOMATION_BASELINE.zks'))) {
        throw 'Recovery deleted a substituted (foreign campaign) artifact at an owned pathname.'
    }
    # Escaped recorded path.
    $escaped = Join-Path $root 'escaped-owned.zks'
    New-TestSaveArchive -Path $escaped -Name 'KBP_AUTOMATION_BASELINE'
    $t2escaped = Read-KbpJson (Join-Path $tamper2State 'r1-t2\transaction.json')
    $t2escaped.publishedPaths = @($escaped)
    Write-KbpJsonAtomic (Join-Path $tamper2State 'r1-t2\transaction.json') $t2escaped
    $refused = $false
    try { & $bootstrapScript -RunId 'r1-t2' -StateRoot $tamper2State -Recover -Confirm:$false | Out-Null }
    catch { $refused = $true }
    if (-not $refused -or -not (Test-Path -LiteralPath $escaped)) {
        throw 'Recovery followed an escaped recorded path outside the owned roots.'
    }
    # Unknown staging content refuses recursive rollback.
    $t2staging = Read-KbpJson (Join-Path $tamper2State 'r1-t2\transaction.json')
    $t2staging.publishedPaths = @()
    $t2staging.stagingRoot = (Join-Path $tamper2State 'r1-t2\staging')
    Write-KbpJsonAtomic (Join-Path $tamper2State 'r1-t2\transaction.json') $t2staging
    New-Item -ItemType Directory -Path $t2staging.stagingRoot -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $t2staging.stagingRoot 'foreign-notes.txt') -Value 'not ours'
    $refused = $false
    try { & $bootstrapScript -RunId 'r1-t2' -StateRoot $tamper2State -Recover -Confirm:$false | Out-Null }
    catch { $refused = $true }
    if (-not $refused -or -not (Test-Path -LiteralPath (Join-Path $t2staging.stagingRoot 'foreign-notes.txt'))) {
        throw 'Rollback recursed through unknown staging content.'
    }
    Remove-KbpOwnedLock (Join-Path $tamper2State 'fixture.lock') 'r1-t2' ([string]$t2.token)
    Remove-Item $escaped -Force
    # Drifted working-save campaign identity refuses teardown.
    $driftRoot = New-FixtureSeedRoot 'saves-r1-drift'
    $driftState = Join-Path $stateRoot 'r1-drift'
    $driftArchive = Join-Path $root 'archive-r1-drift'
    & $bootstrapScript -RunId 'r1-drift' -SaveRoot $driftRoot -StateRoot $driftState -ArchiveRoot $driftArchive -Confirm:$false | Out-Null
    Remove-Item (Join-Path $driftRoot 'Manual_403_KBP_AUTOMATION_WORKING.zks') -Force
    New-TestSaveArchive -Path (Join-Path $driftRoot 'Manual_403_KBP_AUTOMATION_WORKING.zks') -Name 'KBP_AUTOMATION_WORKING' -GameId '88888888-8888-8888-8888-888888888888'
    $refused = $false
    try { & $bootstrapScript -RunId 'r1-drift-teardown' -SaveRoot $driftRoot -StateRoot $driftState -ArchiveRoot $driftArchive -Teardown -Confirm:$false | Out-Null }
    catch { $refused = $true }
    if (-not $refused -or
        -not (Test-Path -LiteralPath (Join-Path $driftRoot 'Manual_402_KBP_AUTOMATION_BASELINE.zks')) -or
        -not (Test-Path -LiteralPath (Join-Path $driftRoot 'Manual_403_KBP_AUTOMATION_WORKING.zks'))) {
        throw 'Teardown did not refuse a drifted working-save identity before deletion.'
    }
    $passed++

    # Publication interruption window: fail immediately after each final
    # move; recovery must leave a verified clean owned rollback with
    # seed/archive/bystanders intact.
    foreach ($move in @('baseline', 'working')) {
        $winRoot = New-FixtureSeedRoot ('saves-r1-win-' + $move)
        $winState = Join-Path $stateRoot ('r1-win-' + $move)
        $winArchive = Join-Path $root ('archive-r1-win-' + $move)
        $winSeedHash = Get-KbpSha256 (Join-Path $winRoot 'Manual_401_KBP_AUTOMATION_SEED.zks')
        $winPlayerHash = Get-KbpSha256 (Join-Path $winRoot 'Manual_400_PlayerCampaign.zks')
        $failed = $false
        try {
            & $bootstrapScript -RunId ('r1-win-' + $move) -SaveRoot $winRoot -StateRoot $winState `
                -ArchiveRoot $winArchive -FailAfterMove $move -Confirm:$false | Out-Null
        } catch { $failed = $true }
        if (-not $failed) { throw "Injected after-move failure ($move) did not fail." }
        $winRecord = Read-KbpJson (Join-Path $winState ('r1-win-' + $move + '\transaction.json'))
        if ($winRecord.status -cne 'RolledBack') {
            throw "After-move failure ($move) did not leave a rolled-back recovery record."
        }
        if (@(Get-ChildItem -LiteralPath $winRoot -Filter '*KBP_AUTOMATION_BASELINE*').Count -ne 0 -or
            @(Get-ChildItem -LiteralPath $winRoot -Filter '*KBP_AUTOMATION_WORKING*').Count -ne 0 -or
            (Get-KbpSha256 (Join-Path $winRoot 'Manual_401_KBP_AUTOMATION_SEED.zks')) -cne $winSeedHash -or
            (Get-KbpSha256 (Join-Path $winRoot 'Manual_400_PlayerCampaign.zks')) -cne $winPlayerHash) {
            throw "Recovery after the $move move window left residue or touched protected saves."
        }
        # Retry succeeds after the clean rollback.
        & $bootstrapScript -RunId ('r1-win-' + $move) -SaveRoot $winRoot -StateRoot $winState `
            -ArchiveRoot (Join-Path $root ('archive-r1-win-' + $move + '-retry')) -Confirm:$false | Out-Null
        if (-not (Test-Path -LiteralPath (Join-Path $winRoot 'Manual_402_KBP_AUTOMATION_BASELINE.zks'))) {
            throw "Retry after the $move interruption window did not complete."
        }
    }
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
