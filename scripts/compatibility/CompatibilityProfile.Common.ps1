Set-StrictMode -Version Latest

. (Join-Path (Split-Path -Parent $PSScriptRoot) 'RuntimeHarness.Common.ps1')

function Get-KbpCompatibilityProfile {
    param([Parameter(Mandatory = $true)][ValidatePattern('^[a-z0-9-]{1,100}$')][string]$ProfileId)
    $root = Get-KbpRepositoryRoot
    $path = Join-Path $root ("compatibility\profiles\$ProfileId.json")
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Compatibility profile is missing: $ProfileId"
    }
    $profile = Read-KbpJson $path
    $required = @('schemaVersion', 'profileId', 'status', 'mods', 'expectedBlueprints')
    $allowed = $required + @('reason')
    $names = @($profile.PSObject.Properties.Name)
    if (@($required | Where-Object { $_ -notin $names }).Count -ne 0 -or
        @($names | Where-Object { $_ -notin $allowed }).Count -ne 0 -or
        [int]$profile.schemaVersion -ne 1 -or [string]$profile.profileId -cne $ProfileId -or
        [string]$profile.status -notin @('available', 'unavailable-local-reference')) {
        throw "Compatibility profile contract is invalid: $ProfileId"
    }
    if ($profile.status -ceq 'available') {
        foreach ($mod in @($profile.mods)) {
            $modRequired = @('ummId', 'directoryName', 'version', 'assemblyName', 'infoSha256',
                'assemblySha256', 'directoryManifestSha256', 'fileCount', 'totalBytes')
            $modNames = @($mod.PSObject.Properties.Name)
            if (@($modRequired | Where-Object { $_ -notin $modNames }).Count -ne 0 -or
                @($modNames | Where-Object { $_ -notin $modRequired }).Count -ne 0 -or
                [string]$mod.ummId -notmatch '^[A-Za-z0-9._-]{1,100}$' -or
                [string]$mod.directoryName -notmatch '^[A-Za-z0-9._-]{1,100}$' -or
                [string]$mod.infoSha256 -notmatch '^[0-9a-f]{64}$' -or
                [string]$mod.assemblySha256 -notmatch '^[0-9a-f]{64}$' -or
                [string]$mod.directoryManifestSha256 -notmatch '^[0-9a-f]{64}$' -or
                [int]$mod.fileCount -lt 2 -or [long]$mod.totalBytes -le 0) {
                throw "Compatibility mod contract is invalid: $($mod.ummId)"
            }
        }
    }
    return $profile
}

function Assert-KbpCompatibilityProfileFixtures {
    param($Profile, [string]$KingmakerInstallDir)
    if ([string]$Profile.status -cne 'available') {
        throw "Compatibility profile is unavailable: $($Profile.profileId): $($Profile.reason)"
    }
    if (-not $KingmakerInstallDir) { $KingmakerInstallDir = Get-KbpGamePath }
    $mods = Join-Path $KingmakerInstallDir 'Mods'
    foreach ($mod in @($Profile.mods)) {
        [void](Assert-KbpCompatibilityModIdentity $mod (Join-Path $mods ([string]$mod.directoryName)))
    }
}
