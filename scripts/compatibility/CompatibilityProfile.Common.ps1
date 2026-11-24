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
            $modAllowed = $modRequired + @('fixtureRelativePath', 'loadedAssemblySha256')
            $modNames = @($mod.PSObject.Properties.Name)
            if (@($modRequired | Where-Object { $_ -notin $modNames }).Count -ne 0 -or
                @($modNames | Where-Object { $_ -notin $modAllowed }).Count -ne 0 -or
                [string]$mod.ummId -notmatch '^[A-Za-z0-9._-]{1,100}$' -or
                [string]$mod.directoryName -notmatch '^[A-Za-z0-9._-]{1,100}$' -or
                [string]$mod.infoSha256 -notmatch '^[0-9a-f]{64}$' -or
                [string]$mod.assemblySha256 -notmatch '^[0-9a-f]{64}$' -or
                (($modNames -contains 'loadedAssemblySha256') -and
                    [string]$mod.loadedAssemblySha256 -notmatch '^[0-9a-f]{64}$') -or
                [string]$mod.directoryManifestSha256 -notmatch '^[0-9a-f]{64}$' -or
                [int]$mod.fileCount -lt 2 -or [long]$mod.totalBytes -le 0 -or
                (($modNames -contains 'fixtureRelativePath') -and
                    [string]$mod.fixtureRelativePath -notmatch '^[A-Za-z0-9._-]{1,100}$')) {
                throw "Compatibility mod contract is invalid: $($mod.ummId)"
            }
        }
    }
    $blueprints = @($profile.expectedBlueprints)
    if (@($blueprints | Where-Object { [string]$_ -cnotmatch '^[0-9a-f]{32}$' }).Count -ne 0 -or
        @($blueprints | Sort-Object -Unique).Count -ne $blueprints.Count) {
        throw "Compatibility blueprint expectations are invalid: $ProfileId"
    }
    if (($ProfileId -ceq 'call-of-the-wild' -and $blueprints.Count -lt 3) -or
        ($ProfileId -cne 'call-of-the-wild' -and $blueprints.Count -ne 0)) {
        throw "Compatibility blueprint expectation count is invalid: $ProfileId"
    }
    return $profile
}

function Assert-KbpLoaderIdentityContract($Loader, [string]$ProfileId) {
    $components = @('unityModManager', 'harmony12', 'harmony2')
    $names = @($Loader.PSObject.Properties.Name)
    if (@($components | Where-Object { $_ -notin $names }).Count -ne 0 -or @($names | Where-Object { $_ -notin $components }).Count -ne 0) {
        throw "Compatibility loader identity must name exactly $($components -join ', '): $ProfileId"
    }
    foreach ($component in $components) {
        $row = $Loader.$component
        $rowNames = @($row.PSObject.Properties.Name)
        if ($rowNames.Count -ne 2 -or $rowNames -notcontains 'version' -or $rowNames -notcontains 'sha256' -or
            -not ($row.version -is [string]) -or $row.version -cnotmatch '^[0-9]+([.][0-9]+){3}$' -or
            -not ($row.sha256 -is [string]) -or $row.sha256 -cnotmatch '^[0-9a-f]{64}$') {
            throw "Compatibility loader identity row is invalid: $ProfileId $component"
        }
    }
    return $Loader
}

# The measured loader rows a runtime run can select, independently of the optional-mod profile
# (compatibility/loaders/<id>.json: UnityModManager, 0Harmony12 and the Harmony2 module 0Harmony12 forwards to).
function Get-KbpLoaderIdentity {
    param([Parameter(Mandatory = $true)][ValidateSet('0.31.1', '0.32.4', 'owner-0.33.0', '0.33.4')][string]$LoaderId)
    # ValidateSet accepts any letter case; the canonical row ID is used from here on.
    $LoaderId = @('0.31.1', '0.32.4', 'owner-0.33.0', '0.33.4' | Where-Object { [string]::Equals($_, $LoaderId, [StringComparison]::OrdinalIgnoreCase) })[0]
    $path = Join-Path (Get-KbpRepositoryRoot) ("compatibility\loaders\$LoaderId.json")
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Loader identity row is missing: $LoaderId" }
    $row = Read-KbpJson $path
    $names = @($row.PSObject.Properties.Name)
    foreach ($field in @('schemaVersion', 'loaderId', 'source')) { if ($names -notcontains $field) { throw "Loader identity row has no ${field}: $LoaderId" } }
    if ([int]$row.schemaVersion -ne 1 -or [string]$row.loaderId -cne $LoaderId) { throw "Loader identity row contract is invalid: $LoaderId" }
    $loader = [pscustomobject]@{ loaderId = $LoaderId; unityModManager = $row.unityModManager; harmony12 = $row.harmony12; harmony2 = $row.harmony2 }
    [void](Assert-KbpLoaderIdentityContract ([pscustomobject]@{ unityModManager = $row.unityModManager; harmony12 = $row.harmony12; harmony2 = $row.harmony2 }) $LoaderId)
    return $loader
}

# Offline preflight: the installed loader files must be exactly the selected loader row (assembly name, version and
# SHA-256 of UnityModManager.dll, 0Harmony12.dll and 0Harmony.dll in the UnityModManager directory).
function Assert-KbpInstalledLoaderIdentity {
    param([Parameter(Mandatory = $true)]$Loader, [string]$UnityModManagerDir, [string]$KingmakerInstallDir)
    if (-not $UnityModManagerDir) {
        if (-not $KingmakerInstallDir) { $KingmakerInstallDir = Get-KbpGamePath }
        $UnityModManagerDir = Join-Path $KingmakerInstallDir 'Kingmaker_Data\Managed\UnityModManager'
    }
    $loader = $Loader
    $files = [ordered]@{ unityModManager = @('UnityModManager.dll', 'UnityModManager'); harmony12 = @('0Harmony12.dll', '0Harmony12'); harmony2 = @('0Harmony.dll', '0Harmony') }
    $observed = [ordered]@{}
    $mismatches = @()
    foreach ($component in $files.Keys) {
        $path = Join-Path $UnityModManagerDir $files[$component][0]
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { $mismatches += "$component missing ($($files[$component][0]))"; continue }
        $name = [Reflection.AssemblyName]::GetAssemblyName($path)
        $row = [ordered]@{ version = $name.Version.ToString(); sha256 = Get-KbpSha256 $path }
        $observed[$component] = $row
        if ($name.Name -cne $files[$component][1] -or $row.version -cne [string]$loader.$component.version -or $row.sha256 -cne [string]$loader.$component.sha256) {
            $mismatches += "$component expected=$($loader.$component.version)/$($loader.$component.sha256) observed=$($name.Name) $($row.version)/$($row.sha256)"
        }
    }
    if ($mismatches.Count -ne 0) { throw "Installed loader does not match loader row $($Loader.loaderId): $($mismatches -join '; ')" }
    return [pscustomobject]$observed
}

# The shared installation preflight of the runtime launcher, the deployment script and the standalone profile test: the
# optional-mod fixtures of the profile and the selected loader row, both against the caller's installation.
function Assert-KbpInstallationPreflight {
    param([Parameter(Mandatory = $true)]$Profile, [Parameter(Mandatory = $true)]$Loader, [string]$KingmakerInstallDir)
    if (-not $KingmakerInstallDir) { $KingmakerInstallDir = Get-KbpGamePath }
    Assert-KbpCompatibilityProfileFixtures -Profile $Profile -KingmakerInstallDir $KingmakerInstallDir
    return Assert-KbpInstalledLoaderIdentity -Loader $Loader -KingmakerInstallDir $KingmakerInstallDir
}

function Assert-KbpCompatibilityProfileFixtures {
    param($Profile, [string]$KingmakerInstallDir)
    if ([string]$Profile.status -cne 'available') {
        throw "Compatibility profile is unavailable: $($Profile.profileId): $($Profile.reason)"
    }
    if (-not $KingmakerInstallDir) { $KingmakerInstallDir = Get-KbpGamePath }
    $mods = Join-Path $KingmakerInstallDir 'Mods'
    foreach ($mod in @($Profile.mods)) {
        [void](Assert-KbpCompatibilityModIdentity $mod (Get-KbpCompatibilitySourcePath $mod $mods))
    }
}
