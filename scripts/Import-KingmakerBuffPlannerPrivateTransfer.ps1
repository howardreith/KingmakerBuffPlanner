[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$TransferZip,

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$LabRoot = 'C:\Dev\KingmakerBuffPlannerLab'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-FileSha256 {
    param([Parameter(Mandatory = $true)][string]$Path)
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

if (-not (Test-Path -LiteralPath $TransferZip -PathType Leaf)) {
    throw "Transfer ZIP not found: $TransferZip"
}
if (-not (Test-Path -LiteralPath $LabRoot -PathType Container)) {
    throw "Desktop lab root not found. Run Initialize-KingmakerBuffPlannerDesktop.ps1 first: $LabRoot"
}

$sidecar = $TransferZip + '.sha256'
if (Test-Path -LiteralPath $sidecar -PathType Leaf) {
    $expectedZipHash = ((Get-Content -LiteralPath $sidecar -Raw).Trim() -split '\s+')[0].ToLowerInvariant()
    $actualZipHash = Get-FileSha256 -Path $TransferZip
    if ($expectedZipHash -ne $actualZipHash) {
        throw "Transfer ZIP hash does not match its sidecar. Expected $expectedZipHash, actual $actualZipHash"
    }
}

$staging = Join-Path $env:TEMP ("KBP-private-import-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $staging -Force | Out-Null

try {
    Expand-Archive -LiteralPath $TransferZip -DestinationPath $staging -Force
    $manifestFiles = @(Get-ChildItem -LiteralPath $staging -Filter 'TRANSFER-MANIFEST.json' -File -Recurse)
    if ($manifestFiles.Count -ne 1) {
        throw "Expected exactly one TRANSFER-MANIFEST.json; found $($manifestFiles.Count)."
    }

    $manifestPath = $manifestFiles[0].FullName
    $payloadRoot = Split-Path -Parent $manifestPath
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    if ($manifest.schemaVersion -ne 1) {
        throw "Unsupported private transfer schema version: $($manifest.schemaVersion)"
    }

    $manifestPaths = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in $manifest.files) {
        $relative = [string]$entry.relativePath
        [void]$manifestPaths.Add($relative)

        $source = Join-Path $payloadRoot $relative
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
            throw "Manifest file is missing from extracted payload: $relative"
        }

        $actualLength = (Get-Item -LiteralPath $source).Length
        if ($actualLength -ne [Int64]$entry.length) {
            throw "Length mismatch for '$relative'. Expected $($entry.length), actual $actualLength"
        }

        $actualHash = Get-FileSha256 -Path $source
        if ($actualHash -ne ([string]$entry.sha256).ToLowerInvariant()) {
            throw "SHA-256 mismatch for '$relative'."
        }
    }

    $actualPayloadFiles = @(Get-ChildItem -LiteralPath $payloadRoot -File -Recurse -Force |
        Where-Object { $_.FullName -ne $manifestPath })

    foreach ($file in $actualPayloadFiles) {
        $relative = $file.FullName.Substring($payloadRoot.Length).TrimStart('\')
        if (-not $manifestPaths.Contains($relative)) {
            throw "Unexpected unmanifested file exists in transfer payload: $relative"
        }
    }

    $transferId = [string]$manifest.transferId
    if ([string]::IsNullOrWhiteSpace($transferId)) {
        throw 'Transfer manifest has no transferId.'
    }

    $copyPlan = New-Object System.Collections.Generic.List[object]
    $conflicts = New-Object System.Collections.Generic.List[string]

    foreach ($entry in $manifest.files) {
        $relative = [string]$entry.relativePath
        $source = Join-Path $payloadRoot $relative

        if ($relative.StartsWith('metadata\', [System.StringComparison]::OrdinalIgnoreCase) -or
            $relative -eq 'README-PRIVATE-TRANSFER.txt') {
            $destination = Join-Path $LabRoot ("incoming\laptop-transfer-metadata\$transferId\" + $relative)
        }
        else {
            $destination = Join-Path $LabRoot $relative
        }

        if (Test-Path -LiteralPath $destination -PathType Leaf) {
            $existingHash = Get-FileSha256 -Path $destination
            if ($existingHash -ne ([string]$entry.sha256).ToLowerInvariant()) {
                $conflicts.Add($destination)
            }
        }

        $copyPlan.Add([ordered]@{
            source = $source
            destination = $destination
            sha256 = ([string]$entry.sha256).ToLowerInvariant()
        })
    }

    if ($conflicts.Count -gt 0) {
        throw "Import would overwrite different existing files. Nothing was copied. Conflicts:`n$($conflicts -join [Environment]::NewLine)"
    }

    foreach ($copy in $copyPlan) {
        if (Test-Path -LiteralPath $copy.destination -PathType Leaf) {
            continue
        }

        $parent = Split-Path -Parent $copy.destination
        if (-not (Test-Path -LiteralPath $parent)) {
            New-Item -ItemType Directory -Path $parent -Force | Out-Null
        }
        Copy-Item -LiteralPath $copy.source -Destination $copy.destination
    }

    $manifestArchive = Join-Path $LabRoot "incoming\laptop-transfer-metadata\$transferId\TRANSFER-MANIFEST.json"
    $manifestParent = Split-Path -Parent $manifestArchive
    if (-not (Test-Path -LiteralPath $manifestParent)) {
        New-Item -ItemType Directory -Path $manifestParent -Force | Out-Null
    }
    Copy-Item -LiteralPath $manifestPath -Destination $manifestArchive -Force

    $report = [ordered]@{
        schemaVersion = 1
        importedAtUtc = [DateTime]::UtcNow.ToString('o')
        transferId = $transferId
        sourceZip = (Resolve-Path -LiteralPath $TransferZip).Path
        sourceZipSha256 = (Get-FileSha256 -Path $TransferZip)
        filesVerified = $manifest.files.Count
        filesCopiedOrAlreadyIdentical = $copyPlan.Count
        liveGameModified = $false
        liveModsModified = $false
        liveSavesModified = $false
    }
    $reportPath = Join-Path $LabRoot "incoming\laptop-transfer-metadata\$transferId\IMPORT-REPORT.json"
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $reportPath -Encoding UTF8

    Write-Host ''
    Write-Host 'Private transfer verified and imported.' -ForegroundColor Green
    Write-Host "Transfer ID:    $transferId"
    Write-Host "Files verified: $($manifest.files.Count)"
    Write-Host "Import report:  $reportPath"
    Write-Host ''
    Write-Host 'No live game, Mods directory, Steam process, or Saved Games directory was modified.'
}
finally {
    if (Test-Path -LiteralPath $staging) {
        Remove-Item -LiteralPath $staging -Recurse -Force
    }
}
