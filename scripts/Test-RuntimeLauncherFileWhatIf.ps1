[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')

# Regression for the powershell.exe -File defect class: a top-level
# script's $PSCmdlet cannot service ShouldProcess when confirmation must
# be evaluated (NullReferenceException; works under -Command). It crashed
# the runtime launcher after its read-only preflights. Safety contract:
# a decision that cannot be evaluated is a REFUSED decision — the
# launcher must abort with an error and mutate nothing.

# Layer 1: the defect class and the fail-closed repair pattern. A minimal
# top-level script using the launcher's exact guard must REFUSE (non-zero
# exit, refusal marker, never proceed) under -File.
$pattern = Join-Path $env:TEMP ('kbp-sp-pattern-' + [Guid]::NewGuid().ToString('N') + '.ps1')
@'
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param()
$ErrorActionPreference = 'Stop'
$shouldProceed = $false
$decisionFailure = $null
try {
    $shouldProceed = $PSCmdlet.ShouldProcess('pattern-target', 'pattern-action')
}
catch [NullReferenceException] {
    $decisionFailure = $_.Exception
}
if ($null -ne $decisionFailure) {
    throw ("Decision evaluation failed; refusing: " + $decisionFailure.Message)
}
if ($shouldProceed) { Write-Host 'PATTERN=proceeded' } else { Write-Host 'PATTERN=no-op' }
'@ | Set-Content -LiteralPath $pattern -Encoding ASCII
try {
    # The refused child writes its error to stderr; under Stop PS 5.1
    # turns the first redirected stderr line into a terminating error.
    $ErrorActionPreference = 'Continue'
    $patternOutput = (& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $pattern 2>&1) -join ' '
    $patternExit = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    if ($patternExit -eq 0 -or $patternOutput -like '*PATTERN=proceeded*') {
        throw "ShouldProcess -File pattern check must refuse; got exit $patternExit.: $patternOutput"
    }
    if ($patternOutput -notlike '*refusing*') {
        throw "ShouldProcess -File pattern check lacks refusal propagation.: $patternOutput"
    }
}
finally {
    Remove-Item -LiteralPath $pattern -Force -ErrorAction SilentlyContinue
}

# Layer 2: the launcher carries the fail-closed guard (narrow catch +
# refusal with error propagation); neither a bare call nor a
# WhatIf-derived fallback is acceptable.
$launcherSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Invoke-KingmakerRuntimeTest.ps1') -Raw
if ($launcherSource -notmatch 'catch \[NullReferenceException\]' -or
    $launcherSource -notmatch 'decisionFailure' -or
    $launcherSource -match 'shouldProceed = -not') {
    throw 'Runtime launcher lost the fail-closed ShouldProcess decision guard.'
}

# Layer 3: the launcher completes its -WhatIf contract under -File and
# mutates nothing. The child writes WhatIf notices to stderr; under Stop
# PS 5.1 turns the first redirected stderr line into a terminating error.
$root = Get-KbpRepositoryRoot
$game = Get-KbpGamePath
$targets = @(
    $script:KbpRuntimeStateRoot,
    $script:KbpRuntimeStagingRoot,
    $script:KbpRuntimeBackupRoot,
    $script:KbpRuntimeEvidenceRoot,
    (Join-Path $game 'Mods'))
$before = @{}
foreach ($target in $targets) {
    $before[$target] = if (Test-Path -LiteralPath $target -PathType Container) {
        @(Get-KbpDirectoryManifest $target)
    } else { $null }
}
$launcher = Join-Path $PSScriptRoot 'Invoke-KingmakerRuntimeTest.ps1'
$ErrorActionPreference = 'Continue'
$output = @(& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $launcher `
        -Scenario 'mod-load-smoke' -WhatIf 2>&1)
$childExit = $LASTEXITCODE
$ErrorActionPreference = 'Stop'
if ($childExit -ne 0) {
    throw "Runtime launcher -File -WhatIf failed with exit code $childExit.: $($output -join ' ')"
}
if (-not (@($output | Where-Object { "$_" -like '*Runtime WhatIf preflight PASS*' }).Count -ge 1)) {
    throw "Runtime launcher -File -WhatIf did not report the purity message.: $($output -join ' ')"
}
$changed = @()
foreach ($target in $targets) {
    $after = if (Test-Path -LiteralPath $target -PathType Container) {
        @(Get-KbpDirectoryManifest $target)
    } else { $null }
    if ($null -eq $before[$target]) {
        if ($null -ne $after) { $changed += @($target) }
    }
    elseif (-not (Test-KbpManifestEqual @($before[$target]) @($after))) {
        $changed += @($target)
    }
}
if (@($changed).Count -ne 0) { throw "Runtime launcher -File -WhatIf changed: $($changed -join ', ')" }

# Layer 4 (decision-evaluation failure sentinel): a REAL run under -File
# (no -WhatIf, so the confirmation decision must be evaluated) must be
# refused with a non-zero exit and must mutate nothing — the exact
# mutation path a naive fallback would have enabled.
$beforeReal = @{}
foreach ($target in $targets) {
    $beforeReal[$target] = if (Test-Path -LiteralPath $target -PathType Container) {
        @(Get-KbpDirectoryManifest $target)
    } else { $null }
}
$ErrorActionPreference = 'Continue'
$realOutput = @(& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $launcher `
        -Scenario 'mod-load-smoke' 2>&1)
$realExit = $LASTEXITCODE
$ErrorActionPreference = 'Stop'
if ($realExit -eq 0) {
    throw "Runtime launcher -File real run must refuse (decision cannot be evaluated); it exited 0.: $($realOutput -join ' ')"
}
if (-not (@($realOutput | Where-Object { "$_" -like '*decision could not be evaluated*' }).Count -ge 1)) {
    throw "Runtime launcher -File real run lacks the decision refusal message.: $($realOutput -join ' ')"
}
$changedReal = @()
foreach ($target in $targets) {
    $after = if (Test-Path -LiteralPath $target -PathType Container) {
        @(Get-KbpDirectoryManifest $target)
    } else { $null }
    if ($null -eq $beforeReal[$target]) {
        if ($null -ne $after) { $changedReal += @($target) }
    }
    elseif (-not (Test-KbpManifestEqual @($beforeReal[$target]) @($after))) {
        $changedReal += @($target)
    }
}
if (@($changedReal).Count -ne 0) { throw "Runtime launcher -File refused run changed: $($changedReal -join ', ')" }
# Layer 5 (single-cast probe gating, review L6/probe preparation): the
# casting probe cannot be launched without the owner's allowance file under
# the lab approvals directory; the selection-only probe never takes one;
# a selection-only -WhatIf is pure.
$outsideAllowance = Join-Path ([IO.Path]::GetTempPath()) ('kbp-probe-allowance-' + [Guid]::NewGuid().ToString('N') + '.json')
Set-Content -LiteralPath $outsideAllowance -Value '{}' -Encoding UTF8
try {
    $probeCases = @(
        @{ Name = 'casting-probe-without-allowance'; Expect = '*requires -ProbeAllowancePath*'
           Args = @('-Scenario', 'live-cast-probe', '-RunId', 'probe-gate-test', '-WhatIf') },
        @{ Name = 'casting-probe-allowance-outside-approvals'; Expect = '*must be an existing file under*'
           Args = @('-Scenario', 'live-cast-probe', '-RunId', 'probe-gate-test', '-ProbeAllowancePath', $outsideAllowance, '-WhatIf') },
        @{ Name = 'selection-probe-with-allowance'; Expect = '*only valid with -Scenario live-cast-probe*'
           Args = @('-Scenario', 'live-cast-probe-select', '-ProbeAllowancePath', $outsideAllowance, '-WhatIf') }
    )
    foreach ($case in $probeCases) {
        $ErrorActionPreference = 'Continue'
        $caseArgs = $case.Args
        $caseOutput = @(& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $launcher @caseArgs 2>&1)
        $caseExit = $LASTEXITCODE
        $ErrorActionPreference = 'Stop'
        if ($caseExit -eq 0 -or -not (@($caseOutput | Where-Object { "$_" -like $case.Expect }).Count -ge 1)) {
            throw "Probe gate case $($case.Name) was not refused as expected.: $($caseOutput -join ' ')"
        }
    }
}
finally { Remove-Item -LiteralPath $outsideAllowance -Force -ErrorAction SilentlyContinue }
$beforeSelect = @{}
foreach ($target in $targets) {
    $beforeSelect[$target] = if (Test-Path -LiteralPath $target -PathType Container) {
        @(Get-KbpDirectoryManifest $target)
    } else { $null }
}
$ErrorActionPreference = 'Continue'
$selectOutput = @(& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $launcher `
        -Scenario 'live-cast-probe-select' -WhatIf 2>&1)
$selectExit = $LASTEXITCODE
$ErrorActionPreference = 'Stop'
if ($selectExit -ne 0 -or -not (@($selectOutput | Where-Object { "$_" -like '*Runtime WhatIf preflight PASS*' }).Count -ge 1)) {
    throw "Selection-only probe -WhatIf failed.: $($selectOutput -join ' ')"
}
foreach ($target in $targets) {
    $after = if (Test-Path -LiteralPath $target -PathType Container) {
        @(Get-KbpDirectoryManifest $target)
    } else { $null }
    if (($null -eq $beforeSelect[$target]) -ne ($null -eq $after) -or
        ($null -ne $after -and -not (Test-KbpManifestEqual @($beforeSelect[$target]) @($after)))) {
        throw "Selection-only probe -WhatIf changed: $target"
    }
}
# Layer 6 (review N1): allowance-to-build binding. A same-commit
# replacement (different package, DLL or MVID) is refused before deploy.
. (Join-Path $PSScriptRoot 'RuntimeAutomation.Common.ps1')
$manifestFixture = [pscustomobject]@{ commit = ('c' * 40); packageSha256 = ('a' * 64)
    dllSha256 = ('b' * 64); assemblyMvid = '11111111-2222-3333-4444-555555555555' }
function New-AllowanceFixtureJson([hashtable]$Override) {
    $value = [ordered]@{ schemaVersion = 2; kind = 'kbp-single-cast-probe'; runId = 'probe-bind-test'
        sourceCommit = ('c' * 40); packageSha256 = ('a' * 64); dllSha256 = ('b' * 64)
        assemblyMvid = '11111111-2222-3333-4444-555555555555'; maximumNativeSubmissions = 1 }
    foreach ($key in $Override.Keys) { $value[$key] = $Override[$key] }
    return ($value | ConvertTo-Json -Compress)
}
if ($null -ne (Get-KbpProbeAllowanceBuildRefusal -AllowanceJson (New-AllowanceFixtureJson @{}) `
        -RunId 'probe-bind-test' -BuildManifest $manifestFixture)) {
    throw 'A matching allowance was refused by the build binding.'
}
$bindingCases = [ordered]@{
    'package' = @{ packageSha256 = ('d' * 64) }; 'dll' = @{ dllSha256 = ('e' * 64) }
    'mvid' = @{ assemblyMvid = '99999999-2222-3333-4444-555555555555' }
    'commit' = @{ sourceCommit = ('f' * 40) }; 'run-id' = @{ runId = 'other-run' }
    'submissions' = @{ maximumNativeSubmissions = 2 }
}
foreach ($case in $bindingCases.Keys) {
    $refusal = Get-KbpProbeAllowanceBuildRefusal -AllowanceJson (New-AllowanceFixtureJson $bindingCases[$case]) `
        -RunId 'probe-bind-test' -BuildManifest $manifestFixture
    if ($refusal -cne $case) { throw "Allowance binding case $case returned '$refusal'." }
}
# Layer 7 (advanced copy): the advanced family is refused for every casting
# or legacy-execution scenario before any save lookup, deployment or
# launch; only the non-casting inspection and workspace scenarios may load
# it.
$familyCases = @(
    @{ Name = 'advanced-probe-select'; Args = @('-Scenario', 'live-cast-probe-select', '-FixtureFamily', 'Advanced', '-WhatIf') },
    @{ Name = 'advanced-bootstrap'; Args = @('-Scenario', 'live-ui-bootstrap', '-FixtureFamily', 'Advanced', '-WhatIf') },
    @{ Name = 'advanced-smoke'; Args = @('-Scenario', 'mod-load-smoke', '-FixtureFamily', 'Advanced', '-WhatIf') }
)
foreach ($case in $familyCases) {
    $ErrorActionPreference = 'Continue'
    $caseArgs = $case.Args
    $caseOutput = @(& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $launcher @caseArgs 2>&1)
    $caseExit = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    if ($caseExit -eq 0 -or -not (@($caseOutput | Where-Object {
            "$_" -like '*may only be loaded by the non-casting scenarios*' }).Count -ge 1)) {
        throw "Advanced family case $($case.Name) was not refused as expected.: $($caseOutput -join ' ')"
    }
}
# Layer 8 (casting qualification): the casting run needs the run-bound
# allowance under the lab approvals directory, the selection-only run never
# takes one, and the build binding refuses a replacement artifact, another
# recipe or an out-of-range budget before anything is deployed.
$outsideQualification = Join-Path ([IO.Path]::GetTempPath()) ('kbp-qual-allowance-' + [Guid]::NewGuid().ToString('N') + '.json')
Set-Content -LiteralPath $outsideQualification -Value '{}' -Encoding UTF8
try {
    $qualificationCases = @(
        @{ Name = 'qual-without-allowance'; Expect = '*requires -QualificationAllowancePath*'
           Args = @('-Scenario', 'live-cast-qual', '-RunId', 'qual-gate-test', '-WhatIf') },
        @{ Name = 'qual-allowance-outside-approvals'; Expect = '*qualification allowance must be an existing file under*'
           Args = @('-Scenario', 'live-cast-qual', '-RunId', 'qual-gate-test', '-QualificationAllowancePath', $outsideQualification, '-WhatIf') },
        @{ Name = 'qual-select-with-allowance'; Expect = '*only valid with -Scenario live-cast-qual*'
           Args = @('-Scenario', 'live-cast-qual-select', '-QualificationAllowancePath', $outsideQualification, '-WhatIf') },
        @{ Name = 'qual-select-animated'; Expect = '*instant mode only*'
           Args = @('-Scenario', 'live-cast-qual-select', '-ExecutionMode', 'animated', '-WhatIf') }
    )
    foreach ($case in $qualificationCases) {
        $ErrorActionPreference = 'Continue'
        $caseArgs = $case.Args
        $caseOutput = @(& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $launcher @caseArgs 2>&1)
        $caseExit = $LASTEXITCODE
        $ErrorActionPreference = 'Stop'
        if ($caseExit -eq 0 -or -not (@($caseOutput | Where-Object { "$_" -like $case.Expect }).Count -ge 1)) {
            throw "Qualification gate case $($case.Name) was not refused as expected.: $($caseOutput -join ' ')"
        }
    }
}
finally { Remove-Item -LiteralPath $outsideQualification -Force -ErrorAction SilentlyContinue }
function New-QualificationFixtureJson([hashtable]$Override) {
    $value = [ordered]@{ schemaVersion = 3; kind = 'kbp-casting-qualification'; runId = 'qual-bind-test'
        sourceCommit = ('c' * 40); packageSha256 = ('a' * 64); dllSha256 = ('b' * 64)
        assemblyMvid = '11111111-2222-3333-4444-555555555555'; fixtureGameId = 'game'
        recipe = 'zero-cost-mixed'; approvedProjectionIds = @(('d' * 64)); maximumNativeSubmissions = 6
        approvedBy = 'Howie'; authority = 'owner mission 2026-09-23 section 4' }
    foreach ($key in $Override.Keys) { $value[$key] = $Override[$key] }
    return ($value | ConvertTo-Json -Compress)
}
if ($null -ne (Get-KbpQualificationAllowanceBuildRefusal -AllowanceJson (New-QualificationFixtureJson @{}) `
        -RunId 'qual-bind-test' -BuildManifest $manifestFixture)) {
    throw 'A matching qualification allowance was refused by the build binding.'
}
$qualificationBindingCases = [ordered]@{
    'package' = @{ packageSha256 = ('d' * 64) }; 'dll' = @{ dllSha256 = ('e' * 64) }
    'mvid' = @{ assemblyMvid = '99999999-2222-3333-4444-555555555555' }
    'commit' = @{ sourceCommit = ('f' * 40) }; 'run-id' = @{ runId = 'other-run' }
    'kind' = @{ kind = 'kbp-single-cast-probe' }; 'recipe' = @{ recipe = 'other' }
    'submissions' = @{ maximumNativeSubmissions = 25 }
}
foreach ($case in $qualificationBindingCases.Keys) {
    $refusal = Get-KbpQualificationAllowanceBuildRefusal -AllowanceJson (New-QualificationFixtureJson $qualificationBindingCases[$case]) `
        -RunId 'qual-bind-test' -BuildManifest $manifestFixture
    if ($refusal -cne $case) { throw "Qualification binding case $case returned '$refusal'." }
}
Write-Host 'Launcher -File WhatIf purity: PASS=11 FAIL=0'
