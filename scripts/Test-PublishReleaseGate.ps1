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

# Release disposition: owner-authorized pre-runtime publication is a distinct,
# explicit path; it never claims human acceptance or the source-only suite.
. (Join-Path $PSScriptRoot 'PublishRelease.Common.ps1')
function Assert-Throws([scriptblock] $Action, [string] $Pattern, [string] $Label) {
    try { & $Action }
    catch {
        if ($_.Exception.Message -notmatch $Pattern) {
            throw "$Label failed with an unexpected message: $($_.Exception.Message)"
        }
        return
    }
    throw "$Label was accepted."
}
Assert-Throws { Resolve-KbpReleaseDisposition -Publish -ConfirmHumanAcceptance -ConfirmOwnerAuthorizedPreRuntimeRelease } 'never both' 'Both dispositions'
Assert-Throws { Resolve-KbpReleaseDisposition -ConfirmOwnerAuthorizedPreRuntimeRelease } 'only with -Publish' 'Owner disposition without -Publish'
Assert-Throws { Resolve-KbpReleaseDisposition -Publish } 'requires -ConfirmHumanAcceptance or -ConfirmOwnerAuthorizedPreRuntimeRelease' 'Publish without a disposition'
$resolved = @(
    (Resolve-KbpReleaseDisposition),
    (Resolve-KbpReleaseDisposition -ConfirmHumanAcceptance),
    (Resolve-KbpReleaseDisposition -Publish -ConfirmHumanAcceptance),
    (Resolve-KbpReleaseDisposition -Publish -ConfirmOwnerAuthorizedPreRuntimeRelease))
if (($resolved -join ',') -cne 'Draft,Draft,HumanAcceptance,OwnerAuthorizedPreRuntime') {
    throw "Release dispositions resolved unexpectedly: $($resolved -join ',')"
}
$passed++

# Only the draft and human-acceptance paths run the broad aggregate; the owner
# path is a fixed offline selection with no launcher/harness/install/purity step.
foreach ($broad in @('Draft', 'HumanAcceptance')) {
    if ((@(Get-KbpReleaseValidationPlan -Disposition $broad) -join ',') -cne 'Test-SourceOnly.ps1') {
        throw "The $broad disposition no longer runs exactly Test-SourceOnly.ps1."
    }
}
$focused = @(Get-KbpReleaseValidationPlan -Disposition 'OwnerAuthorizedPreRuntime')
if (($focused -join ',') -cne 'Validate-Source.ps1,OfflineCSharpSuite,Test-PublishReleaseGate.ps1') {
    throw "Owner-authorized validation plan changed: $($focused -join ',')"
}
foreach ($live in @('Test-SourceOnly.ps1', 'Test-RuntimeHarness.ps1', 'Test-RuntimeLauncherArguments.ps1',
                    'Test-RuntimeLauncherFileWhatIf.ps1', 'Test-DeploymentWhatIf.ps1', 'Test-InstallWhatIf.ps1',
                    'Test-RestoreInstallLocal.ps1', 'Invoke-KingmakerRuntimeTest.ps1', 'Deploy-Local.ps1', 'Install-Local.ps1')) {
    if ($focused -contains $live) { throw "Owner-authorized validation includes a broad or live step: $live" }
}
foreach ($step in $focused) {
    if ($step -cne 'OfflineCSharpSuite' -and -not (Test-Path -LiteralPath (Join-Path $PSScriptRoot $step) -PathType Leaf)) {
        throw "Owner-authorized validation step is missing: $step"
    }
}
# Regression (0.5.3 fresh-clone publish): the offline suite inspects the built
# production assembly, so it must compile HEAD with Build.ps1 before the runner.
$suiteBody = ${function:Invoke-KbpOfflineCSharpSuite}.ToString()
$buildAt = $suiteBody.IndexOf("'scripts\Build.ps1'", [StringComparison]::Ordinal)
$runAt = $suiteBody.IndexOf('& $testExe', [StringComparison]::Ordinal)
if ($buildAt -lt 0 -or $runAt -lt 0 -or $buildAt -gt $runAt) {
    throw 'The offline C# suite does not build the production assembly before running the tests.'
}
Assert-Throws { Get-KbpReleaseValidationPlan -Disposition 'Unknown' } 'Unknown release disposition' 'Unknown disposition'
$passed++

# The owner path requires the current release section (above the first '---')
# to record the authorization and that the release was not run in the game.
$goodNotes = "# Product 9.9.9`n`nOwner-authorized release. It has not been run in the game.`n`n---`n`n# Product 9.9.8`n"
$staleNotes = "# Product 9.9.9`n`nRelease.`n`n---`n`n# Product 9.9.8`n`nOwner-authorized. Not run in the game.`n"
Assert-KbpReleaseNotesDisposition -Notes $goodNotes -Disposition 'OwnerAuthorizedPreRuntime'
Assert-KbpReleaseNotesDisposition -Notes $staleNotes -Disposition 'HumanAcceptance'
Assert-Throws { Assert-KbpReleaseNotesDisposition -Notes $staleNotes -Disposition 'OwnerAuthorizedPreRuntime' } 'owner authorization' 'Authorization only in an older section'
Assert-Throws { Assert-KbpReleaseNotesDisposition -Notes "# Product 9.9.9`n`nOwner-authorized release.`n" -Disposition 'OwnerAuthorizedPreRuntime' } 'not been run in the game' 'Missing not-run-in-game statement'
$passed++

# Generated verification text states exactly what ran on each path.
$broadStatement = 'The asset passed the repository source-only suite, two deterministic clean release builds, and strict package validation.'
if ((Get-KbpReleaseVerificationStatement -Disposition 'HumanAcceptance') -cne $broadStatement) {
    throw 'The human-acceptance verification text changed.'
}
$ownerStatement = Get-KbpReleaseVerificationStatement -Disposition 'OwnerAuthorizedPreRuntime'
if ($ownerStatement -match '(?i)passed the repository source-only suite' -or
    $ownerStatement -notmatch '(?i)^Owner-authorized pre-runtime release\.' -or
    $ownerStatement -notmatch 'complete offline C# behavior suite' -or
    $ownerStatement -notmatch 'two deterministic clean release builds' -or
    $ownerStatement -notmatch '(?i)in-game runs and human acceptance were not performed') {
    throw "Owner-authorized verification text is inaccurate: $ownerStatement"
}
$passed++

# The publisher routes every disposition through the shared helper: the
# aggregate is never invoked directly and the old fixed claim is gone.
foreach ($wiring in @('Resolve-KbpReleaseDisposition', '-ConfirmOwnerAuthorizedPreRuntimeRelease:$ConfirmOwnerAuthorizedPreRuntimeRelease',
                      'Assert-KbpReleaseNotesDisposition -Notes $customNotes -Disposition $disposition',
                      'Invoke-KbpReleaseValidation -Disposition $disposition',
                      'Get-KbpReleaseVerificationStatement -Disposition $disposition')) {
    if ($source.IndexOf($wiring, [StringComparison]::Ordinal) -lt 0) {
        throw "Guarded publisher is missing disposition wiring: $wiring"
    }
}
if ($source.Contains("'Test-SourceOnly.ps1'") -or $source.Contains($broadStatement)) {
    throw 'Guarded publisher bypasses the disposition helper.'
}
$passed++

Write-Host "Guarded publisher gate tests: PASS=$passed FAIL=0"
