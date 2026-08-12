[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [ValidatePattern('^[A-Za-z0-9._-]{1,100}$')][string]$RunId = 'normalize-kbp-automation-working'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeAutomation.Common.ps1')

$pair = Get-KbpDisposableSavePair
$requiredBaseline = 'afca8ac5e42219bc50f428eb334a657cbcc2e31e8f2eb39c6ab53691cbb076d3'
if ($pair.baseline.sha256 -cne $requiredBaseline) {
    throw 'Immutable KBP_AUTOMATION_BASELINE hash does not match the authorized fixture.'
}
$root = Get-KbpRepositoryRoot
$lab = Split-Path -Parent (Split-Path -Parent $root)
$staging = Join-Path $lab "runtime-staging\save-repair-$RunId"
$backupRoot = Join-Path $lab "runtime-backups\save-repair-$RunId"
$evidenceRoot = Join-Path $lab "runtime-evidence\save-repair-$RunId"
foreach ($path in @($staging, $backupRoot, $evidenceRoot)) {
    if (Test-Path -LiteralPath $path) { throw "Save repair run path already exists: $path" }
}
if (-not $PSCmdlet.ShouldProcess($pair.working.path,
    'transactionally normalize the proven dangling main-character link in KBP_AUTOMATION_WORKING')) {
    Write-Host 'Working-save repair WhatIf PASS; no directory or save mutation occurred.'
    return
}

$archiveRoot = Join-Path $staging 'archive'
$backup = Join-Path $backupRoot $pair.working.fileName
$replacement = Join-Path $staging $pair.working.fileName
New-Item -ItemType Directory -Path $archiveRoot,$backupRoot,$evidenceRoot | Out-Null
Copy-Item -LiteralPath $pair.working.path -Destination $backup
$replaced = $false
try {
    [IO.Compression.ZipFile]::ExtractToDirectory($pair.working.path, $archiveRoot)
    $playerPath = Join-Path $archiveRoot 'player.json'
    $partyPath = Join-Path $archiveRoot 'party.json'
    $player = Get-Content -LiteralPath $playerPath -Raw
    $party = Get-Content -LiteralPath $partyPath -Raw
    $mainMatch = [regex]::Match($player,
        '"m_MainCharacter"\s*:\s*\{\s*"m_UniqueId"\s*:\s*"(?<id>[0-9a-f-]{36})"')
    if (-not $mainMatch.Success) { throw 'Working player.json has no exact main-character ID.' }
    $mainId = $mainMatch.Groups['id'].Value
    if ([regex]::Matches($party, '"UniqueId"\s*:\s*"' + [regex]::Escape($mainId) + '"').Count -ne 1 -or
        [regex]::Matches($party, '"IsLink"\s*:\s*true').Count -ne 1) {
        throw 'Working party.json does not have the single proven dangling-link shape.'
    }
    $areaEntries = @(Get-ChildItem -LiteralPath $archiveRoot -Filter '*.json' -File |
        Where-Object Name -notin @('header.json','player.json','party.json','statistic.json'))
    if ($areaEntries.Count -ne 1 -or
        (Get-Content -LiteralPath $areaEntries[0].FullName -Raw).Contains($mainId)) {
        throw 'The area/main-character link contract is not the proven repair case.'
    }
    $normalized = [regex]::Replace($party, '"IsLink"\s*:\s*true', '"IsLink":false', 1)
    [IO.File]::WriteAllText($partyPath, $normalized, [Text.UTF8Encoding]::new($false))
    [IO.Compression.ZipFile]::CreateFromDirectory($archiveRoot, $replacement,
        [IO.Compression.CompressionLevel]::Optimal, $false)
    if ((Get-KbpSha256 $pair.baseline.path) -cne $requiredBaseline) {
        throw 'Immutable baseline changed before replacement.'
    }
    Copy-Item -LiteralPath $replacement -Destination $pair.working.path -Force
    $replaced = $true
    $after = Get-KbpDisposableSavePair
    if ($after.baseline.sha256 -cne $requiredBaseline -or
        $after.working.name -cne 'KBP_AUTOMATION_WORKING' -or
        $after.working.gameId -cne $pair.working.gameId) {
        throw 'Post-repair disposable pair validation failed.'
    }
    $result = [ordered]@{
        schemaVersion = 1; runId = $RunId; status = 'Repaired'
        mainCharacterId = $mainId; normalization = 'party-main-character-IsLink:true-to-false'
        baselineSha256Before = $pair.baseline.sha256; baselineSha256After = $after.baseline.sha256
        workingSha256Before = $pair.working.sha256; workingSha256After = $after.working.sha256
        backupPath = $backup; completedAtUtc = [DateTime]::UtcNow.ToString('o')
    }
    Write-KbpJsonAtomic (Join-Path $evidenceRoot 'save-repair-result.json') $result
    Write-Host "Working-save repair PASS: $(Join-Path $evidenceRoot 'save-repair-result.json')"
}
catch {
    if ($replaced -and (Test-Path -LiteralPath $backup -PathType Leaf)) {
        Copy-Item -LiteralPath $backup -Destination $pair.working.path -Force
    }
    throw
}
