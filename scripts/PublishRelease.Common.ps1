Set-StrictMode -Version Latest

# Release disposition for scripts/Publish-Release.ps1.
#
# Draft runs and -ConfirmHumanAcceptance keep the broad source-only aggregate.
# -ConfirmOwnerAuthorizedPreRuntimeRelease records that the owner authorized
# publication without completed human/runtime acceptance: it replaces that
# aggregate with a fixed offline selection, requires the current release notes
# to say so, and states the checks actually run in the generated notes. Neither
# disposition relaxes the branch, clean-tree, origin, tag, no-replace, build or
# package gates in the publisher.

function Resolve-KbpReleaseDisposition {
    param(
        [switch] $Publish,
        [switch] $ConfirmHumanAcceptance,
        [switch] $ConfirmOwnerAuthorizedPreRuntimeRelease
    )

    if ($ConfirmHumanAcceptance -and $ConfirmOwnerAuthorizedPreRuntimeRelease) {
        throw 'Choose either -ConfirmHumanAcceptance or -ConfirmOwnerAuthorizedPreRuntimeRelease, never both.'
    }
    if ($ConfirmOwnerAuthorizedPreRuntimeRelease -and -not $Publish) {
        throw '-ConfirmOwnerAuthorizedPreRuntimeRelease is valid only with -Publish.'
    }
    if ($Publish -and -not ($ConfirmHumanAcceptance -or $ConfirmOwnerAuthorizedPreRuntimeRelease)) {
        throw 'Public publication requires -ConfirmHumanAcceptance or -ConfirmOwnerAuthorizedPreRuntimeRelease.'
    }
    if (-not $Publish) { return 'Draft' }
    if ($ConfirmOwnerAuthorizedPreRuntimeRelease) { return 'OwnerAuthorizedPreRuntime' }
    return 'HumanAcceptance'
}

function Get-KbpReleaseValidationPlan {
    param([Parameter(Mandatory = $true)][string] $Disposition)

    switch -CaseSensitive ($Disposition) {
        'Draft' { return @('Test-SourceOnly.ps1') }
        'HumanAcceptance' { return @('Test-SourceOnly.ps1') }
        # Offline only: source contracts, every C# behavior test (read-only
        # reflection over the installed game assemblies), and the publisher's
        # own gate. No launcher, harness, deployment, restore or purity step.
        'OwnerAuthorizedPreRuntime' {
            return @('Validate-Source.ps1', 'OfflineCSharpSuite', 'Test-PublishReleaseGate.ps1')
        }
    }
    throw "Unknown release disposition: $Disposition"
}

function Get-KbpCurrentReleaseNotesSection {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string] $Notes)

    # RELEASE-NOTES-DRAFT.md keeps earlier releases below '---' separators.
    $separator = [Regex]::Match($Notes, '(?m)^---[ \t]*\r?$')
    if ($separator.Success) { return $Notes.Substring(0, $separator.Index) }
    return $Notes
}

function Assert-KbpReleaseNotesDisposition {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyString()][string] $Notes,
        [Parameter(Mandatory = $true)][string] $Disposition
    )

    if ($Disposition -cne 'OwnerAuthorizedPreRuntime') { return }
    $current = Get-KbpCurrentReleaseNotesSection -Notes $Notes
    if ($current -notmatch '(?i)owner-authorized') {
        throw 'Owner-authorized pre-runtime release notes must state the owner authorization in the current release section.'
    }
    if ($current -notmatch '(?i)not (?:been )?run in the game') {
        throw 'Owner-authorized pre-runtime release notes must state in the current release section that the release has not been run in the game.'
    }
}

function Get-KbpReleaseVerificationStatement {
    param([Parameter(Mandatory = $true)][string] $Disposition)

    switch -CaseSensitive ($Disposition) {
        'Draft' {
            return 'The asset passed the repository source-only suite, two deterministic clean release builds, and strict package validation.'
        }
        'HumanAcceptance' {
            return 'The asset passed the repository source-only suite, two deterministic clean release builds, and strict package validation.'
        }
        'OwnerAuthorizedPreRuntime' {
            return 'Owner-authorized pre-runtime release. The asset passed focused offline validation (source validation, the complete offline C# behavior suite and the publisher gate tests), two deterministic clean release builds, and strict package validation. The broad source-only aggregate (runtime-harness, launcher, deployment, restore and filesystem-purity checks), in-game runs and human acceptance were not performed for this release.'
        }
    }
    throw "Unknown release disposition: $Disposition"
}

function Invoke-KbpOfflineCSharpSuite {
    $root = Get-KbpRepositoryRoot
    $msbuild = Get-KbpMsBuild
    & $msbuild (Join-Path $root 'tests\KingmakerBuffPlanner.Tests\KingmakerBuffPlanner.Tests.csproj') /t:Rebuild /p:Configuration=Release /m /nologo /v:minimal | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Offline C# test build failed.' }
    $testExe = Join-Path $root 'artifacts\tests\KingmakerBuffPlanner.Tests.exe'
    if (-not (Test-Path -LiteralPath $testExe -PathType Leaf)) { throw 'Offline C# test runner is missing.' }
    $hadGamePath = Test-Path Env:\KBP_TEST_GAME_PATH
    $priorGamePath = $env:KBP_TEST_GAME_PATH
    try {
        $env:KBP_TEST_GAME_PATH = Get-KbpGamePath
        $output = @(& $testExe)
        $testExit = $LASTEXITCODE
    }
    finally {
        if ($hadGamePath) { $env:KBP_TEST_GAME_PATH = $priorGamePath }
        else { Remove-Item Env:\KBP_TEST_GAME_PATH -ErrorAction SilentlyContinue }
    }
    $output | ForEach-Object { Write-Host $_ }
    $summary = @($output | Where-Object { $_ -match '^Protocol tests: PASS=\d+ FAIL=\d+$' })
    if ($summary.Count -ne 1) { throw 'Offline C# runner did not report exactly one test count.' }
    $counts = [regex]::Match($summary[0], 'PASS=(\d+) FAIL=(\d+)')
    $passed = [int]$counts.Groups[1].Value
    $failed = [int]$counts.Groups[2].Value
    if ($testExit -ne 0 -or $failed -ne 0 -or $passed -eq 0) {
        throw "Offline C# suite failed with exit code $testExit (PASS=$passed FAIL=$failed)."
    }
    return $passed
}

function Invoke-KbpReleaseValidation {
    param(
        [Parameter(Mandatory = $true)][string] $Disposition,
        [Parameter(Mandatory = $true)][string] $ScriptsRoot
    )

    foreach ($step in @(Get-KbpReleaseValidationPlan -Disposition $Disposition)) {
        if ($step -ceq 'OfflineCSharpSuite') {
            $passed = Invoke-KbpOfflineCSharpSuite
            Write-Host "Offline C# suite: PASS=$passed FAIL=0"
        }
        else {
            & (Join-Path $ScriptsRoot $step)
        }
    }
    Write-Host "Release validation ($Disposition): $((Get-KbpReleaseValidationPlan -Disposition $Disposition) -join ', ') PASS"
}
