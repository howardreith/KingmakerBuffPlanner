[CmdletBinding()]
param(
    # Folder for the captured logs of the affected existing checks
    # (default artifacts\review-logs).
    [string]$LogRoot
)

# Focused gate for the Kingmaker native-test reservation (protocol 1,
# docs/NATIVE-RESERVATION.md) as this lab implements it. Every case runs in a
# disposable boundary inside artifacts\runtime-harness-tests with a private
# reservation named by KBP_TEST_RESERVATION_ROOT (inherited by every child
# process); the machine-wide record is only observed, before and after, and
# must be unchanged. Separate Windows PowerShell processes run the actual entry,
# restoration, public recovery and fixture scripts. No game, live Mods folder,
# owner save, lab root or other lab is touched. It then runs the directly
# affected existing checks with captured, commit-bound logs. PASS/FAIL/NOT RUN
# lines and a RESULT line; the gate passes only when every required case ran
# and passed (exit 0); a required case that could not run makes it incomplete
# (exit 2); any failure exits 1.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeAutomation.Common.ps1')

$repo = Get-KbpRepositoryRoot
$commit = (& git -C $repo rev-parse HEAD).Trim()
$dirty = [bool](& git -C $repo status --porcelain)
$root = Join-Path $repo ('artifacts\runtime-harness-tests\reservation-' + [Guid]::NewGuid().ToString('N'))
if ([string]::IsNullOrWhiteSpace($LogRoot)) { $LogRoot = Join-Path $repo 'artifacts\review-logs' }
$results = New-Object System.Collections.Generic.List[string]
$processes = New-Object System.Collections.Generic.List[System.Diagnostics.Process]
$handles = New-Object System.Collections.Generic.List[System.IDisposable]
$contender = Join-Path $PSScriptRoot 'NativeReservationContender.ps1'
$fixtureScript = Join-Path $PSScriptRoot 'New-KbpAutomationFixture.ps1'
$restoreLocal = Join-Path $PSScriptRoot 'Restore-Local.ps1'
$priorReservationRoot = $env:KBP_TEST_RESERVATION_ROOT
$script:caseIndex = 0
$script:case = $null

# The machine-wide record, observed only: its presence and bytes.
function Get-ProductionLeaseObservation {
    $path = $script:KbpReservationRecordPath
    if (Test-Path -LiteralPath $path -PathType Leaf) { return 'present:' + (Get-KbpSha256 $path) }
    return 'absent'
}

function Test-Case([string]$Name, [scriptblock]$Body) {
    $script:caseIndex++
    $script:case = Join-Path $root ('case' + $script:caseIndex)
    New-Item -ItemType Directory -Path (Join-Path $script:case 'signals') -Force | Out-Null
    $env:KBP_TEST_RESERVATION_ROOT = Join-Path $script:case 'reservation'
    try {
        & $Body $script:case
        $results.Add("PASS $Name")
    }
    catch { $results.Add("FAIL ${Name}: $($_.Exception.Message)") }
    finally {
        foreach ($handle in $handles) { $handle.Dispose() }
        $handles.Clear()
        foreach ($process in $processes) { if (-not $process.HasExited) { $process.Kill(); [void]$process.WaitForExit(5000) } }
        $processes.Clear()
        $env:KBP_TEST_RESERVATION_ROOT = $priorReservationRoot
    }
}

function Assert-That([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }

function Assert-Throws([scriptblock]$Body, [string]$Fragment, [string]$Message) {
    try { & $Body }
    catch {
        if ($_.Exception.Message -notlike "*$Fragment*") { throw "$Message threw '$($_.Exception.Message)', expected '*$Fragment*'" }
        return
    }
    throw "$Message did not throw"
}

function Get-LeasePath { return (Join-Path $env:KBP_TEST_RESERVATION_ROOT 'reservation.v1.json') }
function Get-LeaseHash { return (Get-KbpSha256 (Get-LeasePath)) }
function Get-Lease { return (Read-KbpReservationRecord (Get-KbpReservationConvention)) }

# The gate's verdict: complete and passing only when nothing failed and no
# required case was left not run.
function Get-GateVerdict([string[]]$Lines) {
    if (@($Lines | Where-Object { $_ -like 'FAIL*' }).Count -gt 0) { return 'fail' }
    if (@($Lines | Where-Object { $_ -like 'NOT RUN*' }).Count -gt 0) { return 'incomplete' }
    if (@($Lines | Where-Object { $_ -like 'PASS*' }).Count -eq 0) { return 'incomplete' }
    return 'pass'
}

# --- stand-ins ---------------------------------------------------------------

function New-StandInLab([string]$Case, [string]$Name) {
    $lab = Join-Path $Case $Name
    foreach ($folder in @('state', 'staging', 'backups', 'evidence', 'game\Mods\Existing')) {
        New-Item -ItemType Directory -Path (Join-Path $lab $folder) -Force | Out-Null
    }
    Set-Content -LiteralPath (Join-Path $lab 'game\Kingmaker.exe') -Value 'stand-in' -Encoding Ascii
    Set-Content -LiteralPath (Join-Path $lab 'game\Mods\Existing\Info.json') -Value '{"Id":"Existing"}' -Encoding Ascii
    return $lab
}

function Get-StandInPackage([string]$Case) {
    $package = Join-Path $Case 'package.zip'
    if (-not (Test-Path -LiteralPath $package)) {
        $content = Join-Path $Case 'package-content\KingmakerBuffPlanner'
        New-Item -ItemType Directory -Path $content -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $content 'Info.json') -Value '{"Id":"KingmakerBuffPlanner"}' -Encoding Ascii
        Set-Content -LiteralPath (Join-Path $content 'KingmakerBuffPlanner.dll') -Value 'stand-in' -Encoding Ascii
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [IO.Compression.ZipFile]::CreateFromDirectory((Split-Path -Parent $content), $package)
    }
    return $package
}

function Get-ModsText([string]$Lab) { return (@(Get-KbpDirectoryManifest (Join-Path $Lab 'game\Mods')) | ConvertTo-Json -Depth 5 -Compress) }

function Enter-InProcess([string]$Case, [string]$Lab, [string]$RunId, [string]$ReservationToken, [scriptblock]$CaptureBaselines) {
    $arguments = @{ PackagePath = (Get-StandInPackage $Case); KingmakerInstallDir = (Join-Path $Lab 'game')
        StateRoot = (Join-Path $Lab 'state'); StagingRoot = (Join-Path $Lab 'staging'); BackupRoot = (Join-Path $Lab 'backups')
        RunId = $RunId; FixtureMode = $true; KnownKingmakerProcessIds = @(); MoveAttempts = 1; MoveDelayMilliseconds = 0 }
    if ($ReservationToken) { $arguments.ReservationToken = $ReservationToken }
    if ($CaptureBaselines) { $arguments.CaptureBaselines = $CaptureBaselines }
    return Enter-KbpRuntimeTransaction @arguments
}

function Restore-InProcess([string]$Lab, [string]$RunId, [switch]$RetainReservation) {
    return Restore-KbpRuntimeTransaction -RunId $RunId -StateRoot (Join-Path $Lab 'state') -FixtureMode -KnownKingmakerProcessIds @() `
        -RetainReservation:$RetainReservation
}

function Start-Contender([string]$Case, [string]$Action, [string]$Name, [string]$Lab, [string]$RunId, [switch]$Barrier) {
    $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', ('"' + $contender + '"'), '-Action', $Action,
        '-Name', $Name, '-RunId', $RunId, '-Signals', ('"' + (Join-Path $Case 'signals') + '"'), '-LabRoot', ('"' + $Lab + '"'),
        '-PackagePath', ('"' + (Get-StandInPackage $Case) + '"'))
    if ($Barrier) { $arguments += '-Barrier' }
    $process = Start-Process -FilePath 'powershell.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $processes.Add($process)
    return $process
}

function Quote([string]$Value) { return "'" + $Value.Replace("'", "''") + "'" }

# Runs a script with named arguments in a separate process (inheriting the
# private reservation). Writes <Name>.done or <Name>.refused (message).
function Start-ScriptChild([string]$Case, [string]$Name, [string]$Script, [hashtable]$Arguments, [string[]]$Switches = @()) {
    $signals = Join-Path $Case 'signals'
    $parts = foreach ($key in $Arguments.Keys) { '-' + $key + ' ' + (Quote ([string]$Arguments[$key])) }
    $parts = @($parts) + @($Switches | ForEach-Object { '-' + $_ })
    $text = ('$ErrorActionPreference = ''Stop''; try { & ' + (Quote $Script) + ' ' + ($parts -join ' ') + ' -Confirm:$false | Out-Null; ' +
        '[IO.File]::WriteAllText(' + (Quote (Join-Path $signals "$Name.done")) + ', ''ok'') } catch { [IO.File]::WriteAllText(' +
        (Quote (Join-Path $signals "$Name.refused")) + ', $_.Exception.Message); exit 3 }')
    $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($text))
    $process = Start-Process -FilePath 'powershell.exe' -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-EncodedCommand', $encoded) -WindowStyle Hidden -PassThru
    $processes.Add($process)
    return $process
}

function Wait-Signal([string]$Case, [string]$Name, [string[]]$What, [int]$Seconds = 120) {
    $until = (Get-Date).AddSeconds($Seconds)
    while ((Get-Date) -lt $until) {
        foreach ($candidate in $What) {
            $path = Join-Path $Case "signals\$Name.$candidate"
            if (Test-Path -LiteralPath $path) { return [pscustomobject]@{ What = $candidate; Text = [IO.File]::ReadAllText($path) } }
        }
        Start-Sleep -Milliseconds 100
    }
    throw "$Name reached none of: $($What -join ', ')"
}

function Wait-File([string]$Path, [int]$Seconds = 120) {
    $until = (Get-Date).AddSeconds($Seconds)
    while (-not (Test-Path -LiteralPath $Path)) {
        if ((Get-Date) -gt $until) { throw "never appeared: $Path" }
        Start-Sleep -Milliseconds 100
    }
}

function Wait-Status([string]$Lab, [string]$RunId, [string]$Status, [int]$Seconds = 60) {
    $statePath = Join-Path $Lab "state\transactions\$RunId\transaction.json"
    $until = (Get-Date).AddSeconds($Seconds)
    while ((Get-Date) -lt $until) {
        if (Test-Path -LiteralPath $statePath) { try { if ([string](Read-KbpJson $statePath).status -ceq $Status) { return } } catch { } }
        Start-Sleep -Milliseconds 100
    }
    throw "run $RunId never reached status $Status"
}

# Keeps a file open without delete sharing, so the directory holding it cannot
# be renamed: the actual transaction pauses in its bounded move retry.
function Hold-File([string]$Path) {
    $handle = [IO.File]::Open($Path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
    $handles.Add($handle)
    return $handle
}

function Get-DeadProcessId {
    $process = Start-Process -FilePath (Join-Path $env:WINDIR 'System32\PING.EXE') -ArgumentList @('-n', '1', '127.0.0.1') -WindowStyle Hidden -PassThru
    [void]$process.WaitForExit(10000)
    return $process.Id
}

# A record as another writer would leave it (common fields complete).
function Set-Lease([string]$Holder, [int]$HolderProcessId, [string]$RunId, [string]$StateRoot = '', [string]$Token = '',
    [string]$HolderStartUtc = '2020-01-01T00:00:00.0000000Z') {
    New-Item -ItemType Directory -Path $env:KBP_TEST_RESERVATION_ROOT -Force | Out-Null
    if (-not $Token) { $Token = [Guid]::NewGuid().ToString('N') }
    $record = [ordered]@{ protocol = 'kingmaker-native-test-reservation/1'; lab = $Holder; runId = $RunId; purpose = 'stand-in'
        token = $Token; holderProcessId = $HolderProcessId; holderStartUtc = $HolderStartUtc; host = $env:COMPUTERNAME
        createdUtc = [DateTime]::UtcNow.ToString('o') }
    if ($StateRoot) { $record.stateRoot = [IO.Path]::GetFullPath($StateRoot).TrimEnd('\') }
    [IO.File]::WriteAllText((Get-LeasePath), ($record | ConvertTo-Json))
}

function Get-MyStartUtc { return (Get-Process -Id $PID).StartTime.ToUniversalTime().ToString('o') }

function New-SeedSaves([string]$SaveRoot) {
    New-Item -ItemType Directory -Path $SaveRoot -Force | Out-Null
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    foreach ($save in @(@{ File = 'Manual_400_PlayerCampaign.zks'; Name = 'PlayerCampaign' }, @{ File = 'Manual_401_KBP_AUTOMATION_SEED.zks'; Name = 'KBP_AUTOMATION_SEED' })) {
        $archive = [IO.Compression.ZipFile]::Open((Join-Path $SaveRoot $save.File), [IO.Compression.ZipArchiveMode]::Create, [Text.Encoding]::UTF8)
        try {
            $writer = [IO.StreamWriter]::new($archive.CreateEntry('header.json').Open(), [Text.Encoding]::UTF8)
            try { $writer.Write('{"Name":"' + $save.Name + '","GameName":"Disposable Automation Campaign","GameId":"11111111-2222-3333-4444-555555555555","Area":"Test Area"}') }
            finally { $writer.Dispose() }
        } finally { $archive.Dispose() }
    }
}

function Get-TreeText([string]$Path) {
    return (@(Get-ChildItem -LiteralPath $Path -File -Recurse | Sort-Object FullName | ForEach-Object { $_.FullName + '=' + (Get-KbpSha256 $_.FullName) }) -join "`n")
}

# --- the cases ---------------------------------------------------------------

$productionBefore = Get-ProductionLeaseObservation
try {
    New-Item -ItemType Directory -Path $root -Force | Out-Null

    Test-Case 'F11/F12: production convention exact; private mode bounded; mixed shared/private targets refused' {
        param($case)
        $saved = $env:KBP_TEST_RESERVATION_ROOT
        $env:KBP_TEST_RESERVATION_ROOT = $null
        try {
            $production = Get-KbpReservationConvention -Targets @($script:KbpRuntimeStateRoot)
            Assert-That ($production.MutexName -ceq 'Global\KingmakerNativeTest.Reservation.v1' -and -not $production.Private) "mutex $($production.MutexName)"
            Assert-That ($production.RecordPath -ceq (Join-Path $env:ProgramData 'KingmakerNativeTest\reservation.v1.json')) "record $($production.RecordPath)"
            $env:KBP_TEST_RESERVATION_ROOT = Join-Path $env:TEMP 'kbp-outside'
            Assert-Throws { Get-KbpReservationConvention } 'is not inside this checkout' 'a private root outside the harness root'
        }
        finally { $env:KBP_TEST_RESERVATION_ROOT = $saved }
        $private = Get-KbpReservationConvention -Targets @((Join-Path $case 'lab'))
        Assert-That ($private.Private -and $private.RecordPath -ceq (Get-LeasePath) -and $private.MutexName -cne 'Global\KingmakerNativeTest.Reservation.v1') 'private convention'
        Assert-Throws { Get-KbpReservationConvention -Targets @((Join-Path $case 'lab'), $script:KbpRuntimeStateRoot) } 'refused for a shared target' 'mixed targets'
        $lab = New-StandInLab $case 'lab'
        Assert-Throws { Enter-KbpRuntimeTransaction -PackagePath (Get-StandInPackage $case) -KingmakerInstallDir (Join-Path $env:ProgramData '.') `
            -StateRoot (Join-Path $lab 'state') -StagingRoot (Join-Path $lab 'staging') -BackupRoot (Join-Path $lab 'backups') -RunId 'mixed' -FixtureMode -KnownKingmakerProcessIds @() } `
            'executable is missing' 'entry with a shared game folder (refused before any reservation)'
        $outside = Start-Process -FilePath 'powershell.exe' -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', ('"' + $contender + '"'),
            '-Action', 'Enter', '-Name', 'outside', '-RunId', 'outside', '-Signals', ('"' + (Join-Path $case 'signals') + '"'), '-LabRoot', ('"' + $env:TEMP + '"'),
            '-PackagePath', ('"' + (Get-StandInPackage $case) + '"')) -WindowStyle Hidden -PassThru
        $processes.Add($outside)
        Assert-That ($outside.WaitForExit(60000) -and $outside.ExitCode -ne 0) 'a contender outside the harness root was not refused'
        Assert-That (-not (Test-Path -LiteralPath (Join-Path $case 'signals\outside.active'))) 'the bounded contender entered'
    }

    Test-Case 'a lab alone (no peer, no reservation folder yet) enters, holds across the transaction and releases' {
        param($case)
        $lab = New-StandInLab $case 'alone'
        Assert-That (-not (Test-Path -LiteralPath $env:KBP_TEST_RESERVATION_ROOT)) 'the reservation folder pre-exists'
        $statePath = Enter-InProcess $case $lab 'alone-run'
        $lease = Get-Lease
        Assert-That ($lease.lab -ceq 'KingmakerBuffPlanner' -and $lease.token -ceq (Read-KbpJson $statePath).reservationToken -and [int]$lease.holderProcessId -eq $PID) 'lease not journaled'
        $restored = Restore-InProcess $lab 'alone-run'
        Assert-That ($restored.restorationVerified -and $null -ne $restored.PSObject.Properties['reservationReleasedAtUtc']) 'no journaled release'
        Assert-That (-not (Test-Path -LiteralPath (Get-LeasePath))) 'not released'
    }

    Test-Case 'two labs exclude each other through the reservation alone, in either arrival order (prepared, active with writer exited, restoring)' {
        param($case)
        $first = New-StandInLab $case 'lab-one'
        $second = New-StandInLab $case 'lab-two'
        foreach ($order in @(@('A', $first, 'B', $second), @('B2', $second, 'A2', $first))) {
            $winner = $order[0]; $winnerLab = $order[1]; $loser = $order[2]; $loserLab = $order[3]
            $loserRefused = {
                param([string]$Name)
                $before = Get-ModsText $loserLab
                [void](Start-Contender $case 'Enter' $Name $loserLab $Name)
                $outcome = Wait-Signal $case $Name @('refused', 'active')
                Assert-That ($outcome.What -ceq 'refused' -and $outcome.Text -like "*reserved by KingmakerBuffPlanner run $winner*") "loser ${Name}: $($outcome.What) $($outcome.Text)"
                Assert-That (-not (Test-Path -LiteralPath (Join-Path $loserLab 'state\deployment.lock')) -and
                    -not (Test-Path -LiteralPath (Join-Path $loserLab "state\transactions\$Name")) -and (Get-ModsText $loserLab) -ceq $before) "loser $Name mutated its lab"
            }
            $hold = Hold-File (Join-Path $winnerLab 'game\Mods\Existing\Info.json')
            $winnerProcess = Start-Contender $case 'Enter' $winner $winnerLab $winner
            Wait-Status $winnerLab $winner 'Prepared'
            & $loserRefused "$loser-prepared"
            $hold.Dispose()
            Assert-That ((Wait-Signal $case $winner @('active', 'refused')).What -ceq 'active') 'winner failed'
            Assert-That ($winnerProcess.WaitForExit(30000)) 'winner did not exit'
            & $loserRefused "$loser-active"
            $hold = Hold-File (Join-Path $winnerLab 'game\Mods\KingmakerBuffPlanner\Info.json')
            [void](Start-Contender $case 'Restore' "$winner-restore" $winnerLab $winner)
            [void](Wait-Signal $case "$winner-restore" @('restoring'))
            Start-Sleep -Milliseconds 1500
            & $loserRefused "$loser-restoring"
            $hold.Dispose()
            Assert-That ((Wait-Signal $case "$winner-restore" @('restored', 'refused')).What -ceq 'restored') 'winner restoration failed'
            Assert-That (-not (Test-Path -LiteralPath (Get-LeasePath))) 'winner kept its reservation'
            $after = Start-Contender $case 'Enter' "$loser-after" $loserLab "$loser-after"
            Assert-That ((Wait-Signal $case "$loser-after" @('active', 'refused')).What -ceq 'active') 'loser not admitted after release'
            Assert-That ($after.WaitForExit(30000)) 'admitted loser did not exit'
            [void](Restore-InProcess $loserLab "$loser-after")
        }
    }

    Test-Case 'simultaneous arrivals from a shared start barrier admit exactly one; the loser leaves no mutation' {
        param($case)
        $first = New-StandInLab $case 'lab-one'
        $second = New-StandInLab $case 'lab-two'
        foreach ($round in 1..3) {
            $start = Join-Path $case 'signals\start'
            if (Test-Path -LiteralPath $start) { Remove-Item -LiteralPath $start -Force }
            $x = Start-Contender $case 'Enter' "X$round" $first "X$round" -Barrier
            $y = Start-Contender $case 'Enter' "Y$round" $second "Y$round" -Barrier
            [void](Wait-Signal $case "X$round" @('ready')); [void](Wait-Signal $case "Y$round" @('ready'))
            [IO.File]::WriteAllText($start, 'go')
            $xo = Wait-Signal $case "X$round" @('active', 'refused'); $yo = Wait-Signal $case "Y$round" @('active', 'refused')
            Assert-That ($x.WaitForExit(30000) -and $y.WaitForExit(30000)) 'a contender did not exit'
            Assert-That (@(@($xo.What, $yo.What) | Where-Object { $_ -ceq 'active' }).Count -eq 1) "round ${round}: $($xo.What)/$($yo.What)"
            $loserLab = if ($xo.What -ceq 'refused') { $first } else { $second }
            $loserRun = if ($xo.What -ceq 'refused') { "X$round" } else { "Y$round" }
            $loserText = if ($xo.What -ceq 'refused') { $xo.Text } else { $yo.Text }
            Assert-That ($loserText -like '*reserved by KingmakerBuffPlanner*') "loser reason: $loserText"
            Assert-That (-not (Test-Path -LiteralPath (Join-Path $loserLab 'state\deployment.lock')) -and
                -not (Test-Path -LiteralPath (Join-Path $loserLab "state\transactions\$loserRun")) -and
                -not (Test-Path -LiteralPath (Join-Path $loserLab 'game\Mods\KingmakerBuffPlanner'))) "round ${round}: the loser mutated its lab"
            if ($xo.What -ceq 'active') { [void](Restore-InProcess $first "X$round") } else { [void](Restore-InProcess $second "Y$round") }
        }
    }

    Test-Case 'F12: partial own and non-UTF-8 records refuse byte-identical; a complete peer with another token format is identified and kept' {
        param($case)
        $lab = New-StandInLab $case 'lab'
        $path = Get-LeasePath
        New-Item -ItemType Directory -Path $env:KBP_TEST_RESERVATION_ROOT -Force | Out-Null
        $stateRoot = [IO.Path]::GetFullPath((Join-Path $lab 'state')).TrimEnd('\').Replace('\', '\\')
        $common = '"protocol":"kingmaker-native-test-reservation/1","runId":"r","purpose":"p","holderProcessId":4,"holderStartUtc":"2020-01-01T00:00:00.0000000Z","host":"h","createdUtc":"2020-01-01T00:00:00.0000000Z"'
        $ownTail = ',"lab":"KingmakerBuffPlanner","token":"0123456789abcdef0123456789abcdef","stateRoot":"' + $stateRoot + '"}'
        $bad = @(
            ('{' + $common.Replace(',"purpose":"p"', '') + $ownTail),
            ('{' + $common.Replace(',"host":"h"', '') + $ownTail),
            ('{' + $common.Replace(',"createdUtc":"2020-01-01T00:00:00.0000000Z"', '') + $ownTail),
            ('{' + $common + $ownTail.Replace('0123456789abcdef0123456789abcdef', 'not-a-hex-token')),
            ('{' + $common + $ownTail.Replace(',"stateRoot":"' + $stateRoot + '"', '')),
            ('{' + $common.Replace('kingmaker-native-test-reservation/1', 'Kingmaker-Native-Test-Reservation/1') + $ownTail))
        foreach ($text in $bad) {
            [IO.File]::WriteAllText($path, $text)
            $hash = Get-LeaseHash
            Assert-Throws { Enter-InProcess $case $lab 'x' } 'malformed or of an unknown protocol' "a partial or case-changed record was accepted: $text"
            Assert-That ((Get-LeaseHash) -ceq $hash) 'a refused record changed'
        }
        foreach ($bytes in @([Text.Encoding]::Unicode.GetBytes('{' + $common + $ownTail), [byte[]](0x7B, 0xFF, 0xFE, 0x7D))) {
            [IO.File]::WriteAllBytes($path, $bytes)
            $hash = Get-LeaseHash
            Assert-Throws { Enter-InProcess $case $lab 'x' } 'malformed or of an unknown protocol' 'a non-UTF-8 record was accepted'
            Assert-That ((Get-LeaseHash) -ceq $hash) 'a non-UTF-8 record changed'
        }
        [IO.File]::WriteAllText($path, '{' + $common + ',"lab":"KingmakerFlight","token":"Flt-7f3a2b1c-Base64+/=="}')
        $hash = Get-LeaseHash
        Assert-Throws { Enter-InProcess $case $lab 'x' } 'reserved by KingmakerFlight run r' 'a valid peer record was not identified'
        Assert-That ((Get-LeaseHash) -ceq $hash) 'the peer record changed'
    }

    Test-Case 'writer liveness compares start instants (any ISO form), and an unparsable start time counts as alive' {
        param($case)
        $start = (Get-Process -Id $PID).StartTime.ToUniversalTime()
        $sameInstant = [pscustomobject]@{ holderProcessId = $PID; holderStartUtc = $start.ToString('yyyy-MM-ddTHH:mm:ss.fffffff') + '+00:00' }
        $otherInstant = [pscustomobject]@{ holderProcessId = $PID; holderStartUtc = $start.AddSeconds(-1).ToString('o') }
        $unparsable = [pscustomobject]@{ holderProcessId = $PID; holderStartUtc = 'not a time' }
        $gone = [pscustomobject]@{ holderProcessId = (Get-DeadProcessId); holderStartUtc = $start.ToString('o') }
        Assert-That (Test-KbpReservationHolderAlive $sameInstant) 'the same instant in another ISO form was taken as gone'
        Assert-That (Test-KbpRecordWriterIsMe $sameInstant) 'this process was not recognized in another ISO form'
        Assert-That (-not (Test-KbpReservationHolderAlive $otherInstant)) 'a reused PID with another start instant was taken as the writer'
        Assert-That (Test-KbpReservationHolderAlive $unparsable) 'an unparsable start time was taken as gone'
        Assert-That (-not (Test-KbpReservationHolderAlive $gone)) 'an exited writer was taken as alive'
    }

    Test-Case 'F1/F6: recovery takes over only its journaled token after the writer exited; live writers refuse; missing or replaced records are incidents' {
        param($case)
        $lab = New-StandInLab $case 'lab'
        $peer = New-StandInLab $case 'peer'
        $labState = Join-Path $lab 'state'
        Set-Lease 'KingmakerBuffPlanner' (Get-DeadProcessId) 'crashed' $labState
        $hash = Get-LeaseHash
        Assert-Throws { Enter-InProcess $case $lab 'after-crash' } 'reserved by KingmakerBuffPlanner run crashed' 'entry over an orphan record'
        Assert-That ((Get-LeaseHash) -ceq $hash) 'the orphan changed'
        $incidentDir = Join-Path $case 'incident'; New-Item -ItemType Directory -Path $incidentDir -Force | Out-Null
        $token = (Get-Lease).token
        Set-Lease 'KingmakerBuffPlanner' $PID 'live' $labState $token
        $claimed = Enter-KbpNativeReservation -RunId 'live' -Purpose 'test' -StateRoot $labState -RecoverToken $token
        Assert-That ($claimed.token -ceq $token) 'self recovery'
        $child = Start-ScriptChild $case 'claim-live' (Join-Path $case 'claim.ps1') @{}
        [IO.File]::WriteAllText((Join-Path $case 'claim.ps1'), ('. ' + (Quote (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')) +
            '; [void](Enter-KbpNativeReservation -RunId live -Purpose test -StateRoot ' + (Quote $labState) + ' -RecoverToken ' + (Quote $token) + ')'))
        $child = Start-ScriptChild $case 'claim-live2' (Join-Path $case 'claim.ps1') @{}
        $outcome = Wait-Signal $case 'claim-live2' @('done', 'refused')
        Assert-That ($outcome.What -ceq 'refused' -and $outcome.Text -like "*still held by its live process $PID*") "a live writer was taken over: $($outcome.Text)"
        $hash = Get-LeaseHash
        Set-Lease 'KingmakerBuffPlanner' (Get-DeadProcessId) 'dead' $labState $token
        $taken = Enter-KbpNativeReservation -RunId 'dead' -Purpose 'test' -StateRoot $labState -RecoverToken $token
        Assert-That ($taken.token -ceq $token -and [int](Get-Lease).holderProcessId -eq $PID) 'a dead writer''s token was not taken over with the same token'
        Assert-Throws { [void](Enter-KbpNativeReservation -RunId 'dead' -Purpose 'test' -StateRoot (Join-Path $peer 'state') -RecoverToken $token) } 'lost its Kingmaker native-test reservation' 'recovery from another state root'
        Set-Lease 'KingmakerMountedCombat' 4 'kmc'
        $hash = Get-LeaseHash
        Assert-Throws { [void](Enter-KbpNativeReservation -RunId 'dead' -Purpose 'test' -StateRoot $labState -RecoverToken $token -IncidentDirectory $incidentDir) } 'lost its Kingmaker native-test reservation' 'recovery over a replaced record'
        Assert-That ((Get-LeaseHash) -ceq $hash -and (Test-Path -LiteralPath (Join-Path $incidentDir 'reservation-incident.json'))) 'replaced record changed or no incident'
        $first = Get-KbpSha256 (Join-Path $incidentDir 'reservation-incident.json')
        Remove-Item -LiteralPath (Get-LeasePath) -Force
        Assert-Throws { [void](Enter-KbpNativeReservation -RunId 'dead' -Purpose 'test' -StateRoot $labState -RecoverToken $token -IncidentDirectory $incidentDir) } 'now: no record' 'recovery over a missing record'
        Assert-That ((Get-KbpSha256 (Join-Path $incidentDir 'reservation-incident.json')) -ceq $first) 'the first incident was replaced'
    }

    Test-Case 'F6: public Restore-Local admits before any save, registry or Mods change (foreign legacy lease, live entry writer, two same-run recoverers, Mods-only route)' {
        param($case)
        $lab = New-StandInLab $case 'lab'
        $labState = Join-Path $lab 'state'; $labEvidence = Join-Path $lab 'evidence'
        # A transaction recorded before this lab took part, with a pending protected-save comparison, under a peer's reservation.
        [void](Enter-InProcess $case $lab 'legacy')
        $statePath = Join-Path $labState 'transactions\legacy\transaction.json'
        $state = Read-KbpJson $statePath
        Remove-Item -LiteralPath (Get-LeasePath) -Force
        $state.PSObject.Properties.Remove('reservationToken'); Write-KbpJsonAtomic $statePath $state
        $saveRoot = Join-Path $case 'saves'; New-SeedSaves $saveRoot
        $baseline = Save-KbpProtectedSaveBaseline -TransactionDirectory (Split-Path -Parent $statePath) -RunId 'legacy' -Scenario 'live-cast-qual' `
            -FixtureFamily 'Automation' -WorkingFileName 'Manual_2_KBP_AUTOMATION_WORKING.zks' -SaveRoot $saveRoot -Snapshot (Get-KbpSaveFolderSnapshot -SaveRoot $saveRoot)
        Set-Lease 'KingmakerWakingRune' 4 'wr'
        $leaseHash = Get-LeaseHash; $labTree = Get-TreeText $lab
        foreach ($route in @(@(), @('-RestoreModsOnly'))) {
            $routeArgs = @{ RunId = 'legacy'; StateRoot = $labState; EvidenceRoot = $labEvidence; Confirm = $false }
            if ($route.Count) { $routeArgs.RestoreModsOnly = $true }
            Assert-Throws { & $restoreLocal @routeArgs } 'reserved by KingmakerWakingRune run wr' "Restore-Local $($route -join ' ') under a foreign lease"
            Assert-That ((Get-LeaseHash) -ceq $leaseHash -and (Get-TreeText $lab) -ceq $labTree -and -not [bool](Read-KbpJson $baseline).compared) 'a refused recovery changed something'
        }
        Remove-Item -LiteralPath (Get-LeasePath) -Force
        # A live entry writer (this process) refuses recovery from another process before any change.
        [void](Enter-InProcess $case (New-StandInLab $case 'lab2') 'writer')
        $lab2 = Join-Path $case 'lab2'
        $tree2 = Get-TreeText $lab2
        [void](Start-ScriptChild $case 'recover-live' $restoreLocal @{ RunId = 'writer'; StateRoot = (Join-Path $lab2 'state'); EvidenceRoot = (Join-Path $lab2 'evidence') })
        $outcome = Wait-Signal $case 'recover-live' @('done', 'refused')
        Assert-That ($outcome.What -ceq 'refused' -and $outcome.Text -like "*still held by its live process $PID*" -and (Get-TreeText $lab2) -ceq $tree2) "recovery under a live writer: $($outcome.Text)"
        [void](Restore-InProcess $lab2 'writer')
        # Two same-run recoverers: the second refuses while the first is inside its (held) move.
        $lab3 = New-StandInLab $case 'lab3'
        $entry = Start-Contender $case 'Enter' 'entry3' $lab3 'run3'
        Assert-That ((Wait-Signal $case 'entry3' @('active', 'refused')).What -ceq 'active' -and $entry.WaitForExit(30000)) 'entry3 failed'
        $hold = Hold-File (Join-Path $lab3 'game\Mods\KingmakerBuffPlanner\Info.json')
        $r1 = Start-ScriptChild $case 'recover-a' $restoreLocal @{ RunId = 'run3'; StateRoot = (Join-Path $lab3 'state'); EvidenceRoot = (Join-Path $lab3 'evidence') }
        Start-Sleep -Milliseconds 2500
        $r2 = Start-ScriptChild $case 'recover-b' $restoreLocal @{ RunId = 'run3'; StateRoot = (Join-Path $lab3 'state'); EvidenceRoot = (Join-Path $lab3 'evidence') }
        $second = Wait-Signal $case 'recover-b' @('done', 'refused')
        Assert-That ($second.What -ceq 'refused' -and ($second.Text -like '*Another restore is active*' -or $second.Text -like '*still held by its live process*')) "a second same-run recoverer was admitted: $($second.Text)"
        $hold.Dispose()
        Assert-That ((Wait-Signal $case 'recover-a' @('done', 'refused')).What -ceq 'done') 'the first recoverer failed'
        Assert-That (-not (Test-Path -LiteralPath (Get-LeasePath))) 'the recovered run kept its reservation'
    }

    Test-Case 'F7: a lease replaced during a blocked Mods move stops the restoration at the next boundary; a lease removed before release fails it durably' {
        param($case)
        $lab = New-StandInLab $case 'lab'
        $entry = Start-Contender $case 'Enter' 'e' $lab 'moving'
        Assert-That ((Wait-Signal $case 'e' @('active', 'refused')).What -ceq 'active' -and $entry.WaitForExit(30000)) 'entry failed'
        $hold = Hold-File (Join-Path $lab 'game\Mods\KingmakerBuffPlanner\Info.json')
        [void](Start-Contender $case 'Restore' 'r' $lab 'moving')
        [void](Wait-Signal $case 'r' @('restoring'))
        Start-Sleep -Milliseconds 2500
        Set-Lease 'KingmakerFlight' 4 'flight-took-over'
        $replacement = Get-LeaseHash
        $hold.Dispose()
        $outcome = Wait-Signal $case 'r' @('restored', 'refused')
        Assert-That ($outcome.What -ceq 'refused' -and $outcome.Text -like '*lost its Kingmaker native-test reservation*') "restoration went on: $($outcome.Text)"
        Assert-That ((Get-LeaseHash) -ceq $replacement) 'the replacement record changed'
        Assert-That (-not (Test-Path -LiteralPath (Join-Path $lab 'game\Mods')) -and (Test-Path -LiteralPath (Join-Path $lab 'backups\moving\Mods.original'))) 'a shared change followed the loss'
        $incident = Get-Content -LiteralPath (Join-Path $lab 'state\transactions\moving\reservation-incident.json') -Raw | ConvertFrom-Json
        Assert-That ($incident.progress -like '*staged Mods moved to quarantine*' -and $incident.observed -like '*KingmakerFlight*') "incident: $($incident | ConvertTo-Json -Compress)"
        # Removed immediately before the final release (a fresh private boundary: the replacement above stays as evidence).
        $env:KBP_TEST_RESERVATION_ROOT = Join-Path $case 'reservation2'
        $lab2 = New-StandInLab $case 'lab2'
        [void](Enter-InProcess $case $lab2 'release')
        $restored = Restore-InProcess $lab2 'release' -RetainReservation
        Assert-That ($restored.restorationVerified -and (Test-Path -LiteralPath (Get-LeasePath))) 'the retained reservation is gone'
        Remove-Item -LiteralPath (Get-LeasePath) -Force
        Assert-Throws { [void](Complete-KbpRunReservation -StateRoot (Join-Path $lab2 'state') -RunId 'release' -Token $restored.reservationToken) } 'could not release' 'release of a removed lease'
        Assert-That ($null -eq (Read-KbpJson (Join-Path $lab2 'state\transactions\release\transaction.json')).PSObject.Properties['reservationReleasedAtUtc']) 'a failed release was journaled as done'
        Assert-That (Test-Path -LiteralPath (Join-Path $lab2 'state\transactions\release\reservation-incident.json')) 'no incident for the failed release'
    }

    Test-Case 'F8: a lock left after the Restored write keeps the reservation until a retry reconciles it; a lock the entry could not release keeps it' {
        param($case)
        $lab = New-StandInLab $case 'lab'
        $statePath = Enter-InProcess $case $lab 'reconcile'
        $state = Read-KbpJson $statePath
        $restored = Restore-InProcess $lab 'reconcile' -RetainReservation
        New-KbpOwnedLock (Join-Path $lab 'state\deployment.lock') 'reconcile' $state.token
        Assert-That (-not (Complete-KbpRunReservation -StateRoot (Join-Path $lab 'state') -RunId 'reconcile' -Token $restored.reservationToken)) 'released with the lock present'
        Assert-Throws { Enter-InProcess $case (New-StandInLab $case 'peer') 'peer' } 'reserved by KingmakerBuffPlanner run reconcile' 'a peer while the lock is unreconciled'
        $again = Restore-InProcess $lab 'reconcile'
        Assert-That (-not (Test-Path -LiteralPath (Join-Path $lab 'state\deployment.lock')) -and -not (Test-Path -LiteralPath (Get-LeasePath)) -and
            $null -ne $again.PSObject.Properties['reservationReleasedAtUtc']) 'the retry did not reconcile the lock and release'
        # Lock-removal failure during admission (the confirmation fails and cannot remove the new lock).
        $lab2 = New-StandInLab $case 'lab2'
        function Confirm-KbpLockedWithoutForeignLease { throw 'injected confirmation failure; its lock could not be removed' }
        try { Assert-Throws { Enter-InProcess $case $lab2 'stuck' } 'could not release its own lock' 'entry whose lock removal failed' }
        finally { Remove-Item -LiteralPath Function:\Confirm-KbpLockedWithoutForeignLease }
        Assert-That ((Get-Lease).runId -ceq 'stuck') 'the reservation was released ahead of the stuck lock'
    }

    Test-Case 'F9: a missing or mismatched original backup refuses before the live Mods moves; an unavailable launcher baseline stops the entry with Mods untouched' {
        param($case)
        foreach ($damage in @('missing', 'mismatch')) {
            $lab = New-StandInLab $case "lab-$damage"
            [void](Enter-InProcess $case $lab $damage)
            $backup = Join-Path $lab "backups\$damage\Mods.original"
            if ($damage -ceq 'missing') { [IO.Directory]::Move($backup, (Join-Path $lab 'moved-away')) }
            else { [IO.File]::AppendAllText((Join-Path $backup 'Existing\Info.json'), 'altered') }
            $live = Get-ModsText $lab
            Assert-Throws { Restore-InProcess $lab $damage } $(if ($damage -ceq 'missing') { 'backup is missing' } else { 'does not match the recorded original manifest' }) "restore with a $damage backup"
            Assert-That ((Get-ModsText $lab) -ceq $live -and (Test-Path -LiteralPath (Join-Path $lab 'game\Mods\.kbp-runtime-sentinel.json'))) "the live Mods changed with a $damage backup"
            Assert-That ((Get-Lease).runId -ceq $damage) 'the refused restoration released'
            Remove-Item -LiteralPath (Get-LeasePath) -Force
        }
        $lab = New-StandInLab $case 'lab-baseline'
        $before = Get-ModsText $lab
        Assert-Throws { Enter-InProcess $case $lab 'baseline' -CaptureBaselines { param($dir) throw 'stand-in baseline unavailable' } } 'stand-in baseline unavailable' 'entry with an unavailable baseline'
        Assert-That ((Get-ModsText $lab) -ceq $before -and -not (Test-Path -LiteralPath (Join-Path $lab 'game\Mods\KingmakerBuffPlanner'))) 'the live Mods changed'
        Assert-That (-not (Test-Path -LiteralPath (Get-LeasePath))) 'the rolled-back entry kept its reservation'
    }

    Test-Case 'F10: an unfinished preparation rollback or a string-valued marker keeps the reservation through resolution' {
        param($case)
        $lab = New-StandInLab $case 'lab'
        [void](Enter-InProcess $case $lab 'prep')
        $evidence = Join-Path $lab 'evidence\prep'; New-Item -ItemType Directory -Path $evidence -Force | Out-Null
        $working = Join-Path $case 'Manual_2_KBP_AUTOMATION_WORKING.zks'
        [IO.File]::WriteAllText($working, 'original')
        $originalSha = Get-KbpSha256 $working
        [IO.File]::WriteAllText($working, 'changed by the preparation')
        Write-KbpJsonAtomic (Join-Path $evidence 'heroism-preparation-transaction.json') ([ordered]@{ schemaVersion = 1; runId = 'prep'; status = 'BackedUp'
            workingPath = $working; backupPath = (Join-Path $evidence 'heroism-working-before.zks'); originalSha256 = $originalSha })
        $restored = Restore-InProcess $lab 'prep'
        Assert-That ($restored.restorationVerified -and (Get-Lease).runId -ceq 'prep') 'released over an unfinished preparation'
        Assert-Throws { Enter-InProcess $case (New-StandInLab $case 'peer') 'peer' } 'reserved by KingmakerBuffPlanner run prep' 'a peer during the unfinished preparation'
        [IO.File]::WriteAllText($working, 'original')
        $baselinePath = Join-Path $lab 'state\transactions\prep\protected-saves-before.json'
        [IO.File]::WriteAllText($baselinePath, '{"runId":"prep","compared":"false"}')
        [void](Restore-InProcess $lab 'prep')
        Assert-That ((Get-Lease).runId -ceq 'prep') 'released on a string-valued completion marker'
        [IO.File]::WriteAllText($baselinePath, '{"runId":"prep","compared":true}')
        [void](Restore-InProcess $lab 'prep')
        Assert-That (-not (Test-Path -LiteralPath (Get-LeasePath))) 'not released once every obligation was settled'
    }

    Test-Case 'F5: the launcher reserves before its first write, reads admission inputs under the lease, carries it through entry and restoration and releases after its last observation' {
        param($case)
        $lab = New-StandInLab $case 'lab'
        $labState = Join-Path $lab 'state'
        $targets = @($labState, (Join-Path $lab 'staging'), (Join-Path $lab 'backups'), (Join-Path $lab 'evidence'), (Join-Path $lab 'game'))
        $inputs = [ordered]@{ savePair = 'pair-1'; loader = 'loader-1'; display = $null }
        $observed = @{}
        $read = { $observed.lease = Get-Lease; $observed.count = [int]$observed.count + 1; $inputs }
        Set-Lease 'KingmakerFlight' 4 'peer-run'
        Assert-Throws { [void](Enter-KbpLaunchReservation -RunId 'L' -StateRoot $labState -Targets $targets -ReadInputs $read -PreflightDigest (Get-KbpAdmissionDigest $inputs)) } 'reserved by KingmakerFlight' 'a launch under a peer'
        Assert-That ([int]$observed.count -eq 0 -and @(Get-ChildItem -LiteralPath $labState -Force).Count -eq 0) 'a refused launch read inputs or wrote transaction work'
        Remove-Item -LiteralPath (Get-LeasePath) -Force
        Assert-Throws { [void](Enter-KbpLaunchReservation -RunId 'L' -StateRoot $labState -Targets $targets -ReadInputs $read -PreflightDigest 'stale') } 'admission inputs changed' 'a launch whose inputs changed'
        Assert-That (-not (Test-Path -LiteralPath (Get-LeasePath)) -and @(Get-ChildItem -LiteralPath $labState -Force).Count -eq 0) 'a refused launch kept its reservation or wrote work'
        $admission = Enter-KbpLaunchReservation -RunId 'L' -StateRoot $labState -Targets $targets -ReadInputs $read -PreflightDigest (Get-KbpAdmissionDigest $inputs)
        Assert-That ($observed.lease.token -ceq $admission.Lease.token -and [int]$observed.lease.holderProcessId -eq $PID) 'admission inputs were read outside the lease'
        [void](Enter-InProcess $case $lab 'L' -ReservationToken $admission.Lease.token)
        $restored = Restore-InProcess $lab 'L' -RetainReservation
        Assert-That ($restored.restorationVerified -and (Get-Lease).token -ceq $admission.Lease.token) 'the launcher lost its reservation at restoration'
        $peer = New-StandInLab $case 'peer'
        $early = Start-Contender $case 'Enter' 'peer-early' $peer 'peer-early'
        Assert-That ((Wait-Signal $case 'peer-early' @('active', 'refused')).What -ceq 'refused') 'a peer entered before the launcher''s final observation'
        Assert-That (Complete-KbpLaunchReservation -RunId 'L' -StateRoot $labState -Token $admission.Lease.token -Targets $targets) 'the launcher did not release'
        $late = Start-Contender $case 'Enter' 'peer-late' $peer 'peer-late'
        Assert-That ((Wait-Signal $case 'peer-late' @('active', 'refused')).What -ceq 'active' -and $late.WaitForExit(30000)) 'the peer was not admitted after the release'
        [void](Restore-InProcess $peer 'peer-late')
        # The launcher's own order (source): WhatIf return, then reservation, then the first write, entry with the lease and the
        # baseline capture, restoration retaining it, and release after the completion record's last observation.
        $text = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Invoke-KingmakerRuntimeTest.ps1'))
        $at = @{
            whatIf = $text.IndexOf("Write-Host 'Runtime WhatIf preflight PASS")
            reserve = $text.IndexOf('$launchAdmission = Enter-KbpLaunchReservation')
            evidence = $text.IndexOf('New-Item -ItemType Directory -Path $evidence | Out-Null')
            deploy = $text.IndexOf('-ReservationToken $launchToken')
            restore = $text.IndexOf('-SkipProtectedSaveComparison -RetainReservation')
            record = $text.IndexOf('$completionExited = @(Get-Process -Name Kingmaker')
            release = $text.IndexOf('$launchReleased = Complete-KbpLaunchReservation') }
        Assert-That ($at.whatIf -gt 0 -and $at.reserve -gt $at.whatIf -and $at.evidence -gt $at.reserve -and $at.deploy -gt $at.evidence -and
            $at.restore -gt $at.deploy -and $at.record -gt $at.restore -and $at.release -gt $at.record) "launcher order: $($at | ConvertTo-Json -Compress)"
    }

    $gameRunning = @(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue).Count -ne 0
    $fixtureCases = @('F1/F2: fixture -Recover refuses a live bootstrap, makes missing/replaced leases incidents, and resumes across interruptions',
        'F3: a delayed fixture caller cannot act on inputs chosen before admission',
        'F4: fixture verification finishes under both guards; protected-save drift keeps the incident, lock and reservation')
    if ($gameRunning) {
        foreach ($name in $fixtureCases) { $results.Add("NOT RUN ${name}: a Kingmaker process is running (the fixture script checks for a real game)") }
    }
    else {
    Test-Case $fixtureCases[0] {
        param($case)
        $saveRoot = Join-Path $case 'saves'; New-SeedSaves $saveRoot
        $state = Join-Path $case 'fixture-state'; $archive = Join-Path $case 'archive'; $signals = Join-Path $case 'signals'
        $boot = @{ RunId = 'boot'; SaveRoot = $saveRoot; StateRoot = $state; ArchiveRoot = $archive; TestPauseAt = 'FinalComparison'; TestSignalRoot = $signals }
        $bootstrap = Start-ScriptChild $case 'boot' $fixtureScript $boot
        Wait-File (Join-Path $signals 'paused-FinalComparison')
        $savesText = Get-TreeText $saveRoot; $lockHash = Get-KbpSha256 (Join-Path $state 'fixture.lock'); $leaseHash = Get-LeaseHash
        $recover = @{ RunId = 'boot'; SaveRoot = $saveRoot; StateRoot = $state }
        [void](Start-ScriptChild $case 'recover-live' $fixtureScript $recover @('Recover'))
        $outcome = Wait-Signal $case 'recover-live' @('done', 'refused')
        Assert-That ($outcome.What -ceq 'refused' -and $outcome.Text -like '*still held by its live process*') "recovery over a live bootstrap: $($outcome.Text)"
        Assert-That ((Get-TreeText $saveRoot) -ceq $savesText -and (Get-KbpSha256 (Join-Path $state 'fixture.lock')) -ceq $lockHash -and (Get-LeaseHash) -ceq $leaseHash) 'the refused recovery changed state'
        # The bootstrap is interrupted (its process ends at the final comparison).
        $bootstrap.Kill(); [void]$bootstrap.WaitForExit(10000)
        $original = [IO.File]::ReadAllBytes((Get-LeasePath))
        Set-Lease 'KingmakerWakingRune' 4 'wr'
        $replacement = Get-LeaseHash; $savesText = Get-TreeText $saveRoot
        [void](Start-ScriptChild $case 'recover-replaced' $fixtureScript $recover @('Recover'))
        $outcome = Wait-Signal $case 'recover-replaced' @('done', 'refused')
        Assert-That ($outcome.What -ceq 'refused' -and $outcome.Text -like '*lost its Kingmaker native-test reservation*') "recovery over a replaced lease: $($outcome.Text)"
        Assert-That ((Get-LeaseHash) -ceq $replacement -and (Get-TreeText $saveRoot) -ceq $savesText -and (Test-Path -LiteralPath (Join-Path $state 'boot\reservation-incident.json'))) 'replaced lease: state changed or no incident'
        Remove-Item -LiteralPath (Get-LeasePath) -Force
        [void](Start-ScriptChild $case 'recover-missing' $fixtureScript $recover @('Recover'))
        $outcome = Wait-Signal $case 'recover-missing' @('done', 'refused')
        Assert-That ($outcome.What -ceq 'refused' -and $outcome.Text -like '*now: no record*' -and (Get-TreeText $saveRoot) -ceq $savesText) "recovery over a missing lease: $($outcome.Text)"
        Assert-That ((Get-Content -LiteralPath (Join-Path $state 'boot\reservation-incident.json') -Raw) -like '*KingmakerWakingRune*') 'the first incident was replaced'
        [IO.File]::WriteAllBytes((Get-LeasePath), $original)   # the owner puts the run's own record back
        $recoverAdopted = $recover.Clone(); $recoverAdopted.FailAfterRecoveryStep = 'Adopted'
        [void](Start-ScriptChild $case 'recover-adopted' $fixtureScript $recoverAdopted @('Recover'))
        Assert-That ((Wait-Signal $case 'recover-adopted' @('done', 'refused')).Text -like '*after the recovery took over*') 'no interruption after adoption'
        $recoverLock = $recover.Clone(); $recoverLock.FailAfterRecoveryStep = 'LockRemoved'
        [void](Start-ScriptChild $case 'recover-lock' $fixtureScript $recoverLock @('Recover'))
        Assert-That ((Wait-Signal $case 'recover-lock' @('done', 'refused')).Text -like '*after the recovery removed the fixture lock*') 'no interruption after lock removal'
        Assert-That (-not (Test-Path -LiteralPath (Join-Path $state 'fixture.lock')) -and (Test-Path -LiteralPath (Get-LeasePath))) 'the reservation went before the lock'
        [void](Start-ScriptChild $case 'recover-final' $fixtureScript $recover @('Recover'))
        Assert-That ((Wait-Signal $case 'recover-final' @('done', 'refused')).What -ceq 'done') 'the final release was not resumed'
        Assert-That (-not (Test-Path -LiteralPath (Get-LeasePath)) -and @(Get-ChildItem -LiteralPath $saveRoot -Filter '*KBP_AUTOMATION_BASELINE*').Count -eq 0 -and
            (Test-Path -LiteralPath (Join-Path $saveRoot 'Manual_400_PlayerCampaign.zks')) -and
            $null -ne (Read-KbpJson (Join-Path $state 'boot\transaction.json')).PSObject.Properties['reservationReleasedAtUtc']) 'recovery did not finish cleanly'
    }

    Test-Case $fixtureCases[1] {
        param($case)
        $saveRoot = Join-Path $case 'saves'; New-SeedSaves $saveRoot
        $state = Join-Path $case 'fixture-state'; $signals = Join-Path $case 'signals'
        $delayed = Start-ScriptChild $case 'delayed' $fixtureScript @{ RunId = 'delayed'; SaveRoot = $saveRoot; StateRoot = $state; ArchiveRoot = (Join-Path $case 'archive-b')
            TestPauseAt = 'BeforeAdmission'; TestSignalRoot = $signals }
        Wait-File (Join-Path $signals 'paused-BeforeAdmission')
        & $fixtureScript -RunId 'first' -SaveRoot $saveRoot -StateRoot $state -ArchiveRoot (Join-Path $case 'archive-a') -Confirm:$false | Out-Null
        $pair = Get-TreeText $saveRoot
        [IO.File]::WriteAllText((Join-Path $signals 'resume-BeforeAdmission'), 'go')
        $outcome = Wait-Signal $case 'delayed' @('done', 'refused')
        Assert-That ($outcome.What -ceq 'refused' -and ($outcome.Text -like '*Existing KBP_AUTOMATION artifact*' -or $outcome.Text -like '*inputs changed*')) "the delayed bootstrap went on: $($outcome.Text)"
        Assert-That ((Get-TreeText $saveRoot) -ceq $pair -and -not (Test-Path -LiteralPath (Get-LeasePath)) -and -not (Test-Path -LiteralPath (Join-Path $state 'fixture.lock'))) 'the first run''s pair changed or ownership leaked'
        # A teardown refuses a pair whose identity changed before its admission.
        Remove-Item -LiteralPath (Join-Path $signals 'resume-BeforeAdmission'), (Join-Path $signals 'paused-BeforeAdmission') -Force
        [void](Start-ScriptChild $case 'teardown' $fixtureScript @{ RunId = 'td'; SaveRoot = $saveRoot; StateRoot = $state; ArchiveRoot = (Join-Path $case 'archive-a')
            TestPauseAt = 'BeforeAdmission'; TestSignalRoot = $signals } @('Teardown'))
        Wait-File (Join-Path $signals 'paused-BeforeAdmission')
        $baselinePath = @(Get-ChildItem -LiteralPath $saveRoot -Filter '*KBP_AUTOMATION_BASELINE*')[0].FullName
        $zip = [IO.Compression.ZipFile]::Open($baselinePath, [IO.Compression.ZipArchiveMode]::Update)
        try { $writer = [IO.StreamWriter]::new($zip.CreateEntry('drift.txt').Open()); try { $writer.Write('changed before admission') } finally { $writer.Dispose() } } finally { $zip.Dispose() }
        $changedPair = Get-TreeText $saveRoot
        [IO.File]::WriteAllText((Join-Path $signals 'resume-BeforeAdmission'), 'go')
        $outcome = Wait-Signal $case 'teardown' @('done', 'refused')
        Assert-That ($outcome.What -ceq 'refused' -and $outcome.Text -like '*Sealed baseline hash drifted*') "teardown acted on a changed pair: $($outcome.Text)"
        Assert-That ((Get-TreeText $saveRoot) -ceq $changedPair -and -not (Test-Path -LiteralPath (Get-LeasePath)) -and -not (Test-Path -LiteralPath (Join-Path $state 'fixture.lock'))) 'the refused teardown deleted or kept ownership'
    }

    Test-Case $fixtureCases[2] {
        param($case)
        $saveRoot = Join-Path $case 'saves'; New-SeedSaves $saveRoot
        $state = Join-Path $case 'fixture-state'; $signals = Join-Path $case 'signals'
        & $fixtureScript -RunId 'pair' -SaveRoot $saveRoot -StateRoot $state -ArchiveRoot (Join-Path $case 'archive') -Confirm:$false | Out-Null
        [void](Start-ScriptChild $case 'teardown' $fixtureScript @{ RunId = 'td'; SaveRoot = $saveRoot; StateRoot = $state; ArchiveRoot = (Join-Path $case 'archive')
            TestPauseAt = 'FinalComparison'; TestSignalRoot = $signals } @('Teardown'))
        Wait-File (Join-Path $signals 'paused-FinalComparison')
        Assert-Throws { [void](Enter-KbpNativeReservation -RunId 'contender' -Purpose 'test' -StateRoot (Join-Path $case 'other-state')) } 'reserved by KingmakerBuffPlanner run td' 'a contender during the final comparison'
        [IO.File]::AppendAllText((Join-Path $saveRoot 'Manual_400_PlayerCampaign.zks'), 'owner change')
        [IO.File]::WriteAllText((Join-Path $signals 'resume-FinalComparison'), 'go')
        $outcome = Wait-Signal $case 'teardown' @('done', 'refused')
        Assert-That ($outcome.What -ceq 'refused' -and $outcome.Text -like '*touched a protected save*') "teardown passed over drift: $($outcome.Text)"
        Assert-That ((Test-Path -LiteralPath (Join-Path $state 'teardown-incident-td.json')) -and (Test-Path -LiteralPath (Join-Path $state 'fixture.lock')) -and (Get-Lease).runId -ceq 'td') 'the teardown drift did not keep the incident, lock and reservation'
        # Bootstrap drift.
        $saveRoot2 = Join-Path $case 'saves2'; New-SeedSaves $saveRoot2
        $state2 = Join-Path $case 'fixture-state2'
        $env:KBP_TEST_RESERVATION_ROOT = Join-Path $case 'reservation2'
        Remove-Item -LiteralPath (Join-Path $signals 'paused-FinalComparison'), (Join-Path $signals 'resume-FinalComparison') -Force
        [void](Start-ScriptChild $case 'boot' $fixtureScript @{ RunId = 'boot'; SaveRoot = $saveRoot2; StateRoot = $state2; ArchiveRoot = (Join-Path $case 'archive2')
            TestPauseAt = 'FinalComparison'; TestSignalRoot = $signals })
        Wait-File (Join-Path $signals 'paused-FinalComparison')
        [IO.File]::AppendAllText((Join-Path $saveRoot2 'Manual_400_PlayerCampaign.zks'), 'owner change')
        [IO.File]::WriteAllText((Join-Path $signals 'resume-FinalComparison'), 'go')
        $outcome = Wait-Signal $case 'boot' @('done', 'refused')
        $journal = Read-KbpJson (Join-Path $state2 'boot\transaction.json')
        Assert-That ($outcome.What -ceq 'refused' -and @($journal.protectedSaveDrift) -contains 'Manual_400_PlayerCampaign.zks' -and [string]$journal.status -ceq 'RolledBack') "bootstrap drift: $($outcome.Text)"
        Assert-That (@(Get-ChildItem -LiteralPath $saveRoot2 -Filter '*KBP_AUTOMATION_BASELINE*').Count -eq 0 -and (Test-Path -LiteralPath (Join-Path $state2 'fixture.lock')) -and
            (Get-Lease).runId -ceq 'boot' -and (Test-Path -LiteralPath (Join-Path $state2 'boot\protected-save-incident.json'))) 'the bootstrap drift did not keep the incident, lock and reservation after cleanup'
    }
    }

    Test-Case 'refused and failed entries release their own reservation, so another contender is admitted' {
        param($case)
        $lab = New-StandInLab $case 'lab'
        New-KbpOwnedLock (Join-Path $lab 'state\deployment.lock') 'stale' 'token'
        Assert-Throws { Enter-InProcess $case $lab 'refused' } 'Unresolved runtime deployment lock' 'entry over an unresolved lock'
        Assert-That (-not (Test-Path -LiteralPath (Get-LeasePath))) 'the refused entry kept the reservation'
        $failing = New-StandInLab $case 'failing'
        Assert-Throws { Enter-KbpRuntimeTransaction -PackagePath (Get-StandInPackage $case) -KingmakerInstallDir (Join-Path $failing 'game') `
            -StateRoot (Join-Path $failing 'state') -StagingRoot (Join-Path $failing 'staging') -BackupRoot (Join-Path $failing 'backups') `
            -RunId 'failing' -FixtureMode -KnownKingmakerProcessIds @() -ExpectedPackageSha256 ('0' * 64) } 'changed after admission' 'entry with a refused package copy'
        Assert-That (-not (Test-Path -LiteralPath (Get-LeasePath))) 'the failed entry kept the reservation'
        $peer = New-StandInLab $case 'peer'
        $process = Start-Contender $case 'Enter' 'after-refusal' $peer 'after-refusal'
        Assert-That ((Wait-Signal $case 'after-refusal' @('active', 'refused')).What -ceq 'active' -and $process.WaitForExit(30000)) 'no contender admitted after the releases'
        [void](Restore-InProcess $peer 'after-refusal')
    }

    Test-Case 'F13: the gate verdict is incomplete when a required case is not run, and failing on any failure' {
        param($case)
        Assert-That ((Get-GateVerdict @('PASS a', 'PASS b')) -ceq 'pass') 'complete run'
        Assert-That ((Get-GateVerdict @('PASS a', 'NOT RUN b: a Kingmaker process is running')) -ceq 'incomplete') 'a skipped required case passed'
        Assert-That ((Get-GateVerdict @('PASS a', 'NOT RUN b: x', 'FAIL c: y')) -ceq 'fail') 'a failure was not failing'
        Assert-That ((Get-GateVerdict @()) -ceq 'incomplete') 'an empty run passed'
    }

    # --- F15/F11: the directly affected existing checks, each in its own process with its own private reservation
    #     boundary, with commit-bound captured logs.
    New-Item -ItemType Directory -Path $LogRoot -Force | Out-Null
    $package = Join-Path $repo ('artifacts\local-runtime\' + (Get-KbpVersion) + '\KingmakerBuffPlanner-' + (Get-KbpVersion) + '-local-runtime.zip')
    foreach ($check in @(@{ Name = 'Test-DisplayModsRecovery'; Pass = 'Display Mods recovery: PASS=16 FAIL=0'; Needs = $null },
            @{ Name = 'Test-RuntimeHarness'; Pass = 'Runtime harness filesystem tests: PASS='; Needs = $package })) {
        if ($check.Needs -and -not (Test-Path -LiteralPath $check.Needs -PathType Leaf)) {
            $results.Add("NOT RUN affected existing check $($check.Name): its local package is not built ($($check.Needs))")
            continue
        }
        $log = Join-Path $LogRoot ("$($check.Name)-$($commit.Substring(0, 12)).log")
        $header = "commit=$commit dirty=$dirty check=$($check.Name) startedUtc=$([DateTime]::UtcNow.ToString('o'))"
        $output = @(& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "$($check.Name).ps1") 2>&1 | ForEach-Object { "$_" })
        $exit = $LASTEXITCODE
        [IO.File]::WriteAllLines($log, [string[]](@($header) + $output + @("exit=$exit finishedUtc=$([DateTime]::UtcNow.ToString('o'))")))
        $summary = @($output | Where-Object { $_ -like "$($check.Pass)*" }) | Select-Object -Last 1
        if ($exit -eq 0 -and $summary) { $results.Add("PASS affected existing check $($check.Name): $summary (log $log)") }
        else { $results.Add("FAIL affected existing check $($check.Name): exit $exit (log $log)") }
    }
}
finally {
    $env:KBP_TEST_RESERVATION_ROOT = $priorReservationRoot
    foreach ($process in $processes) { if (-not $process.HasExited) { $process.Kill() } }
    if (Test-Path -LiteralPath $root) {
        $resolved = [IO.Path]::GetFullPath($root)
        [void](Assert-KbpPathWithin -Path $resolved -Root (Join-Path $repo 'artifacts\runtime-harness-tests'))
        Remove-Item -LiteralPath $resolved -Recurse -Force -ErrorAction SilentlyContinue
    }
}
$productionAfter = Get-ProductionLeaseObservation
if ($productionAfter -ceq $productionBefore) { $results.Add("PASS the machine-wide reservation record was not touched ($productionBefore before and after)") }
else { $results.Add("FAIL the machine-wide reservation record changed during the gate: $productionBefore -> $productionAfter") }

$results | ForEach-Object { Write-Host $_ }
$verdict = Get-GateVerdict $results.ToArray()
$counts = @{ pass = @($results | Where-Object { $_ -like 'PASS*' }).Count; fail = @($results | Where-Object { $_ -like 'FAIL*' }).Count
    notrun = @($results | Where-Object { $_ -like 'NOT RUN*' }).Count }
Write-Host "RESULT verdict=$verdict commit=$commit dirty=$dirty total=$($results.Count) passed=$($counts.pass) failed=$($counts.fail) notrun=$($counts.notrun)"
if ($verdict -ceq 'fail') { exit 1 }
if ($verdict -ceq 'incomplete') { exit 2 }
exit 0
