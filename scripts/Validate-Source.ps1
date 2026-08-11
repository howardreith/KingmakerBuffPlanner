[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

$root = Get-KbpRepositoryRoot
$required = @(
    'KingmakerBuffPlanner.sln', 'Version.props',
    'src\KingmakerBuffPlanner\KingmakerBuffPlanner.csproj',
    'src\KingmakerBuffPlanner\Info.json',
    'planning\KINGMAKER-BUFF-PLANNER-MISSION.md',
    'AUTONOMOUS-RESUME.md', 'AUTONOMOUS-BLOCKERS.md')
$assertions = 0
foreach ($relative in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $root $relative) -PathType Leaf)) {
        throw "Required source file is missing: $relative"
    }
    $assertions++
}

$missionSource = Join-Path $root 'planning\CODEX-KINGMAKER-BUFF-PLANNER-AUTONOMOUS-MISSION.md'
$missionCopy = Join-Path $root 'planning\KINGMAKER-BUFF-PLANNER-MISSION.md'
if ((Get-KbpSha256 $missionSource) -cne (Get-KbpSha256 $missionCopy)) {
    throw 'Authoritative mission copy is not byte-identical.'
}
$assertions++

$version = Get-KbpVersion
$info = Get-Content -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\Info.json') -Raw | ConvertFrom-Json
if ($info.Id -cne 'KingmakerBuffPlanner' -or
    $info.AssemblyName -cne 'KingmakerBuffPlanner.dll' -or
    $info.EntryMethod -cne 'KingmakerBuffPlanner.Main.Load' -or
    $info.Version -cne $version -or
    $info.ManagerVersion -cne '0.28.2') {
    throw 'Info.json standalone identity or version is invalid.'
}
$assertions++

[xml]$project = Get-Content -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\KingmakerBuffPlanner.csproj') -Raw
$namespace = @{ msb = 'http://schemas.microsoft.com/developer/msbuild/2003' }
$target = Select-Xml -Xml $project -Namespace $namespace -XPath '//msb:TargetFrameworkVersion' | Select-Object -First 1 -ExpandProperty Node
$language = Select-Xml -Xml $project -Namespace $namespace -XPath '//msb:LangVersion' | Select-Object -First 1 -ExpandProperty Node
$references = @(Select-Xml -Xml $project -Namespace $namespace -XPath '//msb:Reference[msb:HintPath]' | ForEach-Object Node)
if ($target.InnerText -cne 'v4.7' -or $language.InnerText -cne '7.3') {
    throw 'Production target must remain .NET Framework 4.7 and C# 7.3.'
}
if ($references.Count -lt 3 -or @($references | Where-Object { $_.Private -cne 'False' }).Count -ne 0) {
    throw 'Every installed binary reference must explicitly disable Copy Local.'
}
$assertions += 2

$tracked = @(& git -C $root ls-files)
if ($LASTEXITCODE -ne 0) { throw 'Unable to inventory tracked files.' }
$prohibited = @($tracked | Where-Object {
    $_ -match '(?i)\.(dll|zks|sav|pfx|p12|key)$' -or
    $_ -match '(?i)(^|/)(auth\.json|credentials[^/]*)$'
})
if ($prohibited.Count -ne 0) { throw "Prohibited tracked payloads: $($prohibited -join ', ')" }
$assertions++

$identityFiles = Get-ChildItem -LiteralPath (Join-Path $root 'src') -Recurse -File
$foreignIdentity = @($identityFiles | Select-String -Pattern 'KingmakerGunslinger|TabletopAddedRules')
if ($foreignIdentity.Count -ne 0) { throw 'Foreign product identity was found in production source.' }
$assertions++

$ignored = (& git -C $root check-ignore 'GamePath.props').Trim()
if ($LASTEXITCODE -ne 0 -or $ignored -cne 'GamePath.props') {
    throw 'Machine-local GamePath.props must remain ignored.'
}
[void](Get-KbpGamePath)
$assertions++

Write-Host "Source validation: PASS=$assertions FAIL=0"
