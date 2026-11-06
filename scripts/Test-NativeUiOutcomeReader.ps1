[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeAutomation.Common.ps1')
$fixture = Join-Path (Get-KbpRepositoryRoot) ('artifacts\native-ui-reader-' + [Guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $fixture)
$request = [pscustomobject]@{ runId='reader-test'; scenario='live-workspace-native-ui'; evidenceDirectory=$fixture
    parameters=[pscustomobject]@{ nativeUiStage='qualify' } }
$captures = @('nui-hud.png','nui-native-popup.png','nui-planner-casting.png','nui-planner-popup.png','nui-gamelog.png',
    'nui-resources.png','nui-resources-all.png','nui-resources-selected-again.png','nui-resources-reopened.png',
    'nui-playtest-policy-cast-available.png','nui-playtest-policy-require-full-plan.png')
foreach ($capture in $captures) { [IO.File]::WriteAllText((Join-Path $fixture $capture), 'offline reader fixture, not a rendered capture') }
$reason = '(native-submission-disabled:runtime-test-session:live-workspace-native-ui:cf-grant-absent).'
$summary = 'Buff Planner: Long ' + [char]0x2014 + ' 0 cast. Not available. ' + $reason
$detail = 'Not cast: spell (caster -> recipient) [casting 1] ' + [char]0x2014 + ' not attempted: Not available. ' + $reason
$record = [ordered]@{ SchemaVersion=9; RunId='reader-test'; Stage='qualify'; violations=@(); Failures=@(); MissingCaptures=@()
    RunsStarted=0; LeaseReleasedAtEnd=$true; FullScreenModeAtEnd=$false; NativePopupOpened=$true; PlannerOpened=$true
    Captures=$captures; PlannerPopupPresenter='native-description-window'; RoutinePressRefused=$true
    RefusalCastingLabels=@('spell (caster -> recipient) [casting 1]'); GameLogPlannerLines=@($summary,$detail)
    AvailabilityStates=@('cast-available|Require full plan','require-full-plan|Require full plan'); AvailabilityIntentRestored=$true
    Switches=@(1..3 | ForEach-Object { @{ PlannerOpenAfter=$false; NativeShownAfter=$true; PlannerReopened=$true
        SelectedSourceBefore='source'; SelectedSourceAfterReopen='source' } }) }
$path = Join-Path $fixture 'native-ui.json'
Write-KbpJsonAtomic $path $record
Assert-KbpScenarioOutcome $request
$passed=1
$good = [IO.File]::ReadAllText($path)
foreach ($change in @(
    {param($r) $r.GameLogPlannerLines=@($r.GameLogPlannerLines[0])},
    {param($r) $r.GameLogPlannerLines+=@($r.GameLogPlannerLines[1])},
    {param($r) $r.GameLogPlannerLines[0]=$r.GameLogPlannerLines[0].Replace('0 cast','1 cast')},
    {param($r) $r.GameLogPlannerLines[1]=$r.GameLogPlannerLines[1].Replace('recipient','wrong owner')},
    {param($r) $r.GameLogPlannerLines[1]=$r.GameLogPlannerLines[1].Replace('cf-grant-absent','wrong-code')},
    {param($r) $r.AvailabilityStates=@($r.AvailabilityStates[0])},
    {param($r) $r.AvailabilityIntentRestored=$false},
    {param($r) $r.RoutinePressRefused=$false}, {param($r) $r.RunsStarted=1},
    {param($r) $r.SchemaVersion=8})) {
    $bad = $good | ConvertFrom-Json
    & $change $bad
    Write-KbpJsonAtomic $path $bad
    $refused=$false
    try { Assert-KbpScenarioOutcome $request } catch { $refused=$true }
    if (-not $refused) { throw 'Invalid native UI refusal/policy evidence was admitted.' }
    $passed++
}
$legacy=$good|ConvertFrom-Json; $legacy.SchemaVersion=8; $legacy.GameLogPlannerLines=@('historical one-line refusal')
Write-KbpJsonAtomic $path $legacy
Assert-KbpScenarioOutcome $request
$passed++
# Schema 10 (0.6.12): the native state button names the current policy and
# autosave is silent. The schema-9 captions are refused under schema 10, and
# schema 9 keeps refusing the new captions.
$current = $good | ConvertFrom-Json
$current.SchemaVersion = 10
$current.AvailabilityStates = @('cast-available|Plan: Cast available', 'require-full-plan|Plan: Require full')
$current | Add-Member -NotePropertyName AvailabilityPolicyCaption -NotePropertyValue 'Plan: Cast available' -Force
$current | Add-Member -NotePropertyName ResourcesSaveStatus -NotePropertyValue '' -Force
Write-KbpJsonAtomic $path $current
Assert-KbpScenarioOutcome $request
$passed++
$schema10 = $current | ConvertTo-Json -Depth 8
foreach ($change in @(
    {param($r) $r.AvailabilityStates=@('cast-available|Require full plan','require-full-plan|Require full plan')},
    {param($r) $r.AvailabilityStates=@('cast-available|Plan: Cast available','require-full-plan|Require full plan')},
    {param($r) $r.AvailabilityStates=@($r.AvailabilityStates[0])},
    {param($r) $r.AvailabilityPolicyCaption='Require full plan'},
    {param($r) $r.ResourcesSaveStatus='Saved'},
    {param($r) $r.SchemaVersion=9})) {
    $bad = $schema10 | ConvertFrom-Json
    & $change $bad
    Write-KbpJsonAtomic $path $bad
    $refused=$false
    try { Assert-KbpScenarioOutcome $request } catch { $refused=$true }
    if (-not $refused) { throw 'Invalid schema-10 native UI policy evidence was admitted.' }
    $passed++
}
Write-Host "Native UI outcome reader: PASS=$passed FAIL=0 (synthetic offline records; schema 8/9 verdicts retained, schema 10 added)."
