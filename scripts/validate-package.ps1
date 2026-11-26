[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$PackagePath,
    # Optional: write the identities read from the inspected snapshot (ZIP digest, DLL digest, MVID, embedded source
    # revision, loader references) for receipt producers and consumers.
    [string]$ReportPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

$root = Get-KbpRepositoryRoot
# PowerShell filesystem semantics for the caller's path (relative to the current PowerShell location).
$package = (Resolve-Path -LiteralPath $PackagePath).ProviderPath
[void](Assert-KbpPathWithin -Path $package -Root (Join-Path $root 'artifacts'))
if ($ReportPath) {
    $ReportPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ReportPath)
    if ([string]::Equals([IO.Path]::GetFullPath($ReportPath), [IO.Path]::GetFullPath($package), [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The validation report path must not be the package path.'
    }
}
# The same pin-definition and manifest contract as the build (offline; no download, no cache needed).
[void](& (Join-Path $PSScriptRoot 'Get-PinnedLoaderReferences.ps1') -DefinitionOnly)
$pins = Get-Content -LiteralPath (Join-Path $root 'scripts\loader-reference-pins.json') -Raw | ConvertFrom-Json
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

# Inspect and hash one private snapshot, so the reported ZIP digest and the inspected contents are the same bytes even
# if the caller's file is replaced meanwhile.
$temporary = Join-Path ([IO.Path]::GetTempPath()) ('kbp-package-' + [Guid]::NewGuid().ToString('N'))
try {
    New-Item -ItemType Directory -Path $temporary | Out-Null
    $snapshot = Join-Path $temporary 'package.zip'
    [IO.File]::Copy($package, $snapshot)
    $snapshotSha256 = Get-KbpSha256 $snapshot
    $archive = [IO.Compression.ZipFile]::OpenRead($snapshot)
    try {
        $actual = @($archive.Entries | ForEach-Object FullName | Sort-Object)
        $expected = @(
            'KingmakerBuffPlanner/Info.json',
            'KingmakerBuffPlanner/KingmakerBuffPlanner.dll',
            'KingmakerBuffPlanner/LICENSE',
            'KingmakerBuffPlanner/NativeEffectOverrides.json',
            'KingmakerBuffPlanner/THIRD-PARTY-NOTICES.md') | Sort-Object
        if (($actual -join "`n") -cne ($expected -join "`n")) {
            throw "Package entry allowlist mismatch. Observed: $($actual -join ', ')"
        }
        $fixedZipTime = [DateTime]::SpecifyKind([DateTime]'2000-01-01T00:00:00', [DateTimeKind]::Unspecified)
        foreach ($entry in $archive.Entries) {
            if ($entry.LastWriteTime.DateTime -ne $fixedZipTime) {
                throw "Package timestamp is not deterministic: $($entry.FullName)"
            }
        }
    }
    finally { $archive.Dispose() }

    $extracted = Join-Path $temporary 'extracted'
    [IO.Compression.ZipFile]::ExtractToDirectory($snapshot, $extracted)
    $mod = Join-Path $extracted 'KingmakerBuffPlanner'
    $dllPath = Join-Path $mod 'KingmakerBuffPlanner.dll'
    $info = Get-Content -LiteralPath (Join-Path $mod 'Info.json') -Raw | ConvertFrom-Json
    $name = [Reflection.AssemblyName]::GetAssemblyName($dllPath)
    if ($info.Id -cne 'KingmakerBuffPlanner' -or
        $info.AssemblyName -cne 'KingmakerBuffPlanner.dll' -or
        $name.Name -cne 'KingmakerBuffPlanner' -or
        $info.Version -cne (Get-KbpVersion)) {
        throw 'Package identity/version validation failed.'
    }
    # Installing over an earlier release depends on the unchanged loader
    # identity; the public UMM listing shows the branded name and author.
    if ($info.EntryMethod -cne 'KingmakerBuffPlanner.Main.Load' -or
        $info.DisplayName -cne "Mr. Bard's Kingmaker Buff Planner" -or
        $info.Author -cne 'Mr. Bard') {
        throw 'Package entry point or public UMM metadata validation failed.'
    }
    # Verify the bytes distributed inside the assembly. Loading from bytes leaves the extracted DLL unlocked.
    $dllBytes = [IO.File]::ReadAllBytes($dllPath)
    $assembly = [Reflection.Assembly]::Load($dllBytes)
    # UMM 0.31.1 baseline: the packaged manifest declares the pinned minimum and the packaged DLL's actual AssemblyRefs
    # name exactly the pinned loader assemblies (UnityModManager and 0Harmony12) and no other loader or Harmony
    # assembly; lowering Info.json on a DLL compiled against a newer loader is refused.
    if (-not ($info.ManagerVersion -is [string]) -or $info.ManagerVersion -cne [string]$pins.minimumManagerVersion) {
        throw "Packaged ManagerVersion must be the JSON string $($pins.minimumManagerVersion)."
    }
    $referenced = @($assembly.GetReferencedAssemblies())
    foreach ($pin in @($pins.references)) {
        $matches = @($referenced | Where-Object { $_.Name -ceq [string]$pin.Name })
        if ($matches.Count -ne 1 -or $matches[0].Version.ToString() -cne [string]$pin.Version) {
            throw "Packaged DLL must reference pinned $($pin.Name) $($pin.Version); lowering Info.json alone is not sufficient."
        }
    }
    $pinnedNames = @($pins.references | ForEach-Object { [string]$_.Name })
    $extraLoader = @($referenced | Where-Object { ($_.Name -match '^(0?Harmony|UnityModManager)' ) -and $pinnedNames -cnotcontains $_.Name })
    if ($extraLoader.Count -ne 0) {
        throw "Packaged DLL references an unpinned loader or Harmony assembly: $(@($extraLoader | ForEach-Object { $_.FullName }) -join ', ')"
    }
    # Documentation and data entries must be text: no assembly can travel under an allowed name.
    foreach ($textEntry in @('THIRD-PARTY-NOTICES.md', 'NativeEffectOverrides.json', 'LICENSE', 'Info.json')) {
        $textBytes = [IO.File]::ReadAllBytes((Join-Path $mod $textEntry))
        if (($textBytes.Length -ge 2 -and $textBytes[0] -eq 0x4D -and $textBytes[1] -eq 0x5A) -or [Array]::IndexOf($textBytes, [byte]0) -ge 0) {
            throw "Package text entry contains binary content: $textEntry"
        }
        try { [void][Text.UTF8Encoding]::new($false, $true).GetString($textBytes) }
        catch { throw "Package text entry is not valid UTF-8 text: $textEntry" }
    }
    foreach ($locale in @('enGB', 'zhCN')) {
        $catalog = $assembly.GetManifestResourceStream("KBP.Text.$locale.json")
        if ($null -eq $catalog) { throw "Missing packaged catalog for $locale." }
        try { $hash = (Get-FileHash -Algorithm SHA256 -InputStream $catalog).Hash.ToLowerInvariant() }
        finally { $catalog.Dispose() }
        if ($hash -cne (Get-KbpSha256 (Join-Path $root "src/KingmakerBuffPlanner/Localization/$locale.json"))) {
            throw "Packaged catalog differs from the source catalog for $locale."
        }
    }
    # The installable package carries the repository's MIT license verbatim.
    if ((Get-KbpSha256 (Join-Path $mod 'LICENSE')) -cne (Get-KbpSha256 (Join-Path $root 'LICENSE'))) {
        throw 'Packaged LICENSE does not match the repository LICENSE.'
    }
    # The source revision the build stamped into the DLL (BuildInfo.Commit).
    $buildInfo = $assembly.GetType('KingmakerBuffPlanner.BuildInfo', $false)
    $commitField = if ($buildInfo) { $buildInfo.GetField('Commit', [Reflection.BindingFlags]'NonPublic, Public, Static') } else { $null }
    if ($null -eq $commitField) { throw 'Packaged DLL has no embedded source revision (BuildInfo.Commit).' }
    $embeddedCommit = [string]$commitField.GetRawConstantValue()
    $dllSha256 = Get-KbpSha256 $dllPath
    $mvid = $assembly.ManifestModule.ModuleVersionId.ToString('D')
    $loaderReferences = @($referenced | Where-Object { $pinnedNames -ccontains $_.Name } | Sort-Object Name | ForEach-Object { [ordered]@{ name = $_.Name; version = $_.Version.ToString() } })
}
finally {
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Recurse -Force }
}
if ($ReportPath) {
    [ordered]@{
        schema = 'kbp.package-validation.v1'
        package_sha256 = $snapshotSha256
        dll_sha256 = $dllSha256
        dll_mvid = $mvid
        embedded_commit = $embeddedCommit
        manager_version = [string]$info.ManagerVersion
        dll_loader_references = $loaderReferences
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $ReportPath -Encoding UTF8
}
Write-Host "Package validation: PASS=13 FAIL=0 sha256=$snapshotSha256 dll=$dllSha256 mvid=$mvid commit=$embeddedCommit"
