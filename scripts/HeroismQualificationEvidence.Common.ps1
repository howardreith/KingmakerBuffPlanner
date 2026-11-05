Set-StrictMode -Version Latest

function Assert-KbpHeroismQualificationSelection($Outcome) {
    $selection = $Outcome.selection
    $context = $selection.effectContext
    $forecast = @($Outcome.forecast)
    # The native Newtonsoft serializer preserves dictionary reference IDs.
    # Accept its numeric string metadata, while still requiring actual
    # GUID-to-boolean facts and rejecting references or other properties.
    $facts = @($context.casterFacts.PSObject.Properties | Where-Object { $_.Name -cne '$id' })
    $referenceId = $context.casterFacts.PSObject.Properties['$id']
    $referenceIdValid = $null -eq $referenceId -or
        ($referenceId.Value -is [string] -and $referenceId.Value -cmatch '^[1-9][0-9]*$')
    if ([string]$selection.recipe -cne 'heroism-successor' -or -not [bool]$selection.selected -or
        @($selection.castings).Count -ne 2 -or $null -eq $context -or
        [string]$context.heroismAbilityGuid -cne '5ab0d42fb68c9e34abae4921822b9d63' -or
        [string]$context.successorAbilityGuid -cnotmatch '^[0-9a-f]{32}$' -or
        [string]$context.successorAbilityGuid -ceq [string]$context.heroismAbilityGuid -or
        ([string]$context.successorAbilityGuid -cne '3e4ab69ada402d145a5e0ad3ad4b8564' -and
            [string]$context.successorName -cne 'Heightened Awareness') -or
        [string]::IsNullOrWhiteSpace([string]$context.casterUnitId) -or
        [string]::IsNullOrWhiteSpace([string]$context.primaryRecipientUnitId) -or
        [string]$context.casterUnitId -ceq [string]$context.primaryRecipientUnitId -or
        $null -eq $context.discoveredExpression -or $null -eq $context.boundExpression -or
        -not $referenceIdValid -or $facts.Count -lt 1 -or
        @($facts | Where-Object {
            $_.Name -cnotmatch '^[0-9a-f]{32}$' -or $_.Value -isnot [bool] }).Count -ne 0 -or
        @($context.observationUnitIds) -cnotcontains [string]$context.primaryRecipientUnitId) {
        throw 'Heroism selection lacks real distinct sources, readable condition facts or explicit recipient evidence.'
    }
    if (($forecast.name -join ',') -cne 'heroism-first,heroism-recast,heroism-skip' -or
        @($forecast[0].castingIds).Count -ne 2 -or
        (@($forecast[0].castingIds) -join ',') -cne (@($forecast[1].castingIds) -join ',') -or
        @($forecast[2].castingIds).Count -ne 1 -or
        [string]$forecast[2].castingIds[0] -cne [string]$forecast[0].castingIds[1] -or
        @($forecast | Where-Object { [string]$_.projectionId -cnotmatch '^[0-9a-f]{64}$' -or
            [string]::IsNullOrWhiteSpace([string]$_.canonicalContract) }).Count -ne 0) {
        throw 'Heroism forecast must bind first/recast/skip in order with exactly five native submissions.'
    }
}
