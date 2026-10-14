[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeAutomation.Common.ps1')

$boundary = [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetTempPath()) ('kbp-policy-evidence-' + [Guid]::NewGuid().ToString('N'))))
New-Item -ItemType Directory -Path $boundary | Out-Null
$passed = 0
try {
    $allowance = [ordered]@{
        approvedProjectionIds = @('projection-shared', 'projection-witness', 'projection-available')
        executionMode = 'instant'
        maximumNativeSubmissions = 4
    }
    $request = [pscustomobject]@{
        runId = 'policy-reader-run'
        scenario = 'live-cast-qual'
        evidenceDirectory = $boundary
        parameters = [pscustomobject]@{
            qualificationAllowance = ($allowance | ConvertTo-Json -Compress)
        }
    }
    $baseline = [ordered]@{
        schemaVersion = 2
        runId = 'policy-reader-run'
        scenario = 'live-cast-qual'
        castingScenario = $true
        allowanceStatus = 'valid'
        terminalReason = 'completed'
        violations = @()
        failures = @()
        executionMode = 'instant'
        maximumSubmissions = 4
        plannedSubmissions = 4
        selection = [ordered]@{ selected = $true; recipe = 'policy-shared-powerful' }
        forecast = @(
            [ordered]@{ name = 'shared'; projectionId = 'projection-shared' },
            [ordered]@{ name = 'witness'; projectionId = 'projection-witness' },
            [ordered]@{ name = 'cast-available'; projectionId = 'projection-available' }
        )
        share = [ordered]@{
            expectedSpend = 2; independentDemand = 2
            reservoirBeforeShared = 6; reservoirAfterShared = 4
            reservoirBeforeWitness = 4; reservoirAfterWitness = 4
            reservoirBeforeShortage = 4; reservoirAfterShortage = 4
            persisted = $true; draftDisarmKeptCasting = $true
            baselineArmed = ''; shortageCastings = 3
        }
        policy = [ordered]@{
            before = 'require-full-plan'; after = 'cast-available'; reloaded = 'cast-available'
            strictMode = 'Ordinary'; plannerMode = 'ReadyCastsOnly'; hudMode = 'ReadyCastsOnly'
            authoredIntentPreserved = $true
            strictOmissions = @(
                'qual-cast-1|already-satisfied', 'qual-cast-2|already-satisfied',
                'qual-short-3|resource-pool-exhausted:pool-shape:0<1')
            availableOmissions = @(
                'qual-cast-1|already-satisfied', 'qual-cast-2|already-satisfied',
                'qual-short-3|resource-pool-exhausted:pool-shape:0<1')
            executableCastingIds = @('qual-short-1', 'qual-short-2')
            projectedCosts = @(
                'qual-short-1|native=pool-shape:1|enhancements=reservoir:2',
                'qual-short-2|native=pool-shape:1|enhancements=reservoir:2')
            noOpReason = 'nothing-to-cast:5'
            globalRefusalReason = 'combat-active'
            postchecksSpentNothing = $true
        }
        steps = @(
            [ordered]@{ name = 'shared'; applyAllowed = $true; terminal = 'completed'; entries = @(); omissions = @(); availability = @() },
            [ordered]@{ name = 'witness'; applyAllowed = $true; terminal = 'completed'; entries = @(); omissions = @(); availability = @() },
            [ordered]@{
                name = 'shortage'; applyAllowed = $false; terminal = $null; entries = @()
                projectionId = $null
                omissions = @(
                    'qual-cast-1|already-satisfied', 'qual-cast-2|already-satisfied',
                    'qual-short-3|resource-pool-exhausted:pool-shape:0<1')
                availability = @('qual-short-1:2>2', 'qual-short-2:2>2')
            },
            [ordered]@{
                name = 'cast-available'; applyAllowed = $true; terminal = 'completed'
                projectionId = 'projection-available'
                entries = @(
                    'qual-cast-1=Skipped;submitted=False;spend=False;free=False;detail=already',
                    'qual-cast-2=Skipped;submitted=False;spend=False;free=False;detail=already',
                    'qual-short-1=EffectConfirmed;submitted=True;spend=True;free=False;detail=cast',
                    'qual-short-2=EffectConfirmed;submitted=True;spend=True;free=False;detail=cast',
                    'qual-short-3=Omitted;submitted=False;spend=False;free=False;detail=blocked')
                omissions = @(
                    'qual-cast-1|already-satisfied', 'qual-cast-2|already-satisfied',
                    'qual-short-3|resource-pool-exhausted:pool-shape:0<1')
                availability = @('qual-short-1:2>0', 'qual-short-2:2>0')
            }
        )
    }

    function Write-Outcome($Value) {
        Write-KbpJsonAtomic (Join-Path $boundary 'qual-outcome.json') $Value
    }
    function Copy-Outcome {
        return ($baseline | ConvertTo-Json -Depth 20 | ConvertFrom-Json)
    }
    function Assert-Rejected([string]$Name, [scriptblock]$Mutation) {
        $value = Copy-Outcome
        & $Mutation $value
        Write-Outcome $value
        $rejected = $false
        try { Assert-KbpScenarioOutcome -Request $request }
        catch {
            if ($_.Exception.Message -notlike 'Policy qualification evidence*' -and
                $_.Exception.Message -notlike 'Share qualification evidence*') { throw }
            $rejected = $true
        }
        if (-not $rejected) { throw "Policy evidence mutation was accepted: $Name" }
        Write-Host "PASS reject-$Name"
        $script:passed++
    }

    Write-Outcome $baseline
    Assert-KbpScenarioOutcome -Request $request
    Write-Host 'PASS accept-complete-policy-evidence'
    $passed++

    Assert-Rejected 'old-schema' { param($value) $value.schemaVersion = 1 }
    Assert-Rejected 'reordered-affordable-subset' {
        param($value) $value.policy.executableCastingIds = @('qual-short-2', 'qual-short-1')
    }
    Assert-Rejected 'strict-native-spend' {
        param($value) $value.steps[2].availability = @('qual-short-1:2>1')
    }
    Assert-Rejected 'unexplained-omission' {
        param($value)
        $value.policy.strictOmissions = @('qual-short-3|blocked')
        $value.steps[2].omissions = @('qual-short-3|blocked')
    }
    Assert-Rejected 'wrong-confirmed-order' {
        param($value)
        $value.steps[3].entries[2] = 'qual-short-2=EffectConfirmed;submitted=True;spend=True;free=False;detail=cast'
        $value.steps[3].entries[3] = 'qual-short-1=EffectConfirmed;submitted=True;spend=True;free=False;detail=cast'
    }
    Assert-Rejected 'postcheck-spend' { param($value) $value.policy.postchecksSpentNothing = $false }
    Write-Host "Policy qualification evidence checks: PASS=$passed FAIL=0; focused offline reader only."
}
finally {
    $temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
    if (-not $boundary.StartsWith($temporaryRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Unsafe policy-evidence cleanup path.'
    }
    Remove-Item -LiteralPath $boundary -Recurse -Force
}
