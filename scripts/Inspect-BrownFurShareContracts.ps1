[CmdletBinding()]
param(
    [string]$AssemblyPath,
    [string]$ProductAssemblyPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

$root = Get-KbpRepositoryRoot
$game = Get-KbpGamePath
if ([string]::IsNullOrWhiteSpace($AssemblyPath)) {
    $AssemblyPath = Join-Path $game `
        'Mods\KingmakerGunslinger\KingmakerGunslinger.dll'
}
if ([string]::IsNullOrWhiteSpace($ProductAssemblyPath)) {
    $ProductAssemblyPath = Join-Path $root `
        'artifacts\build\Release\KingmakerBuffPlanner.dll'
}
if (-not (Test-Path -LiteralPath $AssemblyPath -PathType Leaf)) {
    throw "Optional Brown Fur provider assembly is absent: $AssemblyPath"
}
$AssemblyPath = (Resolve-Path -LiteralPath $AssemblyPath).Path

$searchRoots = @(
    (Split-Path -Parent $AssemblyPath),
    (Join-Path $game 'Kingmaker_Data\Managed'),
    (Join-Path $game 'UnityModManager'),
    (Join-Path $game 'Mods\CallOfTheWild')
) | Where-Object { Test-Path -LiteralPath $_ -PathType Container } |
    Select-Object -Unique
$resolver = [ResolveEventHandler]{
    param($sender, $eventArgs)
    $name = ([Reflection.AssemblyName]$eventArgs.Name).Name + '.dll'
    foreach ($directory in $searchRoots) {
        $candidate = Join-Path $directory $name
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return [Reflection.Assembly]::ReflectionOnlyLoadFrom($candidate)
        }
    }
    return $null
}

$passes = 0
function Assert-Contract([bool]$Condition, [string]$Failure) {
    if (-not $Condition) { throw $Failure }
    $script:passes++
}

$requiredTypes = [ordered]@{
    'KingmakerGunslinger.BrownFur.BrownFurIdentityCatalog' = @(
        'get_All', 'PowerfulAbility', 'PowerfulBuff', 'PowerfulActivatable')
    'KingmakerGunslinger.BrownFur.BrownFurPlayerIntentRuntime' = @(
        'Observe', 'Consume', 'Clear', 'Find', 'IsBrownFurToggle')
    'KingmakerGunslinger.BrownFur.BrownFurCastIntentRuntime' = @(
        'Arm', 'Clear', 'FindRecord')
    'KingmakerGunslinger.BrownFur.BrownFurShareTargetingRuntime' = @(
        'Begin', 'Release', 'Clear', 'TryOverrideAnchor',
        'TryOverrideTarget', 'TryOverrideApproachDistance')
    'KingmakerGunslinger.BrownFur.BrownFurShareTargetPolicy' = @(
        'Decide', 'IsWilling')
    'KingmakerGunslinger.BrownFur.BrownFurCastPolicy' = @('Decide')
    'KingmakerGunslinger.BrownFur.BrownFurCastCommitCoordinator`6' = @(
        'Begin', 'AttachRule', 'AttachProcess', 'Commit', 'EndCommand',
        'ProcessTerminal')
    'KingmakerGunslinger.BrownFur.BrownFurExactDebitPolicy' = @(
        'TryDebitExact')
    'KingmakerGunslinger.BrownFur.BrownFurShareTargetAnchorPatch' = @(
        'Postfix')
    'KingmakerGunslinger.BrownFur.BrownFurShareCanTargetPatch' = @('Postfix')
    'KingmakerGunslinger.BrownFur.BrownFurShareApproachDistancePatch' = @(
        'Postfix')
}
$expectedIdentities = [ordered]@{
    Feature = 'b7e929dac874cd22d173ee8f4fe0bfa4'
    Activatable = '8641e6c39ff133ad71f669e35e1ee688'
    Marker = '215a03a25c8ff8b76114bf7513869d6c'
    Supremacy = 'c69cd7091219708f981272f2ac057135'
    PowerfulFeature = 'b3bbed7e12463e4c434cd81eda7ab2dd'
    Reservoir = '3b775ee982444493b3de8f7bc31bd872'
}

[AppDomain]::CurrentDomain.add_ReflectionOnlyAssemblyResolve($resolver)
try {
    $assemblyName = [Reflection.AssemblyName]::GetAssemblyName($AssemblyPath)
    Assert-Contract ($assemblyName.Name -ceq 'KingmakerGunslinger') `
        "Unexpected optional provider assembly name: $($assemblyName.Name)"
    $assembly = [Reflection.Assembly]::ReflectionOnlyLoadFrom($AssemblyPath)
    $flags = [Reflection.BindingFlags]'Public,NonPublic,Instance,Static,DeclaredOnly'
    foreach ($entry in $requiredTypes.GetEnumerator()) {
        $type = $assembly.GetType([string]$entry.Key, $false)
        Assert-Contract ($null -ne $type) "Missing provider type: $($entry.Key)"
        foreach ($methodName in @($entry.Value)) {
            $methods = @($type.GetMethods($flags) |
                Where-Object Name -CEQ $methodName)
            Assert-Contract ($methods.Count -gt 0) `
                "Missing provider method: $($entry.Key)::$methodName"
        }
    }

    $rawText = [Text.Encoding]::Unicode.GetString(
        [IO.File]::ReadAllBytes($AssemblyPath))
    $profileSource = Get-Content -LiteralPath (Join-Path $root `
        'src\KingmakerBuffPlanner\Compatibility\BrownFurShareTransmutationProfile.cs') -Raw
    $powerfulProfileSource = Get-Content -LiteralPath (Join-Path $root `
        'src\KingmakerBuffPlanner\Compatibility\BrownFurPowerfulChangeProfile.cs') -Raw
    foreach ($identity in $expectedIdentities.GetEnumerator()) {
        Assert-Contract ($rawText.Contains([string]$identity.Value)) `
            "Provider metadata lacks $($identity.Key) identity $($identity.Value)."
        Assert-Contract (($profileSource + $powerfulProfileSource).Contains(
                [string]$identity.Value)) `
            "Planner profile lacks $($identity.Key) identity $($identity.Value)."
    }

    if (Test-Path -LiteralPath $ProductAssemblyPath -PathType Leaf) {
        $productName = [Reflection.AssemblyName]::GetAssemblyName(
            $ProductAssemblyPath)
        Assert-Contract ($productName.Name -ceq 'KingmakerBuffPlanner') `
            "Unexpected product assembly name: $($productName.Name)"
        $product = [Reflection.Assembly]::ReflectionOnlyLoadFrom(
            (Resolve-Path -LiteralPath $ProductAssemblyPath).Path)
        $forbidden = @($product.GetReferencedAssemblies() | Where-Object {
            $_.Name -in @('KingmakerGunslinger', 'CallOfTheWild')
        })
        Assert-Contract ($forbidden.Count -eq 0) `
            ('Product gained a forbidden optional-mod reference: ' +
                (($forbidden | ForEach-Object Name) -join ', '))
    }

    Write-Output ('Brown Fur provider assembly: ' + $AssemblyPath)
    Write-Output ('Brown Fur provider version: ' + $assemblyName.Version)
    Write-Output ('Brown Fur provider SHA-256: ' +
        (Get-KbpSha256 $AssemblyPath))
    Write-Output ('Brown Fur provider MVID: ' +
        $assembly.ManifestModule.ModuleVersionId)
    Write-Host "Brown Fur assembly contract: PASS=$passes FAIL=0"
}
finally {
    [AppDomain]::CurrentDomain.remove_ReflectionOnlyAssemblyResolve($resolver)
}
