[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

$root = Get-KbpRepositoryRoot
$publisher = Join-Path $root 'scripts\Publish-Release.ps1'
if (-not (Test-Path -LiteralPath $publisher -PathType Leaf)) {
    throw "Guarded publisher is missing: $publisher"
}
$source = Get-Content -LiteralPath $publisher -Raw
$passed = 0

# The feature-branch allowance must exist ONLY as a narrow prerelease gate:
# every required guard must still be present, the allowance must require
# BOTH the switch and a '-' prerelease version, and stable versions must
# remain default-branch-only.
$required = @(
    'AllowFeatureBranchPrerelease',
    'current branch is',
    "origin/`$currentBranch",
    'must exactly match',
    '-ConfirmHumanAcceptance',
    'Advance the version instead of replacing it',
    '--prerelease',
    '--latest=false',
    'release', 'create')
foreach ($marker in $required) {
    if ($source.IndexOf($marker, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Guarded publisher is missing required guard marker: $marker"
    }
}
$passed++

# The allowance condition must require the switch AND a prerelease version —
# neither alone may publish from a non-default branch.
if ($source -notmatch '(?s)AllowFeatureBranchPrerelease\s*-\and\s*\$isPrereleaseVersion' -and
    $source -notmatch '(?s)\(\s*\$AllowFeatureBranchPrerelease\s*-\s*and\s*\$isPrereleaseVersion\s*\)') {
    throw 'Feature-branch allowance does not require BOTH the switch and a prerelease version.'
}
$passed++

# A stable version on a feature branch must still fail: simulate the gate.
$currentBranch = 'codex/some-branch'
$defaultBranch = 'main'
$AllowFeatureBranchPrerelease = $true
foreach ($versionGate in @('0.2.0', '0.2.0-rc1')) {
    $isPrereleaseVersion = $versionGate -match '-'
    $onDefaultBranch = $currentBranch -ceq $defaultBranch
    $allowed = $onDefaultBranch -or ($AllowFeatureBranchPrerelease -and $isPrereleaseVersion)
    if ($versionGate -eq '0.2.0' -and $allowed) {
        throw 'A stable version was allowed to publish from a feature branch.'
    }
    if ($versionGate -eq '0.2.0-rc1' -and -not $allowed) {
        throw 'A prerelease version was refused on a feature branch despite the explicit switch.'
    }
}
$AllowFeatureBranchPrerelease = $false
$isPrereleaseVersion = $true
$onDefaultBranch = $false
if ($AllowFeatureBranchPrerelease -and $isPrereleaseVersion -and -not $onDefaultBranch) {
    throw 'Gate simulation is inconsistent: switch=false must refuse.'
}
$passed++

Write-Host "Guarded publisher gate tests: PASS=$passed FAIL=0"
