[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][ValidateSet('classic', 'qualification')][string]$Kind,
    [Parameter(Mandatory = $true)][string]$RunId,
    [Parameter(Mandatory = $true)][string]$SelectionRunId,
    [Parameter(Mandatory = $true)][string]$ExpectedCommit,
    [Parameter(Mandatory = $true)][ValidateSet('qualification-frozen', 'rc-frozen')][string]$FreezeKind,
    [Parameter(Mandatory = $true)][ValidateSet('instant', 'animated')][string]$ExecutionMode,
    [Parameter(Mandatory = $true)][string]$ApprovedBy,
    [Parameter(Mandatory = $true)][string]$Authority,
    [string]$ApprovalsRoot,
    [string]$EvidenceRoot,
    [string]$BackupRoot
)

# One run-bound casting allowance, written mechanically from recorded
# evidence (batch 3, section 3; review C5): a Classic cast allowance
# (kbp-classic-cast, schema 2) or a casting-qualification allowance
# (kbp-casting-qualification, schema 5) for the recipe the selection run
# selected (zero-cost-mixed or finite-direct-mixed; final review C4). It
# binds:
# - the frozen build (commit, package, DLL, MVID) of this checkout's HEAD;
# - the selection run's plan digest (Classic) or ordered forecast
#   projections (qualification), made with that build in this mode;
# - the selection run's compatibility profile, identity digest and WORKING
#   save (from its run-completion.json), with the run's purpose;
# - a budget equal to the plan's submissions (at most 24).
# The allowance and its authorization record are created exclusively: an
# existing file is never overwritten and a run id is never reused. Nothing
# is launched or deployed here.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ([string]::IsNullOrEmpty($ApprovalsRoot)) { $ApprovalsRoot = Join-Path $repoRoot '..\..\approvals' }
if ([string]::IsNullOrEmpty($EvidenceRoot)) { $EvidenceRoot = $script:KbpRuntimeEvidenceRoot }
if ([string]::IsNullOrEmpty($BackupRoot)) { $BackupRoot = Join-Path $script:KbpLabRoot 'runtime-backups' }
if ($RunId -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$') { throw "Run id is not a safe identifier: $RunId" }
if ((Test-Path -LiteralPath (Join-Path $EvidenceRoot $RunId))) { throw "Run id $RunId already has evidence; it is never reused." }

$freezePath = Join-Path (Join-Path (Join-Path $BackupRoot $FreezeKind) $ExpectedCommit) 'FREEZE.json'
if (-not (Test-Path -LiteralPath $freezePath -PathType Leaf)) { throw "No freeze record for $ExpectedCommit under $FreezeKind." }
$freeze = Read-KbpJson $freezePath
$head = (& git -C $repoRoot rev-parse HEAD).Trim()
if ([string]$freeze.commit -cne $ExpectedCommit -or $head -cne $ExpectedCommit) {
    throw "Freeze record or checkout HEAD is not $ExpectedCommit (freeze=$($freeze.commit); head=$head)."
}
foreach ($field in @('packageSha256', 'dllSha256')) {
    if ([string]$freeze.$field -cnotmatch '^[0-9a-f]{64}$') { throw "The freeze record has no valid $field." }
}

$selectionDir = Join-Path $EvidenceRoot $SelectionRunId
$result = Read-KbpJson (Join-Path $selectionDir 'runtime-result.json')
$request = Read-KbpJson (Join-Path $selectionDir 'runtime-request.json')
$completion = Read-KbpJson (Join-Path $selectionDir 'run-completion.json')
if ([string]$result.status -cne 'PASS' -or -not [bool]$completion.complete) {
    throw 'The selection run did not pass with a complete lifecycle; no allowance is written.'
}
if ([string]$request.expectedCommit -cne $ExpectedCommit -or
    [string]$request.expectedPackageSha256 -cne [string]$freeze.packageSha256 -or
    [string]$request.expectedDllSha256 -cne [string]$freeze.dllSha256) {
    throw 'The selection run was not made with the frozen build; no allowance is written.'
}
$gameId = [string]$request.parameters.expectedGameId
$profileId = [string]$completion.profileId
$identity = [string]$completion.compatibilityIdentity
$workingSha256 = [string]$completion.fixture.workingSha256
if ([string]::IsNullOrWhiteSpace($gameId) -or $gameId -cne [string]$completion.fixture.gameId) {
    throw 'The selection run has no single fixture campaign; no allowance is written.'
}
if ($profileId -cne [string]$request.profileId -or $identity -cnotmatch '^[0-9a-f]{64}$' -or
    $workingSha256 -cnotmatch '^[0-9a-f]{64}$' -or $workingSha256 -cne [string]$request.parameters.workingSha256) {
    throw 'The selection run did not record one profile, identity and WORKING save; no allowance is written.'
}

$allowance = [ordered]@{}
$details = New-Object System.Collections.Generic.List[string]
if ($Kind -ceq 'classic') {
    $plan = Read-KbpJson (Join-Path $selectionDir 'classic-plan.json')
    $outcome = Read-KbpJson (Join-Path $selectionDir 'classic-outcome.json')
    if ([string]$request.scenario -cne 'live-classic-select' -or [bool]$outcome.castingScenario -or
        @($outcome.violations).Count -ne 0) {
        throw 'The selection run is not a clean Classic selection; no allowance is written.'
    }
    if ([string]$plan.executionMode -cne $ExecutionMode -or [string]$request.parameters.executionMode -cne $ExecutionMode) {
        throw "The Classic selection was made in $($plan.executionMode) mode, not $ExecutionMode; no allowance is written."
    }
    $digest = [string]$plan.planDigest
    $steps = @($plan.steps).Count
    if ($digest -cnotmatch '^[0-9a-f]{64}$' -or $digest -cne [string]$outcome.planDigest -or $steps -lt 1 -or
        $steps -gt 24 -or [int]$outcome.planSteps -ne $steps) {
        throw 'The Classic selection did not record one plan digest with 1..24 steps; no allowance is written.'
    }
    $purpose = "Classic Long-routine cast of the selected plan in $ExecutionMode mode through the HUD routine entry under a single-use grant"
    $allowance = [ordered]@{
        schemaVersion = 2; kind = 'kbp-classic-cast'; runId = $RunId
        sourceCommit = [string]$freeze.commit; packageSha256 = [string]$freeze.packageSha256
        dllSha256 = [string]$freeze.dllSha256; assemblyMvid = [string]$freeze.assemblyMvid
        fixtureGameId = $gameId; executionMode = $ExecutionMode; routineId = 'long'
        approvedPlanDigest = $digest; maximumNativeSubmissions = $steps
        approvedBy = $ApprovedBy; authority = $Authority
        compatibilityProfileId = $profileId; compatibilityIdentity = $identity; workingSaveSha256 = $workingSha256
        purpose = $purpose
    }
    $details.Add("Plan digest $digest ($steps step(s)):")
    foreach ($step in @($plan.steps)) {
        $details.Add("  - step $($step.index): provider $($step.provider); targets $(@($step.targets) -join ', '); pool $($step.pool); unlimited $($step.unlimited)")
    }
}
else {
    $outcome = Read-KbpJson (Join-Path $selectionDir 'qual-outcome.json')
    # Final review C4: the recipe is the one the selection run selected;
    # each recipe has its own purpose, and the budget below is the sum of
    # the forecast castings, exactly as the host's boundary counts them.
    $recipe = [string]$outcome.selection.recipe
    $purposes = @{
        'zero-cost-mixed' = "zero-cost-mixed casting-first qualification in $ExecutionMode mode (stop, complete, repeat, recast, held disable, recover)"
        'finite-direct-mixed' = "finite-direct-mixed casting-first qualification in $ExecutionMode mode (paid spell slots: stop, complete, repeat, recast)"
        'group-mixed' = "group-mixed casting-first qualification in $ExecutionMode mode (a direct casting primes one recipient; the caster-centred group casting then covers the others in one invocation with that recipient pre-covered, with a target-anchored group casting where one exists)"
        'ability-pool-direct' = "ability-pool-direct casting-first qualification in $ExecutionMode mode (one plain buff from an ability whose pool holds a single use: cast once, a repeat casts nothing, and Always recast is refused for want of the resource)"
        'enhanced-direct' = "enhanced-direct casting-first qualification in $ExecutionMode mode (one direct buff cast plain, then again on another recipient with a per-casting class-feature enhancement chosen through the workspace; the enhancement's resource, the stat modifier it raises and the caster's toggles are read natively)"
    }
    if ([string]$request.scenario -cne 'live-cast-qual-select' -or [bool]$outcome.castingScenario -or
        -not [bool]$outcome.selection.selected -or -not $purposes.ContainsKey($recipe) -or
        @($outcome.violations).Count -ne 0) {
        throw 'The selection run is not a clean qualification selection of a known recipe; no allowance is written.'
    }
    $requestedRecipe = if (@($request.parameters.PSObject.Properties | ForEach-Object Name) -ccontains 'qualificationRecipe') {
        [string]$request.parameters.qualificationRecipe } else { '' }
    if ((-not [string]::IsNullOrEmpty($requestedRecipe) -and $requestedRecipe -cne $recipe) -or
        ([string]::IsNullOrEmpty($requestedRecipe) -and $recipe -cne 'zero-cost-mixed')) {
        throw "The selection run was asked for another recipe than the $recipe it selected; no allowance is written."
    }
    $forecast = @($outcome.forecast)
    $ids = @($forecast | ForEach-Object { [string]$_.projectionId })
    $budget = 0
    foreach ($step in $forecast) { $budget += @($step.castingIds).Count }
    if ($ids.Count -lt 1 -or $ids.Count -gt 8 -or @($ids | Where-Object { $_ -cnotmatch '^[0-9a-f]{64}$' }).Count -ne 0 -or
        $budget -lt 1 -or $budget -gt 24) {
        throw 'The forecast does not name 1..8 projection ids with a 1..24 submission budget; no allowance is written.'
    }
    $purpose = $purposes[$recipe]
    $allowance = [ordered]@{
        schemaVersion = 5; kind = 'kbp-casting-qualification'; runId = $RunId
        sourceCommit = [string]$freeze.commit; packageSha256 = [string]$freeze.packageSha256
        dllSha256 = [string]$freeze.dllSha256; assemblyMvid = [string]$freeze.assemblyMvid
        fixtureGameId = $gameId; recipe = $recipe; executionMode = $ExecutionMode
        approvedProjectionIds = $ids; maximumNativeSubmissions = $budget
        approvedBy = $ApprovedBy; authority = $Authority
        compatibilityProfileId = $profileId; compatibilityIdentity = $identity; workingSaveSha256 = $workingSha256
        purpose = $purpose
    }
    $details.Add("Castings: $(@($outcome.selection.castings) -join ', ')")
    foreach ($step in $forecast) {
        $details.Add("  - $($step.name): projection $($step.projectionId); castings $(@($step.castingIds) -join ', ')")
    }
}

$approvals = [IO.Path]::GetFullPath($ApprovalsRoot)
if (-not (Test-Path -LiteralPath $approvals -PathType Container)) { throw "The approvals folder is missing: $approvals" }
$allowancePath = Join-Path $approvals ($RunId + '.json')
$recordPath = Join-Path $approvals ($RunId + '.authorization.md')
if ((Test-Path -LiteralPath $allowancePath) -or (Test-Path -LiteralPath $recordPath)) {
    throw "An allowance or record for $RunId already exists; it is never overwritten."
}
$record = @(
    "# Authorization record for $RunId",
    '',
    "- Allowance: $allowancePath (exclusive creation; refused if it existed).",
    "- Written mechanically by scripts/New-KbpRunAllowance.ps1 under the authority named in the",
    "  allowance ($Authority; approvedBy $ApprovedBy). No person typed or individually inspected it.",
    "- Purpose: $purpose.",
    "- Build: commit $($freeze.commit), version $($freeze.version), package $($freeze.packageSha256),",
    "  DLL $($freeze.dllSha256), MVID $($freeze.assemblyMvid) (frozen under $FreezeKind\$($freeze.commit)).",
    "- Selection evidence: $SelectionRunId (PASS, complete; the selection ran in $([string]$request.parameters.executionMode) mode, this allowance approves $ExecutionMode).",
    "- Fixture: campaign $gameId, WORKING save SHA-256 $workingSha256; profile $profileId with identity",
    "  $identity. The launcher refuses any other profile, identity or WORKING save.",
    "- Maximum native submissions: $($allowance.maximumNativeSubmissions). No retry, no save write."
) + @($details) -join [Environment]::NewLine
$created = New-Object System.Collections.Generic.List[string]
try {
    foreach ($pair in @(@($allowancePath, (($allowance | ConvertTo-Json -Compress) + [Environment]::NewLine)),
            @($recordPath, ($record + [Environment]::NewLine)))) {
        $stream = [IO.File]::Open($pair[0], [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
        $created.Add($pair[0])
        try { $bytes = [Text.UTF8Encoding]::new($false).GetBytes($pair[1]); $stream.Write($bytes, 0, $bytes.Length) }
        finally { $stream.Dispose() }
    }
}
catch {
    # A half-written pair is never left behind as an approval.
    foreach ($path in $created) { Remove-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue }
    throw
}
Write-Host "Allowance written: $allowancePath"
$allowancePath
