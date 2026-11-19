[CmdletBinding()]
param([Parameter(Mandatory = $true)][string]$CandidateDll)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
$managed = Join-Path (Get-KbpGamePath) 'Kingmaker_Data/Managed'
$umm = [Reflection.Assembly]::LoadFrom((Join-Path $managed 'UnityModManager/UnityModManager.dll'))
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $CandidateDll).Path)
$settingsType = $assembly.GetType('KingmakerBuffPlanner.GameAdapters.PlannerLanguageSettings', $true)
$resolver = $assembly.GetType('KingmakerBuffPlanner.PlannerLanguage', $true).GetMethod('Normalize', [Reflection.BindingFlags]'Static, NonPublic')
$entryType = $umm.GetType('UnityModManagerNet.UnityModManager+ModEntry', $true)
$loader = @($settingsType.BaseType.GetMethods() | Where-Object { $_.Name -eq 'Load' -and $_.GetParameters().Length -eq 1 })[0].MakeGenericMethod($settingsType)
$entry = [Runtime.Serialization.FormatterServices]::GetUninitializedObject($entryType)
$boundary = Join-Path ([IO.Path]::GetTempPath()) ('KbpLocaleSettings-' + [Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($boundary)
try {
    $entryType.GetField('Path').SetValue($entry, $boundary)
    $settings = $loader.Invoke($null, @($entry))
    if ($settings.Language -cne 'automatic') { throw 'Missing preference did not default to Automatic.' }
    $path = $settings.GetPath($entry)
    if ([IO.Path]::GetFullPath($path) -ine [IO.Path]::Combine($boundary, 'Settings.xml')) { throw 'Unexpected UMM settings path.' }
    foreach ($language in @('zhCN','enGB','automatic')) {
        $settings.Language = $language
        $settings.Save($entry)
        $settings = $loader.Invoke($null, @($entry))
        if ($settings.Language -cne $language) { throw "UMM round trip failed for $language." }
    }
    [IO.File]::WriteAllText($path,'<PlannerLanguageSettings><Language>future</Language></PlannerLanguageSettings>')
    $before = Get-KbpSha256 $path
    $loaded = $loader.Invoke($null, @($entry))
    if ($loaded.Language -cne 'future' -or $resolver.Invoke($null, @([string]$loaded.Language)) -cne 'automatic' -or (Get-KbpSha256 $path) -cne $before) {
        throw 'Resolving an unknown preference did not preserve the file.'
    }
    [IO.File]::WriteAllText($path,'<PlannerLanguageSettings><Language>zhCN</Language></PlannerLanguageSettings>')
    if ($loader.Invoke($null, @($entry)).Language -cne 'zhCN') { throw 'Guarded startup fixture does not match actual UMM serialization.' }
    Write-Host 'Installed UMM language preference: PASS=6 FAIL=0'
} finally {
    $safeRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
    $resolved = [IO.Path]::GetFullPath($boundary)
    if (!$resolved.StartsWith($safeRoot, [StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($resolved) -notmatch '^KbpLocaleSettings-[0-9a-f]{32}$') { throw 'Unsafe cleanup path.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
