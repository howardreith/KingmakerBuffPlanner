[CmdletBinding()]
param(
    [string]$CallOfTheWildAssemblyPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

$game = Get-KbpGamePath
$managed = Join-Path $game 'Kingmaker_Data\Managed'
if ([string]::IsNullOrWhiteSpace($CallOfTheWildAssemblyPath)) {
    $CallOfTheWildAssemblyPath = Join-Path $game 'Mods\CallOfTheWild\CallOfTheWild.dll'
}
if (-not (Test-Path -LiteralPath $CallOfTheWildAssemblyPath -PathType Leaf)) {
    throw "Call of the Wild assembly is absent: $CallOfTheWildAssemblyPath"
}

$assemblyDirectory = Split-Path -Path $CallOfTheWildAssemblyPath -Parent
$resolver = [ResolveEventHandler]{
    param($sender, $eventArgs)
    $name = ([Reflection.AssemblyName]$eventArgs.Name).Name + '.dll'
    foreach ($directory in @($assemblyDirectory, $managed,
            (Join-Path $managed 'UnityModManager'))) {
        $candidate = Join-Path $directory $name
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return [Reflection.Assembly]::ReflectionOnlyLoadFrom($candidate)
        }
    }
    return $null
}

[AppDomain]::CurrentDomain.add_ReflectionOnlyAssemblyResolve($resolver)
try {
    $assembly = [Reflection.Assembly]::ReflectionOnlyLoadFrom($CallOfTheWildAssemblyPath)
    Write-Output ('CallOfTheWild SHA-256: ' + (Get-KbpSha256 $CallOfTheWildAssemblyPath))
    Write-Output ('CallOfTheWild MVID: ' + $assembly.ManifestModule.ModuleVersionId)
    $contracts = @(
        [pscustomobject]@{ Type = 'CallOfTheWild.SpellbookMechanics.CanNotUseSpells'; Field = '' },
        [pscustomobject]@{ Type = 'CallOfTheWild.SpellbookMechanics.CompanionSpellbook'; Field = 'spellbook' },
        [pscustomobject]@{ Type = 'CallOfTheWild.SpellbookMechanics.GetKnownSpellsFromMemorizationSpellbook'; Field = 'spellbook' }
    )
    $flags = [Reflection.BindingFlags]'Public,NonPublic,Instance,DeclaredOnly'
    foreach ($contract in $contracts) {
        $type = $assembly.GetType($contract.Type, $true)
        $fields = @($type.GetFields($flags) | Sort-Object Name)
        Write-Output ''
        Write-Output ('=== ' + $type.FullName + ' ===')
        foreach ($field in $fields) {
            Write-Output ('field ' + $field.FieldType.FullName + ' ' + $field.Name)
        }
        if ([string]::IsNullOrWhiteSpace($contract.Field)) { continue }
        $relationship = @($fields | Where-Object Name -ceq $contract.Field)
        if ($relationship.Count -ne 1 -or
            $relationship[0].FieldType.FullName -cne
                'Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook') {
            throw "Unexpected relationship contract: $($contract.Type)::$($contract.Field)"
        }
    }
}
finally {
    [AppDomain]::CurrentDomain.remove_ReflectionOnlyAssemblyResolve($resolver)
}
