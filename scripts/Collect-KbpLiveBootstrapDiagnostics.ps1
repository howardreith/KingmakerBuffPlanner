[CmdletBinding()]
param(
    [string]$LabRoot = 'C:\Dev\KingmakerBuffPlannerLab',
    [string]$GameRoot = 'C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Kingmaker'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repo = Join-Path $LabRoot 'repo\KingmakerBuffPlanner'
$installed = Join-Path $GameRoot 'Mods\KingmakerBuffPlanner'
$stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
$out = Join-Path $LabRoot "incoming\ui-bootstrap-0.0.3-failure\diagnostics-$stamp"
New-Item -ItemType Directory -Force -Path $out | Out-Null

function Copy-IfPresent([string]$Source, [string]$Name) {
    if (Test-Path -LiteralPath $Source -PathType Leaf) {
        Copy-Item -LiteralPath $Source -Destination (Join-Path $out $Name) -Force
    }
}

function Get-Sha([string]$Path) {
    if (Test-Path -LiteralPath $Path -PathType Leaf) {
        return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    return $null
}

$gameProcesses = @(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue)
if ($gameProcesses.Count -ne 0) {
    Write-Warning 'Kingmaker is currently running. Exit the game after reproducing before relying on the final log copy.'
}

$outputLog = Join-Path $env:USERPROFILE 'AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\output_log.txt'
Copy-IfPresent $outputLog 'output_log.txt'

if (Test-Path -LiteralPath $outputLog -PathType Leaf) {
    $patterns = @(
        'KingmakerBuffPlanner',
        '\[KBP',
        'Exception',
        'NullReferenceException',
        'MissingMethodException',
        'TypeLoadException',
        'FileNotFoundException',
        'Harmony',
        'F10',
        'OnToggle',
        'OnUpdate',
        'AreaDidLoad',
        'Scene'
    )
    Select-String -LiteralPath $outputLog -Pattern $patterns -Context 4,16 |
        Out-String -Width 500 |
        Set-Content -LiteralPath (Join-Path $out 'relevant-log-context.txt') -Encoding UTF8
}

$possibleUmmLogs = @(
    (Join-Path $GameRoot 'UnityModManager.log'),
    (Join-Path $GameRoot 'UnityModManager\UnityModManager.log'),
    (Join-Path $GameRoot 'UnityModManager\Logs\UnityModManager.log')
)
$index = 0
foreach ($log in $possibleUmmLogs) {
    if (Test-Path -LiteralPath $log -PathType Leaf) {
        $index++
        Copy-Item -LiteralPath $log -Destination (Join-Path $out "UnityModManager-$index.log") -Force
    }
}

$installedInfo = Join-Path $installed 'Info.json'
$installedDll = Join-Path $installed 'KingmakerBuffPlanner.dll'
Copy-IfPresent $installedInfo 'installed-Info.json'

$assembly = $null
if (Test-Path -LiteralPath $installedDll -PathType Leaf) {
    try {
        $assemblyName = [Reflection.AssemblyName]::GetAssemblyName($installedDll)
        $assembly = [ordered]@{
            fullName = $assemblyName.FullName
            version = $assemblyName.Version.ToString()
        }
    }
    catch {
        $assembly = [ordered]@{
            error = $_.Exception.ToString()
        }
    }
}

$git = $null
if (Test-Path -LiteralPath (Join-Path $repo '.git')) {
    $git = [ordered]@{
        branch = (& git -C $repo branch --show-current).Trim()
        head = (& git -C $repo rev-parse HEAD).Trim()
        status = @(& git -C $repo status --short)
        recent = @(& git -C $repo log -5 --oneline)
    }
}

$files = @()
if (Test-Path -LiteralPath $installed -PathType Container) {
    $files = @(Get-ChildItem -LiteralPath $installed -File -Recurse -Force | Sort-Object FullName | ForEach-Object {
        [ordered]@{
            relativePath = $_.FullName.Substring($installed.Length).TrimStart('\')
            length = $_.Length
            sha256 = Get-Sha $_.FullName
            lastWriteUtc = $_.LastWriteTimeUtc.ToString('o')
        }
    })
}

$state = [ordered]@{
    schemaVersion = 1
    capturedAtUtc = [DateTime]::UtcNow.ToString('o')
    humanObservation = 'UMM shows KBP 0.0.3 active; loaded campaign has no KBP HUD controls; F10 does nothing.'
    computerName = $env:COMPUTERNAME
    gameRoot = $GameRoot
    installedRoot = $installed
    installedExists = Test-Path -LiteralPath $installed -PathType Container
    installedInfoExists = Test-Path -LiteralPath $installedInfo -PathType Leaf
    installedDllExists = Test-Path -LiteralPath $installedDll -PathType Leaf
    installedDllSha256 = Get-Sha $installedDll
    installedAssembly = $assembly
    installedFiles = $files
    outputLog = $outputLog
    outputLogExists = Test-Path -LiteralPath $outputLog -PathType Leaf
    gameProcessCount = $gameProcesses.Count
    deploymentLockExists = Test-Path -LiteralPath (Join-Path $LabRoot 'runtime-state\deployment.lock')
    repository = $git
}
$state | ConvertTo-Json -Depth 10 |
    Set-Content -LiteralPath (Join-Path $out 'diagnostic-state.json') -Encoding UTF8

Write-Host ''
Write-Host 'KBP live-bootstrap diagnostic capture complete.' -ForegroundColor Green
Write-Host "Output: $out"
Write-Host ''
Write-Host 'No game, mod, save, repository, or runtime state was modified.'
