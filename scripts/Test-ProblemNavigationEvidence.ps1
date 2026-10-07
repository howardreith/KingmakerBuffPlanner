[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeAutomation.Common.ps1')

$boundary = Join-Path ([IO.Path]::GetTempPath()) ('kbp-problem-evidence-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $boundary | Out-Null
$commit = 'c' * 40
$hash = 'a' * 64
$actions = @('problem-moon', 'problem-next', 'problem-previous', 'problem-escape-focus', 'problem-escape-close')
$passed = 0
function New-ProblemEvidence {
    param([string]$Directory)
    $observations = @()
    foreach ($index in 0..2) {
        $position = if ($index -eq 1) { 2 } else { 1 }
        $observations += [ordered]@{
            castingId = if ($index -eq 1) { 'wp2a-blocked-last' } else { 'wp2a-blocked-late' }
            routineId = 'long'; sourceId = 'buff'; position = $position; count = 2
            chipX = 600; chipY = 400; chipVisibleFraction = 1
            catalogueX = 100; catalogueY = 500; routineX = 500; routineY = 800
            inspectorScroll = 1; previousEnabled = ($position -gt 1); nextEnabled = ($position -lt 2)
            inspectorText = "Long Problem $position of 2. Not ready: this casting is a Draft; finish its choices and mark it Ready"
            footerText = "Showing Problem $position of 2"; documentSignature = $hash
            machineReasons = @('unresolved-saved-request')
        }
        Write-KbpJsonAtomic (Join-Path $Directory ('physical-input-' + $actions[$index] + '.ack.json')) ([ordered]@{
            runId = 'problems-test'; actionId = $actions[$index]; action = 'click'; deliveryFailed = $false })
        [IO.File]::WriteAllBytes((Join-Path $Directory ('problem-' + ($index + 1) + '.png')), (New-Object byte[] 2048))
    }
    foreach ($action in $actions[3..4]) {
        Write-KbpJsonAtomic (Join-Path $Directory ('physical-input-' + $action + '.ack.json')) ([ordered]@{
            runId = 'problems-test'; actionId = $action; action = 'key-escape'; deliveryFailed = $false })
    }
    return [ordered]@{
        schemaVersion = 1; runId = 'problems-test'; sourceCommit = $commit; packageSha256 = $hash; dllSha256 = $hash
        assemblyMvid = '11111111-2222-3333-4444-555555555555'; screenWidth = 1920; screenHeight = 1080
        plannerClosedBeforeHud = $true; coldSessionBeforeHud = $true; plannerOpenedByHud = $true
        graphOverflow = $true; firstChipVisibleBeforeReveal = $false; graphScrollBefore = 1; graphScrollAfter = 0.2
        sourceId = 'buff'; firstCastingId = 'wp2a-blocked-late'; secondCastingId = 'wp2a-blocked-last'
        documentBefore = $hash; documentAfter = $hash; profileBeforeSha256 = $hash; profileAfterSha256 = $hash
        resourcesBefore = 'pools'; resourcesAfter = 'pools'; effectsBefore = 'effects'; effectsAfter = 'effects'
        runsStarted = 0; dispatchAttempts = 0; undoBefore = $false; undoAfter = $false; acceptedDigestAfter = $null
        escapeLeftFocus = $true; plannerClosedAfterEscape = $true; inputLeaseReleased = $true
        acknowledged = $actions; failures = @(); violations = @(); observations = $observations
    }
}
$cases = [ordered]@{
    'valid' = $null
    'wrong-candidate' = { param($r, $d) $r.sourceCommit = 'd' * 40 }
    'generic-open' = { param($r, $d) $r.plannerOpenedByHud = $false }
    'hierarchy-without-screen-point' = { param($r, $d) $r.observations[0].chipX = $null }
    'tiny-card-sliver' = { param($r, $d) $r.observations[0].chipVisibleFraction = 0.05 }
    'no-real-scroll' = { param($r, $d) $r.graphScrollAfter = $r.graphScrollBefore }
    'wrong-order' = { param($r, $d) $r.observations[0].castingId = 'wp2a-blocked-last' }
    'reason-not-visible' = { param($r, $d) $r.observations[0].inspectorText = 'generic planner' }
    'authored-intent-changed' = { param($r, $d) $r.documentAfter = 'b' * 64 }
    'profile-written' = { param($r, $d) $r.profileAfterSha256 = 'b' * 64 }
    'native-dispatch' = { param($r, $d) $r.dispatchAttempts = 1 }
    'native-resource' = { param($r, $d) $r.resourcesAfter = 'spent' }
    'undo-changed' = { param($r, $d) $r.undoAfter = $true }
    'review-authorized' = { param($r, $d) $r.acceptedDigestAfter = 'digest' }
    'missing-native-input' = { param($r, $d) $r.acknowledged = @('problem-next') }
    'wrong-input-action' = { param($r, $d)
        Write-KbpJsonAtomic (Join-Path $d 'physical-input-problem-moon.ack.json') ([ordered]@{
            runId = 'problems-test'; actionId = 'problem-moon'; action = 'hover'; deliveryFailed = $false }) }
    'missing-screenshot' = { param($r, $d) Remove-Item -LiteralPath (Join-Path $d 'problem-1.png') }
    'lease-not-released' = { param($r, $d) $r.inputLeaseReleased = $false }
}
try {
    foreach ($case in $cases.GetEnumerator()) {
        $directory = Join-Path $boundary $case.Key
        New-Item -ItemType Directory -Path $directory | Out-Null
        $record = New-ProblemEvidence $directory
        if ($null -ne $case.Value) { & $case.Value $record $directory }
        Write-KbpJsonAtomic (Join-Path $directory 'physical-workspace-problems.json') $record
        $request = [PSCustomObject]@{
            runId = 'problems-test'; scenario = 'live-workspace-physical'; evidenceDirectory = $directory
            expectedCommit = $commit; expectedPackageSha256 = $hash; expectedDllSha256 = $hash
            parameters = [PSCustomObject]@{ physicalExpectation = 'problems' }
        }
        $rejected = $false
        try { Assert-KbpScenarioOutcome $request } catch { $rejected = $true }
        if ($rejected -ne ($case.Key -ne 'valid')) { throw "Problem evidence case failed: $($case.Key)" }
        $passed++
    }
}
finally {
    $resolved = [IO.Path]::GetFullPath($boundary)
    [void](Assert-KbpPathWithin -Path $resolved -Root ([IO.Path]::GetTempPath()))
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
Write-Host "Problem navigation evidence: PASS=$passed FAIL=0"
