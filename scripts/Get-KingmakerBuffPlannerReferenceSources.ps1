[CmdletBinding()]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$LabRoot = 'C:\Dev\KingmakerBuffPlannerLab'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($null -eq (Get-Command git -ErrorAction SilentlyContinue | Select-Object -First 1)) {
    throw 'git is required and was not found on PATH.'
}

$referenceRoot = Join-Path $LabRoot 'reference-source'
if (-not (Test-Path -LiteralPath $referenceRoot)) {
    New-Item -ItemType Directory -Path $referenceRoot -Force | Out-Null
}

$repositories = @(
    [ordered]@{ name = 'BubbleBuffs'; url = 'https://github.com/factubsio/BubbleBuffs.git' },
    [ordered]@{ name = 'BuffIt2TheLimit'; url = 'https://github.com/Gh05d/wrath-epic-buffing.git' },
    [ordered]@{ name = 'PathfinderAutoBuff'; url = 'https://github.com/ilkar399/PathfinderAutoBuff.git' },
    [ordered]@{ name = 'KingmakerRebalance'; url = 'https://github.com/Holic75/KingmakerRebalance.git' }
)

$inventory = New-Object System.Collections.Generic.List[object]

foreach ($repository in $repositories) {
    $destination = Join-Path $referenceRoot $repository.name
    $gitDirectory = Join-Path $destination '.git'

    if (-not (Test-Path -LiteralPath $gitDirectory)) {
        if (Test-Path -LiteralPath $destination) {
            $children = @(Get-ChildItem -LiteralPath $destination -Force)
            if ($children.Count -gt 0) {
                Write-Warning "Reference destination already contains an immutable transferred snapshot; leaving it unchanged: $destination"
            }
            else {
                & git clone $repository.url $destination
                if ($LASTEXITCODE -ne 0) {
                    throw "Unable to clone reference repository: $($repository.url)"
                }
            }
        }
        else {
            & git clone $repository.url $destination
            if ($LASTEXITCODE -ne 0) {
                throw "Unable to clone reference repository: $($repository.url)"
            }
        }
    }

    if (Test-Path -LiteralPath $gitDirectory) {
        $status = @(& git -C $destination status --short)
        if ($status.Count -gt 0) {
            throw "Reference repository is dirty. It must remain read-only: $destination"
        }

        $inventory.Add([ordered]@{
            name = $repository.name
            url = $repository.url
            mode = 'git-clone'
            path = $destination
            head = (& git -C $destination rev-parse HEAD).Trim()
            branch = (& git -C $destination branch --show-current).Trim()
        })
    }
    else {
        $files = @(Get-ChildItem -LiteralPath $destination -File -Recurse -Force)
        $inventory.Add([ordered]@{
            name = $repository.name
            url = $repository.url
            mode = 'immutable-transferred-snapshot'
            path = $destination
            fileCount = $files.Count
            head = $null
            branch = $null
        })
    }
}

$manifest = [ordered]@{
    schemaVersion = 1
    generatedAtUtc = [DateTime]::UtcNow.ToString('o')
    references = $inventory
    policy = @(
        'Reference repositories are read-only.',
        'Do not add them as project references or Git submodules.',
        'Record exact source and license when adapting code.',
        'Do not ship their binaries or art assets in the Buff Planner package.'
    )
}
$manifestPath = Join-Path $referenceRoot 'REFERENCE-SOURCE-MANIFEST.json'
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

Write-Host ''
Write-Host 'Public reference source is available.' -ForegroundColor Green
Write-Host "Manifest: $manifestPath"
