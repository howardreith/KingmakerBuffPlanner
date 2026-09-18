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

    # --- Guarded automation-fixture bootstrap ---
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
                    '","GameId":"' + $GameId + '","Area":"Jamandi Aldori''s Mansion"}')
            } finally { $writer.Dispose() }
            $partyEntry = $archive.CreateEntry('party.json')
            $writer = [IO.StreamWriter]::new($partyEntry.Open(), [Text.Encoding]::UTF8)
            try { $writer.Write('{"members":[]}') } finally { $writer.Dispose() }
        } finally { $archive.Dispose() }
    }

    $saveRoot = Join-Path $root 'saves'
    $fixtureBackup = Join-Path $root 'fixture-backup'
    New-Item -ItemType Directory -Path $saveRoot | Out-Null
    New-TestSaveArchive -Path (Join-Path $saveRoot 'Manual_400_PlayerCampaign.zks') -Name 'PlayerCampaign' -GameName 'Valued Campaign'
    $seedPath = Join-Path $saveRoot 'Manual_401_KBP_AUTOMATION_SEED.zks'
    New-TestSaveArchive -Path $seedPath -Name 'KBP_AUTOMATION_SEED'
    $playerBefore = Get-KbpSha256 (Join-Path $saveRoot 'Manual_400_PlayerCampaign.zks')
    $seedBefore = Get-KbpSha256 $seedPath

    $bootstrapScript = Join-Path $repo 'scripts\New-KbpAutomationFixture.ps1'
    & $bootstrapScript -RunId 'harness-bootstrap' -SaveRoot $saveRoot -BackupRoot $fixtureBackup -Confirm:$false | Out-Null
    $baselinePath = Join-Path $saveRoot 'Manual_402_KBP_AUTOMATION_BASELINE.zks'
    $workingPath = Join-Path $saveRoot 'Manual_403_KBP_AUTOMATION_WORKING.zks'
    if (-not (Test-Path -LiteralPath $baselinePath) -or -not (Test-Path -LiteralPath $workingPath) -or
        -not (Test-Path -LiteralPath (Join-Path $fixtureBackup 'fixture-manifest.json'))) {
        throw 'Fixture bootstrap did not create the sealed pair and manifest.'
    }
    function Read-TestHeaderName {
        param([string]$Path)
        $archive = [IO.Compression.ZipFile]::OpenRead($Path)
        try {
            $reader = [IO.StreamReader]::new($archive.GetEntry('header.json').Open())
            try { return ((($reader.ReadToEnd()) | ConvertFrom-Json).Name) } finally { $reader.Dispose() }
        } finally { $archive.Dispose() }
    }
    if ((Read-TestHeaderName $baselinePath) -cne 'KBP_AUTOMATION_BASELINE' -or
        (Read-TestHeaderName $workingPath) -cne 'KBP_AUTOMATION_WORKING') {
        throw 'Fixture pair headers were not rewritten to the harness contract names.'
    }
    if ((Get-KbpSha256 $seedPath) -cne $seedBefore -or
        (Get-KbpSha256 (Join-Path $saveRoot 'Manual_400_PlayerCampaign.zks')) -cne $playerBefore) {
        throw 'Fixture bootstrap mutated the seed or an ordinary save.'
    }
    $manifest = Read-KbpJson (Join-Path $fixtureBackup 'fixture-manifest.json')
    if ($manifest.seed.sha256 -cne $seedBefore -or
        $manifest.baseline.sha256 -cne (Get-KbpSha256 $baselinePath)) {
        throw 'Fixture manifest provenance hashes are not exact.'
    }
    $passed++

    $refused = $false
    try { & $bootstrapScript -RunId 'harness-bootstrap-2' -SaveRoot $saveRoot -BackupRoot (Join-Path $root 'fixture-backup-2') -Confirm:$false | Out-Null }
    catch { $refused = $true }
    if (-not $refused) { throw 'A second fixture bootstrap did not refuse.' }
    $passed++

    & $bootstrapScript -RunId 'harness-teardown' -SaveRoot $saveRoot -Teardown -Confirm:$false | Out-Null
    if ((Test-Path -LiteralPath $baselinePath) -or (Test-Path -LiteralPath $workingPath) -or
        -not (Test-Path -LiteralPath $seedPath) -or
        (Get-KbpSha256 $seedPath) -cne $seedBefore -or
        (Get-KbpSha256 (Join-Path $saveRoot 'Manual_400_PlayerCampaign.zks')) -cne $playerBefore) {
        throw 'Teardown did not remove exactly the disposable pair.'
    }
    $passed++

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
