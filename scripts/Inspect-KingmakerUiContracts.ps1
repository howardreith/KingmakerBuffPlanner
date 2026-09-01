[CmdletBinding()]
param(
    [string]$TypePattern = '.*',
    [string]$MethodPattern = '.*',
    [switch]$ListMembers
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

$game = Get-KbpGamePath
$managed = Join-Path $game 'Kingmaker_Data\Managed'
$assemblyPath = Join-Path $managed 'Assembly-CSharp.dll'
$resolver = [ResolveEventHandler]{
    param($sender, $eventArgs)
    $name = ([Reflection.AssemblyName]$eventArgs.Name).Name + '.dll'
    $path = Join-Path $managed $name
    if (Test-Path -LiteralPath $path -PathType Leaf) {
        return [Reflection.Assembly]::ReflectionOnlyLoadFrom($path)
    }
    return $null
}

$opCodes = @{}
foreach ($field in [Reflection.Emit.OpCodes].GetFields(
    [Reflection.BindingFlags]'Public,Static')) {
    $opCode = [Reflection.Emit.OpCode]$field.GetValue($null)
    $opCodes[([int]$opCode.Value -band 0xffff)] = $opCode
}

function Format-Member($Member) {
    if ($null -eq $Member) { return '<null>' }
    return $Member.DeclaringType.FullName + '::' + $Member.Name
}

function Read-MethodIl([Reflection.MethodBase]$Method) {
    $body = $Method.GetMethodBody()
    if ($null -eq $body) { return @('<no method body>') }
    [byte[]]$bytes = $body.GetILAsByteArray()
    $module = $Method.Module
    $lines = [Collections.Generic.List[string]]::new()
    $position = 0
    while ($position -lt $bytes.Length) {
        $offset = $position
        $first = [int]$bytes[$position++]
        $key = if ($first -eq 0xfe) {
            if ($position -ge $bytes.Length) { throw 'Truncated two-byte opcode.' }
            0xfe00 -bor [int]$bytes[$position++]
        } else { $first }
        if (-not $opCodes.ContainsKey($key)) { throw "Unknown IL opcode 0x$($key.ToString('x4'))." }
        $opCode = [Reflection.Emit.OpCode]$opCodes[$key]
        $operand = ''
        switch ([string]$opCode.OperandType) {
            'InlineNone' { }
            'ShortInlineI' {
                $operand = [int]$bytes[$position]
                if ($operand -ge 128) { $operand -= 256 }
                $position++
            }
            'InlineI' { $operand = [BitConverter]::ToInt32($bytes, $position); $position += 4 }
            'InlineI8' { $operand = [BitConverter]::ToInt64($bytes, $position); $position += 8 }
            'ShortInlineR' { $operand = [BitConverter]::ToSingle($bytes, $position); $position += 4 }
            'InlineR' { $operand = [BitConverter]::ToDouble($bytes, $position); $position += 8 }
            'ShortInlineBrTarget' {
                $delta = [int]$bytes[$position]
                if ($delta -ge 128) { $delta -= 256 }
                $position++
                $operand = 'IL_' + (($position + $delta).ToString('x4'))
            }
            'InlineBrTarget' {
                $delta = [BitConverter]::ToInt32($bytes, $position)
                $position += 4
                $operand = 'IL_' + (($position + $delta).ToString('x4'))
            }
            'InlineSwitch' {
                $count = [BitConverter]::ToInt32($bytes, $position)
                $position += 4
                $base = $position + (4 * $count)
                $targets = for ($index = 0; $index -lt $count; $index++) {
                    $delta = [BitConverter]::ToInt32($bytes, $position)
                    $position += 4
                    'IL_' + (($base + $delta).ToString('x4'))
                }
                $operand = '(' + ($targets -join ', ') + ')'
            }
            'InlineString' {
                $token = [BitConverter]::ToInt32($bytes, $position)
                $position += 4
                $operand = '"' + $module.ResolveString($token).Replace('"', '\"') + '"'
            }
            { $_ -in @('InlineMethod', 'InlineField', 'InlineType', 'InlineTok') } {
                $token = [BitConverter]::ToInt32($bytes, $position)
                $position += 4
                try { $operand = Format-Member $module.ResolveMember($token) }
                catch { $operand = 'token 0x' + $token.ToString('x8') }
            }
            'InlineSig' {
                $token = [BitConverter]::ToInt32($bytes, $position)
                $position += 4
                $operand = 'signature 0x' + $token.ToString('x8')
            }
            'ShortInlineVar' { $operand = [int]$bytes[$position]; $position++ }
            'InlineVar' { $operand = [BitConverter]::ToUInt16($bytes, $position); $position += 2 }
            default { throw "Unsupported operand type $($opCode.OperandType)." }
        }
        $lines.Add(('IL_{0:x4}: {1,-12} {2}' -f $offset, $opCode.Name, $operand).TrimEnd())
    }
    return $lines
}

$contracts = [ordered]@{
    'Kingmaker.UI.ServiceWindow.FullScreenTabsWindow' = @(
        'OnShow', 'OnHide', 'ToggleShow', 'OnButtonClose', 'OnHotKeyEscPressed',
        '<OnShow>b__11_0', '<OnHide>b__12_0', 'OnHotkeyToShow', 'PlayShowSound')
    'Kingmaker.UI.ServiceWindow.ServiceWindowTabs' = @(
        'Show', 'OnShow', 'OnHide', 'Hide', 'ShowScreen')
    'Kingmaker.Game' = @(
        'StartMode', 'StopMode', 'DoStartMode', 'DoStopMode', 'get_CurrentMode')
    'Kingmaker.UI.StaticCanvas' = @(
        'OnGameModeStart', 'OnGameModeStop', 'HandleFullScreenUiChanged', 'SetHUDVisible')
    'Kingmaker.GameModes.GameModesFactory' = @('Create', 'Initialize')
    'Kingmaker.Controllers.Clicks.PointerController' = @('get_InGui', 'Tick', 'Activate', 'Deactivate')
    'Kingmaker.View.CameraRig' = @('OnGameModeStart', 'OnGameModeStop', 'UpdateInternal',
        'TickScroll', 'GetCameraScrollShiftByMouse', 'IsScrollActive', 'get_AnyMoveCameraKeyIsDown')
    'Kingmaker.View.CameraZoom' = @('UpdateInputFromMouse')
    'Kingmaker.UI.ServiceWindow.ServiceWindowController' = @(
        'HandleOpenInventory', 'HandleOpenCharScreen', 'HandleOpenJournal',
        'HandleOpenMap', 'HandleOpenSpellbook', 'OnHotKeyPressed')
    'Kingmaker.UI.UISoundManager' = @('Play')
    'Kingmaker.UI.Constructor.ButtonPF' = @(
        'OnPointerClick', 'OnPointerEnter', 'OnPointerExit')
    'Kingmaker.UI.Tooltip.TooltipTrigger' = @(
        'SetNameAndDescription', 'ShowTooltipManual', 'OnPointerEnter', 'OnPointerExit')
}

[AppDomain]::CurrentDomain.add_ReflectionOnlyAssemblyResolve($resolver)
try {
    $assembly = [Reflection.Assembly]::ReflectionOnlyLoadFrom($assemblyPath)
    $flags = [Reflection.BindingFlags]'Public,NonPublic,Instance,Static,DeclaredOnly'
    Write-Output ('Assembly-CSharp SHA-256: ' + (Get-KbpSha256 $assemblyPath))
    Write-Output ('Assembly-CSharp MVID: ' + $assembly.ManifestModule.ModuleVersionId)
    foreach ($entry in $contracts.GetEnumerator()) {
        if ([string]$entry.Key -notmatch $TypePattern) { continue }
        $type = $assembly.GetType([string]$entry.Key, $true)
        if ($ListMembers) {
            Write-Output ''
            Write-Output ('=== ' + $entry.Key + ' declared members ===')
            foreach ($field in @($type.GetFields($flags) | Sort-Object Name)) {
                Write-Output ('field ' + $field.FieldType.FullName + ' ' + $field.Name)
            }
            foreach ($property in @($type.GetProperties($flags) | Sort-Object Name)) {
                Write-Output ('property ' + $property.PropertyType.FullName + ' ' + $property.Name)
            }
        }
        foreach ($methodName in @($entry.Value)) {
            if ($methodName -notmatch $MethodPattern) { continue }
            $methods = @($type.GetMethods($flags) | Where-Object Name -ceq $methodName)
            if ($methods.Count -eq 0) { throw "Method not found: $($entry.Key)::$methodName" }
            foreach ($method in $methods) {
                $parameters = @($method.GetParameters() | ForEach-Object { $_.ParameterType.FullName })
                Write-Output ''
                Write-Output ('=== ' + $entry.Key + '::' + $method.Name +
                    '(' + ($parameters -join ', ') + ') ===')
                Read-MethodIl $method
            }
        }
    }
}
finally {
    [AppDomain]::CurrentDomain.remove_ReflectionOnlyAssemblyResolve($resolver)
}
