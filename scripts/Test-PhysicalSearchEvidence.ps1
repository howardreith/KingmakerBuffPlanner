[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeAutomation.Common.ps1')

$good = [pscustomobject]@{
    dispatch='physical-os-input';cleanupDispatch='view-callback';query='ligh';typedQuery='ligh';typedWithBindingLetter='lighb'
    modeBefore='FullScreenUI';modeAfterQuery='FullScreenUI';modeAfterBindingLetter='FullScreenUI'
    focusedBeforeTyping=$true;plannerStayedOpen=$true;documentUnchanged=$true;cleanupComplete=$true
    preservedSelectedSource='selected-resistance'
    expectedSources=@('light-spell','light-feature');observedSources=@('selected-resistance','light-feature','light-spell')
    expectedSourcesAfterBindingLetter=1;sourcesAfterBindingLetter=1;captureFailure=$null
    captures=@([pscustomobject]@{file='physical-cf-search-filtered.png';requestFrame=10;completedFrame=11},
        [pscustomobject]@{file='physical-cf-search-binding-text.png';requestFrame=20;completedFrame=21})
}
$cases = [ordered]@{
    valid=$null
    'callback-dispatch'={param($r) $r.dispatch='binding-callback-forced'}
    'missing-focus'={param($r) $r.PSObject.Properties.Remove('focusedBeforeTyping')}
    'wrong-focus-type'={param($r) $r.focusedBeforeTyping='true'}
    'partial-query'={param($r) $r.typedQuery='lig'}
    'binding-opened-native'={param($r) $r.modeAfterBindingLetter='Spellbook'}
    'missing-selected-property'={param($r) $r.PSObject.Properties.Remove('preservedSelectedSource')}
    'empty-selected-source'={param($r) $r.preservedSelectedSource=''}
    'filter-row-missing'={param($r) $r.observedSources=@('light-spell')}
    'selected-row-missing'={param($r) $r.observedSources=@('light-feature','light-spell')}
    'foreign-filter-row'={param($r) $r.observedSources+=@('foreign')}
    'duplicate-filter-row'={param($r) $r.observedSources+=@('light-spell')}
    'missing-binding-expectation'={param($r) $r.PSObject.Properties.Remove('expectedSourcesAfterBindingLetter')}
    'negative-binding-expectation'={param($r) $r.expectedSourcesAfterBindingLetter=-1}
    'binding-letter-count-mismatch'={param($r) $r.sourcesAfterBindingLetter=0}
    'plan-edited'={param($r) $r.documentUnchanged=$false}
    'cleanup-incomplete'={param($r) $r.cleanupComplete=$false}
    'capture-same-frame'={param($r) $r.captures[0].completedFrame=10}
    'capture-failed'={param($r) $r.captureFailure='timeout'}
    'capture-missing'={param($r) $r.captures=@($r.captures[0])}
}
$passed=0
foreach ($case in $cases.GetEnumerator()) {
    $record = $good | ConvertTo-Json -Depth 8 | ConvertFrom-Json
    if ($null -ne $case.Value) { & $case.Value $record }
    $rejected=$false
    try { Assert-KbpPhysicalSearchObservation $record } catch { $rejected=$true }
    if ($rejected -ne ($case.Key -cne 'valid')) { throw "Physical search evidence case failed: $($case.Key)" }
    $passed++
}
Write-Host "Physical search evidence: PASS=$passed FAIL=0"
