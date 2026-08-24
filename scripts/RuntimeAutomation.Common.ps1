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
        [ValidateSet('native-only', 'call-of-the-wild', 'human-reproduction')][string]$ProfileId = 'native-only',
        [object[]]$ExpectedOptionalMods = @(), [string[]]$ExpectedBlueprintGuids = @(),
        [hashtable]$Parameters = @{},
        [ValidateSet('mod-load-smoke', 'native-buff-catalog', 'ui-root-smoke', 'live-ui-bootstrap', 'ui-native-contract-probe', 'final-no-save-core', 'performance-probe')][string]$Scenario = 'mod-load-smoke')
    return [ordered]@{
        schemaVersion = 1
        enabled = $true
        runId = $RunId
        scenario = $Scenario
        profileId = $ProfileId
        expectedModVersion = [string]$BuildManifest.version
        expectedCommit = [string]$BuildManifest.commit
        evidenceDirectory = $EvidenceDirectory
        expectedPackageSha256 = [string]$BuildManifest.packageSha256
        expectedDllSha256 = [string]$BuildManifest.dllSha256
        timeoutSeconds = $TimeoutSeconds
        exitAfterCompletion = $ExitAfterCompletion
        expectedOptionalMods = @($ExpectedOptionalMods)
        expectedBlueprintGuids = @($ExpectedBlueprintGuids)
        parameters = $Parameters
    }
}

function Get-KbpDisposableSavePair {
    $saveRoot = Join-Path $env:USERPROFILE `
        'AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games'
    if (-not (Test-Path -LiteralPath $saveRoot -PathType Container)) {
        throw 'The exact Kingmaker save root is unavailable.'
    }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $matches = @()
    foreach ($file in @(Get-ChildItem -LiteralPath $saveRoot -Filter '*.zks' -File)) {
        $archive = $null
        $reader = $null
        try {
            $archive = [IO.Compression.ZipFile]::OpenRead($file.FullName)
            $entry = $archive.GetEntry('header.json')
            if ($null -eq $entry) { continue }
            $reader = [IO.StreamReader]::new($entry.Open())
            $header = ($reader.ReadToEnd() | ConvertFrom-Json)
            if ($header.Name -in @('KBP_AUTOMATION_BASELINE', 'KBP_AUTOMATION_WORKING')) {
                $matches += [pscustomobject]@{
                    name = [string]$header.Name; fileName = $file.Name; path = $file.FullName
                    sha256 = Get-KbpSha256 $file.FullName; gameName = [string]$header.GameName
                    gameId = [string]$header.GameId; area = [string]$header.Area
                    length = $file.Length
                }
            }
        }
        catch [IO.InvalidDataException] {
            if ($file.Name -like '*KBP_AUTOMATION_*') {
                throw "Authorized disposable save archive is unreadable: $($file.Name)"
            }
            continue
        }
        finally {
            if ($null -ne $reader) { $reader.Dispose() }
            if ($null -ne $archive) { $archive.Dispose() }
        }
    }
    $baseline = @($matches | Where-Object name -ceq 'KBP_AUTOMATION_BASELINE')
    $working = @($matches | Where-Object name -ceq 'KBP_AUTOMATION_WORKING')
    if ($baseline.Count -ne 1 -or $working.Count -ne 1) {
        throw "Disposable save ambiguity: baseline=$($baseline.Count); working=$($working.Count)."
    }
    if ($baseline[0].fileName -notmatch '^Manual_[0-9]+_KBP_AUTOMATION_BASELINE\.zks$' -or
        $working[0].fileName -notmatch '^Manual_[0-9]+_KBP_AUTOMATION_WORKING\.zks$' -or
        $baseline[0].path -ceq $working[0].path -or
        $baseline[0].gameId -cne $working[0].gameId -or
        $baseline[0].gameName -cne $working[0].gameName -or
        $baseline[0].area -cne $working[0].area) {
        throw 'Disposable save pair descriptors are not exact, distinct, and campaign-correlated.'
    }
    return [pscustomobject]@{ baseline = $baseline[0]; working = $working[0] }
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
        profileId = [pscustomobject]@{ expected = $Request.profileId; observed = $Result.profileId }
        loadedModId = [pscustomobject]@{ expected = 'KingmakerBuffPlanner'; observed = $Result.loadedModId }
        loadedModVersion = [pscustomobject]@{ expected = $BuildManifest.version; observed = $Result.loadedModVersion }
        commit = [pscustomobject]@{ expected = $BuildManifest.commit; observed = $Result.commit }
        packageSha256 = [pscustomobject]@{ expected = $BuildManifest.packageSha256; observed = $Result.packageSha256 }
        assemblySha256 = [pscustomobject]@{ expected = $BuildManifest.dllSha256; observed = $Result.assemblySha256 }
        gameVersion = [pscustomobject]@{ expected = '2.1.7'; observed = $Result.gameVersion }
        gameExecutableSha256 = [pscustomobject]@{ expected = '94a779c5423199fcb0470bd89884a3b3875dee2072eb1a7b1d7bc8e67accb1a1'; observed = $Result.gameExecutableSha256 }
        ummVersion = [pscustomobject]@{ expected = '0.32.4.0'; observed = $Result.ummVersion }
        ummSha256 = [pscustomobject]@{ expected = '1387468bc3af41c50fe51859a3bb7af4922891aa8f13a6187e7a348ceaabfd88'; observed = $Result.ummSha256 }
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
    if ([int]$Result.optionalLoadedAssemblyCount -ne @($Request.expectedOptionalMods).Count) {
        throw 'Loaded optional assembly count does not match the requested profile.'
    }
    if ([int]$Result.optionalLoadedUmmEntryCount -ne @($Request.expectedOptionalMods).Count) {
        throw 'Loaded optional UMM entry count does not match the requested profile.'
    }
    if ($Result.status -ceq 'BLOCKED') {
        if ($Request.scenario -cne 'ui-root-smoke' -or
            $Result.stage -cne 'campaign-ui-unavailable' -or
            -not ([string]$Result.exceptionSummary).Contains('Campaign UI is required')) {
            throw 'Runtime BLOCKED result does not match an authorized campaign-UI precondition.'
        }
        return
    }
    if ($Request.scenario -ceq 'live-ui-bootstrap') {
        $screenshotPath = Join-Path $Request.evidenceDirectory 'planner-render.png'
        if (-not (Test-Path -LiteralPath $screenshotPath -PathType Leaf) -or
            [string]$Result.uiRenderScreenshotSha256 -cne (Get-KbpSha256 $screenshotPath) -or
            @($Result.uiRenderExpectedNames).Count -ne 5 -or
            @($Result.uiRenderRowScreenRectangles).Count -ne 5 -or
            @($Result.uiRenderRowEvidence).Count -ne 5 -or
            @($Result.uiRenderDetailsEvidence).Count -lt 1 -or
            [int]$Result.uiRenderBoundRowCount -lt 5 -or
            [string]::IsNullOrWhiteSpace([string]$Result.uiRenderSelectedRowName) -or
            [string]$Result.uiRenderSelectedRowName -cne [string]$Result.uiRenderDetailsTitleText -or
            [string]$Result.uiRenderCanaryEvidence -cne 'absent' -or
            -not ([string]$Result.uiRenderMaskEvidence).Contains('color=RGBA(1.000, 1.000, 1.000, 1.000)') -or
            -not ([string]$Result.uiRenderMaskEvidence).Contains('showGraphic=False')) {
            throw 'Live production screenshot/render evidence is incomplete or inconsistent.'
        }
        $presentationScreenshots = [ordered]@{
            'planner-selected-details.png' = [string]$Result.uiSelectedDetailsScreenshotSha256
            'planner-grid-overview.png' = [string]$Result.uiGridOverviewScreenshotSha256
            'planner-target-colors.png' = [string]$Result.uiTargetColorsScreenshotSha256
            'planner-settings.png' = [string]$Result.uiSettingsScreenshotSha256
            'hud-integration.png' = [string]$Result.uiHudScreenshotSha256
        }
        foreach ($name in $presentationScreenshots.Keys) {
            $path = Join-Path $Request.evidenceDirectory $name
            if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or
                [string]::IsNullOrWhiteSpace([string]$presentationScreenshots[$name]) -or
                [string]$presentationScreenshots[$name] -cne (Get-KbpSha256 $path)) {
                throw "Presentation screenshot evidence is missing or inconsistent: $name"
            }
        }
        if ([int]$Result.uiRenderAbilityIconCount + [int]$Result.uiRenderMissingIconCount -ne
                [int]$Result.uiRenderBoundRowCount -or
            [int]$Result.uiCastingModeControlCount -ne 1 -or
            [int]$Result.uiRetiredPrimaryLabelCount -ne 0 -or
            -not ([string]$Result.uiThemeResolution).Contains('parchment=') -or
            -not ([string]$Result.uiThemeResolution).Contains('font=')) {
            throw 'Live presentation icon, single-mode, filter-removal, or theme evidence is invalid.'
        }
        foreach ($rowEvidence in @($Result.uiRenderRowEvidence)) {
            if (-not ([string]$rowEvidence).Contains('rendererCull=False') -or
                -not ([string]$rowEvidence).Contains('inheritedAlpha=1') -or
                -not ([string]$rowEvidence).Contains('shader=UI/Default') -or
                -not ([string]$rowEvidence).Contains('font=Arial')) {
                throw 'A production row failed the CanvasRenderer/font/alpha/material evidence contract.'
            }
        }
        if ([string]::IsNullOrWhiteSpace([string]$Result.uiCatalogControlEvidence) -or
            -not ([string]$Result.uiCatalogControlEvidence).Contains('longSelected=1') -or
            -not ([string]$Result.uiCatalogControlEvidence).Contains('importantSelected=0') -or
            ([regex]::Matches([string]$Result.uiHudObjectEvidence,
                'spriteInk=0\.961,0\.820,0\.420,1\.000')).Count -ne 4 -or
            ([regex]::Matches([string]$Result.uiHudObjectEvidence, 'innerFrame=True')).Count -ne 4 -or
            [int]$Result.uiCatalogProviderCount -lt [int]$Result.uiCatalogAggregateAbilityCount -or
            [int]$Result.uiCatalogAggregateAbilityCount -lt [int]$Result.uiCatalogVisibleViewModels -or
            [int]$Result.uiDirectSelectedTargetCount -lt 1) {
            throw 'Catalog-control or dark/gold HUD evidence is incomplete.'
        }

        Add-Type -AssemblyName System.Drawing
        $bitmap = [Drawing.Bitmap]::new($screenshotPath)
        try {
            $measure = {
                param([string]$EvidenceRectangle)
                $match = [regex]::Match($EvidenceRectangle,
                    '(?:^|screen=)(?<x1>-?[0-9.]+),(?<y1>-?[0-9.]+)-(?<x2>-?[0-9.]+),(?<y2>-?[0-9.]+)')
                if (-not $match.Success) { throw "Invalid Unity screen rectangle: $EvidenceRectangle" }
                $culture = [Globalization.CultureInfo]::InvariantCulture
                $x1 = [Math]::Max(0, [int][Math]::Ceiling([double]::Parse($match.Groups['x1'].Value, $culture)) + 3)
                $x2 = [Math]::Min($bitmap.Width - 1, [int][Math]::Floor([double]::Parse($match.Groups['x2'].Value, $culture)) - 3)
                $unityY1 = [double]::Parse($match.Groups['y1'].Value, $culture)
                $unityY2 = [double]::Parse($match.Groups['y2'].Value, $culture)
                $y1 = [Math]::Max(0, $bitmap.Height - [int][Math]::Floor($unityY2) + 3)
                $y2 = [Math]::Min($bitmap.Height - 1, $bitmap.Height - [int][Math]::Ceiling($unityY1) - 3)
                if ($x2 -le $x1 -or $y2 -le $y1) { throw 'Screenshot rectangle has no measurable pixels.' }
                $colors = [Collections.Generic.HashSet[int]]::new()
                $minimum = 255
                $maximum = 0
                $samples = 0
                for ($y = $y1; $y -le $y2; $y += 2) {
                    for ($x = $x1; $x -le $x2; $x += 2) {
                        $pixel = $bitmap.GetPixel($x, $y)
                        [void]$colors.Add($pixel.ToArgb())
                        $luminance = [int][Math]::Round(0.2126 * $pixel.R + 0.7152 * $pixel.G + 0.0722 * $pixel.B)
                        $minimum = [Math]::Min($minimum, $luminance)
                        $maximum = [Math]::Max($maximum, $luminance)
                        $samples++
                    }
                }
                [pscustomobject]@{
                    rectangle = $EvidenceRectangle
                    sampleCount = $samples
                    distinctArgb = $colors.Count
                    minimumLuminance = $minimum
                    maximumLuminance = $maximum
                    luminanceRange = $maximum - $minimum
                }
            }
            $rowPixels = @($Result.uiRenderRowScreenRectangles | ForEach-Object { & $measure ([string]$_) })
            $detailsHeading = [string](@($Result.uiRenderDetailsEvidence)[0])
            $detailsPixels = & $measure $detailsHeading
            if (@($rowPixels | Where-Object { $_.distinctArgb -lt 4 -or $_.luminanceRange -lt 12 }).Count -ne 0 -or
                $detailsPixels.distinctArgb -lt 4 -or $detailsPixels.luminanceRange -lt 12) {
                throw 'Actual screenshot pixels do not show readable production row/details contrast.'
            }
            Write-KbpJsonAtomic (Join-Path $Request.evidenceDirectory 'live-row-pixel-evidence.json') ([ordered]@{
                schemaVersion = 1
                screenshotSha256 = [string]$Result.uiRenderScreenshotSha256
                expectedNames = @($Result.uiRenderExpectedNames)
                rowScreenRectangles = @($Result.uiRenderRowScreenRectangles)
                selectedRowName = [string]$Result.uiRenderSelectedRowName
                detailsTitleText = [string]$Result.uiRenderDetailsTitleText
                productionCanary = [string]$Result.uiRenderCanaryEvidence
                rowPixels = $rowPixels
                detailsTitlePixels = $detailsPixels
            })
        }
        finally { $bitmap.Dispose() }
    }
    if ($Request.scenario -in @('native-buff-catalog', 'final-no-save-core')) {
        $catalogPath = Join-Path $Request.evidenceDirectory 'native-buff-catalog.json'
        if (-not (Test-Path -LiteralPath $catalogPath -PathType Leaf)) { throw 'Native catalog evidence is missing.' }
        if ($Result.catalogSha256 -cne (Get-KbpSha256 $catalogPath)) { throw 'Native catalog hash mismatch.' }
        if ([int]$Result.catalogAbilityCount -le 0) { throw 'Native catalog is empty.' }
        $catalog = Read-KbpJson $catalogPath
        if ([int]$catalog.schemaVersion -ne 4 -or
            [string]$catalog.profile -cne [string]$Request.profileId -or
            [int]$catalog.abilityCount -ne [int]$Result.catalogAbilityCount -or
            @($catalog.abilities).Count -ne [int]$catalog.abilityCount) {
            throw 'Native catalog JSON contract does not reconcile with the runtime result.'
        }
        $missingExpressions = @($catalog.abilities | Where-Object {
            [string]::IsNullOrWhiteSpace([string]$_.expression.expressionType)
        })
        if ($missingExpressions.Count -ne 0) { throw 'Native catalog contains expressions without discriminators.' }
        $harmonyPath = Join-Path $Request.evidenceDirectory 'harmony-patch-inventory.json'
        if (-not (Test-Path -LiteralPath $harmonyPath -PathType Leaf)) { throw 'Harmony patch inventory evidence is missing.' }
        if ($Result.harmonyPatchInventorySha256 -cne (Get-KbpSha256 $harmonyPath)) {
            throw 'Harmony patch inventory hash mismatch.'
        }
        $harmonyInventory = Read-KbpJson $harmonyPath
        if ([int]$harmonyInventory.schemaVersion -ne 1 -or
            [string]$harmonyInventory.profileId -cne [string]$Request.profileId -or
            @($harmonyInventory.targets).Count -ne [int]$harmonyInventory.targetCount -or
            [int]$harmonyInventory.targetCount -ne [int]$Result.harmonyPatchTargetCount -or
            [int]$harmonyInventory.patchCount -ne [int]$Result.harmonyPatchRecordCount -or
            [int]$harmonyInventory.multiOwnerTargetCount -ne [int]$Result.harmonyMultiOwnerTargetCount -or
            [int]$harmonyInventory.buffPlannerOverlapTargetCount -ne 0 -or
            [int]$Result.harmonyBuffPlannerOverlapTargetCount -ne 0) {
            throw 'Harmony patch inventory contract does not reconcile with the runtime result.'
        }
        if ($Request.profileId -ceq 'call-of-the-wild') {
            if ([int]$Result.catalogOptionalAbilityCount -le 0 -or
                [int]$Result.catalogOptionalCandidateCount -le 0 -or
                [int]$Result.catalogOptionalIncludedCount -le 0 -or
                [int]$Result.catalogOptionalUnsupportedCount -ne 0 -or
                [int]$catalog.optionalAbilityCount -ne [int]$Result.catalogOptionalAbilityCount -or
                [int]$catalog.optionalCandidateCount -ne [int]$Result.catalogOptionalCandidateCount -or
                [int]$catalog.optionalIncludedCount -ne [int]$Result.catalogOptionalIncludedCount -or
                [int]$catalog.optionalUnsupportedCount -ne [int]$Result.catalogOptionalUnsupportedCount -or
                [int]$Result.harmonyPatchRecordCount -le 0) {
                throw 'Call of the Wild catalog ownership/support counts are invalid.'
            }
            foreach ($guid in @($Request.expectedBlueprintGuids)) {
                $matches = @($catalog.abilities | Where-Object {
                    $_.abilityGuid -ceq $guid -and $_.ownership -ceq 'call-of-the-wild' -and
                    $_.disposition -ceq 'include'
                })
                if ($matches.Count -ne 1) { throw "Expected optional blueprint is not uniquely included: $guid" }
            }
        }
    }
    if ($Request.scenario -ceq 'ui-root-smoke') {
        if ([int]$Result.uiRootCount -ne 1 -or [int]$Result.uiRenderedOpenFrames -le 0 -or
            [int]$Result.uiOpenCloseCycles -lt 21 -or
            [int]$Result.uiScreenWidth -le 0 -or [int]$Result.uiScreenHeight -le 0 -or
            [int]$Result.uiHudButtonCount -ne 4 -or
            [int]$Result.uiHudListenerCount -ne 4 -or
            [string]::IsNullOrWhiteSpace([string]$Result.uiHudAnchorPath) -or
            [string]::IsNullOrWhiteSpace([string]$Result.uiHudRaycastCanvasPath) -or
            [string]$Result.uiHudButtonOrder -cne 'Setup|Long|Important|Short' -or
            -not [bool]$Result.uiHudRowAboveNativeCluster -or
            -not [bool]$Result.uiHudHitboxesOwnRaycasts -or
            [int]$Result.uiHudUnderlyingNativeActivationCount -ne 0 -or
            [int]$Result.uiFullScreenRootCount -ne 1 -or
            -not [bool]$Result.uiFullScreenOpaque -or
            -not [bool]$Result.uiFullScreenBlocksRaycasts -or
            -not [bool]$Result.uiGraphicRaycasterPresent -or
            -not [bool]$Result.uiPresentationValid -or
            [double]$Result.uiPresentationCoverage -lt 0.98 -or
            -not [bool]$Result.uiPresentationOwnsCenterRaycast -or
            -not [string]::IsNullOrEmpty([string]$Result.uiPresentationFailure) -or
            [int]$Result.uiPresentationValidatedCount -le 0 -or
            [int]$Result.uiPresentationValidatedOrder -le 0 -or
            [int]$Result.uiInputLeaseAcquiredOrder -le [int]$Result.uiPresentationValidatedOrder -or
            [string]$Result.uiLifecycleState -cne 'Open' -or
            -not [bool]$Result.uiPlannerOpen -or
            -not [bool]$Result.uiFullScreenModeActive -or
            -not [bool]$Result.uiSelectionDisabled -or
            -not [bool]$Result.uiEventSystemPresent -or
            [int]$Result.uiInputLeaseAcquireCount -le 0 -or
            [int]$Result.uiInputLeaseReleaseCountAfterClose -ne [int]$Result.uiInputLeaseAcquireCount -or
            [bool]$Result.uiFullScreenModeActiveAfterClose -or
            [bool]$Result.uiSelectionDisabledAfterClose -ne [bool]$Result.uiSelectionDisabledBeforeOpen -or
            [bool]$Result.uiPausedAfterClose -ne [bool]$Result.uiPausedBeforeOpen -or
            [string]$Result.uiModeAfterClose -cne [string]$Result.uiModeBeforeOpen -or
            [int]$Result.uiPointerEventCount -lt 2 -or
            [int]$Result.uiScrollEventCount -lt 1 -or [int]$Result.uiDragEventCount -lt 2 -or
            [int]$Result.uiLongPointerEnterCount -ne 1 -or
            [int]$Result.uiLongPointerEventCount -ne 1 -or
            [int]$Result.uiLongListenerCount -ne 1 -or
            [int]$Result.uiLongGroupResolvedCount -ne 1 -or
            [int]$Result.uiLongPlanRevalidatedCount -ne 1 -or
            [int]$Result.uiLongExecutionInvokedCount -ne 0 -or
            [int]$Result.uiLongRefusalCount -ne 1 -or
            [int]$Result.uiLongResultPresentedCount -ne 1 -or
            [string]$Result.uiLongResultMessage -cne 'No Long buffs are configured.' -or
            -not ([string]$Result.uiSetupTooltip).Contains('Ctrl+Shift+B') -or
            -not ([string]$Result.uiLongTooltip).Contains('Long') -or
            [int]$Result.uiInputPlayerCommandCount -ne 0 -or
            [int]$Result.uiInputMovementCommandCount -ne 0 -or
            [int]$Result.uiInputAbilityCommandCount -ne 0 -or
            [int]$Result.uiInputSelectionEventCount -ne 0 -or
            [int]$Result.uiInputAbilityTargetEventCount -ne 0 -or
            -not [bool]$Result.uiInputSelectionUnchanged -or
            -not [bool]$Result.uiInputCameraUnchanged -or
            -not [bool]$Result.uiInputScrollConsumed -or
            -not [bool]$Result.uiInputCancelConsumed -or
            -not [bool]$Result.uiGroupSelectorChanged -or
            [int]$Result.uiReconstructionCount -ne 1) {
            throw 'Full-screen UI/input-isolation result is incomplete or invalid.'
        }
    }
    if ($Request.scenario -ceq 'ui-native-contract-probe') {
        $contractPath = Join-Path $Request.evidenceDirectory 'native-ui-contract.json'
        if (-not (Test-Path -LiteralPath $contractPath -PathType Leaf)) {
            throw 'Native UI contract evidence is missing.'
        }
        if ($Result.nativeUiContractSha256 -cne (Get-KbpSha256 $contractPath)) {
            throw 'Native UI contract hash mismatch.'
        }
        $contract = Read-KbpJson $contractPath
        if ([int]$contract.schemaVersion -ne 1 -or
            [string]::IsNullOrWhiteSpace([string]$contract.eventSystemPath) -or
            [string]::IsNullOrWhiteSpace([string]$contract.staticCanvasPath) -or
            [string]::IsNullOrWhiteSpace([string]$contract.serviceWindowTabsPath) -or
            @($contract.buttons).Count -ne [int]$Result.nativeUiButtonCount -or
            @($contract.buttons).Count -le 0 -or @($contract.raycasters).Count -le 0) {
            throw 'Native UI contract is incomplete or does not reconcile.'
        }
    }
    if ($Request.scenario -ceq 'performance-probe') {
        $performancePath = Join-Path $Request.evidenceDirectory 'performance-profile.json'
        if (-not (Test-Path -LiteralPath $performancePath -PathType Leaf)) {
            throw 'Performance profile evidence is missing.'
        }
        if ([string]$Result.performanceProfileSha256 -cne (Get-KbpSha256 $performancePath)) {
            throw 'Performance profile hash mismatch.'
        }
        $performance = Read-KbpJson $performancePath
        if ([int]$performance.schemaVersion -ne 1 -or
            [string]$performance.runId -cne [string]$Request.runId -or
            [string]$performance.version -cne [string]$BuildManifest.version -or
            [string]$performance.commit -cne [string]$BuildManifest.commit -or
            [bool]$performance.disableHudDiscovery -ne [bool]$Request.parameters.disableHudDiscovery -or
            [int]$performance.requestedDurationSeconds -ne [int]$Request.parameters.durationSeconds -or
            @($performance.samples).Count -lt 1 -or
            [int]$performance.totalFrameCount -lt 1 -or
            [int]$performance.qualifiedSampleCount -lt 1 -or
            -not [bool]$performance.meetsRequestedMinimum) {
            throw 'Performance profile is incomplete, mismatched, or below its requested minimum.'
        }
    }
}
