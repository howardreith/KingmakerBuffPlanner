[CmdletBinding()]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$SourceLabRoot = 'C:\Dev\KingmakerGunslingerLab',

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$SourceRepository = 'C:\Dev\KingmakerGunslingerLab\repo\KingmakerGunslinger',

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$BuffLabRoot = 'C:\Dev\KingmakerBuffPlannerLab',

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$KingmakerInstallDir = 'C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Kingmaker',

    [Parameter()]
    [string]$DestinationZip = '',

    [Parameter()]
    [string]$TabletopPackagePath = '',

    [Parameter()]
    [string]$UnityModManagerArchive = '',

    [Parameter()]
    [string]$DisposableSavePath = '',

    [Parameter()]
    [string[]]$AdditionalPackagePaths = @(),

    [Parameter()]
    [string[]]$AdditionalReferenceRepositories = @()
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
function Get-RelativePath {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$Path
    )

    $normalizedRoot = (Resolve-Path -LiteralPath $Root).Path.TrimEnd('\')
    $normalizedPath = (Resolve-Path -LiteralPath $Path).Path
    if (-not $normalizedPath.StartsWith($normalizedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Path '$normalizedPath' is not beneath root '$normalizedRoot'."
    }

    return $normalizedPath.Substring($normalizedRoot.Length).TrimStart('\')
}

function Test-ExcludedRelativePath {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $segments = $RelativePath -split '[\\/]'
    $excludedDirectories = @(
        '.git', '.vs', '.idea', 'bin', 'obj', 'artifacts',
        'runtime-evidence', 'runtime-backups', 'runtime-staging',
        'TestResults', 'packages', 'node_modules'
    )

    foreach ($segment in $segments) {
        if ($segment -in $excludedDirectories) {
            return $true
        }
    }

    $leaf = [System.IO.Path]::GetFileName($RelativePath)
    if ($leaf -match '^(auth\.json|credentials.*|known_hosts|id_rsa|id_ed25519)$') {
        return $true
    }
    if ($leaf -match '\.(user|suo|pfx|p12|key)$') {
        return $true
    }

    return $false
}

function Copy-FilteredTree {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    $sourceRoot = (Resolve-Path -LiteralPath $Source).Path.TrimEnd('\')
    $files = @(Get-ChildItem -LiteralPath $sourceRoot -File -Recurse -Force | Sort-Object FullName)
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($sourceRoot.Length).TrimStart('\')
        if (Test-ExcludedRelativePath -RelativePath $relative) {
            continue
        }

        $target = Join-Path $Destination $relative
        $parent = Split-Path -Parent $target
        if (-not (Test-Path -LiteralPath $parent)) {
            New-Item -ItemType Directory -Path $parent -Force | Out-Null
        }
        Copy-Item -LiteralPath $file.FullName -Destination $target -Force
    }
}

function Copy-ExactFile {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    if (-not (Test-Path -LiteralPath $Source -PathType Leaf)) {
        throw "Required file not found: $Source"
    }

    $parent = Split-Path -Parent $Destination
    if (-not (Test-Path -LiteralPath $parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }
    Copy-Item -LiteralPath $Source -Destination $Destination -Force
}

function Get-GitSnapshotMetadata {
    param([Parameter(Mandatory = $true)][string]$RepositoryPath)

    if (-not (Test-ExternalCommand -FilePath 'git' -Arguments @(
        '-C', $RepositoryPath,
        'rev-parse', '--is-inside-work-tree'
    ))) {
        return $null
    }

    $head = (& git -C $RepositoryPath rev-parse HEAD).Trim()
    $branch = (& git -C $RepositoryPath branch --show-current).Trim()
    $status = @(& git -C $RepositoryPath status --short)

    return [ordered]@{
        path = (Resolve-Path -LiteralPath $RepositoryPath).Path
        head = $head
        branch = $branch
        dirty = ($status.Count -gt 0)
        status = $status
    }
}

function Export-GitSnapshot {
    param(
        [Parameter(Mandatory = $true)][string]$RepositoryPath,
        [Parameter(Mandatory = $true)][string]$Destination,
        [Parameter(Mandatory = $true)][string]$LogicalName
    )

    $metadata = Get-GitSnapshotMetadata -RepositoryPath $RepositoryPath
    if ($null -eq $metadata) {
        Copy-FilteredTree -Source $RepositoryPath -Destination $Destination
        return [ordered]@{
            logicalName = $LogicalName
            sourcePath = (Resolve-Path -LiteralPath $RepositoryPath).Path
            mode = 'filtered-copy-non-git'
            git = $null
        }
    }

    $archive = Join-Path $env:TEMP ("KBP-reference-" + [Guid]::NewGuid().ToString('N') + '.zip')
    try {
        & git -C $RepositoryPath archive --format=zip --output=$archive HEAD
        if ($LASTEXITCODE -ne 0) {
            throw "git archive failed for reference repository: $RepositoryPath"
        }

        if (-not (Test-Path -LiteralPath $Destination)) {
            New-Item -ItemType Directory -Path $Destination -Force | Out-Null
        }
        Expand-Archive -LiteralPath $archive -DestinationPath $Destination -Force
    }
    finally {
        if (Test-Path -LiteralPath $archive -PathType Leaf) {
            Remove-Item -LiteralPath $archive -Force
        }
    }

    return [ordered]@{
        logicalName = $LogicalName
        sourcePath = (Resolve-Path -LiteralPath $RepositoryPath).Path
        mode = 'git-archive-head'
        git = $metadata
    }
}

function Find-FirstExistingPath {
    param([Parameter(Mandatory = $true)][string[]]$Candidates)

    foreach ($candidate in $Candidates) {
        if (-not [string]::IsNullOrWhiteSpace($candidate) -and (Test-Path -LiteralPath $candidate -PathType Container)) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    return $null
}

function Get-SafeFileFingerprint {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return [ordered]@{
            path = $Path
            exists = $false
            length = $null
            fileVersion = $null
            assemblyVersion = $null
            sha256 = $null
        }
    }

    $item = Get-Item -LiteralPath $Path
    $fileVersion = $null
    $assemblyVersion = $null
    try { $fileVersion = $item.VersionInfo.FileVersion } catch { $fileVersion = $null }
    try { $assemblyVersion = [System.Reflection.AssemblyName]::GetAssemblyName($item.FullName).Version.ToString() } catch { $assemblyVersion = $null }

    return [ordered]@{
        path = $item.FullName
        exists = $true
        length = $item.Length
        fileVersion = $fileVersion
        assemblyVersion = $assemblyVersion
        sha256 = (Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}

Assert-Command -Name 'git'

if (-not (Test-Path -LiteralPath $SourceRepository -PathType Container)) {
    throw "Source Kingmaker repository not found: $SourceRepository"
}
if (-not (Test-Path -LiteralPath $BuffLabRoot -PathType Container)) {
    throw "Buff Planner lab root not found. Run the initialization script first: $BuffLabRoot"
}

if ([string]::IsNullOrWhiteSpace($DestinationZip)) {
    $stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
    $DestinationZip = Join-Path $env:USERPROFILE "Desktop\KingmakerBuffPlanner-PrivateTransfer-$stamp.zip"
}
if (Test-Path -LiteralPath $DestinationZip) {
    throw "Destination already exists; refusing to overwrite: $DestinationZip"
}

$staging = Join-Path $env:TEMP ("KBP-private-transfer-" + [Guid]::NewGuid().ToString('N'))
$payload = Join-Path $staging 'KingmakerBuffPlannerPrivateTransfer'
$metadataRoot = Join-Path $payload 'metadata'
New-Item -ItemType Directory -Path $payload -Force | Out-Null
New-Item -ItemType Directory -Path $metadataRoot -Force | Out-Null

$warnings = New-Object System.Collections.Generic.List[string]
$referenceSnapshots = New-Object System.Collections.Generic.List[object]
$packageSources = New-Object System.Collections.Generic.List[object]

try {
    # 1. Selected, read-only harness patterns from the current Kingmaker lab.
    $harnessDestination = Join-Path $payload 'harness-reference\KingmakerAutomationReference'
    New-Item -ItemType Directory -Path $harnessDestination -Force | Out-Null

    $harnessItems = @(
        'AGENTS.md',
        'AUTONOMOUS-RESUME.md',
        'AUTONOMOUS-BLOCKERS.md',
        'Directory.Build.props',
        'Info.json',
        'docs\ARCHITECTURE.md',
        'docs\TECHNICAL_BASELINE.md',
        'docs\WIN10-AUTONOMOUS-RUNTIME-TESTING.md',
        'docs\WORKING-SAVE-SMOKE.md',
        'scripts\Build-Local.ps1',
        'scripts\build.ps1',
        'scripts\package.ps1',
        'scripts\validate-build-output.ps1',
        'scripts\validate-package.ps1',
        'scripts\validate-repository.ps1',
        'scripts\Invoke-KingmakerRuntimeTest.ps1',
        'scripts\RuntimeAutomation.Common.ps1',
        'scripts\RuntimeHarness.Common.ps1',
        'scripts\Test-RuntimeRequest.ps1',
        'scripts\Test-RuntimeResult.ps1',
        'scripts\compatibility',
        'compatibility',
        'src\KingmakerGunslinger\RuntimeTesting'
    )

    foreach ($relative in $harnessItems) {
        $source = Join-Path $SourceRepository $relative
        if (-not (Test-Path -LiteralPath $source)) {
            $warnings.Add("Harness reference item not found: $relative")
            continue
        }

        $destination = Join-Path $harnessDestination $relative
        if (Test-Path -LiteralPath $source -PathType Container) {
            Copy-FilteredTree -Source $source -Destination $destination
        }
        else {
            Copy-ExactFile -Source $source -Destination $destination
        }
    }

    $sourceGit = Get-GitSnapshotMetadata -RepositoryPath $SourceRepository
    $sourceGit | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $metadataRoot 'source-kingmaker-repository.json') -Encoding UTF8

    # 2. Laptop's Buff Planner intake, useful only as comparative metadata.
    $laptopBuffIntake = Join-Path $BuffLabRoot 'environment-intake.json'
    if (Test-Path -LiteralPath $laptopBuffIntake -PathType Leaf) {
        Copy-ExactFile -Source $laptopBuffIntake -Destination (Join-Path $metadataRoot 'laptop-buff-planner-environment-intake.json')
    }

    # 3. Environment fingerprints only; proprietary game/UMM DLL bytes are not copied.
    $managed = Join-Path $KingmakerInstallDir 'Kingmaker_Data\Managed'
    $umm = Join-Path $managed 'UnityModManager'
    $environment = [ordered]@{
        schemaVersion = 1
        generatedAtUtc = [DateTime]::UtcNow.ToString('o')
        computerName = $env:COMPUTERNAME
        sourceLabRoot = $SourceLabRoot
        sourceRepository = $SourceRepository
        kingmakerInstallDir = $KingmakerInstallDir
        files = @(
            (Get-SafeFileFingerprint -Path (Join-Path $KingmakerInstallDir 'Kingmaker.exe')),
            (Get-SafeFileFingerprint -Path (Join-Path $managed 'Assembly-CSharp.dll')),
            (Get-SafeFileFingerprint -Path (Join-Path $managed 'Assembly-CSharp-firstpass.dll')),
            (Get-SafeFileFingerprint -Path (Join-Path $managed 'Newtonsoft.Json.dll')),
            (Get-SafeFileFingerprint -Path (Join-Path $managed 'UnityEngine.dll')),
            (Get-SafeFileFingerprint -Path (Join-Path $managed 'UnityEngine.UI.dll')),
            (Get-SafeFileFingerprint -Path (Join-Path $umm 'UnityModManager.dll')),
            (Get-SafeFileFingerprint -Path (Join-Path $umm '0Harmony12.dll'))
        )
    }
    $environment | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $metadataRoot 'laptop-environment-fingerprints.json') -Encoding UTF8

    # 4. Public source snapshots if the laptop already has them. Missing ones are cloned on the desktop.
    $referenceCandidates = [ordered]@{
        BubbleBuffs = @(
            (Join-Path $SourceLabRoot 'reference-source\BubbleBuffs'),
            (Join-Path $SourceLabRoot 'private\postbase\mods\source\BubbleBuffs'),
            'C:\Dev\BubbleBuffs'
        )
        BuffIt2TheLimit = @(
            (Join-Path $SourceLabRoot 'reference-source\wrath-epic-buffing'),
            (Join-Path $SourceLabRoot 'private\postbase\mods\source\wrath-epic-buffing'),
            'C:\Dev\wrath-epic-buffing'
        )
        PathfinderAutoBuff = @(
            (Join-Path $SourceLabRoot 'reference-source\PathfinderAutoBuff'),
            (Join-Path $SourceLabRoot 'private\postbase\mods\source\PathfinderAutoBuff'),
            'C:\Dev\PathfinderAutoBuff'
        )
        KingmakerRebalance = @(
            (Join-Path $SourceLabRoot 'private\postbase\mods\source\KingmakerRebalance'),
            (Join-Path $SourceLabRoot 'examples\KingmakerRebalance-master'),
            (Join-Path $SourceLabRoot 'reference-source\KingmakerRebalance')
        )
    }

    foreach ($entry in $referenceCandidates.GetEnumerator()) {
        $source = Find-FirstExistingPath -Candidates $entry.Value
        if ($null -eq $source) {
            $warnings.Add("Public reference source not found locally; desktop clone script will obtain it: $($entry.Key)")
            continue
        }

        $destination = Join-Path $payload ("reference-source\" + $entry.Key)
        $snapshot = Export-GitSnapshot -RepositoryPath $source -Destination $destination -LogicalName $entry.Key
        $referenceSnapshots.Add($snapshot)
    }

    foreach ($additional in $AdditionalReferenceRepositories) {
        if ([string]::IsNullOrWhiteSpace($additional)) {
            continue
        }
        if (-not (Test-Path -LiteralPath $additional -PathType Container)) {
            throw "Additional reference repository not found: $additional"
        }

        $logicalName = [System.IO.Path]::GetFileName((Resolve-Path -LiteralPath $additional).Path.TrimEnd('\'))
        $destination = Join-Path $payload ("reference-source\Additional\" + $logicalName)
        $snapshot = Export-GitSnapshot -RepositoryPath $additional -Destination $destination -LogicalName $logicalName
        $referenceSnapshots.Add($snapshot)
    }

    $referenceSnapshots | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $metadataRoot 'reference-snapshots.json') -Encoding UTF8

    # 5. Exact Call of the Wild runtime candidates already held by the laptop lab.
    $packageDirectory = Join-Path $SourceLabRoot 'private\postbase\mods\packages'
    if (Test-Path -LiteralPath $packageDirectory -PathType Container) {
        $cotwPackages = @(Get-ChildItem -LiteralPath $packageDirectory -File -Recurse |
            Where-Object { $_.Name -match '(?i)CallOfTheWild|Call.of.the.wild' } |
            Sort-Object FullName)

        foreach ($package in $cotwPackages) {
            $target = Join-Path $payload ("incoming\optional-mod-packages\CallOfTheWild\" + $package.Name)
            Copy-ExactFile -Source $package.FullName -Destination $target
            $packageSources.Add([ordered]@{
                category = 'CallOfTheWildPackage'
                source = $package.FullName
                destination = "incoming\optional-mod-packages\CallOfTheWild\$($package.Name)"
            })
        }
    }

    $cotwRoot = Join-Path $SourceLabRoot 'examples\CallOfTheWild'
    if (Test-Path -LiteralPath $cotwRoot -PathType Container) {
        Copy-FilteredTree -Source $cotwRoot -Destination (Join-Path $payload 'examples\CallOfTheWild')
        $packageSources.Add([ordered]@{
            category = 'CallOfTheWildLoadableRoot'
            source = (Resolve-Path -LiteralPath $cotwRoot).Path
            destination = 'examples\CallOfTheWild'
        })
    }
    else {
        $warnings.Add('No loadable Call of the Wild root found at examples\CallOfTheWild.')
    }

    # 6. Explicit optional materials. Nothing sensitive is auto-selected.
    if (-not [string]::IsNullOrWhiteSpace($TabletopPackagePath)) {
        $resolved = (Resolve-Path -LiteralPath $TabletopPackagePath).Path
        $target = Join-Path $payload ("incoming\optional-mod-packages\TabletopAddedRules\" + [System.IO.Path]::GetFileName($resolved))
        Copy-ExactFile -Source $resolved -Destination $target
        $packageSources.Add([ordered]@{
            category = 'TabletopAddedRulesPackage'
            source = $resolved
            destination = "incoming\optional-mod-packages\TabletopAddedRules\$([System.IO.Path]::GetFileName($resolved))"
        })
    }
    else {
        $warnings.Add('No Tabletop Added Rules package was explicitly supplied. Rerun the exporter later when the desired exact build is available.')
    }

    if (-not [string]::IsNullOrWhiteSpace($UnityModManagerArchive)) {
        $resolved = (Resolve-Path -LiteralPath $UnityModManagerArchive).Path
        $target = Join-Path $payload ("incoming\installers\UnityModManager\" + [System.IO.Path]::GetFileName($resolved))
        Copy-ExactFile -Source $resolved -Destination $target
        $packageSources.Add([ordered]@{
            category = 'UnityModManagerArchive'
            source = $resolved
            destination = "incoming\installers\UnityModManager\$([System.IO.Path]::GetFileName($resolved))"
        })
    }
    else {
        $warnings.Add('No Unity Mod Manager archive was explicitly supplied. Installed UMM/Harmony fingerprints were recorded without copying DLLs.')
    }

    if (-not [string]::IsNullOrWhiteSpace($DisposableSavePath)) {
        $resolved = (Resolve-Path -LiteralPath $DisposableSavePath).Path
        $leaf = [System.IO.Path]::GetFileName($resolved)
        if ($leaf -notmatch '(?i)AUTOMATION|WORKING|DISPOSABLE|TEST') {
            throw "The explicitly supplied save does not look disposable from its filename. Rename/copy it first with AUTOMATION, WORKING, DISPOSABLE, or TEST in the name: $leaf"
        }

        $target = Join-Path $payload ("incoming\disposable-save-candidates\" + $leaf)
        if (Test-Path -LiteralPath $resolved -PathType Container) {
            Copy-FilteredTree -Source $resolved -Destination $target
        }
        else {
            Copy-ExactFile -Source $resolved -Destination $target
        }
        $packageSources.Add([ordered]@{
            category = 'ExplicitDisposableSaveCandidate'
            source = $resolved
            destination = "incoming\disposable-save-candidates\$leaf"
        })
    }
    else {
        $warnings.Add('No disposable save was supplied. This is safe; Codex can create or request one only at the mission-defined gate.')
    }

    foreach ($additional in $AdditionalPackagePaths) {
        if ([string]::IsNullOrWhiteSpace($additional)) {
            continue
        }

        $resolved = (Resolve-Path -LiteralPath $additional).Path
        $leaf = [System.IO.Path]::GetFileName($resolved)
        $target = Join-Path $payload ("incoming\optional-mod-packages\Additional\" + $leaf)
        if (Test-Path -LiteralPath $resolved -PathType Container) {
            Copy-FilteredTree -Source $resolved -Destination $target
        }
        else {
            Copy-ExactFile -Source $resolved -Destination $target
        }
        $packageSources.Add([ordered]@{
            category = 'AdditionalExplicitPackage'
            source = $resolved
            destination = "incoming\optional-mod-packages\Additional\$leaf"
        })
    }

    $packageSources | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $metadataRoot 'package-sources.json') -Encoding UTF8

    $readme = @"
KINGMAKER BUFF PLANNER PRIVATE TRANSFER

This archive is intentionally separate from GitHub.

It may contain:
- read-only selected automation-harness patterns;
- immutable public-source snapshots;
- exact third-party mod packages or loadable roots;
- an explicitly chosen disposable save candidate;
- environment fingerprints.

It must never be committed to the KingmakerBuffPlanner repository.

Import it on the desktop only through:
  scripts\Import-KingmakerBuffPlannerPrivateTransfer.ps1

The importer verifies every SHA-256 hash before copying anything and never writes
the live game Mods directory or the live Saved Games directory.
"@
    Set-Content -LiteralPath (Join-Path $payload 'README-PRIVATE-TRANSFER.txt') -Value $readme -Encoding UTF8

    # Reject accidental credentials and proprietary game/UMM reference DLLs.
    $forbiddenLeafNames = @(
        'auth.json', 'credentials', 'credentials.json',
        'Assembly-CSharp.dll', 'Assembly-CSharp-firstpass.dll',
        'UnityModManager.dll', '0Harmony12.dll', '0Harmony.dll',
        'Newtonsoft.Json.dll'
    )

    $payloadFiles = @(Get-ChildItem -LiteralPath $payload -File -Recurse -Force | Sort-Object FullName)
    foreach ($file in $payloadFiles) {
        if ($file.Name -in $forbiddenLeafNames -and $file.DirectoryName -notmatch '(?i)optional-mod-packages|examples') {
            throw "Forbidden credential or proprietary build-reference file entered the transfer payload: $($file.FullName)"
        }
        if ($file.FullName -match '(?i)\\\.ssh\\|\\Steam\\config\\|\\Login Data$|\\Local State$') {
            throw "Credential-bearing path entered the transfer payload: $($file.FullName)"
        }
    }

    $files = New-Object System.Collections.Generic.List[object]
    foreach ($file in $payloadFiles) {
        $relative = $file.FullName.Substring($payload.Length).TrimStart('\')
        $files.Add([ordered]@{
            relativePath = $relative
            length = $file.Length
            sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        })
    }

    $transferId = [Guid]::NewGuid().ToString('D')
    $manifest = [ordered]@{
        schemaVersion = 1
        transferId = $transferId
        generatedAtUtc = [DateTime]::UtcNow.ToString('o')
        sourceComputer = $env:COMPUTERNAME
        sourceUser = $env:USERNAME
        sourceRepository = $sourceGit
        restrictions = @(
            'Never commit this archive or its private payload to Git.',
            'Never execute copied Gunslinger deployment or push helpers.',
            'Never compile the Buff Planner against copied Gunslinger source.',
            'Never treat optional mod binaries as redistributable release payload.',
            'Never copy a disposable save candidate into the live Saved Games directory without the guarded mission workflow.'
        )
        warnings = $warnings
        referenceSnapshots = $referenceSnapshots
        packageSources = $packageSources
        files = $files
    }
    $manifest | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $payload 'TRANSFER-MANIFEST.json') -Encoding UTF8

    $destinationParent = Split-Path -Parent $DestinationZip
    if (-not (Test-Path -LiteralPath $destinationParent)) {
        New-Item -ItemType Directory -Path $destinationParent -Force | Out-Null
    }

    Compress-Archive -LiteralPath $payload -DestinationPath $DestinationZip -CompressionLevel Optimal
    $zipHash = (Get-FileHash -LiteralPath $DestinationZip -Algorithm SHA256).Hash.ToLowerInvariant()
    "$zipHash  $([System.IO.Path]::GetFileName($DestinationZip))" |
        Set-Content -LiteralPath ($DestinationZip + '.sha256') -Encoding ASCII

    Write-Host ''
    Write-Host 'Private laptop-to-desktop transfer created.' -ForegroundColor Green
    Write-Host "ZIP:             $DestinationZip"
    Write-Host "SHA-256:         $zipHash"
    Write-Host "Transfer ID:     $transferId"
    Write-Host "Files:           $($payloadFiles.Count)"
    Write-Host "Warnings:        $($warnings.Count)"
    Write-Host ''
    foreach ($warning in $warnings) {
        Write-Warning $warning
    }
    Write-Host ''
    Write-Host 'Copy the ZIP and its .sha256 sidecar to the desktop. Do not upload them to GitHub.'
}
finally {
    if (Test-Path -LiteralPath $staging) {
        Remove-Item -LiteralPath $staging -Recurse -Force
    }
}


