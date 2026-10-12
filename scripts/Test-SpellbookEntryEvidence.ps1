[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeAutomation.Common.ps1')

# WP2B: the launcher's independent judgement of the guarded spellbook
# handoff accepts exactly one complete, candidate-bound record and refuses
# each targeted defect.
$boundary = Join-Path ([IO.Path]::GetTempPath()) ('kbp-spellbook-evidence-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $boundary | Out-Null
$commit = 'c' * 40
$hash = 'a' * 64
$intent = 'fixture-campaign' + [char]2 + '{"schemaVersion":0}'
$suffixes = @('1', '2', '3', 'fault')
$passed = 0
function New-SpellbookEvidence {
    param([string]$Directory)
    $cycles = @()
    $actions = @()
    for ($index = 0; $index -lt 4; $index++) {
        $suffix = $suffixes[$index]
        $fault = $index -eq 3
        $cycles += [ordered]@{
            cycle = $index + 1; faultInjected = $fault; spellbookShown = $true; buttonAttached = $true
            buttonInteractable = $true; ownedButtonCount = 1; listenerCount = 1; buttonX = 1600; buttonY = 1040
            buttonWidth = 170; buttonHeight = 36; topmostHitIsOwned = $true
            topmostHitPath = 'StaticCanvas/ServiceWindow/BuffPlannerSpellbookButton/Label'
            placement = 'left-of-native-close;rect=0,0-1,1;conflictFree=True'; plannerOpenBeforeClick = $false
            nativeCloseInvocations = 1; openerInvocations = 1; nativeReleases = 1
            workspaceOpens = if ($fault) { 0 } else { 1 }
            handoffState = if ($fault) { 'Failed' } else { 'Completed' }
            handoffFailure = if ($fault) { 'planner-open-refused' } else { '' }
            plannerOpenAfterClick = -not $fault; inputLeaseHeldAfterClick = -not $fault
            spellbookShownAfterClick = $fault; serviceWindowShownAfterClick = $fault
            plannerOwnsFullScreenAfterClick = -not $fault
            plannerRootsAfterClick = if ($fault) { 0 } else { 1 }
            buttonRestoredAfterRecovery = $fault; ownedButtonsAfterRecovery = if ($fault) { 1 } else { 0 }
            plannerClosedAfterEscape = $true; inputLeaseReleasedAfterEscape = $true
            spellbookShownAfterEscape = $false; nativeOwnerActiveAfterEscape = $false
            nativeMenuOpenAfterEscape = $false; ownedButtonsAfterEscape = 0; plannerRootsAfterEscape = 0
        }
        foreach ($pair in @(@("sb-open-$suffix", 'key'), @("sb-click-$suffix", 'click'), @("sb-escape-$suffix", 'key-escape'))) {
            $actions += $pair[0]
            Write-KbpJsonAtomic (Join-Path $Directory ('physical-input-' + $pair[0] + '.ack.json')) ([ordered]@{
                runId = 'spellbook-test'; actionId = $pair[0]; action = $pair[1] })
        }
        foreach ($shot in @("spellbook-$suffix-open.png", "spellbook-$suffix-after-click.png")) {
            [IO.File]::WriteAllBytes((Join-Path $Directory $shot), (New-Object byte[] 2048))
        }
    }
    return [ordered]@{
        schemaVersion = 1; runId = 'spellbook-test'; sourceCommit = $commit; packageSha256 = $hash; dllSha256 = $hash
        assemblyMvid = '11111111-2222-3333-4444-555555555555'; screenWidth = 1920; screenHeight = 1080
        castingFirst = $true; plannerClosedAtStart = $true; openSpellsBinding = 'B'; openSpellsVirtualKey = 66
        documentBefore = $intent; documentAfter = $intent; profileBeforeSha256 = 'absent'; profileAfterSha256 = 'absent'
        resourcesBefore = 'pools'; resourcesAfter = 'pools'; effectsBefore = 'effects'; effectsAfter = 'effects'
        runsStarted = 0; faultArmed = $true; movementCommands = 0; abilityCommands = 0; abilityTargetEvents = 0
        selectionUnchanged = $true; acknowledged = $actions; failures = @(); violations = @(); cycles = $cycles
    }
}
function Add-NewSpellbookPauseEvidence {
    param($Record,[string]$Directory)
    $Record.schemaVersion=2
    $captures=@()
    foreach($suffix in @('1','2','3','fault')){foreach($file in @("spellbook-$suffix-open.png","spellbook-$suffix-after-click.png")){
        $captures+=[ordered]@{file=$file;requestFrame=10;completedFrame=11}
    }}
    $captures+=[ordered]@{file='spellbook-2-paused-switch.png';requestFrame=10;completedFrame=11}
    $captures+=[ordered]@{file='spellbook-closed.png';requestFrame=10;completedFrame=11}
    $Record.pause=[ordered]@{originalPaused=$false;pauseAppliedAtWorld=$true;pausedBeforeKey=$true;
        pausedWithSpellbook=$true;pausedWithPlanner=$true;pausedAfterEscape=$true;originalPauseRestored=$true;
        plannerClosedAfterSwitch=$true;spellbookShownAfterSwitch=$true;leaseReleasedAfterSwitch=$true;pausedAfterSwitch=$true;
        captureFailure=$null;captures=$captures}
    $Record.acknowledged+=@('sb-switch-2')
    Write-KbpJsonAtomic (Join-Path $Directory 'physical-input-sb-switch-2.ack.json') ([ordered]@{runId='spellbook-test';actionId='sb-switch-2';action='key'})
    [IO.File]::WriteAllBytes((Join-Path $Directory 'spellbook-2-paused-switch.png'),(New-Object byte[] 2048))
    [IO.File]::WriteAllBytes((Join-Path $Directory 'spellbook-closed.png'),(New-Object byte[] 2048))
}
$cases = [ordered]@{
    'valid-paused-schema2' = {param($r,$d) Add-NewSpellbookPauseEvidence $r $d}
    'new-required-historical-schema' = $null
    'paused-schema-missing-observation' = {param($r,$d) $r.schemaVersion=2}
    'paused-world-not-applied' = {param($r,$d) Add-NewSpellbookPauseEvidence $r $d; $r.pause.pauseAppliedAtWorld=$false}
    'paused-before-key-missing' = {param($r,$d) Add-NewSpellbookPauseEvidence $r $d; $r.pause.pausedBeforeKey=$false}
    'paused-native-not-held' = {param($r,$d) Add-NewSpellbookPauseEvidence $r $d; $r.pause.pausedWithSpellbook=$false}
    'paused-planner-not-held' = {param($r,$d) Add-NewSpellbookPauseEvidence $r $d; $r.pause.pausedWithPlanner=$false}
    'paused-escape-lost-pause' = {param($r,$d) Add-NewSpellbookPauseEvidence $r $d; $r.pause.pausedAfterEscape=$false}
    'paused-original-not-restored' = {param($r,$d) Add-NewSpellbookPauseEvidence $r $d; $r.pause.originalPauseRestored=$false}
    'paused-switch-left-planner' = {param($r,$d) Add-NewSpellbookPauseEvidence $r $d; $r.pause.plannerClosedAfterSwitch=$false}
    'paused-switch-no-native' = {param($r,$d) Add-NewSpellbookPauseEvidence $r $d; $r.pause.spellbookShownAfterSwitch=$false}
    'paused-switch-lease-held' = {param($r,$d) Add-NewSpellbookPauseEvidence $r $d; $r.pause.leaseReleasedAfterSwitch=$false}
    'paused-switch-unpaused' = {param($r,$d) Add-NewSpellbookPauseEvidence $r $d; $r.pause.pausedAfterSwitch=$false}
    'paused-false-is-string' = {param($r,$d) Add-NewSpellbookPauseEvidence $r $d; $r.pause.originalPaused='false'}
    'paused-capture-same-frame' = {param($r,$d) Add-NewSpellbookPauseEvidence $r $d; $r.pause.captures[2].completedFrame=10}
    'paused-capture-missing' = {param($r,$d) Add-NewSpellbookPauseEvidence $r $d; $r.pause.captures=@($r.pause.captures[0])}

    'valid' = $null
    'valid-footer-pair' = { param($r, $d) foreach ($cycle in $r.cycles) {
        $cycle.placement = 'footer-pair;paired=True;metamagic=537.3/210.0;planner=757.3/210.0;plateExtension=220.0' } }
    'valid-footer-no-book' = { param($r, $d) foreach ($cycle in $r.cycles) {
        $cycle.placement = 'footer-pair;paired=False;metamagic=629.3/290.3;planner=757.3/210.0;plateExtension=0.0' } }
    'footer-overlap' = { param($r, $d) $r.cycles[0].placement =
        'footer-pair;paired=True;metamagic=629.3/290.3;planner=757.3/210.0;plateExtension=220.0' }
    'footer-plate-short' = { param($r, $d) $r.cycles[0].placement =
        'footer-pair;paired=True;metamagic=537.3/210.0;planner=757.3/210.0;plateExtension=0.0' }
    'footer-malformed-with-old-marker' = { param($r, $d) $r.cycles[0].placement =
        'footer-pair;paired=True;conflictFree=True' }
    'footer-missing-visibility' = { param($r, $d) $r.cycles[0].placement =
        'footer-pair;metamagic=537.3/210.0;planner=757.3/210.0;plateExtension=220.0' }
    'footer-invalid-width' = { param($r, $d) $r.cycles[0].placement =
        'footer-pair;paired=False;metamagic=629.3/290.3;planner=757.3/0.0;plateExtension=0.0' }
    'footer-invalid-coordinate' = { param($r, $d) $r.cycles[0].placement =
        'footer-pair;paired=True;metamagic=NaN/210.0;planner=757.3/210.0;plateExtension=220.0' }
    'valid-with-menu-veil' = { param($r, $d)
        $r.acknowledged = @($r.acknowledged) + @('sb-menu-close-1')
        Write-KbpJsonAtomic (Join-Path $d 'physical-input-sb-menu-close-1.ack.json') ([ordered]@{
            runId = 'spellbook-test'; actionId = 'sb-menu-close-1'; action = 'key-escape' }) }
    'wrong-candidate' = { param($r, $d) $r.dllSha256 = 'b' * 64 }
    'host-violation' = { param($r, $d) $r.violations = @('button-not-visible-and-topmost:1') }
    'programmatic-click' = { param($r, $d)
        Write-KbpJsonAtomic (Join-Path $d 'physical-input-sb-click-2.ack.json') ([ordered]@{
            runId = 'spellbook-test'; actionId = 'sb-click-2'; action = 'hover' }) }
    'failed-delivery' = { param($r, $d)
        Write-KbpJsonAtomic (Join-Path $d 'physical-input-sb-open-1.ack.json') ([ordered]@{
            runId = 'spellbook-test'; actionId = 'sb-open-1'; action = 'key'; deliveryFailed = $true }) }
    'fewer-than-three-cycles' = { param($r, $d)
        $r.acknowledged = @($r.acknowledged | Where-Object { $_ -notlike '*-3' }) }
    'native-close-covers-button' = { param($r, $d) $r.cycles[0].topmostHitIsOwned = $false }
    'duplicate-button' = { param($r, $d) $r.cycles[1].ownedButtonCount = 2 }
    'accumulated-listener' = { param($r, $d) $r.cycles[2].listenerCount = 2 }
    'placement-conflict' = { param($r, $d) $r.cycles[0].placement = 'below-native-close;conflictFree=False' }
    'handoff-refused' = { param($r, $d) $r.cycles[0].nativeCloseInvocations = 0 }
    'planner-opened-twice' = { param($r, $d) $r.cycles[1].workspaceOpens = 2 }
    'spellbook-left-open-behind-planner' = { param($r, $d) $r.cycles[0].spellbookShownAfterClick = $true }
    'native-window-left-shown' = { param($r, $d) $r.cycles[0].serviceWindowShownAfterClick = $true }
    'fullscreen-not-owned-by-planner' = { param($r, $d) $r.cycles[1].plannerOwnsFullScreenAfterClick = $false }
    'opened-without-native-release' = { param($r, $d) $r.cycles[2].nativeReleases = 0 }
    'escape-opened-native-menu' = { param($r, $d) $r.cycles[2].nativeMenuOpenAfterEscape = $true }
    'lease-not-released' = { param($r, $d) $r.cycles[0].inputLeaseReleasedAfterEscape = $false }
    'failure-left-limbo' = { param($r, $d) $r.cycles[3].spellbookShownAfterClick = $false }
    'failure-opened-planner' = { param($r, $d) $r.cycles[3].plannerOpenAfterClick = $true }
    'fault-not-armed' = { param($r, $d) $r.faultArmed = $false }
    'plan-written' = { param($r, $d) $r.profileAfterSha256 = $hash }
    'native-state-changed' = { param($r, $d) $r.resourcesAfter = 'spent' }
    'world-input-leak' = { param($r, $d) $r.movementCommands = 1 }
    'missing-screenshot' = { param($r, $d) Remove-Item -LiteralPath (Join-Path $d 'spellbook-2-open.png') }
}
try {
    foreach ($case in $cases.GetEnumerator()) {
        $directory = Join-Path $boundary $case.Key
        New-Item -ItemType Directory -Path $directory | Out-Null
        $record = New-SpellbookEvidence $directory
        if ($null -ne $case.Value) { & $case.Value $record $directory }
        Write-KbpJsonAtomic (Join-Path $directory 'physical-spellbook-entry.json') $record
        $request = [PSCustomObject]@{
            runId = 'spellbook-test'; scenario = 'live-workspace-physical'; evidenceDirectory = $directory
            expectedCommit = $commit; expectedPackageSha256 = $hash; expectedDllSha256 = $hash
            parameters = [PSCustomObject]@{ physicalExpectation = 'spellbook' }
        }
        if($case.Key -ceq 'new-required-historical-schema'){ $request.parameters | Add-Member -NotePropertyName physicalSpellbookSchema -NotePropertyValue 2 }
        $rejected = $false
        try { Assert-KbpScenarioOutcome $request } catch { $rejected = $true }
        if ($rejected -ne ($case.Key -notlike 'valid*')) { throw "Spellbook evidence case failed: $($case.Key)" }
        $passed++
    }
}
finally {
    $resolved = [IO.Path]::GetFullPath($boundary)
    [void](Assert-KbpPathWithin -Path $resolved -Root ([IO.Path]::GetTempPath()))
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
Write-Host "Spellbook entry evidence: PASS=$passed FAIL=0"
