[CmdletBinding()]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$Repository = 'howardreith/KingmakerBuffPlanner',

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$LabRoot = 'C:\Dev\KingmakerBuffPlannerLab',

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$FeatureBranch = 'codex/kingmaker-buff-planner',

    [Parameter()]
    [string]$TransferZip = '',

    [switch]$SkipReferenceSourceClone
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-Command {
    param([Parameter(Mandatory = $true)][string]$Name)

    if ($null -eq (Get-Command $Name -ErrorAction SilentlyContinue | Select-Object -First 1)) {
        throw "Required command not found on PATH: $Name"
    }
}

function Test-ExternalCommand {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter()][string[]]$Arguments = @(),
        [Parameter()][string]$WorkingDirectory = $null
    )

    $priorLocation = Get-Location
    $priorErrorActionPreference = $ErrorActionPreference
    try {
        if (-not [string]::IsNullOrWhiteSpace($WorkingDirectory)) {
            Set-Location -LiteralPath $WorkingDirectory
        }

        $ErrorActionPreference = 'SilentlyContinue'
        & $FilePath @Arguments *> $null
        return ($LASTEXITCODE -eq 0)
    }
    finally {
        $ErrorActionPreference = $priorErrorActionPreference
        Set-Location -LiteralPath $priorLocation
    }
}
Assert-Command -Name 'git'
Assert-Command -Name 'gh'

& gh auth status
if ($LASTEXITCODE -ne 0) {
    throw 'GitHub CLI is not authenticated on the desktop. Run: gh auth login'
}

if (-not (Test-Path -LiteralPath $LabRoot -PathType Container)) {
    throw "Desktop lab does not exist. Run Initialize-KingmakerBuffPlannerDesktop.ps1 first: $LabRoot"
}

$repositoryRoot = Join-Path $LabRoot 'repo\KingmakerBuffPlanner'
$gitDirectory = Join-Path $repositoryRoot '.git'

if (-not (Test-Path -LiteralPath $gitDirectory)) {
    if (Test-Path -LiteralPath $repositoryRoot) {
        $children = @(Get-ChildItem -LiteralPath $repositoryRoot -Force)
        if ($children.Count -gt 0) {
            throw "Repository path is not empty and is not a Git checkout: $repositoryRoot"
        }
    }
    else {
        New-Item -ItemType Directory -Path (Split-Path -Parent $repositoryRoot) -Force | Out-Null
    }

    & git clone --branch $FeatureBranch "https://github.com/$Repository.git" $repositoryRoot
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to clone the standalone repository feature branch.'
    }
}
else {
    $origin = (& git -C $repositoryRoot remote get-url origin).Trim()
    if ($origin -notmatch [Regex]::Escape($Repository)) {
        throw "Existing checkout origin '$origin' does not match '$Repository'."
    }

    $status = @(& git -C $repositoryRoot status --short)
    if ($status.Count -gt 0) {
        throw "Existing desktop checkout is dirty. Refusing to change branches:`n$($status -join [Environment]::NewLine)"
    }

    & git -C $repositoryRoot fetch origin --prune
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to fetch the standalone repository.'
    }

    if (-not (Test-ExternalCommand -FilePath 'git' -Arguments @(
        '-C', $repositoryRoot,
        'switch', $FeatureBranch
    ))) {
        & git -C $repositoryRoot switch -c $FeatureBranch --track "origin/$FeatureBranch"
        if ($LASTEXITCODE -ne 0) {
            throw "Unable to check out feature branch: $FeatureBranch"
        }
    }

    & git -C $repositoryRoot pull --ff-only origin $FeatureBranch
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to fast-forward the desktop feature branch.'
    }
}

$configRoot = Join-Path $repositoryRoot 'config'
$codexHome = Join-Path $LabRoot 'codex-home'
$rulesRoot = Join-Path $codexHome 'rules'
New-Item -ItemType Directory -Path $rulesRoot -Force | Out-Null

$configSource = Join-Path $configRoot 'codex-config.template.toml'
$rulesSource = Join-Path $configRoot 'codex-rules.template.rules'
$agentsSource = Join-Path $configRoot 'CODEX-HOME-AGENTS.template.md'

foreach ($required in @($configSource, $rulesSource, $agentsSource)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
        throw "Required desktop Codex template not found in repository: $required"
    }
}

$configDestination = Join-Path $codexHome 'config.toml'
$rulesDestination = Join-Path $rulesRoot 'default.rules'
$agentsDestination = Join-Path $codexHome 'AGENTS.md'

foreach ($pair in @(
    @($configSource, $configDestination),
    @($rulesSource, $rulesDestination),
    @($agentsSource, $agentsDestination)
)) {
    $source = $pair[0]
    $destination = $pair[1]

    if (Test-Path -LiteralPath $destination -PathType Leaf) {
        $sourceHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
        $destinationHash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
        if ($sourceHash -ne $destinationHash) {
            throw "Existing Codex configuration differs from the repository template. Review it manually rather than overwriting: $destination"
        }
    }
    else {
        Copy-Item -LiteralPath $source -Destination $destination
    }
}

$env:CODEX_HOME = $codexHome
[Environment]::SetEnvironmentVariable('CODEX_HOME', $codexHome, 'User')

if (-not [string]::IsNullOrWhiteSpace($TransferZip)) {
    $importScript = Join-Path $repositoryRoot 'scripts\Import-KingmakerBuffPlannerPrivateTransfer.ps1'
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $importScript -TransferZip $TransferZip -LabRoot $LabRoot
    if ($LASTEXITCODE -ne 0) {
        throw 'Private transfer import failed.'
    }
}

if (-not $SkipReferenceSourceClone) {
    $referenceScript = Join-Path $repositoryRoot 'scripts\Get-KingmakerBuffPlannerReferenceSources.ps1'
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $referenceScript -LabRoot $LabRoot
    if ($LASTEXITCODE -ne 0) {
        throw 'Public reference-source setup failed.'
    }
}

$preflightScript = Join-Path $repositoryRoot 'scripts\Initialize-KingmakerBuffPlannerDesktop.ps1'
if (Test-Path -LiteralPath $preflightScript -PathType Leaf) {
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $preflightScript -LabRoot $LabRoot
    if ($LASTEXITCODE -ne 0) {
        throw 'Desktop environment intake refresh failed.'
    }
}

$head = (& git -C $repositoryRoot rev-parse HEAD).Trim()
$statusFinal = @(& git -C $repositoryRoot status --short)
if ($statusFinal.Count -gt 0) {
    throw "Desktop checkout is unexpectedly dirty after setup:`n$($statusFinal -join [Environment]::NewLine)"
}

Write-Host ''
Write-Host 'Standalone desktop checkout is ready.' -ForegroundColor Green
Write-Host "Repository: $repositoryRoot"
Write-Host "Branch:     $FeatureBranch"
Write-Host "HEAD:       $head"
Write-Host "CODEX_HOME: $codexHome"
Write-Host ''
Write-Host 'Open this repository in the Codex desktop app, or run:'
Write-Host "  codex --cd `"$repositoryRoot`""
Write-Host ''
Write-Host 'No Steam process, live Mods directory, or live save was modified.'


