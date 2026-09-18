[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [ValidatePattern('^[A-Za-z0-9._-]{1,100}$')][string]$RunId = 'bootstrap-automation-fixture',
    # -SaveRoot/-BackupRoot exist for the source-test harness; defaults are
    # the exact production locations.
    [string]$SaveRoot = (Join-Path $env:USERPROFILE 'AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games'),
    [string]$BackupRoot,
    [switch]$Teardown
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

# Guarded bootstrap for the disposable automation fixture pair.
#
# Seed contract (the only accepted input):
#   - exactly one file named  Manual_<n>_KBP_AUTOMATION_SEED.zks
#   - whose header.json Name is exactly KBP_AUTOMATION_SEED
# The seed is created deliberately by the human through the ordinary in-game
# save dialog. Ordinary/valued campaigns never match this contract.
#
# Bootstrap creates:
#   Manual_<n+1>_KBP_AUTOMATION_BASELINE.zks  (sealed, byte-archived offline)
#   Manual_<n+2>_KBP_AUTOMATION_WORKING.zks   (the only mutable fixture)
# Both are copies of the seed with only header.json's Name rewritten, so the
# harness's exact pair discovery accepts them. Every other save must remain
# byte-identical; anything unexpected fails closed before mutation.

if ([string]::IsNullOrWhiteSpace($BackupRoot)) {
    $lab = Split-Path -Parent (Split-Path -Parent (Get-KbpRepositoryRoot))
    $BackupRoot = Join-Path $lab ("runtime-backups\automation-fixture\" + $RunId)
}
if (-not (Test-Path -LiteralPath $SaveRoot -PathType Container)) {
    throw "The exact Kingmaker save root is unavailable: $SaveRoot"
}

function Read-KbpSaveHeader {
    param([string]$Path)
    $archive = $null; $reader = $null
    try {
        $archive = [IO.Compression.ZipFile]::OpenRead($Path)
        $entry = $archive.GetEntry('header.json')
        if ($null -eq $entry) { throw "Save has no header.json: $(Split-Path -Leaf $Path)" }
        $reader = [IO.StreamReader]::new($entry.Open())
        return ($reader.ReadToEnd() | ConvertFrom-Json)
    }
    finally {
        if ($null -ne $reader) { $reader.Dispose() }
        if ($null -ne $archive) { $archive.Dispose() }
    }
}

function Copy-KbpSaveWithHeaderName {
    param([string]$SourcePath, [string]$DestinationPath, [string]$NewName)
    $temporary = $DestinationPath + '.kbp-tmp'
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force }
    $source = $null; $target = $null; $reader = $null; $writer = $null
    try {
        $source = [IO.Compression.ZipFile]::OpenRead($SourcePath)
        $target = [IO.Compression.ZipFile]::Open($temporary,
            [IO.Compression.ZipArchiveMode]::Create, [Text.Encoding]::UTF8)
        foreach ($entry in $source.Entries) {
            $newEntry = $target.CreateEntry($entry.FullName,
                [IO.Compression.CompressionLevel]::Optimal)
            if ($entry.FullName -ceq 'header.json') {
                $reader = [IO.StreamReader]::new($entry.Open())
                $header = $reader.ReadToEnd()
                $reader.Dispose(); $reader = $null
                $pattern = '"Name"\s*:\s*"KBP_AUTOMATION_SEED"'
                $matches = [regex]::Matches($header, $pattern)
                if ($matches.Count -ne 1) {
                    throw 'Seed header.json does not contain exactly one Name field to rewrite.'
                }
                $header = [regex]::Replace($header, $pattern, ('"Name":"' + $NewName + '"'))
                $writer = [IO.StreamWriter]::new($newEntry.Open(), [Text.Encoding]::UTF8)
                $writer.Write($header)
                $writer.Dispose(); $writer = $null
            }
            else {
                $inStream = $entry.Open()
                $outStream = $newEntry.Open()
                try { $inStream.CopyTo($outStream) }
                finally { $inStream.Dispose(); $outStream.Dispose() }
            }
        }
    }
    finally {
        if ($null -ne $reader) { $reader.Dispose() }
        if ($null -ne $writer) { $writer.Dispose() }
        if ($null -ne $source) { $source.Dispose() }
        if ($null -ne $target) { $target.Dispose() }
    }
    Move-KbpTempToFinal -Temporary $temporary -Destination $DestinationPath
}

function Move-KbpTempToFinal {
    param([string]$Temporary, [string]$Destination)
    if (Test-Path -LiteralPath $Destination) { throw "Refusing to overwrite: $Destination" }
    [IO.File]::Move($Temporary, $Destination)
}

Assert-KbpNotRunning -KnownProcessIds $null

if ($Teardown) {
    $baselineFiles = @(Get-ChildItem -LiteralPath $SaveRoot -Filter '*_KBP_AUTOMATION_BASELINE.zks' -File)
    $workingFiles = @(Get-ChildItem -LiteralPath $SaveRoot -Filter '*_KBP_AUTOMATION_WORKING.zks' -File)
    $protected = @(Get-ChildItem -LiteralPath $SaveRoot -Filter '*.zks' -File |
        Where-Object { $_.Name -notmatch '^Manual_[0-9]+_KBP_AUTOMATION_(BASELINE|WORKING)\.zks$' })
    if ($baselineFiles.Count -ne 1 -or $workingFiles.Count -ne 1) {
        throw "Teardown expects exactly one baseline and one working file; found $($baselineFiles.Count)/$($workingFiles.Count)."
    }
    foreach ($file in @($baselineFiles, $workingFiles)) {
        $header = Read-KbpSaveHeader -Path $file[0].FullName
        if ($file[0].Name -notmatch '^Manual_[0-9]+_KBP_AUTOMATION_(BASELINE|WORKING)\.zks$' -or
            $header.Name -cne ($file[0].Name -replace '^Manual_[0-9]+_', '' -replace '\.zks$', '')) {
            throw "Refusing to delete a file that does not match the exact fixture contract: $($file[0].Name)"
        }
    }
    if (-not $PSCmdlet.ShouldProcess(($baselineFiles + $workingFiles | ForEach-Object FullName) -join ', ',
        'Remove the disposable automation fixture pair')) {
        Write-Host 'Fixture teardown WhatIf PASS; no save was removed.'
        return
    }
    $beforeProtected = @($protected | ForEach-Object { "$($_.Name):$(Get-KbpSha256 $_.FullName)" })
    Remove-Item -LiteralPath $baselineFiles[0].FullName -Force
    Remove-Item -LiteralPath $workingFiles[0].FullName -Force
    $afterProtected = @(Get-ChildItem -LiteralPath $SaveRoot -Filter '*.zks' -File |
        Where-Object { $_.Name -notmatch '^Manual_[0-9]+_KBP_AUTOMATION_(BASELINE|WORKING)\.zks$' } |
        ForEach-Object { "$($_.Name):$(Get-KbpSha256 $_.FullName)" })
    if (($beforeProtected -join '`n') -cne ($afterProtected -join '`n')) {
        throw 'Teardown touched a protected save; investigate immediately.'
    }
    Write-Host "Fixture teardown PASS: removed the disposable pair; $($protected.Count) other save(s) byte-identical."
    return
}

# --- Bootstrap ---
$automationFiles = @(Get-ChildItem -LiteralPath $SaveRoot -Filter '*KBP_AUTOMATION*' -File |
    Where-Object { $_.Name -notmatch '^Manual_[0-9]+_KBP_AUTOMATION_SEED\.zks$' })
if ($automationFiles.Count -ne 0) {
    throw "Existing KBP_AUTOMATION artifact(s) present: $(($automationFiles | ForEach-Object Name) -join ', '). Tear down or clear them explicitly first."
}
$seedFiles = @(Get-ChildItem -LiteralPath $SaveRoot -Filter '*_KBP_AUTOMATION_SEED.zks' -File)
if ($seedFiles.Count -ne 1) {
    throw "Expected exactly one KBP_AUTOMATION_SEED save; found $($seedFiles.Count). Create one disposable campaign save named exactly KBP_AUTOMATION_SEED, then rerun."
}
$seed = $seedFiles[0]
if ($seed.Name -notmatch '^Manual_[0-9]+_KBP_AUTOMATION_SEED\.zks$') {
    throw "Seed file name is not the exact in-game shape: $($seed.Name)"
}
$seedHeader = Read-KbpSaveHeader -Path $seed.FullName
if ([string]$seedHeader.Name -cne 'KBP_AUTOMATION_SEED') {
    throw "Seed header Name is not exactly KBP_AUTOMATION_SEED: $($seedHeader.Name)"
}

$allSaves = @(Get-ChildItem -LiteralPath $SaveRoot -Filter '*.zks' -File)
$maxIndex = 0
foreach ($file in $allSaves) {
    if ($file.Name -match '^Manual_([0-9]+)_') {
        $index = [int]$Matches[1]
        if ($index -gt $maxIndex) { $maxIndex = $index }
    }
}
$baselineName = 'Manual_' + ($maxIndex + 1) + '_KBP_AUTOMATION_BASELINE.zks'
$workingName = 'Manual_' + ($maxIndex + 2) + '_KBP_AUTOMATION_WORKING.zks'
$baselinePath = Join-Path $SaveRoot $baselineName
$workingPath = Join-Path $SaveRoot $workingName
$protectedBefore = @{}
foreach ($file in $allSaves) { $protectedBefore[$file.Name] = Get-KbpSha256 $file.FullName }

if (-not $PSCmdlet.ShouldProcess($SaveRoot,
    "create the disposable fixture pair $baselineName and $workingName from the seed")) {
    Write-Host "Fixture bootstrap WhatIf PASS: would seal $baselineName and $workingName from $($seed.Name)."
    return
}

$seedHash = Get-KbpSha256 $seed.FullName
Copy-KbpSaveWithHeaderName -SourcePath $seed.FullName -DestinationPath $baselinePath -NewName 'KBP_AUTOMATION_BASELINE'
Copy-KbpSaveWithHeaderName -SourcePath $seed.FullName -DestinationPath $workingPath -NewName 'KBP_AUTOMATION_WORKING'

foreach ($pair in @(
        @{ Path = $baselinePath; Expected = 'KBP_AUTOMATION_BASELINE' },
        @{ Path = $workingPath; Expected = 'KBP_AUTOMATION_WORKING' })) {
    $header = Read-KbpSaveHeader -Path $pair.Path
    if ([string]$header.Name -cne $pair.Expected -or
        [string]$header.GameName -cne [string]$seedHeader.GameName -or
        [string]$header.GameId -cne [string]$seedHeader.GameId) {
        throw "Created fixture failed descriptor verification: $(Split-Path -Leaf $pair.Path)"
    }
}

foreach ($file in $allSaves) {
    if ((Get-KbpSha256 $file.FullName) -cne $protectedBefore[$file.Name]) {
        throw "A protected save changed during bootstrap: $($file.Name)"
    }
}

New-Item -ItemType Directory -Path $BackupRoot -Force | Out-Null
$seedBackup = Join-Path $BackupRoot $seed.Name
$baselineBackup = Join-Path $BackupRoot $baselineName
Copy-Item -LiteralPath $seed.FullName -Destination $seedBackup
Copy-Item -LiteralPath $baselinePath -Destination $baselineBackup
$manifest = [ordered]@{
    schemaVersion = 1
    runId = $RunId
    createdAtUtc = [DateTime]::UtcNow.ToString('o')
    seed = @{ fileName = $seed.Name; sha256 = $seedHash }
    baseline = @{ fileName = $baselineName; sha256 = Get-KbpSha256 $baselinePath }
    working = @{ fileName = $workingName; sha256 = Get-KbpSha256 $workingPath }
    seedBackupSha256 = Get-KbpSha256 $seedBackup
    baselineBackupSha256 = Get-KbpSha256 $baselineBackup
    gameName = [string]$seedHeader.GameName
    gameId = [string]$seedHeader.GameId
}
$manifestPath = Join-Path $BackupRoot 'fixture-manifest.json'
Write-KbpJsonAtomic $manifestPath $manifest

Write-Host "Fixture bootstrap PASS:"
Write-Host "  baseline $baselineName sha256=$($manifest.baseline.sha256) (sealed; offline copy archived)"
Write-Host "  working  $workingName sha256=$($manifest.working.sha256) (only mutable fixture)"
Write-Host "  seed     $($seed.Name) untouched; $($allSaves.Count) pre-existing save(s) byte-identical"
Write-Host "  archive  $BackupRoot"
