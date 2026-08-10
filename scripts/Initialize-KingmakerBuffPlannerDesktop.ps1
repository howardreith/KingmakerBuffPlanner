[CmdletBinding()]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$LabRoot = 'C:\Dev\KingmakerBuffPlannerLab',

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$KingmakerInstallDir = 'C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Kingmaker',

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$RepositoryName = 'KingmakerBuffPlanner'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-CommandInventory {
    param([Parameter(Mandatory = $true)][string]$Name)

    $command = Get-Command $Name -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $command) {
        return [ordered]@{
            name = $Name
            found = $false
            path = $null
            version = $null
        }
    }

    $version = $null
    try {
        if ($command.Version) {
            $version = $command.Version.ToString()
        }
    }
    catch {
        $version = $null
    }

    return [ordered]@{
        name = $Name
        found = $true
        path = $command.Source
        version = $version
    }
}

function Get-SafeFileInventory {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return [ordered]@{
            path = $Path
            exists = $false
            length = $null
            fileVersion = $null
            productVersion = $null
            assemblyVersion = $null
            sha256 = $null
        }
    }

    $item = Get-Item -LiteralPath $Path
    $fileVersion = $null
    $productVersion = $null
    $assemblyVersion = $null

    try { $fileVersion = $item.VersionInfo.FileVersion } catch { $fileVersion = $null }
    try { $productVersion = $item.VersionInfo.ProductVersion } catch { $productVersion = $null }
    try {
        $assemblyVersion = [System.Reflection.AssemblyName]::GetAssemblyName($item.FullName).Version.ToString()
    }
    catch {
        $assemblyVersion = $null
    }

    return [ordered]@{
        path = $item.FullName
        exists = $true
        length = $item.Length
        fileVersion = $fileVersion
        productVersion = $productVersion
        assemblyVersion = $assemblyVersion
        sha256 = (Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}

function Get-MSBuildInventory {
    $vswhere = 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path -LiteralPath $vswhere -PathType Leaf)) {
        return [ordered]@{
            found = $false
            vswhere = $vswhere
            path = $null
            version = $null
        }
    }

    $paths = @(& $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' 2>$null)
    $path = $paths | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($path)) {
        return [ordered]@{
            found = $false
            vswhere = $vswhere
            path = $null
            version = $null
        }
    }

    $version = $null
    try { $version = (Get-Item -LiteralPath $path).VersionInfo.FileVersion } catch { $version = $null }

    return [ordered]@{
        found = $true
        vswhere = $vswhere
        path = $path
        version = $version
    }
}

$repositoryRoot = Join-Path (Join-Path $LabRoot 'repo') $RepositoryName
$paths = @(
    $LabRoot,
    (Join-Path $LabRoot 'repo'),
    (Join-Path $LabRoot 'incoming'),
    (Join-Path $LabRoot 'codex-home'),
    (Join-Path $LabRoot 'codex-home\rules'),
    (Join-Path $LabRoot 'codex-policy'),
    (Join-Path $LabRoot 'reference-source'),
    (Join-Path $LabRoot 'examples'),
    (Join-Path $LabRoot 'harness-reference'),
    (Join-Path $LabRoot 'runtime-state'),
    (Join-Path $LabRoot 'runtime-staging'),
    (Join-Path $LabRoot 'runtime-evidence'),
    (Join-Path $LabRoot 'runtime-backups'),
    (Join-Path $LabRoot 'artifacts'),
    (Join-Path $LabRoot 'logs')
)

foreach ($path in $paths) {
    if (-not (Test-Path -LiteralPath $path)) {
        New-Item -ItemType Directory -Path $path -Force | Out-Null
    }
}

$managedDir = Join-Path $KingmakerInstallDir 'Kingmaker_Data\Managed'
$ummDir = Join-Path $managedDir 'UnityModManager'
$referencePaths = @(
    (Join-Path $managedDir 'Assembly-CSharp.dll'),
    (Join-Path $managedDir 'Assembly-CSharp-firstpass.dll'),
    (Join-Path $managedDir 'Newtonsoft.Json.dll'),
    (Join-Path $managedDir 'UnityEngine.dll'),
    (Join-Path $managedDir 'UnityEngine.CoreModule.dll'),
    (Join-Path $managedDir 'UnityEngine.AnimationModule.dll'),
    (Join-Path $managedDir 'UnityEngine.AssetBundleModule.dll'),
    (Join-Path $managedDir 'UnityEngine.UI.dll'),
    (Join-Path $ummDir 'UnityModManager.dll'),
    (Join-Path $ummDir '0Harmony12.dll')
)

$targetingPack = 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7'
$gameExe = Join-Path $KingmakerInstallDir 'Kingmaker.exe'
$saveRoot = Join-Path $env:USERPROFILE 'AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games'
$modsRoot = Join-Path $KingmakerInstallDir 'Mods'

$inventory = [ordered]@{
    schemaVersion = 1
    generatedAtUtc = [DateTime]::UtcNow.ToString('o')
    machine = [ordered]@{
        computerName = $env:COMPUTERNAME
        userName = $env:USERNAME
        operatingSystem = [Environment]::OSVersion.VersionString
        is64BitOperatingSystem = [Environment]::Is64BitOperatingSystem
        powerShellVersion = $PSVersionTable.PSVersion.ToString()
    }
    requestedLayout = [ordered]@{
        labRoot = $LabRoot
        repositoryRoot = $repositoryRoot
        codexHome = (Join-Path $LabRoot 'codex-home')
        kingmakerInstallDir = $KingmakerInstallDir
        modsRoot = $modsRoot
        saveRoot = $saveRoot
    }
    commands = @(
        (Get-CommandInventory -Name 'git'),
        (Get-CommandInventory -Name 'gh'),
        (Get-CommandInventory -Name 'codex'),
        (Get-CommandInventory -Name 'pwsh'),
        (Get-CommandInventory -Name 'dotnet'),
        (Get-CommandInventory -Name 'python')
    )
    msbuild = (Get-MSBuildInventory)
    netFramework47TargetingPack = [ordered]@{
        path = $targetingPack
        exists = (Test-Path -LiteralPath $targetingPack -PathType Container)
    }
    game = [ordered]@{
        executable = (Get-SafeFileInventory -Path $gameExe)
        managedDirectoryExists = (Test-Path -LiteralPath $managedDir -PathType Container)
        modsDirectoryExists = (Test-Path -LiteralPath $modsRoot -PathType Container)
        saveDirectoryExists = (Test-Path -LiteralPath $saveRoot -PathType Container)
    }
    references = @($referencePaths | ForEach-Object { Get-SafeFileInventory -Path $_ })
    safety = [ordered]@{
        gameWasLaunched = $false
        modsWereChanged = $false
        savesWereChanged = $false
        softwareWasInstalled = $false
        networkWasUsed = $false
    }
}

$intakePath = Join-Path $LabRoot 'environment-intake.json'
$inventory | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $intakePath -Encoding UTF8

$missing = @()
foreach ($command in $inventory.commands) {
    if (-not $command.found -and $command.name -in @('git', 'gh')) {
        $missing += "Command not found: $($command.name)"
    }
}
if (-not $inventory.msbuild.found) {
    $missing += 'MSBuild not found through vswhere.'
}
if (-not $inventory.netFramework47TargetingPack.exists) {
    $missing += ".NET Framework 4.7 targeting pack not found: $targetingPack"
}
foreach ($reference in $inventory.references) {
    if (-not $reference.exists) {
        $missing += "Kingmaker/UMM reference missing: $($reference.path)"
    }
}

Write-Host ''
Write-Host 'Kingmaker Buff Planner desktop lab initialized.' -ForegroundColor Green
Write-Host "Lab root:       $LabRoot"
Write-Host "Repository:     $repositoryRoot"
Write-Host "Environment:    $intakePath"
Write-Host ''

if ($missing.Count -gt 0) {
    Write-Warning 'The lab folders were created, but setup is not yet complete:'
    foreach ($entry in $missing) {
        Write-Warning "  - $entry"
    }
    Write-Host ''
    Write-Host 'Resolve these items, rerun this script, and compare the regenerated intake.' -ForegroundColor Yellow
}
else {
    Write-Host 'Core build references and tools were found. Continue with the setup guide.' -ForegroundColor Green
}

Write-Host ''
Write-Host 'No game, Mods directory, save, Steam process, credential, or software installation was modified.'
