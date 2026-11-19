# Supplemental static review. Run from the worktree root in Windows PowerShell 5.1+.
# Reads only production source and catalogs. Does not build, install, or launch anything.
[CmdletBinding()]
param([string]$ProjectRoot = (Get-Location).Path)
$ErrorActionPreference = 'Stop'
$source = Join-Path $ProjectRoot 'src/KingmakerBuffPlanner'
$issues = [Collections.Generic.List[string]]::new()
$used = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$refs = 0
$computed = 0
$utf8 = [Text.UTF8Encoding]::new($false, $true)
function Read-Catalog([string]$language) {
    $json = [IO.File]::ReadAllText((Join-Path $source "Localization/$language.json"), $utf8)
    $map = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
    foreach ($property in ($json | ConvertFrom-Json).PSObject.Properties) { $map.Add($property.Name, $property.Value) }
    # These shipped catalogs use one property per line. Reject duplicate members that ConvertFrom-Json collapses.
    $members = [regex]::Matches($json, '(?m)^\s*"[^"\\]+"\s*:')
    if ($map.Count -ne $members.Count) { $issues.Add("$language contains duplicate or nonconforming members") }
    return ,$map
}
function Split-Arguments([string]$code, [int]$start) {
    $parts = [Collections.Generic.List[string]]::new()
    $depth = 0
    $quote = [char]0
    $field = $start
    for ($i = $start; $i -lt $code.Length; $i++) {
        $c = $code[$i]
        if ($quote -ne [char]0) {
            if ($c -eq '\') { $i++; continue }
            if ($c -eq $quote) { $quote = [char]0 }
            continue
        }
        if ($c -eq '"' -or $c -eq "'") { $quote = $c; continue }
        if ('([{'.Contains($c)) { $depth++; continue }
        if (')]}'.Contains($c) -and $depth -gt 0) { $depth--; continue }
        if ($depth -eq 0 -and ($c -eq ',' -or $c -eq ')')) {
            $parts.Add($code.Substring($field, $i - $field).Trim())
            if ($c -eq ')') { return $parts.ToArray() }
            $field = $i + 1
        }
    }
    throw "Unterminated call at offset $start"
}
function Key-Literals([string]$fragment) {
    foreach ($m in [regex]::Matches($fragment, '"([a-zA-Z][a-zA-Z0-9_.-]*\.[a-zA-Z0-9_.-]+)"')) { $m.Groups[1].Value }
}
$en = Read-Catalog 'enGB'
$zh = Read-Catalog 'zhCN'
function Require-Key([string]$key, [int]$argCount, [string]$site) {
    [void]$used.Add($key)
    if (-not $en.ContainsKey($key)) { $issues.Add("$site requires missing key $key"); return }
    $maxIndex = -1
    foreach ($m in [regex]::Matches($en[$key], '\{([0-9]+)\}')) { $maxIndex = [Math]::Max($maxIndex, [int]$m.Groups[1].Value) }
    if ($maxIndex -ge $argCount) { $issues.Add("$site passes $argCount arguments for $key but requires index $maxIndex") }
}
# Domains verified against ItemSourceSelector.SlotLabel, PlannerSourceCategory, routine membership,
# ControllerSearchKeyboard.Keys, ControllerWorkspaceNavigator.Regions, and the powerful-change contracts.
$domains = @{
 'slot.' = 'Armor Belt Head Feet Gloves Neck Ring1 Ring2 Wrist Shoulders Quick Primary Secondary'
 'category.' = 'All Spells Abilities Other'
 'classic.category.' = 'All Spells Abilities Other'
 'classic.keyboard.' = 'Space Delete Clear Apply Cancel'
 'classic.region.' = 'Header Routines Catalogue Casters Castings Recipients Inspector Footer Resources ClearPlan'
 'classic.chip.' = 'long short important'
 'routine.' = 'long short important'
 'compat.powerful.' = 'Strength Dexterity Constitution Intelligence Wisdom Charisma'
 'classic.coverage.' = 'one.one one.many many.one many.many'
 'classic.budget.short.' = 'one.one one.many many.one many.many'
}
foreach ($key in $en.Keys) {
    if (-not $zh.ContainsKey($key)) { $issues.Add("Missing zhCN: $key"); continue }
    foreach ($v in @($en[$key], $zh[$key])) {
        if ([string]::IsNullOrWhiteSpace($v)) { $issues.Add("Blank: $key"); continue }
        if ([regex]::Replace($v, '\{(?:0|[1-9]\d*)\}', '') -match '[{}]') { $issues.Add("Invalid placeholder: $key") }
        $stack = [Collections.Generic.Stack[string]]::new()
        foreach ($m in [regex]::Matches($v, '<[^>]*>')) {
            if ($m.Value -cnotmatch '^<(/?)(b|i|size|color)(?:=[^<>]+)?>$') { $issues.Add("Invalid markup: $key"); continue }
            if ($Matches[1] -eq '/') { if ($stack.Count -eq 0 -or $stack.Pop() -cne $Matches[2]) { $issues.Add("Tag nesting: $key") } }
            else { $stack.Push($Matches[2]) }
        }
        if ($stack.Count -ne 0 -or ([regex]::Replace($v, '<[^>]*>', '') -match '<')) { $issues.Add("Unclosed tag: $key") }
    }
    $eSlots = ([regex]::Matches($en[$key], '\{\d+\}').Value | Sort-Object) -join ','
    $zSlots = ([regex]::Matches($zh[$key], '\{\d+\}').Value | Sort-Object) -join ','
    if ($eSlots -cne $zSlots) { $issues.Add("Slot mismatch: $key") }
    $eTags = [regex]::Matches($en[$key], '<[^>]+>').Value -join '|'
    $zTags = [regex]::Matches($zh[$key], '<[^>]+>').Value -join '|'
    if ($eTags -cne $zTags) { $issues.Add("Tag mismatch: $key") }
}
foreach ($key in $zh.Keys) { if (-not $en.ContainsKey($key)) { $issues.Add("Unexpected zhCN key: $key") } }
foreach ($file in [IO.Directory]::EnumerateFiles($source, '*.cs', [IO.SearchOption]::AllDirectories)) {
    if ($file -match '[\\/](obj|bin)[\\/]') { continue }
    $s = [IO.File]::ReadAllText($file, $utf8)
    if (-not $s.Contains('PlannerText.Get(') -and -not $file.EndsWith('PlannerText.cs')) { continue }
    foreach ($m in [regex]::Matches($s,'\bPlannerText\.Get\(|\bGet\("routine\."')) {
        $refs++
        $start = $m.Index + $m.Value.IndexOf('(') + 1
        $ar = @(Split-Arguments $s $start)
        $first = $ar[0]
        $site = ([IO.Path]::GetFileName($file) + ':' + (1 + [regex]::Matches($s.Substring(0,$start),"\n").Count))
        $n = $ar.Count-1
        if ($first -cmatch '^"([^"]+)"$') { Require-Key $Matches[1] $n $site; continue }
        if ($first.Contains('?') -and -not $first.Contains('+')) {
            # Ternary predicate may compare machine values; only result arms are keys.
            $keys = @([regex]::Matches($first, '[?:]\s*"([^"]+)"') | ForEach-Object { $_.Groups[1].Value })
            if ($keys.Count -gt 0) { foreach ($k in $keys) { Require-Key $k $n $site }; continue }
        }
        $computed++
        if ($first -cmatch '^"([^"]+\.)"\s*\+' -and $domains.ContainsKey($Matches[1])) {
            $prefix = $Matches[1]
            foreach ($suffix in $domains[$prefix].Split(' ')) { Require-Key ($prefix+$suffix) $n $site }
            continue
        }
        if ($first -ceq 'labelKey') {
            $keys = [regex]::Matches($s, 'labelKey\s*=\s*"([^"]+)"') | ForEach-Object { $_.Groups[1].Value }
            foreach ($k in $keys) { Require-Key $k $n $site }
            continue
        }
        if ($first -ceq 'key' -and $n -eq 1 -and $file.EndsWith('CastingRunPresentation.cs')) {
            foreach ($match in [regex]::Matches($s, 'AddCount\(\w+,\s*"([^"]+)"')) { Require-Key $match.Groups[1].Value $n $site }
            continue
        }
        if ($first -in @('key','runKey')) {
            $before = $s.Substring([Math]::Max(0,$m.Index-900),[Math]::Min($m.Index,900))
            $definitions = [regex]::Matches($before, ('string\s+' + [regex]::Escape($first)+'\s*=\s*([^;]+);'))
            if ($definitions.Count -gt 0) {
                $def = $definitions[$definitions.Count-1].Groups[1].Value
                if ($def -cmatch '^"([^"]+\.)"\s*\+' -and $domains.ContainsKey($Matches[1])) {
                    $prefix = $Matches[1]
                    foreach ($suffix in $domains[$prefix].Split(' ')) { Require-Key ($prefix+$suffix) $n $site }
                    continue
                }
                $keys = @(Key-Literals $def)
                if ($keys.Count -gt 0) { foreach ($k in $keys) { Require-Key $k $n $site }; continue }
            }
        }
        $issues.Add("Unresolved lookup at $site with first argument $first")
    }
}
$unused = @($en.Keys | Where-Object { -not $used.Contains($_) } | Sort-Object)
[ordered]@{ english=$en.Count; simplifiedChinese=$zh.Count; callSites=$refs; computedSites=$computed;
    referencedKeys=$used.Count; unreferencedKeys=$unused; errors=$issues.ToArray() } | ConvertTo-Json -Depth 3
if ($issues.Count -gt 0) { exit 1 }
