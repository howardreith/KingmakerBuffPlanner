[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GameRoot,
    [string]$UnityModManagerInstallerPath,
    [string]$OutputDirectory
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
if ($PSVersionTable.PSEdition -ne 'Desktop') {
    throw 'Run this metadata-only collector with Windows powershell.exe, not pwsh.'
}
$root = Get-KbpRepositoryRoot
$GameRoot = (Resolve-Path -LiteralPath $GameRoot).Path
if (-not (Test-Path -LiteralPath (Join-Path $GameRoot 'Kingmaker.exe'))) {
    throw 'GameRoot must identify the installation used for reproduction.'
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $root ('artifacts\share-capture\' +
        [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))
}
$OutputDirectory = Assert-KbpPathWithin -Path $OutputDirectory -Root (Join-Path $root 'artifacts')
if (Test-Path -LiteralPath $OutputDirectory) { throw 'Capture output must be a new directory.' }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$processes = @(Get-CimInstance Win32_Process | Where-Object {
    $_.Name -in @('Kingmaker.exe', 'UnityModManager.exe')
} | Select-Object ProcessId, Name, ExecutablePath, CreationDate, CommandLine)
if (-not $UnityModManagerInstallerPath) {
    $umm = @($processes | Where-Object Name -eq 'UnityModManager.exe')
    if ($umm.Count -eq 1) { $UnityModManagerInstallerPath = Split-Path -Parent $umm[0].ExecutablePath }
}
$mods = Join-Path $GameRoot 'Mods'
$infoRecords = @()
foreach ($directory in @(Get-ChildItem -LiteralPath $mods -Directory)) {
    $infoPath = Join-Path $directory.FullName 'Info.json'
    if (-not (Test-Path -LiteralPath $infoPath -PathType Leaf)) { continue }
    $info = Get-Content -LiteralPath $infoPath -Raw | ConvertFrom-Json
    if ($info.Id -notin @('KingmakerBuffPlanner', 'KingmakerGunslinger')) { continue }
    $infoRecords += [ordered]@{
        path = $infoPath; id = $info.Id; version = $info.Version
        assemblyName = $info.AssemblyName
    }
    Copy-Item -LiteralPath $infoPath -Destination (Join-Path $OutputDirectory ($directory.Name + '-Info.json'))
}
$assemblies = @()
foreach ($file in @(Get-ChildItem -LiteralPath $mods -File -Recurse |
    Where-Object Name -in @('KingmakerBuffPlanner.dll', 'KingmakerGunslinger.dll'))) {
    $bytes = [IO.File]::ReadAllBytes($file.FullName)
    $assembly = [Reflection.Assembly]::ReflectionOnlyLoad($bytes)
    $hash = [Security.Cryptography.SHA256]::Create()
    try { $sha = [BitConverter]::ToString($hash.ComputeHash($bytes)).Replace('-', '').ToLowerInvariant() }
    finally { $hash.Dispose() }
    $assemblies += [ordered]@{
        path = $file.FullName; identity = $assembly.FullName
        sha256 = $sha; mvid = $assembly.ManifestModule.ModuleVersionId.ToString()
        directApiPresent = ($null -ne $assembly.GetType(
            'KingmakerGunslinger.BrownFur.BrownFurDirectCastApi', $false))
        evidenceKind = 'current on-disk metadata; not proof of running-process contents'
    }
}
$logPaths = @(
    (Join-Path $GameRoot 'UnityModManager.log'),
    (Join-Path $GameRoot 'UnityModManager\UnityModManager.log'),
    (Join-Path $GameRoot 'Kingmaker_Data\Managed\UnityModManager\UnityModManager.log'),
    (Join-Path $GameRoot 'Kingmaker_Data\output_log.txt'),
    (Join-Path $env:USERPROFILE 'AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\output_log.txt'),
    (Join-Path $env:USERPROFILE 'AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Player.log'),
    (Join-Path $env:USERPROFILE 'AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Player-prev.log')
)
if ($UnityModManagerInstallerPath) { $logPaths += Join-Path $UnityModManagerInstallerPath 'Log.txt' }
$logs = @()
$index = 0
foreach ($path in $logPaths) {
    $present = Test-Path -LiteralPath $path -PathType Leaf
    $record = [ordered]@{ path = $path; present = $present }
    if ($present) {
        $index++
        $copy = Join-Path $OutputDirectory ("log-$index-" + (Split-Path -Leaf $path))
        Copy-Item -LiteralPath $path -Destination $copy
        $record['copy'] = $copy
        $record['sha256'] = Get-KbpSha256 $copy
        $record['lastWriteUtc'] = (Get-Item -LiteralPath $path).LastWriteTimeUtc.ToString('o')
        Select-String -LiteralPath $copy -Pattern '\[KBP|Routine plan:|Routine outcome:|KingmakerGunslinger|KingmakerBuffPlanner|directCast=|ProviderDirectRuleCast|provider-direct-' |
            ForEach-Object { "$($record.path):$($_.LineNumber):$($_.Line)" } |
            Add-Content -LiteralPath (Join-Path $OutputDirectory 'routing-excerpts.txt') -Encoding UTF8
    }
    $logs += $record
}
[ordered]@{
    schemaVersion = 1; machine = $env:COMPUTERNAME
    capturedAtUtc = [DateTime]::UtcNow.ToString('o'); gameRoot = $GameRoot
    processes = $processes; modInfos = $infoRecords; assemblies = $assemblies; logs = $logs
    runningAssemblyEvidence = 'Read KBP-LOADED-ASSEMBLY records from the reproducing game; disk DLLs alone do not prove loaded identity.'
    gameplay = 'NOT VERIFIED by this read-only capture'
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'capture.json') -Encoding UTF8
Write-Host "Read-only Share capture: $OutputDirectory"
