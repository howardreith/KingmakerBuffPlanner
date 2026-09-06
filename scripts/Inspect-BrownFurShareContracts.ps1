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
        'Arm', 'ValidateDirect', 'BeginDirect', 'Clear', 'FindRecord')
    'KingmakerGunslinger.BrownFur.BrownFurShareTargetingRuntime' = @(
        'Begin', 'Release', 'Clear', 'TryOverrideAnchor',
        'TryOverrideTarget', 'TryOverrideApproachDistance')
    'KingmakerGunslinger.BrownFur.BrownFurShareTargetPolicy' = @(
        'Decide', 'IsWilling')
    'KingmakerGunslinger.BrownFur.BrownFurCastPolicy' = @('Decide')
    'KingmakerGunslinger.BrownFur.BrownFurCastCommitCoordinator`6' = @(
        'Begin', 'BeginDirect', 'AttachRule', 'AttachProcess', 'Commit',
        'EndCommand', 'CompleteDirect', 'CancelDirect', 'FailDirect',
        'DirectProcessAttached', 'ProcessTerminal')
    'KingmakerGunslinger.BrownFur.BrownFurCastExecutionRuntime' = @(
        'BeginDirect', 'CompleteDirectRule', 'InspectDirect', 'CleanupDirect')
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

    $api = $assembly.GetType(
        'KingmakerGunslinger.BrownFur.BrownFurDirectCastApi', $false)
    $handle = $assembly.GetType(
        'KingmakerGunslinger.BrownFur.BrownFurDirectCastHandle', $false)
    $status = $assembly.GetType(
        'KingmakerGunslinger.BrownFur.BrownFurDirectCastStatus', $false)
    Assert-Contract ($null -ne $api -and $api.IsPublic -and
        $api.IsAbstract -and $api.IsSealed) `
        'Direct-cast API type is absent or is not public static.'
    Assert-Contract ($null -ne $handle -and $handle.IsPublic -and
        $handle.IsSealed -and @($handle.GetInterfaces() | Where-Object {
            $_.FullName -ceq 'System.IDisposable'
        }).Count -eq 1) `
        'Direct-cast handle is absent or lacks its public IDisposable contract.'
    Assert-Contract ($null -ne $status -and $status.IsPublic -and
        $status.IsSealed) `
        'Direct-cast status is absent or is not public sealed.'
    $version = $api.GetField('ContractVersion',
        [Reflection.BindingFlags]'Public,Static,DeclaredOnly')
    Assert-Contract ($null -ne $version -and $version.IsLiteral -and
        $version.FieldType.FullName -ceq 'System.Int32' -and
        [int]$version.GetRawConstantValue() -eq 1) `
        'Direct-cast capability version is not exactly 1.'

    function Assert-ExactMethod(
        [Type]$Type,
        [string]$Name,
        [bool]$Static,
        [string]$ReturnType,
        [string[]]$ParameterTypes) {
        $scope = if ($Static) {
            [Reflection.BindingFlags]'Public,Static,DeclaredOnly'
        } else {
            [Reflection.BindingFlags]'Public,Instance,DeclaredOnly'
        }
        $matches = @($Type.GetMethods($scope) | Where-Object {
            $_.Name -ceq $Name -and
            $_.ReturnType.FullName -ceq $ReturnType -and
            [string]::Join('|', @($_.GetParameters() | ForEach-Object {
                $_.ParameterType.FullName
            })) -ceq [string]::Join('|', $ParameterTypes)
        })
        Assert-Contract ($matches.Count -eq 1) `
            "Direct-cast method signature mismatch: $($Type.FullName)::$Name"
    }
    Assert-ExactMethod $api 'Validate' $true $status.FullName @(
        'Kingmaker.UnitLogic.Abilities.AbilityData',
        'Kingmaker.Utility.TargetWrapper')
    Assert-ExactMethod $api 'Begin' $true $handle.FullName @(
        'Kingmaker.UnitLogic.Abilities.AbilityData',
        'Kingmaker.Utility.TargetWrapper')
    Assert-ExactMethod $handle 'Inspect' $false $status.FullName @()
    Assert-ExactMethod $handle 'CompleteRule' $false $status.FullName @(
        'Kingmaker.RuleSystem.Rules.Abilities.RuleCastSpell')
    Assert-ExactMethod $handle 'Cleanup' $false $status.FullName @()
    $statusProperties = [ordered]@{
        Accepted = 'System.Boolean'
        Committed = 'System.Boolean'
        Complete = 'System.Boolean'
        ResidualState = 'System.Boolean'
        State = 'System.String'
        Failure = 'System.String'
        Detail = 'System.String'
        TransactionIdentity = 'System.String'
        ReservoirCost = 'System.Int32'
    }
    foreach ($expected in $statusProperties.GetEnumerator()) {
        $property = $status.GetProperty([string]$expected.Key,
            [Reflection.BindingFlags]'Public,Instance,DeclaredOnly')
        Assert-Contract ($null -ne $property -and
            $property.PropertyType.FullName -ceq [string]$expected.Value -and
            $null -ne $property.GetGetMethod($false) -and
            $null -eq $property.GetSetMethod($false) -and
            $property.GetIndexParameters().Count -eq 0) `
            "Direct-cast status property mismatch: $($expected.Key)"
    }

    $assemblyBytes = [IO.File]::ReadAllBytes($AssemblyPath)
    # PE metadata heaps are not guaranteed to start on an even file offset.
    # Inspect both UTF-16 alignments so the identity gate is deterministic.
    $rawText = [Text.Encoding]::Unicode.GetString($assemblyBytes) +
        [Text.Encoding]::Unicode.GetString(
            $assemblyBytes, 1, $assemblyBytes.Length - 1)
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
