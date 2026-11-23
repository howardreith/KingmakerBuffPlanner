[CmdletBinding()]
param(
    [string]$ReferenceDirectory = '',
    [switch]$VerifyOnly
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$pins = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'loader-reference-pins.json') | ConvertFrom-Json
if ($pins.schemaVersion -ne 1 -or @($pins.references).Count -eq 0 -or @($pins.manifests).Count -eq 0 -or
    -not ($pins.minimumManagerVersion -is [string]) -or
    [string]$pins.minimumManagerVersion -cnotmatch '^[0-9]+([.][0-9]+){2}$') { throw 'Unknown or empty loader reference pin file.' }
$manager = @($pins.references | Where-Object Name -CEQ 'UnityModManager')
if ($manager.Count -ne 1 -or [string]$manager[0].Version -cne ([string]$pins.minimumManagerVersion + '.0')) {
    throw 'The UnityModManager compile reference must equal the declared minimum.'
}
# Exactly the product's compiler references under their canonical file names: a pin cannot be dropped or renamed
# (an unpinned file of the same name in the cache would otherwise be compiled against unchecked).
$canonical = [ordered]@{ 'UnityModManager' = 'UnityModManager.dll'; '0Harmony12' = '0Harmony12.dll' }
if (@($pins.references).Count -ne $canonical.Count) { throw "The pin file must define exactly: $($canonical.Keys -join ', ')." }
foreach ($name in $canonical.Keys) {
    $match = @($pins.references | Where-Object { $_.Name -is [string] -and $_.Name -ceq $name })
    if ($match.Count -ne 1 -or -not ($match[0].File -is [string]) -or $match[0].File -cne $canonical[$name]) {
        throw "The pin file must define $name exactly once as $($canonical[$name])."
    }
}
foreach ($manifest in $pins.manifests) {
    $info = Get-Content -Raw -LiteralPath (Join-Path $repoRoot $manifest) | ConvertFrom-Json
    if (-not ($info.ManagerVersion -is [string]) -or $info.ManagerVersion -cne [string]$pins.minimumManagerVersion) {
        throw "$manifest ManagerVersion must equal the pinned baseline $($pins.minimumManagerVersion)."
    }
}
if ([string]::IsNullOrWhiteSpace($ReferenceDirectory)) { $ReferenceDirectory = Join-Path $repoRoot 'artifacts/references' }
$ReferenceDirectory = [IO.Path]::GetFullPath($ReferenceDirectory)

function Assert-PinnedReference([string]$Path, $Pin) {
    $hash = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($hash -cne [string]$Pin.Sha256) { throw "Pinned loader reference hash mismatch: $Path. Preserve or remove the mismatched file explicitly before retrying." }
    $identity = [Reflection.AssemblyName]::GetAssemblyName($Path)
    if ($identity.Name -cne [string]$Pin.Name -or $identity.Version.ToString() -cne [string]$Pin.Version) {
        throw "Pinned loader assembly identity mismatch: $Path ($($identity.FullName))."
    }
}

$seen = @{}
foreach ($pin in $pins.references) {
    if ([string]$pin.File -cnotmatch '^[A-Za-z0-9._-]+[.]dll$' -or
        [string]$pin.Sha256 -cnotmatch '^[0-9a-f]{64}$' -or
        [string]$pin.Version -cnotmatch '^[0-9]+([.][0-9]+){3}$' -or
        [string]::IsNullOrWhiteSpace([string]$pin.Name) -or
        -not ([Uri]$pin.Url).IsAbsoluteUri -or ([Uri]$pin.Url).Scheme -cne 'https' -or
        $seen.ContainsKey([string]$pin.File)) { throw 'Invalid or duplicate loader reference pin.' }
    $seen[[string]$pin.File] = $true
    if ($pin.ArchiveEntry -and [string]$pin.ArchiveSha256 -cnotmatch '^[0-9a-f]{64}$') { throw "Missing archive hash for $($pin.File)." }
    $target = Join-Path $ReferenceDirectory $pin.File
    if (Test-Path -LiteralPath $target -PathType Leaf) {
        Assert-PinnedReference $target $pin
        continue
    }
    if ($VerifyOnly) { throw "Pinned loader reference missing: $target. Run scripts/Get-PinnedLoaderReferences.ps1 first." }
    [IO.Directory]::CreateDirectory($ReferenceDirectory) | Out-Null
    $work = Join-Path $ReferenceDirectory ('.download-' + [Guid]::NewGuid().ToString('N'))
    [IO.Directory]::CreateDirectory($work) | Out-Null
    try {
        $download = Join-Path $work 'source.download'
        $candidate = Join-Path $work $pin.File
        Invoke-WebRequest -Uri $pin.Url -OutFile $download -UseBasicParsing
        if ($pin.ArchiveEntry) {
            if ((Get-FileHash -LiteralPath $download -Algorithm SHA256).Hash.ToLowerInvariant() -cne [string]$pin.ArchiveSha256) { throw "Pinned archive hash mismatch: $($pin.Url)." }
            Add-Type -AssemblyName System.IO.Compression
            Add-Type -AssemblyName System.IO.Compression.FileSystem
            $archive = [IO.Compression.ZipFile]::OpenRead($download)
            try {
                $entries = @($archive.Entries | Where-Object FullName -CEQ $pin.ArchiveEntry)
                if ($entries.Count -ne 1) { throw "Pinned archive entry is missing or duplicated: $($pin.ArchiveEntry)." }
                [IO.Compression.ZipFileExtensions]::ExtractToFile($entries[0], $candidate, $false)
            }
            finally { $archive.Dispose() }
        }
        else { Copy-Item -LiteralPath $download -Destination $candidate }
        Assert-PinnedReference $candidate $pin
        Move-Item -LiteralPath $candidate -Destination $target
    }
    finally { Remove-Item -LiteralPath $work -Recurse -Force }
}
Write-Output $ReferenceDirectory
