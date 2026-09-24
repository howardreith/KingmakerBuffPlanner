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
$launcher = Join-Path $PSScriptRoot 'Invoke-KingmakerRuntimeTest.ps1'
Invoke-KbpLivePurityWindow -Label 'Runtime launcher -File -WhatIf' -Targets $targets -Action {
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
}

# Layer 4 (decision-evaluation failure sentinel): a REAL run under -File
# (no -WhatIf, so the confirmation decision must be evaluated) must be
# refused with a non-zero exit and must mutate nothing — the exact
# mutation path a naive fallback would have enabled.
Invoke-KbpLivePurityWindow -Label 'Runtime launcher -File refused run' -Targets $targets -Action {
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
}
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
           Args = @('-Scenario', 'live-cast-probe-select', '-ProbeAllowancePath', $outsideAllowance, '-WhatIf') },
        # Review of f7726c9..1332ed8, P3-H: a probe needs the boot/load
        # budget plus its world wait and deadline.
        @{ Name = 'selection-probe-short-timeout'; Expect = '*TimeoutSeconds must be at least 600*'
           Args = @('-Scenario', 'live-cast-probe-select', '-TimeoutSeconds', '300', '-WhatIf') },
        # P3-J: a differently cased scenario binds as the canonical one, so
        # every case-sensitive check below still applies to it.
        @{ Name = 'uppercase-casting-qualification'; Expect = '*live-cast-qual requires -QualificationAllowancePath*'
           Args = @('-Scenario', 'LIVE-CAST-QUAL', '-RunId', 'qual-case-test', '-TimeoutSeconds', '900', '-WhatIf') }
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
Invoke-KbpLivePurityWindow -Label 'Selection-only probe -WhatIf' -Targets $targets -Action {
    $ErrorActionPreference = 'Continue'
    $selectOutput = @(& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $launcher `
            -Scenario 'live-cast-probe-select' -TimeoutSeconds 600 -WhatIf 2>&1)
    $selectExit = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    if ($selectExit -ne 0 -or -not (@($selectOutput | Where-Object { "$_" -like '*Runtime WhatIf preflight PASS*' }).Count -ge 1)) {
        throw "Selection-only probe -WhatIf failed.: $($selectOutput -join ' ')"
    }
}
# Layer 6 (review N1): allowance-to-build binding. A same-commit
# replacement (different package, DLL or MVID) is refused before deploy.
. (Join-Path $PSScriptRoot 'RuntimeAutomation.Common.ps1')
$manifestFixture = [pscustomobject]@{ commit = ('c' * 40); packageSha256 = ('a' * 64)
    dllSha256 = ('b' * 64); assemblyMvid = '11111111-2222-3333-4444-555555555555' }
function New-AllowanceFixtureJson([hashtable]$Override) {
    $value = [ordered]@{ schemaVersion = 3; kind = 'kbp-single-cast-probe'; runId = 'probe-bind-test'
        sourceCommit = ('c' * 40); packageSha256 = ('a' * 64); dllSha256 = ('b' * 64)
        assemblyMvid = '11111111-2222-3333-4444-555555555555'; maximumNativeSubmissions = 1
        approvedProjectionId = ('f' * 64); casterUnitId = 'caster'; targetUnitId = 'target'; sourceId = 'source'
        approvedBy = 'Howie'
        compatibilityProfileId = 'full-user'; compatibilityIdentity = ('e' * 64); workingSaveSha256 = ('9' * 64)
        purpose = 'single-cast probe test' }
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
    'submissions' = @{ maximumNativeSubmissions = 2 }; 'schema' = @{ schemaVersion = 2 }
    'purpose' = @{ purpose = '' }
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
    @{ Name = 'advanced-probe-select'; Args = @('-Scenario', 'live-cast-probe-select', '-FixtureFamily', 'Advanced', '-TimeoutSeconds', '600', '-WhatIf') },
    @{ Name = 'advanced-bootstrap'; Args = @('-Scenario', 'live-ui-bootstrap', '-FixtureFamily', 'Advanced', '-WhatIf') },
    @{ Name = 'advanced-smoke'; Args = @('-Scenario', 'mod-load-smoke', '-FixtureFamily', 'Advanced', '-WhatIf') },
    @{ Name = 'advanced-reload'; Args = @('-Scenario', 'live-workspace-reload', '-FixtureFamily', 'Advanced', '-TimeoutSeconds', '600', '-WhatIf') },
    @{ Name = 'advanced-import'; Args = @('-Scenario', 'live-workspace-import', '-FixtureFamily', 'Advanced', '-WhatIf') }
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
# Layer 7b (advanced profile, mission batch 3 section 5): the advanced copy
# runs only under its own profile, and that profile only with the advanced
# copy; both are refused before any save lookup, deployment or launch.
$profileCases = @(
    @{ Name = 'advanced-copy-full-user'; Args = @('-Scenario', 'live-advanced-inspect', '-FixtureFamily', 'Advanced', '-CompatibilityProfileId', 'full-user', '-WhatIf') },
    @{ Name = 'automation-copy-advanced-profile'; Args = @('-Scenario', 'live-advanced-inspect', '-CompatibilityProfileId', 'advanced-gunslinger-0136', '-WhatIf') },
    @{ Name = 'advanced-profile-smoke'; Args = @('-Scenario', 'mod-load-smoke', '-CompatibilityProfileId', 'advanced-gunslinger-0136', '-WhatIf') }
)
foreach ($case in $profileCases) {
    $ErrorActionPreference = 'Continue'
    $caseArgs = $case.Args
    $caseOutput = @(& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $launcher @caseArgs 2>&1)
    $caseExit = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    if ($caseExit -eq 0 -or -not (@($caseOutput | Where-Object {
            "$_" -like '*runs only with -CompatibilityProfileId advanced-gunslinger-0136*' }).Count -ge 1)) {
        throw "Advanced profile case $($case.Name) was not refused as expected.: $($caseOutput -join ' ')"
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
           Args = @('-Scenario', 'live-cast-qual-select', '-ExecutionMode', 'animated', '-TimeoutSeconds', '900', '-WhatIf') },
        @{ Name = 'qual-select-short-timeout'; Expect = '*at least 900*'
           Args = @('-Scenario', 'live-cast-qual-select', '-WhatIf') },
        @{ Name = 'classic-without-allowance'; Expect = '*requires -ClassicAllowancePath*'
           Args = @('-Scenario', 'live-classic-cast', '-RunId', 'classic-gate-test', '-TimeoutSeconds', '900', '-WhatIf') },
        @{ Name = 'classic-allowance-outside-approvals'; Expect = '*classic allowance must be an existing file under*'
           Args = @('-Scenario', 'live-classic-cast', '-RunId', 'classic-gate-test', '-ClassicAllowancePath', $outsideQualification, '-TimeoutSeconds', '900', '-WhatIf') },
        @{ Name = 'classic-select-with-allowance'; Expect = '*only valid with -Scenario live-classic-cast*'
           Args = @('-Scenario', 'live-classic-select', '-ClassicAllowancePath', $outsideQualification, '-TimeoutSeconds', '900', '-WhatIf') },
        @{ Name = 'classic-select-short-timeout'; Expect = '*at least 900*'
           Args = @('-Scenario', 'live-classic-select', '-WhatIf') },
        @{ Name = 'display-mode-other-scenario'; Expect = '*only valid with -Scenario live-workspace-physical or live-workspace-qual*'
           Args = @('-Scenario', 'live-workspace-import', '-DisplayMode', 'windowed-1920x1080', '-WhatIf') },
        @{ Name = 'physical-short-timeout'; Expect = '*at least 900*'
           Args = @('-Scenario', 'live-workspace-physical', '-WhatIf') }
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
    $value = [ordered]@{ schemaVersion = 5; kind = 'kbp-casting-qualification'; runId = 'qual-bind-test'
        sourceCommit = ('c' * 40); packageSha256 = ('a' * 64); dllSha256 = ('b' * 64)
        assemblyMvid = '11111111-2222-3333-4444-555555555555'; fixtureGameId = 'game'
        recipe = 'zero-cost-mixed'; executionMode = 'instant'; approvedProjectionIds = @(('d' * 64)); maximumNativeSubmissions = 6
        approvedBy = 'Howie'; authority = 'owner mission 2026-09-23 section 4'
        compatibilityProfileId = 'full-user'; compatibilityIdentity = ('e' * 64); workingSaveSha256 = ('9' * 64)
        purpose = 'casting-first qualification test' }
    foreach ($key in $Override.Keys) { $value[$key] = $Override[$key] }
    return ($value | ConvertTo-Json -Compress)
}
if ($null -ne (Get-KbpQualificationAllowanceBuildRefusal -AllowanceJson (New-QualificationFixtureJson @{}) `
        -RunId 'qual-bind-test' -BuildManifest $manifestFixture)) {
    throw 'A matching qualification allowance was refused by the build binding.'
}
# Every recipe the host knows can be approved (the group recipe included).
foreach ($knownRecipe in @('finite-direct-mixed', 'group-mixed')) {
    if ($null -ne (Get-KbpQualificationAllowanceBuildRefusal -AllowanceJson (New-QualificationFixtureJson @{ recipe = $knownRecipe }) `
            -RunId 'qual-bind-test' -BuildManifest $manifestFixture -Recipe $knownRecipe)) {
        throw "A $knownRecipe qualification allowance was refused by the build binding."
    }
}
$qualificationBindingCases = [ordered]@{
    'package' = @{ packageSha256 = ('d' * 64) }; 'dll' = @{ dllSha256 = ('e' * 64) }
    'mvid' = @{ assemblyMvid = '99999999-2222-3333-4444-555555555555' }
    'commit' = @{ sourceCommit = ('f' * 40) }; 'run-id' = @{ runId = 'other-run' }
    'kind' = @{ kind = 'kbp-single-cast-probe' }; 'recipe' = @{ recipe = 'other' }
    'submissions' = @{ maximumNativeSubmissions = 25 }
    'schema' = @{ schemaVersion = 4 }; 'execution-mode' = @{ executionMode = 'hybrid' }
    'purpose' = @{ purpose = ' ' }
}
foreach ($case in $qualificationBindingCases.Keys) {
    $refusal = Get-KbpQualificationAllowanceBuildRefusal -AllowanceJson (New-QualificationFixtureJson $qualificationBindingCases[$case]) `
        -RunId 'qual-bind-test' -BuildManifest $manifestFixture
    if ($refusal -cne $case) { throw "Qualification binding case $case returned '$refusal'." }
}
# Schema 4 names the casting mode: an allowance without it is refused, and
# the launcher's -ExecutionMode must be the approved one.
$modeless = (New-QualificationFixtureJson @{}) -replace ',"executionMode":"instant"', ''
if ((Get-KbpQualificationAllowanceBuildRefusal -AllowanceJson $modeless -RunId 'qual-bind-test' `
        -BuildManifest $manifestFixture) -cne 'missing:executionMode') {
    throw 'An allowance without a casting mode was not refused.'
}
$animatedJson = New-QualificationFixtureJson @{ executionMode = 'animated' }
if ($null -ne (Get-KbpQualificationAllowanceBuildRefusal -AllowanceJson $animatedJson -RunId 'qual-bind-test' `
        -BuildManifest $manifestFixture -ExecutionMode 'animated') -or
    (Get-KbpQualificationAllowanceBuildRefusal -AllowanceJson $animatedJson -RunId 'qual-bind-test' `
        -BuildManifest $manifestFixture -ExecutionMode 'instant') -cne 'execution-mode-differs' -or
    (Get-KbpQualificationAllowanceBuildRefusal -AllowanceJson (New-QualificationFixtureJson @{}) -RunId 'qual-bind-test' `
        -BuildManifest $manifestFixture -ExecutionMode 'animated') -cne 'execution-mode-differs') {
    throw 'The qualification casting-mode binding is wrong.'
}
$launcherText = Get-Content -LiteralPath $launcher -Raw
if ($launcherText -notmatch '-Recipe \$QualificationRecipe -ExecutionMode \$ExecutionMode' -or
    $launcherText -match "live-cast-qual runs in instant mode only") {
    throw 'The launcher does not bind the casting run to its allowance mode.'
}
# The classic allowance binds the build, one casting mode and a budget.
function New-ClassicFixtureJson([hashtable]$Override) {
    $value = [ordered]@{ schemaVersion = 2; kind = 'kbp-classic-cast'; runId = 'classic-bind-test'
        sourceCommit = ('c' * 40); packageSha256 = ('a' * 64); dllSha256 = ('b' * 64)
        assemblyMvid = '11111111-2222-3333-4444-555555555555'; fixtureGameId = 'game'
        executionMode = 'animated'; routineId = 'long'; approvedPlanDigest = ('d' * 64); maximumNativeSubmissions = 3
        approvedBy = 'Howie'; authority = 'owner mission 2026-09-23 batch 3 section 6'
        compatibilityProfileId = 'full-user'; compatibilityIdentity = ('e' * 64); workingSaveSha256 = ('9' * 64)
        purpose = 'classic cast qualification test' }
    foreach ($key in $Override.Keys) { $value[$key] = $Override[$key] }
    return ($value | ConvertTo-Json -Compress)
}
if ($null -ne (Get-KbpClassicAllowanceBuildRefusal -AllowanceJson (New-ClassicFixtureJson @{}) `
        -RunId 'classic-bind-test' -BuildManifest $manifestFixture -ExecutionMode 'animated')) {
    throw 'A matching classic allowance was refused by the build binding.'
}
$classicBindingCases = [ordered]@{
    'package' = @{ packageSha256 = ('d' * 64) }; 'dll' = @{ dllSha256 = ('e' * 64) }
    'mvid' = @{ assemblyMvid = '99999999-2222-3333-4444-555555555555' }
    'commit' = @{ sourceCommit = ('f' * 40) }; 'run-id' = @{ runId = 'other-run' }
    'kind' = @{ kind = 'kbp-casting-qualification' }; 'schema' = @{ schemaVersion = 1 }
    'execution-mode' = @{ executionMode = 'hybrid' }; 'routine' = @{ routineId = 'short' }
    'plan-digest' = @{ approvedPlanDigest = 'XYZ' }; 'submissions' = @{ maximumNativeSubmissions = 25 }
    'purpose' = @{ purpose = '' }
}
foreach ($case in $classicBindingCases.Keys) {
    $refusal = Get-KbpClassicAllowanceBuildRefusal -AllowanceJson (New-ClassicFixtureJson $classicBindingCases[$case]) `
        -RunId 'classic-bind-test' -BuildManifest $manifestFixture -ExecutionMode 'animated'
    if ($refusal -cne $case) { throw "Classic binding case $case returned '$refusal'." }
}
if ((Get-KbpClassicAllowanceBuildRefusal -AllowanceJson (New-ClassicFixtureJson @{}) -RunId 'classic-bind-test' `
        -BuildManifest $manifestFixture -ExecutionMode 'instant') -cne 'execution-mode-differs') {
    throw 'A classic allowance ran in another casting mode.'
}
# Final review C7: what the host's parsers refuse after launch is refused
# before anything is deployed.
$shapeCases = @(
    @('qualification', (New-QualificationFixtureJson @{ extra = 'x' }), 'unknown:extra'),
    @('qualification', (New-QualificationFixtureJson @{ approvedBy = '' }), 'approved-by'),
    @('qualification', (New-QualificationFixtureJson @{ authority = '' }), 'authority'),
    @('qualification', (New-QualificationFixtureJson @{ fixtureGameId = '' }), 'fixture-game-id'),
    @('qualification', (New-QualificationFixtureJson @{ approvedProjectionIds = @('NOTHEX') }), 'projection-ids'),
    @('qualification', (New-QualificationFixtureJson @{ approvedProjectionIds = @(1..9 | ForEach-Object { ('d' * 64) }) }), 'projection-ids'),
    @('classic', (New-ClassicFixtureJson @{ extra = 'x' }), 'unknown:extra'),
    @('classic', (New-ClassicFixtureJson @{ approvedBy = '' }), 'approved-by'),
    @('probe', (New-AllowanceFixtureJson @{ extra = 'x' }), 'unknown:extra'),
    @('probe', (New-AllowanceFixtureJson @{ approvedProjectionId = 'x' }), 'projection-id'),
    @('probe', (New-AllowanceFixtureJson @{ targetUnitId = 'caster' }), 'selection'),
    @('probe', (New-AllowanceFixtureJson @{ sourceId = '' }), 'selection'),
    @('probe', (New-AllowanceFixtureJson @{ approvedBy = '' }), 'approved-by'))
foreach ($shapeCase in $shapeCases) {
    $shapeRefusal = switch ($shapeCase[0]) {
        'qualification' { Get-KbpQualificationAllowanceBuildRefusal -AllowanceJson $shapeCase[1] -RunId 'qual-bind-test' -BuildManifest $manifestFixture }
        'classic' { Get-KbpClassicAllowanceBuildRefusal -AllowanceJson $shapeCase[1] -RunId 'classic-bind-test' -BuildManifest $manifestFixture }
        'probe' { Get-KbpProbeAllowanceBuildRefusal -AllowanceJson $shapeCase[1] -RunId 'probe-bind-test' -BuildManifest $manifestFixture }
    }
    if ($shapeRefusal -cne $shapeCase[2]) {
        throw "A malformed $($shapeCase[0]) allowance was not refused before launch as $($shapeCase[2]): $shapeRefusal"
    }
}
# Review C5: both allowance kinds bind the profile, its identity and the
# WORKING save; the launcher checks them against what it resolved before
# anything is deployed.
foreach ($fixtureJson in @((New-QualificationFixtureJson @{}), (New-ClassicFixtureJson @{}), (New-AllowanceFixtureJson @{}))) {
    $bindingCases = [ordered]@{
        'profile' = @('native-only', ('e' * 64), ('9' * 64))
        'compatibility-identity' = @('full-user', ('f' * 64), ('9' * 64))
        'working-save' = @('full-user', ('e' * 64), ('8' * 64))
    }
    if ($null -ne (Get-KbpAllowanceFixtureBindingRefusal -AllowanceJson $fixtureJson -ProfileId 'full-user' `
            -CompatibilityIdentity ('e' * 64) -WorkingSaveSha256 ('9' * 64) -FixtureGameId 'game')) {
        throw 'A matching allowance fixture binding was refused.'
    }
    # Final review C7: an allowance naming a fixture campaign names this one.
    $namesCampaign = @(($fixtureJson | ConvertFrom-Json).PSObject.Properties | ForEach-Object Name) -ccontains 'fixtureGameId'
    $otherCampaign = Get-KbpAllowanceFixtureBindingRefusal -AllowanceJson $fixtureJson -ProfileId 'full-user' `
        -CompatibilityIdentity ('e' * 64) -WorkingSaveSha256 ('9' * 64) -FixtureGameId 'other-game'
    if (($namesCampaign -and $otherCampaign -cne 'fixture-game-id') -or (-not $namesCampaign -and $null -ne $otherCampaign)) {
        throw "An allowance for another fixture campaign was not refused: $otherCampaign"
    }
    foreach ($case in $bindingCases.Keys) {
        $values = $bindingCases[$case]
        $refusal = Get-KbpAllowanceFixtureBindingRefusal -AllowanceJson $fixtureJson -ProfileId $values[0] `
            -CompatibilityIdentity $values[1] -WorkingSaveSha256 $values[2] -FixtureGameId 'game'
        if ($refusal -cne $case) { throw "Allowance fixture binding case $case returned '$refusal'." }
    }
    if ((Get-KbpAllowanceFixtureBindingRefusal -AllowanceJson $fixtureJson -ProfileId 'full-user' `
            -CompatibilityIdentity ('e' * 64) -WorkingSaveSha256 $null -FixtureGameId 'game') -cne 'working-save') {
        throw 'An allowance without a resolved WORKING save was accepted.'
    }
}
# Re-review: the launcher refuses what the host's parser would refuse only
# after launch (JSON strings, a known profile, lowercase hashes, a purpose of
# at most 400 characters), for every allowance kind.
$formatCases = [ordered]@{
    'purpose' = @{ purpose = ('p' * 401) }
    'binding-format:compatibilityProfileId' = @{ compatibilityProfileId = @('full-user') }
    'binding-format:workingSaveSha256' = @{ workingSaveSha256 = ('A' * 64) }
    'binding-format:compatibilityIdentity' = @{ compatibilityIdentity = @(('e' * 64)) }
}
foreach ($case in $formatCases.Keys) {
    $override = $formatCases[$case]
    $refusals = @(
        (Get-KbpQualificationAllowanceBuildRefusal -AllowanceJson (New-QualificationFixtureJson $override) `
            -RunId 'qual-bind-test' -BuildManifest $manifestFixture),
        (Get-KbpClassicAllowanceBuildRefusal -AllowanceJson (New-ClassicFixtureJson $override) `
            -RunId 'classic-bind-test' -BuildManifest $manifestFixture -ExecutionMode 'animated'),
        (Get-KbpProbeAllowanceBuildRefusal -AllowanceJson (New-AllowanceFixtureJson $override) `
            -RunId 'probe-bind-test' -BuildManifest $manifestFixture))
    foreach ($refusal in $refusals) {
        if ($refusal -cne $case) { throw "Binding format case $case returned '$refusal'." }
    }
}
if ((Get-KbpClassicAllowanceBuildRefusal -AllowanceJson (New-ClassicFixtureJson @{ compatibilityProfileId = 'other-profile' }) `
        -RunId 'classic-bind-test' -BuildManifest $manifestFixture -ExecutionMode 'animated') -cne 'binding-format:compatibilityProfileId') {
    throw 'Binding format case: an unknown profile passed.'
}
# Re-review (harness): the WORKING save is re-checked with the protected
# snapshot, once the run holds its deployment lock (after Deploy-Local).
$probeBindingAt = $launcherText.IndexOf('Get-KbpAllowanceFixtureBindingRefusal -AllowanceJson $probeAllowanceJson')
$workingRecheckAt = $launcherText.IndexOf('throw "The WORKING save changed after it was bound: $($savePair.working.fileName)"')
$realDeployAt = $launcherText.IndexOf('$statePath = & (Join-Path $PSScriptRoot ''Deploy-Local.ps1'')')
$finalStatusAt = $launcherText.IndexOf('$orchestration.finalStatus = if ([bool]$completionRecord.complete)')
if ($probeBindingAt -lt 0 -or $workingRecheckAt -lt 0 -or $realDeployAt -lt 0 -or $finalStatusAt -lt 0 -or
    $workingRecheckAt -lt $realDeployAt -or $finalStatusAt -lt $realDeployAt) {
    throw 'The launcher does not bind the probe allowance, re-check the WORKING save under its lock, or record the final status.'
}
$bindingAt = $launcherText.IndexOf('Get-KbpAllowanceFixtureBindingRefusal -AllowanceJson $classicAllowanceJson')
$qualificationBindingAt = $launcherText.IndexOf('Get-KbpAllowanceFixtureBindingRefusal -AllowanceJson $qualificationAllowanceJson')
$deployAt = $launcherText.IndexOf("-RunId 'runtime-whatif-preflight'")
$pairAt = $launcherText.IndexOf('Get-KbpDisposableSavePair -Family $FixtureFamily')
if ($bindingAt -lt 0 -or $qualificationBindingAt -lt 0 -or $pairAt -lt 0 -or $deployAt -lt 0 -or
    $bindingAt -lt $pairAt -or $qualificationBindingAt -lt $pairAt -or $probeBindingAt -lt $pairAt -or
    $bindingAt -gt $deployAt -or $qualificationBindingAt -gt $deployAt -or $probeBindingAt -gt $deployAt) {
    throw 'The launcher does not bind allowances to the resolved profile and save before deploying.'
}
# Review C6: the launcher's own reading of Classic and physical evidence.
$outcomeRoot = Join-Path $env:TEMP ('kbp-outcome-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $outcomeRoot | Out-Null
try {
    $digest = 'd' * 64
    $classicAllowance = New-ClassicFixtureJson @{ approvedPlanDigest = $digest }
    function New-ClassicOutcomeCase([string]$Name, [string]$Scenario, [hashtable]$Override) {
        $directory = Join-Path $outcomeRoot $Name
        New-Item -ItemType Directory -Path $directory | Out-Null
        $cast = $Scenario -ceq 'live-classic-cast'
        $value = [ordered]@{
            schemaVersion = 2; runId = 'classic-bind-test'; scenario = $Scenario; castingScenario = $cast
            allowanceStatus = if ($cast) { 'valid' } else { 'not-read' }; executionMode = 'animated'
            planDigest = $digest; planSteps = 1; grant = 'consumed'; grantConsumed = $cast
            grantAttempts = if ($cast) { 1 } else { 0 }; quickDisposition = if ($cast) { 'Completed' } else { $null }
            castingFirstRuns = 0; grantArmedAtEnd = $false; classicRunSeen = $false
            steps = @(if ($cast) { [ordered]@{ index = 0; finalStatus = 'EffectConfirmed' } })
            failures = @(); violations = @()
        }
        foreach ($key in $Override.Keys) { $value[$key] = $Override[$key] }
        Write-KbpJsonAtomic (Join-Path $directory 'classic-outcome.json') $value
        return [ordered]@{ runId = 'classic-bind-test'; scenario = $Scenario; evidenceDirectory = $directory
            parameters = @{ executionMode = 'animated'; classicAllowance = $classicAllowance } }
    }
    Assert-KbpScenarioOutcome -Request (New-ClassicOutcomeCase 'cast-good' 'live-classic-cast' @{})
    Assert-KbpScenarioOutcome -Request (New-ClassicOutcomeCase 'select-good' 'live-classic-select' @{})
    $classicOutcomeCases = [ordered]@{
        'other-digest' = @('live-classic-cast', @{ planDigest = ('e' * 64) })
        'grant-twice' = @('live-classic-cast', @{ grantAttempts = 2 })
        'grant-unused' = @('live-classic-cast', @{ grantConsumed = $false })
        'invalid-allowance' = @('live-classic-cast', @{ allowanceStatus = 'plan-differs-from-approval' })
        'over-budget' = @('live-classic-cast', @{ planSteps = 4 })
        'unconfirmed' = @('live-classic-cast', @{ steps = @([ordered]@{ index = 0; finalStatus = 'TimedOutUnconfirmed' }) })
        'violation' = @('live-classic-cast', @{ violations = @('step0:availability-unread') })
        'old-schema' = @('live-classic-cast', @{ schemaVersion = 1 })
        'other-mode' = @('live-classic-cast', @{ executionMode = 'instant' })
        'armed' = @('live-classic-cast', @{ grantArmedAtEnd = $true })
        'select-grant' = @('live-classic-select', @{ grantAttempts = 1 })
        'select-run' = @('live-classic-select', @{ classicRunSeen = $true })
    }
    foreach ($case in $classicOutcomeCases.Keys) {
        $caseRequest = New-ClassicOutcomeCase $case $classicOutcomeCases[$case][0] $classicOutcomeCases[$case][1]
        $refusal = $null
        try { Assert-KbpScenarioOutcome -Request $caseRequest }
        catch { $refusal = $_.Exception.Message }
        if ($null -eq $refusal -or $refusal -notlike '*Classic*') {
            throw "Classic outcome case $case was not refused by the launcher's check: $refusal"
        }
    }
    $physicalActions = @('ws-click-search', 'ws-wheel-grid', 'ws-type-query', 'ws-click-tile',
        'ws-focus-cycle', 'ws-click-search-again', 'ws-type-more', 'ws-escape')
    $physicalKinds = @{ 'ws-click-search' = 'click'; 'ws-wheel-grid' = 'wheel'; 'ws-type-query' = 'type'
        'ws-click-tile' = 'click'; 'ws-focus-cycle' = 'focus-cycle'; 'ws-click-search-again' = 'click'
        'ws-type-more' = 'type'; 'ws-escape' = 'key-escape' }
    function New-PhysicalOutcomeCase([string]$Name, [scriptblock]$Tamper) {
        $directory = Join-Path $outcomeRoot $Name
        New-Item -ItemType Directory -Path $directory | Out-Null
        foreach ($id in $physicalActions) {
            $sent = [ordered]@{ schemaVersion = 1; runId = 'physical-run'; actionId = $id; action = $physicalKinds[$id] }
            if ($id -ceq 'ws-type-query') { $sent.text = 'resi' }
            if ($id -ceq 'ws-type-more') { $sent.text = 's' }
            Write-KbpJsonAtomic (Join-Path $directory "physical-input-$id.json") $sent
            Write-KbpJsonAtomic (Join-Path $directory "physical-input-$id.ack.json") ([ordered]@{
                schemaVersion = 1; runId = 'physical-run'; actionId = $id; action = $physicalKinds[$id]
                processId = 4242; text = if ($sent.Contains('text')) { $sent.text } else { $null } })
        }
        Write-KbpJsonAtomic (Join-Path $directory 'orchestration.json') ([ordered]@{
            runId = 'physical-run'; kingmakerProcessId = 4242; plannerHotkeySentAtUtc = '2026-09-24T00:00:00Z' })
        $record = [ordered]@{ schemaVersion = 1; runId = 'physical-run'; expectedScreen = '1920x1080'
            screen = '1920x1080'; openedPhysically = $true; query = 'resi'; querySuffix = 's'
            acknowledged = $physicalActions; failures = @(); violations = @() }
        Write-KbpJsonAtomic (Join-Path $directory 'physical-workspace.json') $record
        if ($null -ne $Tamper) { & $Tamper $directory }
        return [ordered]@{ runId = 'physical-run'; scenario = 'live-workspace-physical'; evidenceDirectory = $directory
            parameters = @{ expectedScreen = '1920x1080' } }
    }
    Assert-KbpScenarioOutcome -Request (New-PhysicalOutcomeCase 'physical-good' $null)
    $physicalOutcomeCases = [ordered]@{
        'missing-ack' = { param($d) Remove-Item -LiteralPath (Join-Path $d 'physical-input-ws-wheel-grid.ack.json') }
        'failed-ack' = { param($d) Write-KbpJsonAtomic (Join-Path $d 'physical-input-ws-escape.ack.json') ([ordered]@{
            schemaVersion = 1; runId = 'physical-run'; actionId = 'ws-escape'; action = 'key-escape'; deliveryFailed = $true }) }
        'other-action' = { param($d) Write-KbpJsonAtomic (Join-Path $d 'physical-input-ws-click-tile.ack.json') ([ordered]@{
            schemaVersion = 1; runId = 'physical-run'; actionId = 'ws-click-tile'; action = 'hover' }) }
        'other-text' = { param($d) Write-KbpJsonAtomic (Join-Path $d 'physical-input-ws-type-query.json') ([ordered]@{
            schemaVersion = 1; runId = 'physical-run'; actionId = 'ws-type-query'; action = 'type'; text = 'ligh' }) }
        'unacknowledged' = { param($d) $r = Read-KbpJson (Join-Path $d 'physical-workspace.json')
            $r.acknowledged = @('ws-click-search'); Write-KbpJsonAtomic (Join-Path $d 'physical-workspace.json') $r }
        'other-screen' = { param($d) $r = Read-KbpJson (Join-Path $d 'physical-workspace.json')
            $r.screen = '1920x1200'; Write-KbpJsonAtomic (Join-Path $d 'physical-workspace.json') $r }
        'programmatic-open' = { param($d) $r = Read-KbpJson (Join-Path $d 'physical-workspace.json')
            $r.openedPhysically = $false; Write-KbpJsonAtomic (Join-Path $d 'physical-workspace.json') $r }
        'extra-failed-ack' = { param($d) Write-KbpJsonAtomic (Join-Path $d 'physical-input-ws-other.ack.json') ([ordered]@{
            schemaVersion = 1; runId = 'physical-run'; actionId = 'ws-other'; action = 'click'; deliveryFailed = $true }) }
        'extra-request' = { param($d) Write-KbpJsonAtomic (Join-Path $d 'physical-input-ws-extra.json') ([ordered]@{
            schemaVersion = 1; runId = 'physical-run'; actionId = 'ws-extra'; action = 'click' }) }
        'host-fallback-open' = { param($d) Write-KbpJsonAtomic (Join-Path $d 'programmatic-open.json') ([ordered]@{ opened = $true }) }
        'no-hotkey-sent' = { param($d) Write-KbpJsonAtomic (Join-Path $d 'orchestration.json') ([ordered]@{
            runId = 'physical-run'; kingmakerProcessId = 4242 }) }
        'ack-other-process' = { param($d) Write-KbpJsonAtomic (Join-Path $d 'physical-input-ws-click-search.ack.json') ([ordered]@{
            schemaVersion = 1; runId = 'physical-run'; actionId = 'ws-click-search'; action = 'click'; processId = 7; text = $null }) }
        'ack-other-text' = { param($d) Write-KbpJsonAtomic (Join-Path $d 'physical-input-ws-type-more.ack.json') ([ordered]@{
            schemaVersion = 1; runId = 'physical-run'; actionId = 'ws-type-more'; action = 'type'; processId = 4242; text = 'x' }) }
    }
    foreach ($case in $physicalOutcomeCases.Keys) {
        $caseRequest = New-PhysicalOutcomeCase $case $physicalOutcomeCases[$case]
        $refusal = $null
        try { Assert-KbpScenarioOutcome -Request $caseRequest }
        catch { $refusal = $_.Exception.Message }
        if ($null -eq $refusal -or ($refusal -notlike '*hysical*' -and $refusal -notlike '*physical run*')) {
            throw "Physical outcome case $case was not refused by the launcher's check: $refusal"
        }
    }
    # The qualification's own evidence: exactly the approved projections,
    # the approved mode, within the budget, completed without violations.
    $qualAllowance = New-QualificationFixtureJson @{ approvedProjectionIds = @(('1' * 64), ('2' * 64)); executionMode = 'animated'
        maximumNativeSubmissions = 8 }
    function New-QualOutcomeCase([string]$Name, [string]$Scenario, [hashtable]$Override) {
        $directory = Join-Path $outcomeRoot $Name
        New-Item -ItemType Directory -Path $directory | Out-Null
        $cast = $Scenario -ceq 'live-cast-qual'
        $value = [ordered]@{
            schemaVersion = 1; runId = 'qual-bind-test'; scenario = $Scenario; castingScenario = $cast
            allowanceStatus = if ($cast) { 'valid' } else { 'not-required' }; terminalReason = 'completed'
            selection = [ordered]@{ selected = $true; recipe = 'zero-cost-mixed' }
            forecast = @([ordered]@{ name = 'stop'; projectionId = ('1' * 64) }, [ordered]@{ name = 'complete'; projectionId = ('2' * 64) })
            plannedSubmissions = 5; maximumSubmissions = if ($cast) { 8 } else { 0 }; executionMode = 'animated'
            failures = @(); violations = @()
        }
        foreach ($key in $Override.Keys) { $value[$key] = $Override[$key] }
        Write-KbpJsonAtomic (Join-Path $directory 'qual-outcome.json') $value
        return [ordered]@{ runId = 'qual-bind-test'; scenario = $Scenario; evidenceDirectory = $directory
            parameters = @{ executionMode = 'animated'; qualificationAllowance = $qualAllowance } }
    }
    Assert-KbpScenarioOutcome -Request (New-QualOutcomeCase 'qual-cast-good' 'live-cast-qual' @{})
    Assert-KbpScenarioOutcome -Request (New-QualOutcomeCase 'qual-select-good' 'live-cast-qual-select' @{})
    $qualOutcomeCases = [ordered]@{
        'other-projections' = @('live-cast-qual', @{ forecast = @([ordered]@{ name = 'stop'; projectionId = ('3' * 64) }) })
        'over-budget' = @('live-cast-qual', @{ plannedSubmissions = 9 })
        'other-maximum' = @('live-cast-qual', @{ maximumSubmissions = 24 })
        'other-mode' = @('live-cast-qual', @{ executionMode = 'instant' })
        'invalid-allowance' = @('live-cast-qual', @{ allowanceStatus = 'identity-mismatch:dll' })
        'violation' = @('live-cast-qual', @{ violations = @('recover:missing') })
        'not-completed' = @('live-cast-qual', @{ terminalReason = 'failed:disable-wait' })
        'select-casting' = @('live-cast-qual-select', @{ castingScenario = $true })
        'select-unselected' = @('live-cast-qual-select', @{ selection = [ordered]@{ selected = $false } })
    }
    # Final review C6: the launcher reads a probe PASS too.
    $probeAllowanceJson = New-AllowanceFixtureJson @{}
    function New-ProbeOutcomeCase([string]$Name, [string]$Scenario, [hashtable]$Override) {
        $directory = Join-Path $outcomeRoot $Name
        New-Item -ItemType Directory -Path $directory | Out-Null
        $cast = $Scenario -ceq 'live-cast-probe'
        $value = [ordered]@{
            schemaVersion = 2; runId = 'probe-bind-test'; castingScenario = $cast; terminalReason = 'completed'
            allowanceStatus = if ($cast) { 'valid' } else { 'not-read' }; submitted = $cast
            submitReason = if ($cast) { 'probe-submitted:' + ('f' * 64) } else { $null }
            measuredIdentity = 'commit=' + ('c' * 40) + ';package=' + ('a' * 64) + ';dll=' + ('b' * 64) +
                ';mvid=11111111-2222-3333-4444-555555555555'
            invocation = [ordered]@{ outcomeProjectionId = if ($cast) { ('f' * 64) } else { $null }
                entries = @(if ($cast) { [ordered]@{ castingId = 'probe-cast-1'; nativeSubmissionReported = $true } }) }
            cleanup = [ordered]@{ recorded = $true; failures = @() }; violations = @()
        }
        foreach ($key in $Override.Keys) { $value[$key] = $Override[$key] }
        Write-KbpJsonAtomic (Join-Path $directory 'probe-outcome.json') $value
        return [ordered]@{ runId = 'probe-bind-test'; scenario = $Scenario; evidenceDirectory = $directory
            expectedCommit = ('c' * 40); expectedPackageSha256 = ('a' * 64); expectedDllSha256 = ('b' * 64)
            parameters = @{ probeAllowance = $probeAllowanceJson } }
    }
    Assert-KbpScenarioOutcome -Request (New-ProbeOutcomeCase 'probe-cast-good' 'live-cast-probe' @{})
    Assert-KbpScenarioOutcome -Request (New-ProbeOutcomeCase 'probe-select-good' 'live-cast-probe-select' @{})
    $probeOutcomeCases = [ordered]@{
        'invalid-allowance' = @('live-cast-probe', @{ allowanceStatus = 'identity-mismatch:dll' })
        'not-submitted' = @('live-cast-probe', @{ submitted = $false })
        'other-projection' = @('live-cast-probe', @{ submitReason = 'probe-submitted:' + ('e' * 64) })
        'other-outcome-projection' = @('live-cast-probe', @{ invocation = [ordered]@{ outcomeProjectionId = ('e' * 64)
            entries = @([ordered]@{ castingId = 'probe-cast-1'; nativeSubmissionReported = $true }) } })
        'two-submissions' = @('live-cast-probe', @{ invocation = [ordered]@{ outcomeProjectionId = ('f' * 64)
            entries = @([ordered]@{ castingId = 'a'; nativeSubmissionReported = $true },
                [ordered]@{ castingId = 'b'; nativeSubmissionReported = $true }) } })
        'other-build' = @('live-cast-probe', @{ measuredIdentity = 'commit=' + ('0' * 40) })
        'cleanup-failed' = @('live-cast-probe', @{ cleanup = [ordered]@{ recorded = $true; failures = @('lease') } })
        'violation' = @('live-cast-probe', @{ violations = @('after:missing') })
        'select-submitted' = @('live-cast-probe-select', @{ submitted = $true })
    }
    foreach ($case in $probeOutcomeCases.Keys) {
        $caseRequest = New-ProbeOutcomeCase ('probe-' + $case) $probeOutcomeCases[$case][0] $probeOutcomeCases[$case][1]
        $refusal = $null
        try { Assert-KbpScenarioOutcome -Request $caseRequest }
        catch { $refusal = $_.Exception.Message }
        if ($null -eq $refusal -or $refusal -notlike '*robe*') {
            throw "Probe outcome case $case was not refused by the launcher's check: $refusal"
        }
    }
    foreach ($case in $qualOutcomeCases.Keys) {
        $caseRequest = New-QualOutcomeCase ('qual-' + $case) $qualOutcomeCases[$case][0] $qualOutcomeCases[$case][1]
        $refusal = $null
        try { Assert-KbpScenarioOutcome -Request $caseRequest }
        catch { $refusal = $_.Exception.Message }
        if ($null -eq $refusal -or $refusal -notlike '*ualification*') {
            throw "Qualification outcome case $case was not refused by the launcher's check: $refusal"
        }
    }
}
finally { Remove-Item -LiteralPath $outcomeRoot -Recurse -Force -ErrorAction SilentlyContinue }
# Final review C5/C8: a rehearsal is labelled in the request, the
# orchestration record and the completion record; held keys are released in
# the launcher's final cleanup.
$launcherLf = $launcherText.Replace("`r`n", "`n")
if (-not $launcherLf.Contains('if ($ManualRehearseDone) { $scenarioParameters.manualRehearsal = $true }') -or
    -not $launcherLf.Contains('manualRehearsal = [bool]$ManualRehearseDone') -or
    -not $launcherLf.Contains('-ManualRehearsal ([bool]$ManualRehearseDone)') -or
    -not $launcherLf.Contains("finally {`n    # Final review C8: a key held by an interrupted chord is always released.`n" +
        "    try {`n        if (`$null -ne ('KbpPhysicalInput' -as [type])) { [KbpPhysicalInput]::ReleaseTrackedKeys() }")) {
    throw 'The rehearsal is not labelled in every record, or held keys are not released at the end.'
}
$rehearsalRecord = New-KbpRunCompletionRecord -RunId 'r' -Scenario 'live-workspace-manual' -FixtureFamily 'Automation' `
    -ProfileId 'full-user' -ManualRehearsal $true
if (-not [bool]$rehearsalRecord.manualRehearsal) { throw 'The completion record does not label a rehearsal.' }
$passAt = $launcherText.IndexOf('if ($result.status -cne ''PASS'') { throw "Runtime scenario returned $($result.status)." }')
$outcomeAt = $launcherText.IndexOf('Assert-KbpScenarioOutcome -Request $request')
$hostSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot '..\src\KingmakerBuffPlanner\RuntimeTesting\RuntimeTestHost.cs') -Raw
if ($passAt -lt 0 -or $outcomeAt -lt $passAt -or
    -not $hostSource.Contains('{ "grantConsumed", record.GrantConsumed },') -or
    -not $hostSource.Contains('{ "grantAttempts", record.GrantAttempts },')) {
    throw 'The launcher does not read the scenario evidence itself after a PASS.'
}
# Re-review: every key the launcher's outcome check reads is one the host
# writes, and the judged physical actions are the record's own list.
$pinnedKeys = @('"schemaVersion", 2', '"runId", _request.RunId', '"scenario", _request.Scenario',
    '"castingScenario", record.CastingScenario', '"allowanceStatus", record.AllowanceStatus',
    '"executionMode", record.ExecutionMode', '"planDigest", record.PlanDigest', '"planSteps", record.PlanSteps',
    '"quickDisposition", record.QuickDisposition', '"castingFirstRuns", record.CastingFirstRuns',
    '"grantArmedAtEnd", record.GrantArmedAtEnd', '"classicRunSeen", record.ClassicRunSeen',
    '"finalStatus", step.FinalStatus', '"openedPhysically", record.OpenedPhysically',
    '"screen", record.ScreenWidth + "x" + record.ScreenHeight', '"query", record.Query',
    '"querySuffix", record.QuerySuffix', '"acknowledged", new JArray(record.Acknowledged',
    '"terminalReason", record.TerminalReason', '"selected", selection.Selected',
    '"plannedSubmissions", record.PlannedSubmissions', '"maximumSubmissions", record.MaximumSubmissions',
    '"projectionId", step.ProjectionId', '"failures", new JArray(record.Failures',
    '"violations", new JArray(record.Violations()', '\"actionId\":', '\"action\":', '\"runId\":')
foreach ($key in $pinnedKeys) {
    if (-not $hostSource.Contains($key)) { throw "The host no longer writes a key the launcher reads: $key" }
}
$recordSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot '..\src\KingmakerBuffPlanner\RuntimeTesting\PhysicalWorkspaceRecord.cs') -Raw
$actionsBlock = [regex]::Match($recordSource, 'public static readonly string\[\] Actions =\s*\{([^}]*)\}').Groups[1].Value
$recordActions = @([regex]::Matches($actionsBlock, '"([^"]+)"') | ForEach-Object { $_.Groups[1].Value })
$launcherActionsBlock = [regex]::Match((Get-Content -LiteralPath (Join-Path $PSScriptRoot 'RuntimeAutomation.Common.ps1') -Raw),
    "expected = @\(([^)]*)\)").Groups[1].Value
$launcherActions = @([regex]::Matches($launcherActionsBlock, "'([^']+)'") | ForEach-Object { $_.Groups[1].Value })
if ($recordActions.Count -ne 8 -or ($recordActions -join ',') -cne ($launcherActions -join ',')) {
    throw "The launcher's judged physical actions differ from the record's: $($launcherActions -join ',')"
}
# The allowance writer (review C5): what it writes from recorded selection
# evidence is exactly what the launcher's own checks accept, bound to the
# selection run's profile, identity and WORKING save; it never overwrites,
# never reuses a run id and refuses unclean or mismatched evidence.
$writerRoot = Join-Path $env:TEMP ('kbp-writer-' + [Guid]::NewGuid().ToString('N'))
$writerScript = Join-Path $PSScriptRoot 'New-KbpRunAllowance.ps1'
try {
    $writerHead = (& git -C (Join-Path $PSScriptRoot '..') rev-parse HEAD).Trim()
    $writerApprovals = Join-Path $writerRoot 'approvals'
    $writerEvidence = Join-Path $writerRoot 'evidence'
    $writerBackups = Join-Path $writerRoot 'backups'
    foreach ($directory in @($writerApprovals, $writerEvidence, (Join-Path $writerBackups "rc-frozen\$writerHead"))) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }
    $writerFreeze = [ordered]@{ schemaVersion = 1; commit = $writerHead; packageSha256 = ('a' * 64); dllSha256 = ('b' * 64)
        assemblyMvid = '11111111-2222-3333-4444-555555555555'; version = '0.0.0-test' }
    Write-KbpJsonAtomic (Join-Path $writerBackups "rc-frozen\$writerHead\FREEZE.json") $writerFreeze
    $writerManifest = [pscustomobject]@{ commit = $writerHead; packageSha256 = ('a' * 64); dllSha256 = ('b' * 64)
        assemblyMvid = '11111111-2222-3333-4444-555555555555' }
    function New-WriterSelection([string]$Name, [string]$Scenario, [string]$Mode, [hashtable]$Files, [string]$Status = 'PASS') {
        $directory = Join-Path $writerEvidence $Name
        New-Item -ItemType Directory -Path $directory | Out-Null
        Write-KbpJsonAtomic (Join-Path $directory 'runtime-result.json') ([ordered]@{ status = $Status })
        Write-KbpJsonAtomic (Join-Path $directory 'runtime-request.json') ([ordered]@{
            scenario = $Scenario; expectedCommit = $writerHead; expectedPackageSha256 = ('a' * 64)
            expectedDllSha256 = ('b' * 64); profileId = 'full-user'
            parameters = [ordered]@{ expectedGameId = 'game'; workingSha256 = ('9' * 64); executionMode = $Mode } })
        Write-KbpJsonAtomic (Join-Path $directory 'run-completion.json') ([ordered]@{
            complete = $true; profileId = 'full-user'; compatibilityIdentity = ('e' * 64)
            fixture = [ordered]@{ workingSha256 = ('9' * 64); gameId = 'game' } })
        foreach ($file in $Files.Keys) { Write-KbpJsonAtomic (Join-Path $directory $file) $Files[$file] }
    }
    $forecastIds = @(('1' * 64), ('2' * 64), ('3' * 64), ('3' * 64), ('3' * 64))
    New-WriterSelection 'qual-select' 'live-cast-qual-select' 'instant' @{ 'qual-outcome.json' = [ordered]@{
        castingScenario = $false; violations = @()
        selection = [ordered]@{ selected = $true; recipe = 'zero-cost-mixed'; castings = @('qual-cast-1', 'qual-cast-2', 'qual-cast-3') }
        forecast = @(
            [ordered]@{ name = 'stop'; projectionId = $forecastIds[0]; castingIds = @('qual-cast-1', 'qual-cast-2', 'qual-cast-3') },
            [ordered]@{ name = 'complete'; projectionId = $forecastIds[1]; castingIds = @('qual-cast-2', 'qual-cast-3') },
            [ordered]@{ name = 'recast'; projectionId = $forecastIds[2]; castingIds = @('qual-cast-1') },
            [ordered]@{ name = 'disable'; projectionId = $forecastIds[3]; castingIds = @('qual-cast-1') },
            [ordered]@{ name = 'recover'; projectionId = $forecastIds[4]; castingIds = @('qual-cast-1') }) } }
    # Final review C4: a finite-direct-mixed selection (asked for with
    # -QualificationRecipe) gets a finite allowance whose budget is the
    # sum of its forecast castings (stop 2 + complete 1 + recast 1).
    $finiteIds = @(('4' * 64), ('5' * 64), ('6' * 64))
    New-WriterSelection 'finite-select' 'live-cast-qual-select' 'instant' @{ 'qual-outcome.json' = [ordered]@{
        castingScenario = $false; violations = @()
        selection = [ordered]@{ selected = $true; recipe = 'finite-direct-mixed'; castings = @('qual-cast-1', 'qual-cast-2') }
        forecast = @(
            [ordered]@{ name = 'stop'; projectionId = $finiteIds[0]; castingIds = @('qual-cast-1', 'qual-cast-2') },
            [ordered]@{ name = 'complete'; projectionId = $finiteIds[1]; castingIds = @('qual-cast-2') },
            [ordered]@{ name = 'recast'; projectionId = $finiteIds[2]; castingIds = @('qual-cast-1') }) } }
    $finiteRequestPath = Join-Path $writerEvidence 'finite-select\runtime-request.json'
    $finiteRequest = Read-KbpJson $finiteRequestPath
    $finiteRequest.parameters | Add-Member -NotePropertyName qualificationRecipe -NotePropertyValue 'finite-direct-mixed'
    Write-KbpJsonAtomic $finiteRequestPath $finiteRequest
    # The same finite selection recorded as asked for the default recipe is
    # inconsistent evidence and is refused.
    New-WriterSelection 'finite-unasked' 'live-cast-qual-select' 'instant' @{ 'qual-outcome.json' = [ordered]@{
        castingScenario = $false; violations = @()
        selection = [ordered]@{ selected = $true; recipe = 'finite-direct-mixed'; castings = @('qual-cast-1', 'qual-cast-2') }
        forecast = @([ordered]@{ name = 'stop'; projectionId = $finiteIds[0]; castingIds = @('qual-cast-1', 'qual-cast-2') }) } }
    $classicPlan = [ordered]@{ executionMode = 'animated'; planDigest = ('d' * 64)
        steps = @([ordered]@{ index = 0; provider = 'p'; targets = @('t'); pool = 'x|unlimited'; unlimited = $true }) }
    $classicSelection = [ordered]@{ castingScenario = $false; planDigest = ('d' * 64); planSteps = 1; violations = @() }
    New-WriterSelection 'classic-select' 'live-classic-select' 'animated' @{
        'classic-plan.json' = $classicPlan; 'classic-outcome.json' = $classicSelection }
    New-WriterSelection 'classic-failed' 'live-classic-select' 'animated' @{
        'classic-plan.json' = $classicPlan; 'classic-outcome.json' = $classicSelection } 'FAIL'
    $writerCommon = @{ ExpectedCommit = $writerHead; FreezeKind = 'rc-frozen'; ApprovedBy = 'Howie'
        Authority = 'writer test'; ApprovalsRoot = $writerApprovals; EvidenceRoot = $writerEvidence; BackupRoot = $writerBackups }
    $qualPath = & $writerScript -Kind qualification -RunId 'qual-run' -SelectionRunId 'qual-select' -ExecutionMode animated @writerCommon |
        Select-Object -Last 1
    $qualJson = [IO.File]::ReadAllText($qualPath)
    $qualWritten = $qualJson | ConvertFrom-Json
    if ($null -ne (Get-KbpQualificationAllowanceBuildRefusal -AllowanceJson $qualJson -RunId 'qual-run' `
            -BuildManifest $writerManifest -Recipe 'zero-cost-mixed' -ExecutionMode 'animated') -or
        $null -ne (Get-KbpAllowanceFixtureBindingRefusal -AllowanceJson $qualJson -ProfileId 'full-user' `
            -CompatibilityIdentity ('e' * 64) -WorkingSaveSha256 ('9' * 64) -FixtureGameId 'game') -or
        [int]$qualWritten.maximumNativeSubmissions -ne 8 -or
        ((@($qualWritten.approvedProjectionIds) -join ',') -cne ($forecastIds -join ',')) -or
        -not (Test-Path -LiteralPath (Join-Path $writerApprovals 'qual-run.authorization.md') -PathType Leaf)) {
        throw 'The written qualification allowance is not what the launcher accepts.'
    }
    $finitePath = & $writerScript -Kind qualification -RunId 'finite-run' -SelectionRunId 'finite-select' -ExecutionMode instant @writerCommon |
        Select-Object -Last 1
    $finiteJson = [IO.File]::ReadAllText($finitePath)
    $finiteWritten = $finiteJson | ConvertFrom-Json
    if ($null -ne (Get-KbpQualificationAllowanceBuildRefusal -AllowanceJson $finiteJson -RunId 'finite-run' `
            -BuildManifest $writerManifest -Recipe 'finite-direct-mixed' -ExecutionMode 'instant') -or
        (Get-KbpQualificationAllowanceBuildRefusal -AllowanceJson $finiteJson -RunId 'finite-run' `
            -BuildManifest $writerManifest -Recipe 'zero-cost-mixed' -ExecutionMode 'instant') -cne 'recipe-differs' -or
        [string]$finiteWritten.recipe -cne 'finite-direct-mixed' -or [int]$finiteWritten.maximumNativeSubmissions -ne 4 -or
        -not ([string]$finiteWritten.purpose).StartsWith('finite-direct-mixed casting-first qualification in instant mode') -or
        ((@($finiteWritten.approvedProjectionIds) -join ',') -cne ($finiteIds -join ','))) {
        throw 'The written finite qualification allowance is not what the launcher accepts.'
    }
    $classicPath = & $writerScript -Kind classic -RunId 'classic-run' -SelectionRunId 'classic-select' -ExecutionMode animated @writerCommon |
        Select-Object -Last 1
    $classicJson = [IO.File]::ReadAllText($classicPath)
    if ($null -ne (Get-KbpClassicAllowanceBuildRefusal -AllowanceJson $classicJson -RunId 'classic-run' `
            -BuildManifest $writerManifest -ExecutionMode 'animated') -or
        $null -ne (Get-KbpAllowanceFixtureBindingRefusal -AllowanceJson $classicJson -ProfileId 'full-user' `
            -CompatibilityIdentity ('e' * 64) -WorkingSaveSha256 ('9' * 64) -FixtureGameId 'game') -or
        [string]($classicJson | ConvertFrom-Json).approvedPlanDigest -cne ('d' * 64)) {
        throw 'The written Classic allowance is not what the launcher accepts.'
    }
    $classicBytes = [IO.File]::ReadAllBytes($classicPath)
    $writerRefusals = [ordered]@{
        'existing-allowance' = @{ Kind = 'classic'; RunId = 'classic-run'; SelectionRunId = 'classic-select'; ExecutionMode = 'animated' }
        'failed-selection' = @{ Kind = 'classic'; RunId = 'classic-run-2'; SelectionRunId = 'classic-failed'; ExecutionMode = 'animated' }
        'other-mode' = @{ Kind = 'classic'; RunId = 'classic-run-3'; SelectionRunId = 'classic-select'; ExecutionMode = 'instant' }
        'other-kind-evidence' = @{ Kind = 'qualification'; RunId = 'qual-run-2'; SelectionRunId = 'classic-select'; ExecutionMode = 'animated' }
        'reused-run-id' = @{ Kind = 'classic'; RunId = 'classic-select'; SelectionRunId = 'classic-select'; ExecutionMode = 'animated' }
        'recipe-not-asked-for' = @{ Kind = 'qualification'; RunId = 'finite-run-2'; SelectionRunId = 'finite-unasked'; ExecutionMode = 'instant' }
    }
    foreach ($case in $writerRefusals.Keys) {
        $arguments = $writerRefusals[$case]
        $refused = $false
        try { & $writerScript @arguments @writerCommon | Out-Null }
        catch { $refused = $true }
        if (-not $refused) { throw "The allowance writer accepted case $case." }
        if ($case -cne 'existing-allowance' -and
            (Test-Path -LiteralPath (Join-Path $writerApprovals ($arguments.RunId + '.json')))) {
            throw "The allowance writer left an allowance for refused case $case."
        }
    }
    $otherCommon = @{} + $writerCommon
    $otherCommon.ExpectedCommit = ('0' * 40)
    $refused = $false
    try { & $writerScript -Kind classic -RunId 'classic-run-4' -SelectionRunId 'classic-select' -ExecutionMode animated @otherCommon | Out-Null }
    catch { $refused = $true }
    if (-not $refused -or -not [Linq.Enumerable]::SequenceEqual([byte[]]$classicBytes, [byte[]][IO.File]::ReadAllBytes($classicPath))) {
        throw 'The allowance writer accepted another commit or changed an existing allowance.'
    }
}
finally { Remove-Item -LiteralPath $writerRoot -Recurse -Force -ErrorAction SilentlyContinue }
# Display modes: a window larger than the session's display is refused; the
# Unity arguments name exactly the size; the owner's settings add none.
if (-not (Test-KbpDisplayModeSupported -Size '1920x1080' -DisplaySize '1920x1200') -or
    (Test-KbpDisplayModeSupported -Size '2560x1440' -DisplaySize '1920x1200') -or
    -not (Test-KbpDisplayModeSupported -Size '2560x1440' -DisplaySize '3840x2160') -or
    (Test-KbpDisplayModeSupported -Size '1920x1080' -DisplaySize 'unknown') -or
    @(Get-KbpDisplayModeArguments -Size $null).Count -ne 0 -or
    ((Get-KbpDisplayModeArguments -Size '1920x1080') -join ' ') -cne '-screen-fullscreen 0 -popupwindow -screen-width 1920 -screen-height 1080') {
    throw 'The display-mode rules are wrong.'
}
if ((Get-KbpSessionDisplaySize) -notmatch '^[0-9]{3,5}x[0-9]{3,5}$') { throw 'The session display size was not measured.' }
# The game registry restoration, on a scratch key of this test only.
$scratchName = 'KingmakerBuffPlannerLabTest-' + [Guid]::NewGuid().ToString('N')
$scratchKey = 'HKCU:\Software\' + $scratchName
$scratch = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey('Software\' + $scratchName)
try {
    $scratch.SetValue('Screenmanager Resolution Width_h182942802', 1920, [Microsoft.Win32.RegistryValueKind]::DWord)
    $scratch.SetValue('Screenmanager Fullscreen mode_h3630240806', 1, [Microsoft.Win32.RegistryValueKind]::DWord)
    $scratch.SetValue('Binary_h1', [byte[]](1, 2, 3, 0), [Microsoft.Win32.RegistryValueKind]::Binary)
    $scratch.SetValue('Text_h2', 'kept', [Microsoft.Win32.RegistryValueKind]::String)
    $scratch.SetValue('Large_h3', [long]5000000000, [Microsoft.Win32.RegistryValueKind]::QWord)
    $snapshot = Get-KbpRegistryValueSnapshot -KeyPath $scratchKey
    if (@(Restore-KbpRegistryValues -KeyPath $scratchKey -Snapshot $snapshot).Count -ne 0) {
        throw 'An unchanged key was rewritten.'
    }
    $scratch.SetValue('Screenmanager Resolution Width_h182942802', 1600, [Microsoft.Win32.RegistryValueKind]::DWord)
    $scratch.SetValue('Binary_h1', [byte[]](9), [Microsoft.Win32.RegistryValueKind]::Binary)
    $scratch.DeleteValue('Text_h2')
    $scratch.SetValue('Added_h4', 'new', [Microsoft.Win32.RegistryValueKind]::String)
    $restoredValues = @(Restore-KbpRegistryValues -KeyPath $scratchKey -Snapshot $snapshot)
    $after = Get-KbpRegistryValueSnapshot -KeyPath $scratchKey
    if (@(Compare-KbpRegistrySnapshot -Before $snapshot -After $after).Count -ne 0 -or $restoredValues.Count -ne 4 -or
        [int]$scratch.GetValue('Screenmanager Resolution Width_h182942802') -ne 1920 -or
        $scratch.GetValueKind('Large_h3') -ne [Microsoft.Win32.RegistryValueKind]::QWord -or
        $null -ne $scratch.GetValue('Added_h4')) {
        throw "The registry key was not restored exactly: $($restoredValues -join ', ')"
    }
}
finally {
    $scratch.Dispose()
    [Microsoft.Win32.Registry]::CurrentUser.DeleteSubKeyTree('Software\' + $scratchName, $false)
}
# The finite recipe binds; a recipe named on the launcher must match.
$finiteJson = New-QualificationFixtureJson @{ recipe = 'finite-direct-mixed' }
if ($null -ne (Get-KbpQualificationAllowanceBuildRefusal -AllowanceJson $finiteJson -RunId 'qual-bind-test' `
        -BuildManifest $manifestFixture -Recipe 'finite-direct-mixed') -or
    (Get-KbpQualificationAllowanceBuildRefusal -AllowanceJson $finiteJson -RunId 'qual-bind-test' `
        -BuildManifest $manifestFixture -Recipe 'zero-cost-mixed') -cne 'recipe-differs') {
    throw 'The qualification recipe binding is wrong.'
}
$ErrorActionPreference = 'Continue'
$recipeOutput = @(& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $launcher -Scenario live-cast-probe-select `
    -QualificationRecipe finite-direct-mixed -WhatIf 2>&1)
$recipeExit = $LASTEXITCODE
$ErrorActionPreference = 'Stop'
if ($recipeExit -eq 0 -or -not (@($recipeOutput | Where-Object { "$_" -like '*only valid with -Scenario live-cast-qual-select*' }).Count -ge 1)) {
    throw "A qualification recipe on another scenario was not refused: $($recipeOutput -join ' ')"
}
Write-Host 'Launcher -File WhatIf purity: PASS=12 FAIL=0'
