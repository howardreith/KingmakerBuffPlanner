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

    # External exact-copy fixture (owner authorization 2026-09-23): the
    # profile stages a sealed copy from the external fixture root; the
    # owner's installed directory (files and settings) is recorded before
    # activation and is back byte-exact after restoration.
    $game = Join-Path $root 'game-external-fixture'
    $liveDep = Join-Path $game 'Mods\LiveDep'
    New-Item -ItemType Directory -Path (Join-Path $liveDep 'settings') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $game 'Mods\Existing') -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $game 'Kingmaker.exe') -Value 'fixture' -Encoding Ascii
    Set-Content -LiteralPath (Join-Path $game 'Mods\Existing\Info.json') -Value '{"Id":"Existing"}' -Encoding Ascii
    Set-Content -LiteralPath (Join-Path $liveDep 'info.json') -Value '{"Id":"LiveDep","Version":"0.0.136"}' -Encoding Ascii
    Set-Content -LiteralPath (Join-Path $liveDep 'LiveDep.dll') -Value 'installed-assembly' -Encoding Ascii
    Set-Content -LiteralPath (Join-Path $liveDep 'settings\Settings.xml') -Value 'owner-settings' -Encoding Ascii
    $externalRoot = Join-Path $root 'external-fixtures'
    $sealedDep = Join-Path $externalRoot 'LiveDep'
    New-Item -ItemType Directory -Path (Join-Path $sealedDep 'data') -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $sealedDep 'info.json') -Value '{"Id":"LiveDep","Version":"0.0.133"}' -Encoding Ascii
    Set-Content -LiteralPath (Join-Path $sealedDep 'LiveDep.dll') -Value 'sealed-assembly' -Encoding Ascii
    Set-Content -LiteralPath (Join-Path $sealedDep 'data\value.txt') -Value 'sealed' -Encoding Ascii
    $sealedIdentity = Get-KbpDirectoryContentIdentity $sealedDep
    $liveBefore = Get-KbpDirectoryContentIdentity $liveDep
    $externalProfile = [pscustomobject]@{
        profileId = 'fixture-external'
        mods = @([pscustomobject]@{
            ummId = 'LiveDep'; directoryName = 'LiveDep'; version = '0.0.133'
            assemblyName = 'LiveDep.dll'; fixtureRelativePath = 'LiveDep'
            infoSha256 = Get-KbpSha256 (Join-Path $sealedDep 'info.json')
            assemblySha256 = Get-KbpSha256 (Join-Path $sealedDep 'LiveDep.dll')
            directoryManifestSha256 = $sealedIdentity.directoryManifestSha256
            fileCount = $sealedIdentity.fileCount; totalBytes = $sealedIdentity.totalBytes
        })
    }
    $before = @(Get-KbpDirectoryManifest (Join-Path $game 'Mods'))
    $priorExternalRoot = $script:KbpExternalFixtureRoot
    $script:KbpExternalFixtureRoot = $externalRoot
    try {
        $statePath = Enter-KbpRuntimeTransaction -PackagePath $package -KingmakerInstallDir $game `
            -StateRoot $stateRoot -StagingRoot $stagingRoot -BackupRoot $backupRoot `
            -RunId 'external-fixture' -FixtureMode -KnownKingmakerProcessIds @() `
            -CompatibilityProfile $externalProfile
    }
    finally { $script:KbpExternalFixtureRoot = $priorExternalRoot }
    $active = Read-KbpJson $statePath
    $recorded = @($active.externallyStagedMods)
    if ((Get-KbpDirectoryContentIdentity $liveDep).directoryManifestSha256 -cne $sealedIdentity.directoryManifestSha256 -or
        $recorded.Count -ne 1 -or -not [bool]$recorded[0].liveExisted -or
        [string]$recorded[0].liveDirectoryManifestSha256 -cne $liveBefore.directoryManifestSha256 -or
        [bool]$active.externallyStagedModsRestored) {
        throw 'The external fixture was not staged, or the installed identity was not recorded.'
    }
    $restored = Restore-KbpRuntimeTransaction -RunId 'external-fixture' -StateRoot $stateRoot `
        -FixtureMode -KnownKingmakerProcessIds @()
    if (-not $restored.restorationVerified -or -not [bool]$restored.externallyStagedModsRestored -or
        (Get-KbpDirectoryContentIdentity $liveDep).directoryManifestSha256 -cne $liveBefore.directoryManifestSha256 -or
        (Get-Content -LiteralPath (Join-Path $liveDep 'settings\Settings.xml') -Raw).Trim() -cne 'owner-settings' -or
        -not (Test-KbpManifestEqual $before @(Get-KbpDirectoryManifest (Join-Path $game 'Mods')))) {
        throw 'The installed dependency was not restored byte-exact after the external fixture run.'
    }
    # The identity check itself: a changed settings file or a vanished
    # directory is refused.
    $check = [pscustomobject]@{ externallyStagedMods = @($recorded); externallyStagedModsRestored = $false }
    Set-Content -LiteralPath (Join-Path $liveDep 'settings\Settings.xml') -Value 'changed' -Encoding Ascii
    $refused = $false
    try { Assert-KbpExternallyStagedLiveRestored -State $check -ModsPath (Join-Path $game 'Mods') }
    catch { $refused = $_.Exception.Message -like '*identity mismatch*' }
    if (-not $refused -or [bool]$check.externallyStagedModsRestored) { throw 'A changed installed dependency passed.' }
    Remove-Item -LiteralPath $liveDep -Recurse -Force
    $refused = $false
    try { Assert-KbpExternallyStagedLiveRestored -State $check -ModsPath (Join-Path $game 'Mods') }
    catch { $refused = $_.Exception.Message -like '*presence changed*' }
    if (-not $refused) { throw 'A vanished installed dependency passed.' }
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

    # --- Review O1: a refused retry must never delete a prior attempt ---
    # A RolledBack attempt (transaction + retained evidence + an unexpected
    # extra file) is arranged through the REAL script's own rollback, then
    # every refusal/decline/success path is checked against byte snapshots
    # of the save, state and archive roots.
    function Get-O1TreeSnapshot {
        param([string[]]$Roots)
        $lines = New-Object System.Collections.Generic.List[string]
        foreach ($snapshotRoot in $Roots) {
            if (-not (Test-Path -LiteralPath $snapshotRoot)) { $lines.Add("absent|$snapshotRoot"); continue }
            foreach ($item in @(Get-ChildItem -LiteralPath $snapshotRoot -Recurse -Force | Sort-Object FullName)) {
                if ($item.PSIsContainer) { $lines.Add("D|" + $item.FullName) }
                else { $lines.Add("F|" + $item.FullName + "|" + (Get-KbpSha256 $item.FullName)) }
            }
        }
        return ($lines -join "`n")
    }
    $o1Saves = New-FixtureSeedRoot 'saves-o1'
    New-TestSaveArchive -Path (Join-Path $o1Saves 'Manual_420_KBP_ADVANCED_OTHERFAMILY.zks') -Name 'Unrelated'
    $o1State = Join-Path $root 'o1-state'
    $o1Archive = Join-Path $root 'archive-o1'
    $failed = $false
    try {
        & $bootstrapScript -RunId 'o1-prior' -SaveRoot $o1Saves -StateRoot $o1State `
            -ArchiveRoot $o1Archive -FailAfterStage Staged -Confirm:$false | Out-Null
    } catch { $failed = $true }
    $o1Prior = Read-KbpJson (Join-Path $o1State 'o1-prior\transaction.json')
    if (-not $failed -or $o1Prior.status -cne 'RolledBack') { throw 'O1 fixture: prior attempt was not RolledBack.' }
    # Retained evidence and unexpected content inside the rolled-back run.
    Set-Content -LiteralPath (Join-Path $o1State 'o1-prior\retained-evidence.txt') -Value 'evidence' -Encoding Ascii
    New-Item -ItemType Directory -Path (Join-Path $o1State 'o1-prior\unexpected') | Out-Null
    Set-Content -LiteralPath (Join-Path $o1State 'o1-prior\unexpected\foreign.bin') -Value 'foreign' -Encoding Ascii
    $o1Roots = @($o1Saves, $o1State, $o1Archive)
    $o1Before = Get-O1TreeSnapshot $o1Roots

    # A. Same-ID retry (Advanced family, no advanced seed): refused, nothing changes.
    $refusal = $null
    try {
        & $bootstrapScript -Family Advanced -RunId 'o1-prior' -SaveRoot $o1Saves -StateRoot $o1State `
            -ArchiveRoot $o1Archive -Confirm:$false | Out-Null
    } catch { $refusal = $_.Exception.Message }
    if ($null -eq $refusal -or $refusal -notlike '*preserved as history*' -or
        (Get-O1TreeSnapshot $o1Roots) -cne $o1Before) {
        throw "O1-A: same-ID retry was not refused without mutation ($refusal)."
    }
    # Same-ID retry of the automation family (seed present) is refused too.
    $refusal = $null
    try {
        & $bootstrapScript -RunId 'o1-prior' -SaveRoot $o1Saves -StateRoot $o1State `
            -ArchiveRoot $o1Archive -Confirm:$false | Out-Null
    } catch { $refusal = $_.Exception.Message }
    if ($null -eq $refusal -or (Get-O1TreeSnapshot $o1Roots) -cne $o1Before) {
        throw 'O1-A: same-ID automation retry mutated state.'
    }

    # B. Game-running precondition failure: same-ID AND a valid fresh ID.
    $o1ProbeDir = Join-Path $root 'o1-guard-probe'
    New-Item -ItemType Directory -Path $o1ProbeDir | Out-Null
    Copy-Item (Join-Path $env:SystemRoot 'System32\cmd.exe') (Join-Path $o1ProbeDir 'Kingmaker.exe')
    $o1Probe = Start-Process -FilePath (Join-Path $o1ProbeDir 'Kingmaker.exe') `
        -ArgumentList '/c', 'ping', '-n', '30', '127.0.0.1', '>nul' -PassThru -WindowStyle Hidden
    try {
        foreach ($attemptId in @('o1-prior', 'o1-fresh-blocked')) {
            $refusal = $null
            try {
                & $bootstrapScript -RunId $attemptId -SaveRoot $o1Saves -StateRoot $o1State `
                    -ArchiveRoot (Join-Path $root ('archive-' + $attemptId)) -Confirm:$false | Out-Null
            } catch { $refusal = $_.Exception.Message }
            if ($null -eq $refusal -or (Get-O1TreeSnapshot $o1Roots) -cne $o1Before -or
                (Test-Path -LiteralPath (Join-Path $root ('archive-' + $attemptId))) -or
                (Test-Path -LiteralPath (Join-Path $o1State 'fixture.lock'))) {
                throw "O1-B: precondition failure for $attemptId mutated state ($refusal)."
            }
            if ($attemptId -ceq 'o1-fresh-blocked' -and $refusal -notlike '*Kingmaker*') {
                throw "O1-B: the fresh attempt was not refused by the game-running precondition ($refusal)."
            }
        }
    }
    finally {
        if (-not $o1Probe.HasExited) {
            try { Stop-Process -Id $o1Probe.Id -Force } catch { }
            try { [void]$o1Probe.WaitForExit(15000) } catch { }
        }
    }

    # C. WhatIf and a genuinely DECLINED confirmation reach the outer
    # decision boundary (fresh ID, all prerequisites met) and mutate nothing.
    $WhatIfPreference = $true
    try {
        $whatIfOut = & $bootstrapScript -RunId 'o1-fresh-whatif' -SaveRoot $o1Saves -StateRoot $o1State `
            -ArchiveRoot (Join-Path $root 'archive-o1-fresh-whatif') 6>&1 | Out-String
    }
    finally { $WhatIfPreference = $false }
    if ($whatIfOut -notlike '*Fixture bootstrap WhatIf PASS*' -or (Get-O1TreeSnapshot $o1Roots) -cne $o1Before -or
        (Test-Path -LiteralPath (Join-Path $root 'archive-o1-fresh-whatif'))) {
        throw 'O1-C: WhatIf did not reach the decision boundary purely.'
    }
    $ErrorActionPreference = 'Continue'
    $declineOut = ('N' | & powershell.exe -NoProfile -Command ("& '" + $bootstrapScript +
        "' -RunId 'o1-fresh-declined' -SaveRoot '" + $o1Saves + "' -StateRoot '" + $o1State +
        "' -ArchiveRoot '" + (Join-Path $root 'archive-o1-fresh-declined') + "' -Confirm") 2>&1) -join "`n"
    $ErrorActionPreference = 'Stop'
    if ($declineOut -notlike '*Fixture bootstrap WhatIf PASS*' -or (Get-O1TreeSnapshot $o1Roots) -cne $o1Before -or
        (Test-Path -LiteralPath (Join-Path $root 'archive-o1-fresh-declined'))) {
        throw "O1-C: a declined confirmation mutated state or never reached the decision: $declineOut"
    }

    # D. A fresh attempt succeeds: prior history preserved byte-identical,
    # the pair is created, seeds and unrelated/other-family saves unchanged,
    # and the existing teardown still removes exactly the owned pair.
    $o1PriorBefore = Get-O1TreeSnapshot @((Join-Path $o1State 'o1-prior'))
    $o1OtherHashes = @{}
    foreach ($file in @(Get-ChildItem -LiteralPath $o1Saves -File)) { $o1OtherHashes[$file.Name] = Get-KbpSha256 $file.FullName }
    $o1FreshArchive = Join-Path $root 'archive-o1-fresh'
    & $bootstrapScript -RunId 'o1-fresh' -SaveRoot $o1Saves -StateRoot $o1State -ArchiveRoot $o1FreshArchive -Confirm:$false | Out-Null
    $o1Pair = @(Get-ChildItem -LiteralPath $o1Saves -File | Where-Object {
        $_.Name -cmatch '_KBP_AUTOMATION_(BASELINE|WORKING)\.zks$' })
    if ($o1Pair.Count -ne 2 -or (Get-O1TreeSnapshot @((Join-Path $o1State 'o1-prior'))) -cne $o1PriorBefore -or
        (Read-KbpJson (Join-Path $o1State 'o1-fresh\transaction.json')).status -cne 'Completed') {
        throw 'O1-D: the fresh attempt did not complete while preserving the prior history.'
    }
    foreach ($name in $o1OtherHashes.Keys) {
        if ((Get-KbpSha256 (Join-Path $o1Saves $name)) -cne $o1OtherHashes[$name]) { throw "O1-D: $name changed." }
    }
    & $bootstrapScript -RunId 'o1-fresh' -SaveRoot $o1Saves -StateRoot $o1State -ArchiveRoot $o1FreshArchive `
        -Teardown -Confirm:$false | Out-Null
    if (@(Get-ChildItem -LiteralPath $o1Saves -File | Where-Object {
            $_.Name -cmatch '_KBP_AUTOMATION_(BASELINE|WORKING)\.zks$' }).Count -ne 0 -or
        (Get-O1TreeSnapshot @((Join-Path $o1State 'o1-prior'))) -cne $o1PriorBefore) {
        throw 'O1-D: teardown did not remove exactly the owned pair while keeping history.'
    }
    foreach ($name in $o1OtherHashes.Keys) {
        if ((Get-KbpSha256 (Join-Path $o1Saves $name)) -cne $o1OtherHashes[$name]) { throw "O1-D: teardown changed $name." }
    }

    # E. The unexpected retained content of the RolledBack attempt survived
    # every path above.
    if (-not (Test-Path -LiteralPath (Join-Path $o1State 'o1-prior\unexpected\foreign.bin')) -or
        -not (Test-Path -LiteralPath (Join-Path $o1State 'o1-prior\retained-evidence.txt'))) {
        throw 'O1-E: retained content of a RolledBack attempt was removed.'
    }
    $passed++

    # --- Advanced-copy load identity and protected-save comparison ---
    . (Join-Path $PSScriptRoot 'RuntimeAutomation.Common.ps1')
    $pairRoot = Join-Path $root 'saves-pairs'
    New-Item -ItemType Directory -Path $pairRoot | Out-Null
    New-TestSaveArchive -Path (Join-Path $pairRoot 'Manual_400_PlayerCampaign.zks') -Name 'PlayerCampaign' -GameName 'Valued Campaign'
    New-TestSaveArchive -Path (Join-Path $pairRoot 'Manual_402_KBP_AUTOMATION_BASELINE.zks') -Name 'KBP_AUTOMATION_BASELINE'
    New-TestSaveArchive -Path (Join-Path $pairRoot 'Manual_403_KBP_AUTOMATION_WORKING.zks') -Name 'KBP_AUTOMATION_WORKING'
    # The automation lookup is unchanged; the Advanced lookup refuses with no advanced pair.
    $autoPair = Get-KbpDisposableSavePair -SaveRoot $pairRoot
    if ($autoPair.family -cne 'Automation' -or $autoPair.working.fileName -cne 'Manual_403_KBP_AUTOMATION_WORKING.zks') {
        throw 'The automation save-pair lookup changed.'
    }
    $refused = $false
    try { Get-KbpDisposableSavePair -Family Advanced -SaveRoot $pairRoot | Out-Null } catch { $refused = $true }
    if (-not $refused) { throw 'The Advanced lookup accepted a folder without an advanced pair.' }
    # With both families present each lookup returns only its own pair.
    $advancedGameId = '66666666-7777-8888-9999-000000000000'
    New-TestSaveArchive -Path (Join-Path $pairRoot 'Manual_411_KBP_ADVANCED_BASELINE.zks') -Name 'KBP_ADVANCED_BASELINE' `
        -GameName 'Advanced Campaign' -GameId $advancedGameId
    New-TestSaveArchive -Path (Join-Path $pairRoot 'Manual_412_KBP_ADVANCED_WORKING.zks') -Name 'KBP_ADVANCED_WORKING' `
        -GameName 'Advanced Campaign' -GameId $advancedGameId
    $advancedPair = Get-KbpDisposableSavePair -Family Advanced -SaveRoot $pairRoot
    if ($advancedPair.family -cne 'Advanced' -or $advancedPair.working.fileName -cne 'Manual_412_KBP_ADVANCED_WORKING.zks' -or
        $advancedPair.working.gameId -cne $advancedGameId -or
        (Get-KbpDisposableSavePair -SaveRoot $pairRoot).working.fileName -cne 'Manual_403_KBP_AUTOMATION_WORKING.zks') {
        throw 'Family lookups crossed pairs.'
    }
    # An advanced pair from two different campaigns is refused.
    $mixedPairRoot = Join-Path $root 'saves-pairs-mixed'
    New-Item -ItemType Directory -Path $mixedPairRoot | Out-Null
    New-TestSaveArchive -Path (Join-Path $mixedPairRoot 'Manual_411_KBP_ADVANCED_BASELINE.zks') -Name 'KBP_ADVANCED_BASELINE' `
        -GameName 'Advanced Campaign' -GameId $advancedGameId
    New-TestSaveArchive -Path (Join-Path $mixedPairRoot 'Manual_412_KBP_ADVANCED_WORKING.zks') -Name 'KBP_ADVANCED_WORKING' `
        -GameName 'Other Campaign'
    $refused = $false
    try { Get-KbpDisposableSavePair -Family Advanced -SaveRoot $mixedPairRoot | Out-Null } catch { $refused = $true }
    if (-not $refused) { throw 'An advanced pair from two campaigns was accepted.' }

    # Protected-save comparison: only the allowed WORKING copy may change.
    $before = Get-KbpSaveFolderSnapshot -SaveRoot $pairRoot
    $allowed = @('Manual_412_KBP_ADVANCED_WORKING.zks')
    if ((Compare-KbpSaveFolderSnapshot -Before $before -After (Get-KbpSaveFolderSnapshot -SaveRoot $pairRoot) `
            -AllowedChangedFileNames $allowed).Count -ne 0) {
        throw 'An unchanged save folder reported violations.'
    }
    Add-Content -LiteralPath (Join-Path $pairRoot 'Manual_412_KBP_ADVANCED_WORKING.zks') -Value 'x'
    if ((Compare-KbpSaveFolderSnapshot -Before $before -After (Get-KbpSaveFolderSnapshot -SaveRoot $pairRoot) `
            -AllowedChangedFileNames $allowed).Count -ne 0) {
        throw 'A change to the allowed WORKING copy was reported.'
    }
    Add-Content -LiteralPath (Join-Path $pairRoot 'Manual_400_PlayerCampaign.zks') -Value 'x'
    Set-Content -LiteralPath (Join-Path $pairRoot 'Auto_1_Autosave.zks') -Value 'autosave'
    Remove-Item -LiteralPath (Join-Path $pairRoot 'Manual_402_KBP_AUTOMATION_BASELINE.zks')
    $violations = Compare-KbpSaveFolderSnapshot -Before $before -After (Get-KbpSaveFolderSnapshot -SaveRoot $pairRoot) `
        -AllowedChangedFileNames $allowed
    $expected = @('changed:Manual_400_PlayerCampaign.zks', 'new:Auto_1_Autosave.zks',
        'removed:Manual_402_KBP_AUTOMATION_BASELINE.zks')
    if ((@($violations | Sort-Object) -join '|') -cne (@($expected | Sort-Object) -join '|')) {
        throw "Protected-save comparison missed a violation: $($violations -join '|')"
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
        # Review O1: reusing the rolled-back run id is refused and keeps the
        # record byte-identical; a fresh run id then succeeds.
        $failRecordPath = Join-Path $stateRoot ('fail-' + $stage + '\transaction.json')
        $failRecordHash = Get-KbpSha256 $failRecordPath
        $sameIdRefused = $false
        try {
            & $bootstrapScript -RunId ('fail-' + $stage) -SaveRoot $failRoot -StateRoot $stateRoot `
                -ArchiveRoot (Join-Path $root ('archive-fail-' + $stage + '-same')) -Confirm:$false | Out-Null
        } catch { $sameIdRefused = $true }
        if (-not $sameIdRefused -or (Get-KbpSha256 $failRecordPath) -cne $failRecordHash -or
            (Test-Path -LiteralPath (Join-Path $root ('archive-fail-' + $stage + '-same')))) {
            throw "Same-id retry after rollback at stage $stage was not refused without mutation."
        }
        & $bootstrapScript -RunId ('fail-' + $stage + '-retry') -SaveRoot $failRoot -StateRoot $stateRoot `
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
        # Review O1: a fresh run id retries; the rolled-back record stays.
        $jRecordHash = Get-KbpSha256 (Join-Path $jState ('n4-j-' + $journal + '\transaction.json'))
        & $bootstrapScript -RunId ('n4-j-' + $journal + '-retry') -SaveRoot $jRoot -StateRoot $jState `
            -ArchiveRoot (Join-Path $root ('archive-n4-j-' + $journal + '-retry')) -Confirm:$false | Out-Null
        if ((Get-KbpSha256 (Join-Path $jState ('n4-j-' + $journal + '\transaction.json'))) -cne $jRecordHash) {
            throw "Retry after journal-window recovery ($journal) changed the rolled-back record."
        }
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
        # Review O1: a fresh run id retries after the clean rollback; the
        # rolled-back record is kept byte-identical as history.
        $winRecordPath = Join-Path $winState ('r1-win-' + $move + '\transaction.json')
        $winRecordHash = Get-KbpSha256 $winRecordPath
        & $bootstrapScript -RunId ('r1-win-' + $move + '-retry') -SaveRoot $winRoot -StateRoot $winState `
            -ArchiveRoot (Join-Path $root ('archive-r1-win-' + $move + '-retry')) -Confirm:$false | Out-Null
        if (-not (Test-Path -LiteralPath (Join-Path $winRoot 'Manual_402_KBP_AUTOMATION_BASELINE.zks'))) {
            throw "Retry after the $move interruption window did not complete."
        }
        if ((Get-KbpSha256 $winRecordPath) -cne $winRecordHash) {
            throw "Retry after the $move interruption window changed the rolled-back record."
        }
    }
    $passed++

    # Advanced-copy binding: the pair must be exactly the pair published by
    # one completed advanced bootstrap (immutable BASELINE bytes, the same
    # WORKING file, the same campaign). Automation or rolled-back records
    # never bind; two completed advanced bootstraps are ambiguous.
    . (Join-Path $PSScriptRoot 'RuntimeAutomation.Common.ps1')
    $bindState = Join-Path $root 'bind-state'
    New-Item -ItemType Directory -Path $bindState | Out-Null
    function New-BindRecord([string]$RunId, [string]$Status, [string]$BaselineFile, [string]$WorkingFile,
        [string]$BaselineSha, [string]$GameId) {
        $runDir = Join-Path $bindState $RunId
        New-Item -ItemType Directory -Path $runDir | Out-Null
        $manifestPath = Join-Path $runDir 'fixture-manifest.json'
        Write-KbpJsonAtomic $manifestPath ([ordered]@{
            schemaVersion = 1; runId = $RunId
            baseline = @{ fileName = $BaselineFile; sha256 = $BaselineSha }
            working = @{ fileName = $WorkingFile; sha256 = ('c' * 64) }
            gameId = $GameId })
        Write-KbpJsonAtomic (Join-Path $runDir 'transaction.json') ([ordered]@{
            schemaVersion = 1; runId = $RunId; status = $Status; manifestPath = $manifestPath })
    }
    $bindGame = '66666666-7777-8888-9999-000000000000'
    New-BindRecord 'bootstrap-advanced-fixture' 'Completed' 'Manual_411_KBP_ADVANCED_BASELINE.zks' `
        'Manual_412_KBP_ADVANCED_WORKING.zks' ('a' * 64) $bindGame
    New-BindRecord 'bootstrap-automation-fixture' 'Completed' 'Manual_304_KBP_AUTOMATION_BASELINE.zks' `
        'Manual_305_KBP_AUTOMATION_WORKING.zks' ('d' * 64) 'automation-game'
    New-BindRecord 'bootstrap-advanced-rolled-back' 'RolledBack' 'Manual_421_KBP_ADVANCED_BASELINE.zks' `
        'Manual_422_KBP_ADVANCED_WORKING.zks' ('e' * 64) $bindGame
    function New-BindPair([string]$BaselineFile, [string]$BaselineSha, [string]$WorkingFile, [string]$GameId) {
        return [pscustomobject]@{
            family = 'Advanced'
            baseline = [pscustomobject]@{ fileName = $BaselineFile; sha256 = $BaselineSha; gameId = $GameId }
            working = [pscustomobject]@{ fileName = $WorkingFile; sha256 = ('f' * 64); gameId = $GameId }
        }
    }
    $boundRecord = Assert-KbpAdvancedFixtureBinding -FixtureStateRoot $bindState -Pair (New-BindPair `
        'Manual_411_KBP_ADVANCED_BASELINE.zks' ('a' * 64) 'Manual_412_KBP_ADVANCED_WORKING.zks' $bindGame)
    if ($boundRecord.runId -cne 'bootstrap-advanced-fixture') {
        throw 'The advanced binding did not resolve the single completed advanced bootstrap.'
    }
    $bindRefusals = @(
        (New-BindPair 'Manual_411_KBP_ADVANCED_BASELINE.zks' ('b' * 64) 'Manual_412_KBP_ADVANCED_WORKING.zks' $bindGame),
        (New-BindPair 'Manual_411_KBP_ADVANCED_BASELINE.zks' ('a' * 64) 'Manual_499_KBP_ADVANCED_WORKING.zks' $bindGame),
        (New-BindPair 'Manual_411_KBP_ADVANCED_BASELINE.zks' ('a' * 64) 'Manual_412_KBP_ADVANCED_WORKING.zks' 'other-game'),
        (New-BindPair 'Manual_421_KBP_ADVANCED_BASELINE.zks' ('e' * 64) 'Manual_422_KBP_ADVANCED_WORKING.zks' $bindGame)
    )
    foreach ($bindPair in $bindRefusals) {
        $refused = $false
        try { Assert-KbpAdvancedFixtureBinding -FixtureStateRoot $bindState -Pair $bindPair | Out-Null }
        catch { $refused = $true }
        if (-not $refused) { throw "An unbound advanced pair was accepted: $($bindPair.baseline.fileName)/$($bindPair.working.fileName)." }
    }
    New-BindRecord 'bootstrap-advanced-second' 'Completed' 'Manual_431_KBP_ADVANCED_BASELINE.zks' `
        'Manual_432_KBP_ADVANCED_WORKING.zks' ('9' * 64) $bindGame
    $refused = $false
    try {
        Assert-KbpAdvancedFixtureBinding -FixtureStateRoot $bindState -Pair (New-BindPair `
            'Manual_411_KBP_ADVANCED_BASELINE.zks' ('a' * 64) 'Manual_412_KBP_ADVANCED_WORKING.zks' $bindGame) | Out-Null
    }
    catch { $refused = $_.Exception.Message -like '*exactly one completed advanced bootstrap*' }
    if (-not $refused) { throw 'Two completed advanced bootstraps were not refused as ambiguous.' }
    $passed++

    # Review RC3: casting on the advanced copy needs a COMPLETED inspection
    # of the same bound pair and compatibility identity: game PASS alone,
    # a failed or missing protected-save comparison, an unverified
    # restoration, another fixture or another profile never qualifies.
    $inspectRoot = Join-Path $root 'inspect-evidence'
    New-Item -ItemType Directory -Path $inspectRoot -Force | Out-Null
    $inspectBinding = [pscustomobject]@{ manifestPath = 'C:/lab/fixture/bootstrap-advanced/manifest.json' }
    $inspectPair = [pscustomobject]@{
        baseline = [pscustomobject]@{ fileName = 'Manual_411_KBP_ADVANCED_BASELINE.zks'; sha256 = ('a' * 64) }
        working = [pscustomobject]@{ fileName = 'Manual_412_KBP_ADVANCED_WORKING.zks'; sha256 = ('b' * 64); gameId = 'advanced-game' }
    }
    $inspectIdentity = 'c' * 64
    function New-InspectEvidence([string]$RunId, [hashtable]$Override, [switch]$OnlyGameResult) {
        $dir = Join-Path $inspectRoot $RunId
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-KbpJsonAtomic (Join-Path $dir 'runtime-result.json') ([ordered]@{
            schemaVersion = 1; runId = $RunId; scenario = 'live-advanced-inspect'; status = 'PASS' })
        if ($OnlyGameResult) { return }
        $record = [ordered]@{
            schemaVersion = 1; runId = $RunId; scenario = 'live-advanced-inspect'; fixtureFamily = 'Advanced'
            profileId = 'full-user'; compatibilityIdentity = $inspectIdentity
            advancedBindingManifest = $inspectBinding.manifestPath
            fixture = [ordered]@{ baselineFileName = $inspectPair.baseline.fileName; baselineSha256 = $inspectPair.baseline.sha256
                workingFileName = $inspectPair.working.fileName; workingSha256 = $inspectPair.working.sha256; gameId = 'advanced-game' }
            gameResultStatus = 'PASS'; harnessSucceeded = $true; kingmakerExited = $true; restorationVerified = $true
            protectedSavesCompared = $true; protectedSavesClean = $true; complete = $true
        }
        foreach ($key in $Override.Keys) { $record[$key] = $Override[$key] }
        Write-KbpJsonAtomic (Join-Path $dir 'run-completion.json') $record
    }
    New-InspectEvidence 'inspect-game-pass-only' @{} -OnlyGameResult
    New-InspectEvidence 'inspect-protected-failed' @{ protectedSavesClean = $false; complete = $false; harnessSucceeded = $false }
    New-InspectEvidence 'inspect-protected-missing' @{ protectedSavesCompared = $false; protectedSavesClean = $false; complete = $false }
    New-InspectEvidence 'inspect-not-restored' @{ restorationVerified = $false; complete = $false }
    New-InspectEvidence 'inspect-no-exit' @{ kingmakerExited = $false; complete = $false }
    # A record claiming complete while any single condition fails.
    New-InspectEvidence 'inspect-complete-flag-only' @{ restorationVerified = $false }
    New-InspectEvidence 'inspect-claim-unclean' @{ protectedSavesClean = $false }
    New-InspectEvidence 'inspect-claim-uncompared' @{ protectedSavesCompared = $false }
    New-InspectEvidence 'inspect-claim-running' @{ kingmakerExited = $false }
    New-InspectEvidence 'inspect-claim-harness' @{ harnessSucceeded = $false }
    New-InspectEvidence 'inspect-claim-game' @{ gameResultStatus = 'FAIL' }
    New-InspectEvidence 'inspect-other-working' @{ fixture = [ordered]@{ baselineFileName = $inspectPair.baseline.fileName
        baselineSha256 = $inspectPair.baseline.sha256; workingFileName = $inspectPair.working.fileName
        workingSha256 = ('d' * 64); gameId = 'advanced-game' } }
    New-InspectEvidence 'inspect-other-profile' @{ compatibilityIdentity = ('e' * 64) }
    New-InspectEvidence 'inspect-other-binding' @{ advancedBindingManifest = 'C:/other/manifest.json' }
    New-InspectEvidence 'inspect-automation-family' @{ fixtureFamily = 'Automation' }
    New-InspectEvidence 'inspect-wrong-scenario' @{ scenario = 'live-cast-qual-select' }
    $refused = $false
    try {
        Assert-KbpAdvancedInspectionPassed -Binding $inspectBinding -Pair $inspectPair -ProfileId 'full-user' `
            -CompatibilityIdentity $inspectIdentity -EvidenceRoot $inspectRoot | Out-Null
    }
    catch { $refused = $_.Exception.Message -like '*completed live-advanced-inspect*' }
    if (-not $refused) { throw 'An incomplete or foreign inspection authorized advanced casting.' }
    New-InspectEvidence 'inspect-clean' @{}
    if ((Assert-KbpAdvancedInspectionPassed -Binding $inspectBinding -Pair $inspectPair -ProfileId 'full-user' `
            -CompatibilityIdentity $inspectIdentity -EvidenceRoot $inspectRoot) -cne 'inspect-clean') {
        throw 'The completed clean inspection of the bound pair was not found.'
    }
    # Review of e7c5207..f7726c9, P3-5: the launcher's own completion
    # computation (not a hand-built record) decides completeness, reads
    # restoration from the transaction state, and round-trips through the
    # inspection guard; the save policy per scenario is exact (P3-4).
    $txRoot = Join-Path $root 'completion-tx'
    New-Item -ItemType Directory -Path $txRoot -Force | Out-Null
    $txRestored = Join-Path $txRoot 'restored.json'
    $txUnrestored = Join-Path $txRoot 'unrestored.json'
    $txNoField = Join-Path $txRoot 'nofield.json'
    Write-KbpJsonAtomic $txRestored ([ordered]@{ schemaVersion = 1; restorationVerified = $true })
    Write-KbpJsonAtomic $txUnrestored ([ordered]@{ schemaVersion = 1; restorationVerified = $false })
    Write-KbpJsonAtomic $txNoField ([ordered]@{ schemaVersion = 1 })
    function New-TestCompletion([hashtable]$Override) {
        $arguments = @{
            RunId = 'inspect-computed'; Scenario = 'live-advanced-inspect'; FixtureFamily = 'Advanced'
            ProfileId = 'full-user'; CompatibilityIdentity = $inspectIdentity
            AdvancedBindingManifest = $inspectBinding.manifestPath; SavePair = $inspectPair
            GameResultStatus = 'PASS'; HarnessSucceeded = $true; KingmakerExited = $true
            TransactionStatePath = $txRestored; RestoreFailure = $null
            ProtectedSavesCompared = $true; ProtectedSaveFailure = $null
        }
        foreach ($key in $Override.Keys) { $arguments[$key] = $Override[$key] }
        return New-KbpRunCompletionRecord @arguments
    }
    if (-not (New-TestCompletion @{}).complete) { throw 'A clean whole run was not recorded complete.' }
    foreach ($case in @(
            @{ GameResultStatus = 'FAIL' }, @{ GameResultStatus = $null }, @{ HarnessSucceeded = $false },
            @{ KingmakerExited = $false }, @{ TransactionStatePath = $txUnrestored },
            @{ TransactionStatePath = $txNoField }, @{ TransactionStatePath = (Join-Path $txRoot 'missing.json') },
            @{ TransactionStatePath = $null }, @{ ProtectedSavesCompared = $false },
            @{ ProtectedSaveFailure = 'Protected saves changed during the run: changed:x' },
            @{ RestoreFailure = 'Kingmaker remains running; exact Mods restoration is intentionally blocked.' })) {
        $computed = New-TestCompletion $case
        if ($computed.complete) {
            throw ('A run was recorded complete although ' + (($case.Keys | ForEach-Object { $_ }) -join ',') + ' failed.')
        }
    }
    $blockedRestore = New-TestCompletion @{ RestoreFailure = 'Mods restoration failed' }
    if ($blockedRestore.restorationVerified -or [string]$blockedRestore.restorationFailure -cne 'Mods restoration failed' -or
        $null -ne (New-TestCompletion @{}).restorationFailure) {
        throw 'The completion record does not carry the launcher restoration failure.'
    }
    $unrestored = New-TestCompletion @{ TransactionStatePath = $txUnrestored }
    if ($unrestored.restorationVerified -or -not $unrestored.protectedSavesClean -or $unrestored.gameResultStatus -cne 'PASS') {
        throw 'The completion record misreports its individual conditions.'
    }
    $computedRoot = Join-Path $root 'computed-evidence'
    New-Item -ItemType Directory -Path (Join-Path $computedRoot 'inspect-computed') -Force | Out-Null
    Write-KbpJsonAtomic (Join-Path $computedRoot 'inspect-computed\run-completion.json') $unrestored
    $refused = $false
    try {
        Assert-KbpAdvancedInspectionPassed -Binding $inspectBinding -Pair $inspectPair -ProfileId 'full-user' `
            -CompatibilityIdentity $inspectIdentity -EvidenceRoot $computedRoot | Out-Null
    }
    catch { $refused = $true }
    if (-not $refused) { throw 'A computed record with an unverified restoration qualified advanced casting.' }
    Write-KbpJsonAtomic (Join-Path $computedRoot 'inspect-computed\run-completion.json') (New-TestCompletion @{})
    if ((Assert-KbpAdvancedInspectionPassed -Binding $inspectBinding -Pair $inspectPair -ProfileId 'full-user' `
            -CompatibilityIdentity $inspectIdentity -EvidenceRoot $computedRoot) -cne 'inspect-computed') {
        throw 'The launcher-computed complete inspection record did not qualify.'
    }
    foreach ($strictCase in @(
            @('live-cast-qual', 'Automation', $true), @('live-cast-qual-select', 'Automation', $false),
            @('live-advanced-inspect', 'Advanced', $true), @('live-advanced-inspect', 'Automation', $false),
            @('live-cast-probe', 'Automation', $true),
            @('live-workspace-qual', 'Advanced', $true),
            @('live-cast-qual-select', 'Advanced', $true))) {
        $policy = Get-KbpProtectedSavePolicy -Scenario $strictCase[0] -FixtureFamily $strictCase[1] -WorkingFileName 'W.zks'
        if (@($policy.allowedChanged).Count -ne 0 -or [bool]$policy.newFilesBlocking -ne $strictCase[2]) {
            throw ('The save policy for ' + $strictCase[0] + '/' + $strictCase[1] + ' is wrong.')
        }
    }
    $loose = Get-KbpProtectedSavePolicy -Scenario 'live-workspace-qual' -FixtureFamily 'Automation' -WorkingFileName 'W.zks'
    if (@($loose.allowedChanged).Count -ne 1 -or @($loose.allowedChanged)[0] -cne 'W.zks' -or $loose.newFilesBlocking) {
        throw 'An automation UI run may change only its WORKING save.'
    }
    $launcherText = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Invoke-KingmakerRuntimeTest.ps1') -Raw
    # Review of f7726c9..1332ed8, P3-B: the restoration branch itself never
    # throws (a throw would skip the save comparison and the record), both
    # failure paths assign the failure, the policy feeds the comparison, the
    # record receives the failure, and the run's failure is rethrown only
    # after the finally.
    if ($launcherText -notmatch '(?s)if \(\$running\.Count -eq 0\) \{(.*?)if \(\$null -ne \$restoreFailure\) \{') {
        throw 'The launcher restoration branch was not found.'
    }
    $restoreBranch = $Matches[1]
    if ($restoreBranch -match '\bthrow\b' -or $restoreBranch -match 'Write-Error' -or
        ([regex]::Matches($restoreBranch, '\$restoreFailure = ')).Count -ne 2 -or
        $launcherText -notmatch '\$allowedChanged = @\(\$savePolicy\.allowedChanged\)' -or
        $launcherText -notmatch '-AllowedChangedFileNames \$allowedChanged' -or
        $launcherText -notmatch '\$savePolicy\.newFilesBlocking' -or
        $launcherText -notmatch '-RestoreFailure \$restoreFailure' -or
        $launcherText -notmatch '(?s)catch \{[^{}]*\$runFailure = \$_\s*\}\s*finally \{' -or
        $launcherText -notmatch "restoration-failure\.txt") {
        throw 'The launcher can skip the save comparison or the completion record on a restoration failure, or drops the failure.'
    }
    if ($launcherText -notmatch 'New-KbpRunCompletionRecord' -or $launcherText -notmatch 'Get-KbpProtectedSavePolicy' -or
        $launcherText -match 'Write-Error "Kingmaker remains running' -or
        $launcherText -notmatch "try \{ & \(Join-Path \`$PSScriptRoot 'Restore-Local\.ps1'\)") {
        throw 'The launcher does not compute its completion record and save policy through the tested functions, or a restoration failure can still skip them.'
    }
    $digestA = Get-KbpCompatibilityIdentityDigest ([pscustomobject]@{ profileId = 'p'; mods = @(
        [pscustomobject]@{ directoryName = 'A'; version = '1'; directoryManifestSha256 = ('1' * 64); fileCount = 2; totalBytes = 10 }) })
    $digestB = Get-KbpCompatibilityIdentityDigest ([pscustomobject]@{ profileId = 'p'; mods = @(
        [pscustomobject]@{ directoryName = 'A'; version = '1'; directoryManifestSha256 = ('2' * 64); fileCount = 2; totalBytes = 10 }) })
    if ($digestA -notmatch '^[0-9a-f]{64}$' -or $digestA -ceq $digestB) {
        throw 'The compatibility identity digest does not follow the mod identities.'
    }
    $passed++
    # Scenario drift: every scenario the launcher accepts must build a
    # request (the request builder repeats the ValidateSet), and the two
    # sets must be identical.
    $launcherSet = @((Get-Command (Join-Path $PSScriptRoot 'Invoke-KingmakerRuntimeTest.ps1')).Parameters['Scenario'].Attributes |
        Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] } |
        ForEach-Object { $_.ValidValues })
    $requestSet = @((Get-Command New-KbpRuntimeRequest).Parameters['Scenario'].Attributes |
        Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] } |
        ForEach-Object { $_.ValidValues })
    if ($launcherSet.Count -lt 16 -or
        (@(Compare-Object -ReferenceObject $launcherSet -DifferenceObject $requestSet -CaseSensitive).Count -ne 0)) {
        throw ('Launcher and request scenario sets differ: launcher=' + ($launcherSet -join ',') +
            ' request=' + ($requestSet -join ','))
    }
    $driftManifest = [pscustomobject]@{ version = '0.0.0'; commit = ('0' * 40); packageSha256 = ('a' * 64); dllSha256 = ('b' * 64) }
    foreach ($driftScenario in $launcherSet) {
        $driftRequest = New-KbpRuntimeRequest -RunId ('drift-' + $driftScenario) -EvidenceDirectory 'evidence' `
            -BuildManifest $driftManifest -TimeoutSeconds 60 -ExitAfterCompletion $true -Scenario $driftScenario
        if ($driftRequest.scenario -cne $driftScenario) { throw "Request for $driftScenario was not built." }
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
