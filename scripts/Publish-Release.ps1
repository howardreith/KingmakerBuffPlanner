[CmdletBinding()]
param(
    [string] $ReleaseNotesPath,

    [switch] $Publish,

    [switch] $ConfirmHumanAcceptance,

    [switch] $AllowPrivateRepositoryRelease
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

function Assert-CommandAvailable {
    param([Parameter(Mandatory = $true)][string] $Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "$Name is required but was not found on PATH."
    }
}

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory = $true)][string] $FilePath,
        [Parameter()][string[]] $Arguments = @()
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$FilePath failed with exit code $LASTEXITCODE."
    }
}

function Get-NativeCommandOutput {
    param(
        [Parameter(Mandatory = $true)][string] $FilePath,
        [Parameter()][string[]] $Arguments = @()
    )

    $output = & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$FilePath failed with exit code $LASTEXITCODE."
    }

    return (($output | ForEach-Object { [string] $_ }) -join "`n").Trim()
}

function Test-NativeCommand {
    param(
        [Parameter(Mandatory = $true)][string] $FilePath,
        [Parameter()][string[]] $Arguments = @()
    )

    $priorErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'SilentlyContinue'
        & $FilePath @Arguments *> $null
        return ($LASTEXITCODE -eq 0)
    }
    finally {
        $ErrorActionPreference = $priorErrorActionPreference
    }
}

$root = Get-KbpRepositoryRoot
Assert-CommandAvailable -Name 'git'
Assert-CommandAvailable -Name 'gh'

Push-Location $root
try {
    $status = Get-NativeCommandOutput -FilePath 'git' -Arguments @(
        'status', '--porcelain'
    )
    if (-not [string]::IsNullOrWhiteSpace($status)) {
        throw 'Release publishing requires a clean working tree.'
    }

    Invoke-NativeCommand -FilePath 'gh' -Arguments @(
        'auth', 'status', '--hostname', 'github.com'
    )

    $repositoryJson = Get-NativeCommandOutput -FilePath 'gh' -Arguments @(
        'repo', 'view',
        '--json', 'nameWithOwner,defaultBranchRef,isPrivate'
    )
    $repositoryInfo = $repositoryJson | ConvertFrom-Json
    $repository = [string] $repositoryInfo.nameWithOwner
    $defaultBranch = [string] $repositoryInfo.defaultBranchRef.name
    $isPrivate = [bool] $repositoryInfo.isPrivate
    if ([string]::IsNullOrWhiteSpace($repository) -or
        [string]::IsNullOrWhiteSpace($defaultBranch)) {
        throw 'GitHub CLI did not return the repository or its default branch.'
    }

    $currentBranch = Get-NativeCommandOutput -FilePath 'git' -Arguments @(
        'rev-parse', '--abbrev-ref', 'HEAD'
    )
    if ($currentBranch -ne $defaultBranch) {
        throw "Release publishing must run from the default branch '$defaultBranch'; current branch is '$currentBranch'."
    }

    Invoke-NativeCommand -FilePath 'git' -Arguments @(
        'fetch', '--prune', '--tags', 'origin', $defaultBranch
    )

    $head = Get-NativeCommandOutput -FilePath 'git' -Arguments @(
        'rev-parse', 'HEAD'
    )
    $remoteHead = Get-NativeCommandOutput -FilePath 'git' -Arguments @(
        'rev-parse', "origin/$defaultBranch"
    )
    if ($head -ne $remoteHead) {
        throw "HEAD ($head) must exactly match origin/$defaultBranch ($remoteHead) before publishing."
    }

    $origin = Get-NativeCommandOutput -FilePath 'git' -Arguments @(
        'remote', 'get-url', 'origin'
    )
    if ($origin -notmatch [Regex]::Escape($repository)) {
        throw "Origin '$origin' does not match GitHub repository '$repository'."
    }

    $version = Get-KbpVersion
    if ([string]::IsNullOrWhiteSpace($version) -or
        $version -notmatch '^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$') {
        throw "Version.props does not contain valid semantic version text: $version"
    }

    if ($Publish -and $isPrivate -and -not $AllowPrivateRepositoryRelease) {
        throw 'The repository is private. Use -AllowPrivateRepositoryRelease only when a private release is intentional.'
    }

    if ($Publish -and -not $ConfirmHumanAcceptance) {
        throw 'Public publication requires -ConfirmHumanAcceptance.'
    }

    $notesSourcePath = if ([string]::IsNullOrWhiteSpace($ReleaseNotesPath)) {
        Join-Path $root 'docs\RELEASE-NOTES-DRAFT.md'
    }
    elseif ([IO.Path]::IsPathRooted($ReleaseNotesPath)) {
        $ReleaseNotesPath
    }
    else {
        Join-Path $root $ReleaseNotesPath
    }
    if (-not (Test-Path -LiteralPath $notesSourcePath -PathType Leaf)) {
        throw "Release notes file was not found: $notesSourcePath"
    }
    $customNotes = (Get-Content -LiteralPath $notesSourcePath -Raw).Trim()
    if ([string]::IsNullOrWhiteSpace($customNotes)) {
        throw 'Release notes are empty.'
    }
    if (-not $customNotes.Contains($version)) {
        throw "Release notes do not mention current version $version."
    }
    if ($Publish -and
        $customNotes -match '(?i)not a public release|publication status:.*(?:local|draft)|acceptance (?:remains|is) (?:required|pending)|local-only') {
        throw 'Release notes still identify the candidate as local-only or awaiting acceptance.'
    }

    $tag = "v$version"
    $title = "Kingmaker Buff Planner $tag"
    $existingRelease = $null

    if (Test-NativeCommand -FilePath 'gh' -Arguments @(
        'release', 'view', $tag, '--repo', $repository
    )) {
        $existingReleaseJson = Get-NativeCommandOutput -FilePath 'gh' -Arguments @(
            'release', 'view', $tag,
            '--repo', $repository,
            '--json', 'isDraft,isImmutable,url'
        )
        $existingRelease = $existingReleaseJson | ConvertFrom-Json
        if (-not [bool] $existingRelease.isDraft) {
            throw "Published GitHub release '$tag' already exists. Advance the version instead of replacing it."
        }
        if ([bool] $existingRelease.isImmutable) {
            throw "GitHub release '$tag' is immutable and cannot be refreshed."
        }
    }

    & (Join-Path $PSScriptRoot 'Test-SourceOnly.ps1')
    $packagePath = & (Join-Path $PSScriptRoot 'Build-Release.ps1') |
        Select-Object -Last 1
    if (-not (Test-Path -LiteralPath $packagePath -PathType Leaf)) {
        throw "Build-Release.ps1 did not produce a package: $packagePath"
    }
    & (Join-Path $PSScriptRoot 'validate-package.ps1') `
        -PackagePath $packagePath

    $statusAfterBuild = Get-NativeCommandOutput -FilePath 'git' -Arguments @(
        'status', '--porcelain'
    )
    if (-not [string]::IsNullOrWhiteSpace($statusAfterBuild)) {
        throw "Qualification modified tracked or unignored files:$([Environment]::NewLine)$statusAfterBuild"
    }

    $assetName = [IO.Path]::GetFileName($packagePath)
    $packageHash = Get-KbpSha256 $packagePath
    $releaseDirectory = Split-Path -Parent $packagePath
    $checksumsPath = Join-Path $releaseDirectory 'SHA256SUMS.txt'
    "$packageHash  $assetName" |
        Set-Content -LiteralPath $checksumsPath -Encoding ASCII

    if (Test-NativeCommand -FilePath 'git' -Arguments @(
        'show-ref', '--verify', '--quiet', "refs/tags/$tag"
    )) {
        $tagCommit = Get-NativeCommandOutput -FilePath 'git' -Arguments @(
            'rev-list', '-n', '1', $tag
        )
        if ($tagCommit -ne $head) {
            throw "Existing tag '$tag' resolves to $tagCommit, not release commit $head."
        }
    }
    else {
        Invoke-NativeCommand -FilePath 'git' -Arguments @(
            'tag', '-a', $tag, '-m', $title, $head
        )
    }

    if (-not (Test-NativeCommand -FilePath 'git' -Arguments @(
        'ls-remote', '--exit-code', '--tags', 'origin', "refs/tags/$tag"
    ))) {
        Invoke-NativeCommand -FilePath 'git' -Arguments @(
            'push', 'origin', "refs/tags/$tag"
        )
    }

    $generatedNotesLines = @(
        '## Installation',
        '',
        "1. Download **$assetName** from **Assets** below.",
        '2. In Unity Mod Manager, select Pathfinder: Kingmaker and drag the ZIP into the **Mods** tab.',
        '3. Launch the game and enable Kingmaker Buff Planner.',
        '',
        "Do not download GitHub's automatically generated **Source code** archives; they are not the installable Unity Mod Manager package.",
        '',
        '## Verification',
        '',
        "SHA-256: $packageHash",
        '',
        "Release commit: $head",
        '',
        'The asset passed the repository source-only suite, two deterministic clean release builds, and strict package validation.'
    )
    $notes = $customNotes + [Environment]::NewLine +
        [Environment]::NewLine +
        ($generatedNotesLines -join [Environment]::NewLine)
    $generatedNotesPath = Join-Path $releaseDirectory "release-notes-$version.md"
    $notes | Set-Content -LiteralPath $generatedNotesPath -Encoding UTF8

    if ($null -eq $existingRelease) {
        $releaseArguments = @(
            'release', 'create', $tag,
            $packagePath,
            $checksumsPath,
            '--repo', $repository,
            '--title', $title,
            '--notes-file', $generatedNotesPath,
            '--verify-tag'
        )
        if ($version.Contains('-')) {
            $releaseArguments += '--prerelease'
            $releaseArguments += '--latest=false'
        }
        if (-not $Publish) {
            $releaseArguments += '--draft'
        }

        Invoke-NativeCommand -FilePath 'gh' -Arguments $releaseArguments
    }
    else {
        Invoke-NativeCommand -FilePath 'gh' -Arguments @(
            'release', 'upload', $tag,
            $packagePath,
            $checksumsPath,
            '--repo', $repository,
            '--clobber'
        )

        $editArguments = @(
            'release', 'edit', $tag,
            '--repo', $repository,
            '--title', $title,
            '--notes-file', $generatedNotesPath,
            '--verify-tag'
        )
        if ($Publish) {
            $editArguments += '--draft=false'
        }
        else {
            $editArguments += '--draft'
        }
        if ($version.Contains('-')) {
            $editArguments += '--prerelease'
            if ($Publish) {
                $editArguments += '--latest=false'
            }
        }
        else {
            $editArguments += '--prerelease=false'
            if ($Publish) {
                $editArguments += '--latest'
            }
        }

        Invoke-NativeCommand -FilePath 'gh' -Arguments $editArguments
    }

    $releaseUrl = Get-NativeCommandOutput -FilePath 'gh' -Arguments @(
        'release', 'view', $tag,
        '--repo', $repository,
        '--json', 'url',
        '--jq', '.url'
    )

    Write-Host "Release: $releaseUrl"
    Write-Host "State: $(if ($Publish) { 'published' } else { 'draft' })"
    Write-Host "Asset: $packagePath"
    Write-Host "SHA-256: $packageHash"
}
finally {
    Pop-Location
}
