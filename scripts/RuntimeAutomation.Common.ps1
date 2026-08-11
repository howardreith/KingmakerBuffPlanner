Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')

function Read-KbpBuildManifest([string]$PackagePath) {
    $manifestPath = $PackagePath + '.build-local.json'
    $manifest = Read-KbpJson $manifestPath
    $root = Get-KbpRepositoryRoot
    $head = (& git -C $root rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Unable to read Git HEAD.' }
    if ($manifest.schemaVersion -ne 1 -or
        $manifest.generator -cne 'scripts/Build-Local.ps1' -or
        $manifest.version -cne (Get-KbpVersion) -or
        $manifest.commit -cne $head -or
        $manifest.packagePath -cne $PackagePath -or
        $manifest.packageSha256 -cne (Get-KbpSha256 $PackagePath) -or
        $manifest.validated -ne $true) {
        throw 'Build-local manifest does not match the exact package and clean HEAD.'
    }
    return $manifest
}

function Get-KbpTimestampedLogLines([string]$Path, [DateTime]$NotBefore) {
    $records = @()
    foreach ($line in @(Get-Content -LiteralPath $Path -ErrorAction Stop)) {
        if ($line -match '^\[(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})\] (?<message>.*)$') {
            $timestamp = [DateTime]::ParseExact(
                $Matches.timestamp, 'yyyy-MM-dd HH:mm:ss',
                [Globalization.CultureInfo]::InvariantCulture,
                [Globalization.DateTimeStyles]::AssumeLocal)
            if ($timestamp -ge $NotBefore) {
                $records += [pscustomobject]@{
                    timestamp = $timestamp
                    message = [string]$Matches.message
                }
            }
        }
    }
    return @($records)
}

function Assert-KbpSteamSafety {
    param([string]$SteamPath = 'C:\Program Files (x86)\Steam\steam.exe')
    if (-not (Test-Path -LiteralPath $SteamPath -PathType Leaf)) { throw 'The exact Steam executable is missing.' }
    Assert-KbpNotRunning
    $installers = @(Get-Process -Name UnityModManager -ErrorAction SilentlyContinue)
    if ($installers.Count -ne 0) { throw 'Unity Mod Manager installer is running; runtime launch is ambiguous.' }
    $steamProcesses = @(Get-Process -Name steam -ErrorAction SilentlyContinue)
    if ($steamProcesses.Count -ne 1) { throw 'Exactly one already-running Steam client is required.' }
    $steam = $steamProcesses[0]
    if (-not $steam.Path.Equals((Resolve-Path -LiteralPath $SteamPath).Path, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Running Steam executable path is unexpected.'
    }

    $steamRoot = Split-Path -Parent $SteamPath
    $connectionLog = Join-Path $steamRoot 'logs\connection_log.txt'
    $cloudLog = Join-Path $steamRoot 'logs\cloud_log.txt'
    $connection = @(Get-KbpTimestampedLogLines $connectionLog $steam.StartTime)
    $cloud = @(Get-KbpTimestampedLogLines $cloudLog $steam.StartTime |
        Where-Object message -match '^\[AppID 640820\]')
    if ($connection.Count -eq 0 -or $cloud.Count -eq 0) { throw 'Current Steam-session safety logs are incomplete.' }

    $lastLoggedOn = @($connection | Where-Object message -match '\[(Logged On|Logging On|Connected),' |
        Sort-Object timestamp | Select-Object -Last 1)
    $lastLoggedOff = @($connection | Where-Object message -match '\[(Logged Off|Logging Off),' |
        Sort-Object timestamp | Select-Object -Last 1)
    if ($lastLoggedOff.Count -ne 1 -or
        ($lastLoggedOn.Count -eq 1 -and $lastLoggedOff[0].timestamp -lt $lastLoggedOn[0].timestamp)) {
        throw 'Steam Offline Mode is not proven for the current session.'
    }
    $offlineCloud = @($cloud | Where-Object message -match 'Sync Disabled.*offlineMode=true|offlineMode=true' |
        Sort-Object timestamp | Select-Object -Last 1)
    $successfulTransfer = @($cloud | Where-Object message -match '(Download|Upload) OK|Success\.' |
        Sort-Object timestamp | Select-Object -Last 1)
    if ($offlineCloud.Count -ne 1 -or
        ($successfulTransfer.Count -eq 1 -and $offlineCloud[0].timestamp -le $successfulTransfer[0].timestamp)) {
        throw 'Steam Cloud disabled/offline state is not proven after the latest App 640820 transfer.'
    }

    $appManifest = Join-Path $steamRoot 'steamapps\appmanifest_640820.acf'
    $manifestText = Get-Content -LiteralPath $appManifest -Raw
    if ($manifestText -notmatch '"StateFlags"\s+"4"' -or
        $manifestText -notmatch '"buildid"\s+"6757524"') {
        throw 'Kingmaker Steam app state is not the qualified fully-installed build.'
    }
    return [ordered]@{
        steamProcessId = $steam.Id
        steamStartedAtUtc = $steam.StartTime.ToUniversalTime().ToString('o')
        loggedOffAtUtc = $lastLoggedOff[0].timestamp.ToUniversalTime().ToString('o')
        offlineCloudAtUtc = $offlineCloud[0].timestamp.ToUniversalTime().ToString('o')
        cloudPolicy = 'Sync Disabled; offlineMode=true'
        appManifestSha256 = Get-KbpSha256 $appManifest
    }
}

function New-KbpRuntimeRequest {
    param(
        [string]$RunId, [string]$EvidenceDirectory, $BuildManifest,
        [int]$TimeoutSeconds, [bool]$ExitAfterCompletion,
        [ValidateSet('mod-load-smoke', 'native-buff-catalog', 'ui-root-smoke')][string]$Scenario = 'mod-load-smoke')
    return [ordered]@{
        schemaVersion = 1
        enabled = $true
        runId = $RunId
        scenario = $Scenario
        expectedModVersion = [string]$BuildManifest.version
        expectedCommit = [string]$BuildManifest.commit
        evidenceDirectory = $EvidenceDirectory
        expectedPackageSha256 = [string]$BuildManifest.packageSha256
        expectedDllSha256 = [string]$BuildManifest.dllSha256
        timeoutSeconds = $TimeoutSeconds
        exitAfterCompletion = $ExitAfterCompletion
        parameters = [ordered]@{}
    }
}

function Wait-KbpNewKingmakerProcess {
    param([int[]]$PreexistingIds, [int]$TimeoutSeconds)
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        $candidates = @(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue |
            Where-Object { $PreexistingIds -notcontains $_.Id } | Sort-Object StartTime)
        if ($candidates.Count -eq 1) { return $candidates[0] }
        if ($candidates.Count -gt 1) { throw 'More than one new Kingmaker process appeared.' }
        Start-Sleep -Milliseconds 250
    }
    throw 'Steam did not create exactly one Kingmaker process before the launch timeout.'
}

function Assert-KbpRuntimeResult {
    param($Result, $Request, $BuildManifest)
    $checks = [ordered]@{
        schemaVersion = [pscustomobject]@{ expected = 1; observed = $Result.schemaVersion }
        runId = [pscustomobject]@{ expected = $Request.runId; observed = $Result.runId }
        scenario = [pscustomobject]@{ expected = $Request.scenario; observed = $Result.scenario }
        loadedModId = [pscustomobject]@{ expected = 'KingmakerBuffPlanner'; observed = $Result.loadedModId }
        loadedModVersion = [pscustomobject]@{ expected = $BuildManifest.version; observed = $Result.loadedModVersion }
        commit = [pscustomobject]@{ expected = $BuildManifest.commit; observed = $Result.commit }
        packageSha256 = [pscustomobject]@{ expected = $BuildManifest.packageSha256; observed = $Result.packageSha256 }
        assemblySha256 = [pscustomobject]@{ expected = $BuildManifest.dllSha256; observed = $Result.assemblySha256 }
        gameVersion = [pscustomobject]@{ expected = '2.1.7'; observed = $Result.gameVersion }
        gameExecutableSha256 = [pscustomobject]@{ expected = '94a779c5423199fcb0470bd89884a3b3875dee2072eb1a7b1d7bc8e67accb1a1'; observed = $Result.gameExecutableSha256 }
        ummVersion = [pscustomobject]@{ expected = '0.28.2.0'; observed = $Result.ummVersion }
        ummSha256 = [pscustomobject]@{ expected = '75b96e25a3a9fbadb47dd14a4ab490cb8c98143a6242aff3bba6145cd3047f39'; observed = $Result.ummSha256 }
        harmonyVersion = [pscustomobject]@{ expected = '1.2.0.1'; observed = $Result.harmonyVersion }
        harmonySha256 = [pscustomobject]@{ expected = 'aa1cd48317254985d8b700cc74953477d1b40c3022ce9aa4c95ed2b8327e1292'; observed = $Result.harmonySha256 }
    }
    $mismatches = @()
    foreach ($name in $checks.Keys) {
        if ([string]$checks[$name].observed -cne [string]$checks[$name].expected) {
            $mismatches += @("$name expected=$($checks[$name].expected) observed=$($checks[$name].observed)")
        }
    }
    if ($mismatches.Count -ne 0) { throw "Runtime result identity/hash mismatch: $($mismatches -join '; ')" }
    if ($Result.status -notin @('PASS', 'FAIL', 'BLOCKED')) { throw 'Runtime result status is invalid.' }
    if (@($Result.assertions).Count -lt 5) { throw 'Runtime result assertion list is incomplete.' }
    if ($Request.scenario -ceq 'native-buff-catalog') {
        $catalogPath = Join-Path $Request.evidenceDirectory 'native-buff-catalog.json'
        if (-not (Test-Path -LiteralPath $catalogPath -PathType Leaf)) { throw 'Native catalog evidence is missing.' }
        if ($Result.catalogSha256 -cne (Get-KbpSha256 $catalogPath)) { throw 'Native catalog hash mismatch.' }
        if ([int]$Result.catalogAbilityCount -le 0) { throw 'Native catalog is empty.' }
        $catalog = Read-KbpJson $catalogPath
        if ([int]$catalog.schemaVersion -ne 4 -or
            [int]$catalog.abilityCount -ne [int]$Result.catalogAbilityCount -or
            @($catalog.abilities).Count -ne [int]$catalog.abilityCount) {
            throw 'Native catalog JSON contract does not reconcile with the runtime result.'
        }
        $missingExpressions = @($catalog.abilities | Where-Object {
            [string]::IsNullOrWhiteSpace([string]$_.expression.expressionType)
        })
        if ($missingExpressions.Count -ne 0) { throw 'Native catalog contains expressions without discriminators.' }
    }
    if ($Request.scenario -ceq 'ui-root-smoke') {
        if ([int]$Result.uiRootCount -ne 1 -or [int]$Result.uiRenderedOpenFrames -le 0 -or
            [int]$Result.uiOpenCloseCycles -lt 2 -or
            [int]$Result.uiScreenWidth -le 0 -or [int]$Result.uiScreenHeight -le 0) {
            throw 'UI root smoke result is incomplete or invalid.'
        }
    }
}
