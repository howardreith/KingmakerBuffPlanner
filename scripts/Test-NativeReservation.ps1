[CmdletBinding()]
param(
    # Folder for the captured logs of the affected existing checks
    # (default artifacts\review-logs).
    [string]$LogRoot
)

# Focused gate for the Kingmaker native-test reservation (protocol 1,
# docs/NATIVE-RESERVATION.md) as this lab implements it: acquire before any
# mutable read, hold for the whole tool, release last only after the tool's
# own verification; refuse-before-change releases; anything unresolved stays
# reserved for the owner. Every case runs in a disposable boundary inside
# artifacts\runtime-harness-tests with a private reservation named by
# KBP_TEST_RESERVATION_ROOT (restored in finally); the machine-wide record is
# only observed, before and after. Separate Windows PowerShell processes run
# the actual entry, restoration, Restore-Local and fixture scripts. No game,
# live Mods folder, owner save, lab root or other lab is touched. It then runs
# the directly affected existing checks (display recovery, runtime harness,
# and the loader-baseline transaction cases) with captured, commit-bound
# output. The gate passes only when every required case ran and passed
# (exit 0); a required case that could not run makes it incomplete (exit 2);
# any failure exits 1.
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

function Get-ProductionLeaseObservation {
    $path = $script:KbpReservationRecordPath
    if (Test-Path -LiteralPath $path -PathType Leaf) { return 'present:' + (Get-KbpSha256 $path) }
    return 'absent'
}

function Test-Case([string]$Name, [scriptblock]$Body) {
    $script:caseIndex++
    $case = Join-Path $root ('case' + $script:caseIndex)
    New-Item -ItemType Directory -Path (Join-Path $case 'signals') -Force | Out-Null
    $env:KBP_TEST_RESERVATION_ROOT = Join-Path $case 'reservation'
    try {
        & $Body $case
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

function Get-GateVerdict([string[]]$Lines) {
    if (@($Lines | Where-Object { $_ -like 'FAIL*' }).Count -gt 0) { return 'fail' }
    if (@($Lines | Where-Object { $_ -like 'NOT RUN*' }).Count -gt 0) { return 'incomplete' }
    if (@($Lines | Where-Object { $_ -like 'PASS*' }).Count -eq 0) { return 'incomplete' }
    return 'pass'
}

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

function Get-ModsText([string]$Lab) {
    $mods = Join-Path $Lab 'game\Mods'
    if (-not (Test-Path -LiteralPath $mods)) { return 'absent' }
    return (@(Get-KbpDirectoryManifest $mods) | ConvertTo-Json -Depth 5 -Compress)
}

function Enter-InProcess([string]$Case, [string]$Lab, [string]$RunId, [string]$ReservationToken) {
    $arguments = @{ PackagePath = (Get-StandInPackage $Case); KingmakerInstallDir = (Join-Path $Lab 'game')
        StateRoot = (Join-Path $Lab 'state'); StagingRoot = (Join-Path $Lab 'staging'); BackupRoot = (Join-Path $Lab 'backups')
        RunId = $RunId; FixtureMode = $true; KnownKingmakerProcessIds = @(); MoveAttempts = 1; MoveDelayMilliseconds = 0 }
    if ($ReservationToken) { $arguments.ReservationToken = $ReservationToken }
    return Enter-KbpRuntimeTransaction @arguments
}

function Restore-InProcess([string]$Lab, [string]$RunId, [string]$BorrowedToken) {
    $arguments = @{ RunId = $RunId; StateRoot = (Join-Path $Lab 'state'); FixtureMode = $true; KnownKingmakerProcessIds = @() }
    if ($BorrowedToken) { $arguments.BorrowedToken = $BorrowedToken }
    return Restore-KbpRuntimeTransaction @arguments
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

function Wait-Status([string]$Lab, [string]$RunId, [string]$Status, [int]$Seconds = 60) {
    $statePath = Join-Path $Lab "state\transactions\$RunId\transaction.json"
    $until = (Get-Date).AddSeconds($Seconds)
    while ((Get-Date) -lt $until) {
        if (Test-Path -LiteralPath $statePath) { try { if ([string](Read-KbpJson $statePath).status -ceq $Status) { return } } catch { } }
        Start-Sleep -Milliseconds 100
    }
    throw "run $RunId never reached status $Status"
}

function Hold-File([string]$Path) {
    $handle = [IO.File]::Open($Path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
    $handles.Add($handle)
    return $handle
}

function Set-Lease([string]$Holder, [int]$HolderProcessId, [string]$RunId, [string]$StateRoot = '', [string]$Token = '') {
    New-Item -ItemType Directory -Path $env:KBP_TEST_RESERVATION_ROOT -Force | Out-Null
    if (-not $Token) { $Token = [Guid]::NewGuid().ToString('N') }
    $record = [ordered]@{ protocol = 'kingmaker-native-test-reservation/1'; lab = $Holder; runId = $RunId; purpose = 'stand-in'
        token = $Token; holderProcessId = $HolderProcessId; holderStartUtc = '2020-01-01T00:00:00.0000000Z'; host = $env:COMPUTERNAME
        createdUtc = [DateTime]::UtcNow.ToString('o') }
    if ($StateRoot) { $record.stateRoot = [IO.Path]::GetFullPath($StateRoot).TrimEnd('\') }
    [IO.File]::WriteAllText((Get-LeasePath), ($record | ConvertTo-Json))
}

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
    if (-not (Test-Path -LiteralPath $Path)) { return 'absent' }
    return (@(Get-ChildItem -LiteralPath $Path -File -Recurse | Sort-Object FullName | ForEach-Object { $_.FullName + '=' + (Get-KbpSha256 $_.FullName) }) -join "`n")
}

$productionBefore = Get-ProductionLeaseObservation
try {
    New-Item -ItemType Directory -Path $root -Force | Out-Null

    Test-Case 'convention: production values exact; private mode bounded to harness roots; mixed shared/private targets refused (R1-F11)' {
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
        Assert-That ((Get-KbpReservationConvention -Targets @((Join-Path $case 'lab'))).RecordPath -ceq (Get-LeasePath)) 'private record path'
        Assert-Throws { Get-KbpReservationConvention -Targets @((Join-Path $case 'lab'), $script:KbpRuntimeStateRoot) } 'refused for a shared target' 'mixed targets'
        $outside = Start-Process -FilePath 'powershell.exe' -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', ('"' + $contender + '"'),
            '-Action', 'Enter', '-Name', 'outside', '-RunId', 'outside', '-Signals', ('"' + (Join-Path $case 'signals') + '"'), '-LabRoot', ('"' + $env:TEMP + '"'),
            '-PackagePath', ('"' + (Get-StandInPackage $case) + '"')) -WindowStyle Hidden -PassThru
        $processes.Add($outside)
        Assert-That ($outside.WaitForExit(60000) -and $outside.ExitCode -ne 0 -and -not (Test-Path -LiteralPath (Join-Path $case 'signals\outside.active'))) 'an unbounded contender ran'
    }

    Test-Case 'a lab alone (no peer, no reservation folder yet) holds the reservation across processes and releases after verified restoration' {
        param($case)
        $lab = New-StandInLab $case 'alone'
        Assert-That (-not (Test-Path -LiteralPath $env:KBP_TEST_RESERVATION_ROOT)) 'the reservation folder pre-exists'
        $statePath = Enter-InProcess $case $lab 'alone-run'
        $lease = Get-Lease
        Assert-That ($lease.lab -ceq 'KingmakerBuffPlanner' -and $lease.runId -ceq 'alone-run' -and $lease.token -ceq (Read-KbpJson $statePath).reservationToken) 'lease not journaled'
        $restored = Restore-InProcess $lab 'alone-run'
        Assert-That ($restored.restorationVerified -and -not (Test-Path -LiteralPath (Get-LeasePath))) 'not released after verified restoration'
    }

    Test-Case 'two labs exclude each other through the reservation alone, in either arrival order (prepared, active after the entry process exited, restoring)' {
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
            Assert-That ((Wait-Signal $case "$loser-after" @('active', 'refused')).What -ceq 'active' -and $after.WaitForExit(30000)) 'loser not admitted after release'
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

    Test-Case 'parser (R1-F12): incomplete own records, non-UTF-8 and duplicate keys refuse byte-identical; a complete peer with a long spaced token is identified and kept' {
        param($case)
        $lab = New-StandInLab $case 'lab'
        $path = Get-LeasePath
        New-Item -ItemType Directory -Path $env:KBP_TEST_RESERVATION_ROOT -Force | Out-Null
        $stateRoot = [IO.Path]::GetFullPath((Join-Path $lab 'state')).TrimEnd('\').Replace('\', '\\')
        $own = '{"protocol":"kingmaker-native-test-reservation/1","lab":"KingmakerBuffPlanner","runId":"r","token":"0123456789abcdef0123456789abcdef","holderProcessId":4,"holderStartUtc":"2020-01-01T00:00:00.0000000Z","stateRoot":"' + $stateRoot + '"}'
        $bad = @(
            $own.Replace(',"token":"0123456789abcdef0123456789abcdef"', ''),
            $own.Replace(',"holderProcessId":4', ''),
            $own.Replace('"holderProcessId":4', '"holderProcessId":0'),
            $own.Replace(',"holderStartUtc":"2020-01-01T00:00:00.0000000Z"', ''),
            $own.Replace(',"stateRoot":"' + $stateRoot + '"', ''),
            $own.Replace('0123456789abcdef0123456789abcdef', 'NOT-HEX'),
            $own.Replace('kingmaker-native-test-reservation/1', 'Kingmaker-Native-Test-Reservation/1'),
            $own.Replace('"lab":"KingmakerBuffPlanner"', '"lab":"KingmakerFlight","lab":"KingmakerBuffPlanner"'),
            $own.Replace('"token":"0123456789abcdef0123456789abcdef"', '"token":"ffffffffffffffffffffffffffffffff","token":"0123456789abcdef0123456789abcdef"'))
        foreach ($text in $bad) {
            [IO.File]::WriteAllText($path, $text)
            $hash = Get-LeaseHash
            Assert-Throws { Enter-InProcess $case $lab 'x' } 'malformed or of an unknown protocol' "accepted: $text"
            Assert-That ((Get-LeaseHash) -ceq $hash) 'a refused record changed'
        }
        foreach ($bytes in @([Text.Encoding]::Unicode.GetBytes($own), [byte[]](0x7B, 0xFF, 0xFE, 0x7D))) {
            [IO.File]::WriteAllBytes($path, $bytes)
            $hash = Get-LeaseHash
            Assert-Throws { Enter-InProcess $case $lab 'x' } 'malformed or of an unknown protocol' 'a non-UTF-8 record was accepted'
            Assert-That ((Get-LeaseHash) -ceq $hash) 'a non-UTF-8 record changed'
        }
        $peerToken = ('a1 ' * 100).Trim()
        [IO.File]::WriteAllText($path, '{"protocol":"kingmaker-native-test-reservation/1","lab":"Some Peer Lab","runId":"r","token":"' + $peerToken + '","holderProcessId":4,"holderStartUtc":"whenever"}')
        $hash = Get-LeaseHash
        Assert-Throws { Enter-InProcess $case $lab 'x' } 'reserved by Some Peer Lab run r' 'a valid peer record was not identified'
        Assert-That ((Get-LeaseHash) -ceq $hash) 'the peer record changed'
    }

    Test-Case 'binding (R2-F9): a forwarded token admits only its own run and state root; a restoration continues only the journaled reservation; foreign, missing or unreadable leases refuse and leave everything' {
        param($case)
        $lab = New-StandInLab $case 'lab'
        $other = New-StandInLab $case 'other'
        $lease = Enter-KbpNativeReservation -RunId 'run-a' -Purpose 'test' -StateRoot (Join-Path $lab 'state')
        Assert-Throws { Enter-InProcess $case $lab 'run-b' $lease.token } 'does not hold its Kingmaker native-test reservation' 'a token borrowed for another run'
        Assert-Throws { Enter-InProcess $case $other 'run-a' $lease.token } 'does not hold its Kingmaker native-test reservation' 'a token borrowed for another state root'
        Assert-That (@(Get-ChildItem -LiteralPath (Join-Path $lab 'state') -Force).Count -eq 0 -and @(Get-ChildItem -LiteralPath (Join-Path $other 'state') -Force).Count -eq 0) 'a refused borrower created work'
        [void](Enter-InProcess $case $lab 'run-a' $lease.token)
        Assert-That ((Get-Lease).token -ceq $lease.token) 'the borrowed reservation changed'
        Assert-Throws { Restore-InProcess $lab 'run-a' ([Guid]::NewGuid().ToString('N')) } 'does not name the caller' 'a restoration with another borrowed token'
        $live = Get-ModsText $lab
        foreach ($replacement in @('foreign', 'missing', 'unreadable')) {
            $saved = [IO.File]::ReadAllBytes((Get-LeasePath))
            switch ($replacement) {
                'foreign' { Set-Lease 'KingmakerWakingRune' 4 'wr' }
                'missing' { Remove-Item -LiteralPath (Get-LeasePath) -Force }
                'unreadable' { [IO.File]::WriteAllText((Get-LeasePath), '{ not json') }
            }
            $hash = if (Test-Path -LiteralPath (Get-LeasePath)) { Get-LeaseHash } else { 'absent' }
            Assert-Throws { Restore-InProcess $lab 'run-a' } 'does not hold its Kingmaker native-test reservation' "restoration under a $replacement lease"
            Assert-That ((Get-ModsText $lab) -ceq $live -and $(if (Test-Path -LiteralPath (Get-LeasePath)) { Get-LeaseHash } else { 'absent' }) -ceq $hash) "the $replacement lease case changed something"
            [IO.File]::WriteAllBytes((Get-LeasePath), $saved)
        }
        $restored = Restore-InProcess $lab 'run-a' $lease.token
        Assert-That ($restored.restorationVerified -and (Get-Lease).token -ceq $lease.token) 'a borrowing restoration released the caller''s reservation'
        Exit-KbpNativeReservation -RunId 'run-a' -Token $lease.token -StateRoot (Join-Path $lab 'state')
    }

    Test-Case 'loss during a blocked move (R1-F7): the next attempt stops before moving; the replacement stays; the reservation is not released' {
        param($case)
        $lab = New-StandInLab $case 'lab'
        $entry = Start-Contender $case 'Enter' 'e' $lab 'moving'
        Assert-That ((Wait-Signal $case 'e' @('active', 'refused')).What -ceq 'active' -and $entry.WaitForExit(30000)) 'entry failed'
        $hold = Hold-File (Join-Path $lab 'game\Mods\KingmakerBuffPlanner\Info.json')
        [void](Start-Contender $case 'Restore' 'r' $lab 'moving')
        [void](Wait-Signal $case 'r' @('restoring'))
        Start-Sleep -Milliseconds 2500
        $live = Get-ModsText $lab
        Set-Lease 'KingmakerFlight' 4 'flight-took-over'
        $replacement = Get-LeaseHash
        $hold.Dispose()
        $outcome = Wait-Signal $case 'r' @('restored', 'refused')
        Assert-That ($outcome.What -ceq 'refused' -and $outcome.Text -like '*does not hold its Kingmaker native-test reservation*') "restoration went on: $($outcome.Text)"
        Assert-That ((Get-LeaseHash) -ceq $replacement -and (Get-ModsText $lab) -ceq $live -and (Test-Path -LiteralPath (Join-Path $lab 'backups\moving\Mods.original'))) 'a move happened after the loss'
    }

    Test-Case 'backup verification (R1-F9): a missing or mismatched original refuses before any move, in the active and the resumed layout; the failed run keeps its reservation' {
        param($case)
        foreach ($layout in @('active-missing', 'active-mismatch', 'resumed-mismatch')) {
            $env:KBP_TEST_RESERVATION_ROOT = Join-Path $case "reservation-$layout"
            $lab = New-StandInLab $case "lab-$layout"
            $statePath = Enter-InProcess $case $lab 'run'
            $backup = Join-Path $lab 'backups\run\Mods.original'
            if ($layout -ceq 'resumed-mismatch') {
                [IO.Directory]::Move((Join-Path $lab 'game\Mods'), (Join-Path $lab 'backups\run\Mods.staged'))
                $state = Read-KbpJson $statePath; $state.status = 'RestorationFailed'; Write-KbpJsonAtomic $statePath $state
            }
            if ($layout -ceq 'active-missing') { [IO.Directory]::Move($backup, (Join-Path $lab 'moved-away')) }
            else { [IO.File]::AppendAllText((Join-Path $backup 'Existing\Info.json'), 'altered') }
            $live = Get-ModsText $lab
            $backupText = Get-TreeText $backup
            Assert-Throws { Restore-InProcess $lab 'run' } $(if ($layout -ceq 'active-missing') { 'backup is missing' } else { 'does not match the recorded original manifest' }) "restore ($layout)"
            Assert-That ((Get-ModsText $lab) -ceq $live -and (Get-TreeText $backup) -ceq $backupText) "something moved ($layout)"
            Assert-That ((Get-Lease).runId -ceq 'run') "the failed restoration released ($layout)"
        }
    }

    Test-Case 'refusals before any shared change release; a failure after one keeps the reservation for the owner' {
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
        Assert-That ((Get-Lease).runId -ceq 'after-refusal') 'the active transaction does not hold the reservation'
    }

    Test-Case 'a separate Restore-Local cannot interleave with an entry still running for the same run (per-run mutex)' {
        param($case)
        $lab = New-StandInLab $case 'lab'
        $hold = Hold-File (Join-Path $lab 'game\Mods\Existing\Info.json')
        $entry = Start-Contender $case 'Enter' 'slow' $lab 'slow'
        Wait-Status $lab 'slow' 'Prepared'
        [void](Start-ScriptChild $case 'restore-early' $restoreLocal @{ RunId = 'slow'; StateRoot = (Join-Path $lab 'state'); EvidenceRoot = (Join-Path $lab 'evidence') })
        $outcome = Wait-Signal $case 'restore-early' @('done', 'refused')
        Assert-That ($outcome.What -ceq 'refused' -and $outcome.Text -like '*Another restore is active*') "a restoration interleaved with the entry: $($outcome.Text)"
        $hold.Dispose()
        Assert-That ((Wait-Signal $case 'slow' @('active', 'refused')).What -ceq 'active' -and $entry.WaitForExit(30000)) 'the entry failed'
        [void](Start-ScriptChild $case 'restore-late' $restoreLocal @{ RunId = 'slow'; StateRoot = (Join-Path $lab 'state'); EvidenceRoot = (Join-Path $lab 'evidence') })
        Assert-That ((Wait-Signal $case 'restore-late' @('done', 'refused')).What -ceq 'done' -and -not (Test-Path -LiteralPath (Get-LeasePath))) 'the standalone continuation did not restore and release'
    }

    Test-Case 'public Restore-Local admits before any change: a peer''s lease refuses the ordinary and the Mods-only route; a journal it does not know refuses' {
        param($case)
        $lab = New-StandInLab $case 'lab'
        $labState = Join-Path $lab 'state'; $labEvidence = Join-Path $lab 'evidence'
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
        foreach ($modsOnly in @($false, $true)) {
            $routeArgs = @{ RunId = 'legacy'; StateRoot = $labState; EvidenceRoot = $labEvidence; Confirm = $false }
            if ($modsOnly) { $routeArgs.RestoreModsOnly = $true }
            Assert-Throws { & $restoreLocal @routeArgs } 'reserved by KingmakerWakingRune run wr' "Restore-Local (Mods only: $modsOnly) under a peer's lease"
            Assert-That ((Get-LeaseHash) -ceq $leaseHash -and (Get-TreeText $lab) -ceq $labTree -and -not [bool](Read-KbpJson $baseline).compared) 'a refused recovery changed something'
        }
        $state = Read-KbpJson $statePath; $state.status = 'Deployed'; Write-KbpJsonAtomic $statePath $state
        Remove-Item -LiteralPath (Get-LeasePath) -Force
        $labTree = Get-TreeText $lab
        Assert-Throws { & $restoreLocal -RunId 'legacy' -StateRoot $labState -EvidenceRoot $labEvidence -Confirm:$false } 'identity is invalid' 'Restore-Local over an unknown phase'
        Assert-That ((Get-TreeText $lab) -ceq $labTree -and -not (Test-Path -LiteralPath (Get-LeasePath))) 'an invalid journal admitted something'
    }

    $gameRunning = @(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue).Count -ne 0
    $fixtureCase = 'fixture script: reserves before any read, refuses under a held reservation, releases after verified success, keeps it after a failure that changed the saves; -Recover is refused while it is kept'
    if ($gameRunning) { $results.Add("NOT RUN ${fixtureCase}: a Kingmaker process is running (the fixture script checks for a real game)") }
    else {
    Test-Case $fixtureCase {
        param($case)
        $saveRoot = Join-Path $case 'saves'; New-SeedSaves $saveRoot
        $state = Join-Path $case 'fixture-state'
        Set-Lease 'KingmakerFlight' 4 'flight'
        $savesText = Get-TreeText $saveRoot
        Assert-Throws { & $fixtureScript -RunId 'held' -SaveRoot $saveRoot -StateRoot $state -ArchiveRoot (Join-Path $case 'archive-held') -Confirm:$false | Out-Null } 'reserved by KingmakerFlight run flight' 'bootstrap under a held reservation'
        Assert-That ((Get-TreeText $saveRoot) -ceq $savesText -and -not (Test-Path -LiteralPath $state)) 'the refused bootstrap read into or changed state'
        Remove-Item -LiteralPath (Get-LeasePath) -Force
        & $fixtureScript -RunId 'boot' -SaveRoot $saveRoot -StateRoot $state -ArchiveRoot (Join-Path $case 'archive') -Confirm:$false | Out-Null
        Assert-That (-not (Test-Path -LiteralPath (Get-LeasePath))) 'a verified bootstrap kept its reservation'
        & $fixtureScript -RunId 'td' -Teardown -SaveRoot $saveRoot -StateRoot $state -ArchiveRoot (Join-Path $case 'archive') -Confirm:$false | Out-Null
        Assert-That (-not (Test-Path -LiteralPath (Get-LeasePath)) -and @(Get-ChildItem -LiteralPath $saveRoot -Filter '*KBP_AUTOMATION_BASELINE*').Count -eq 0) 'a verified teardown kept its reservation'
        Assert-Throws { & $fixtureScript -RunId 'fail' -SaveRoot $saveRoot -StateRoot $state -ArchiveRoot (Join-Path $case 'archive-fail') -FailAfterStage 'Published' -Confirm:$false | Out-Null } 'Injected failure after stage Published' 'injected failure after publication'
        Assert-That ((Get-Lease).runId -ceq 'fail') 'a failure after changing the saves released the reservation'
        $leaseHash = Get-LeaseHash
        Assert-Throws { & $fixtureScript -RunId 'fail' -Recover -SaveRoot $saveRoot -StateRoot $state -Confirm:$false | Out-Null } 'reserved by KingmakerBuffPlanner run fail' 'recovery while the reservation is kept'
        Assert-That ((Get-LeaseHash) -ceq $leaseHash) 'the kept reservation changed'
        $text = [IO.File]::ReadAllText($fixtureScript)
        $leaseAt = $text.IndexOf('$fixtureLease = Enter-KbpNativeReservation')
        Assert-That ($leaseAt -gt 0 -and $leaseAt -lt $text.IndexOf('function Assert-KbpFixtureGameClosed') -and $text.IndexOf("if (-not [bool]`$WhatIfPreference) {") -lt $leaseAt -and
            $leaseAt -lt $text.IndexOf("Get-ChildItem -LiteralPath `$SaveRoot")) 'the fixture script does not reserve before its first mutable read'
    }
    }

    Test-Case 'launcher and standalone deployment (R1-F5, R2-F2, R2-F6): reserve before every mutable read and the run-ID check, never under WhatIf; borrow for the run; check before launch and cleanup; release after the final observation and before the completion record' {
        param($case)
        $text = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Invoke-KingmakerRuntimeTest.ps1'))
        $at = [ordered]@{
            guard = $text.IndexOf('if (-not $requestedWhatIf) {')
            reserve = $text.IndexOf('$launchLease = Enter-KbpNativeReservation')
            display = $text.IndexOf('$admissionDisplaySnapshot = Get-KbpSessionDisplaySnapshot')
            loader = $text.IndexOf('$installedLoader = Assert-KbpInstallationPreflight')
            saves = $text.IndexOf('Get-KbpDisposableSavePair -Family $FixtureFamily')
            steamSafety = $text.IndexOf('$steamSafety = Assert-KbpSteamSafety')
            runIdCheck = $text.IndexOf('throw "Runtime run ID is already present and cannot be reused')
            deploy = $text.IndexOf('-ReservationToken $launchLease.token `')
            launchCheck = $text.IndexOf('if (-not (Test-KbpLaunchLease)) { throw $launchLeaseLost }')
            steam = $text.IndexOf('[void](Start-Process -FilePath $SteamPath')
            cleanupCheck = $text.IndexOf('if ($transactionEntered -and -not (Test-KbpLaunchLease)) {')
            restore = $text.IndexOf('-SkipProtectedSaveComparison -ReservationToken $launchLease.token')
            observe = $text.IndexOf('$finalKingmakerExited = @(Get-Process -Name Kingmaker')
            release = $text.IndexOf('Exit-KbpNativeReservation -RunId $runId -Token $launchLease.token')
            record = $text.IndexOf('$completionRecord = New-KbpRunCompletionRecord') }
        $keys = @($at.Keys)
        for ($i = 1; $i -lt $keys.Count; $i++) {
            Assert-That ($at[$keys[$i - 1]] -ge 0 -and $at[$keys[$i]] -gt $at[$keys[$i - 1]]) "launcher order: $($keys[$i - 1]) ($($at[$keys[$i - 1]])) before $($keys[$i]) ($($at[$keys[$i]]))"
        }
        Assert-That ($text.Contains('$completionExited = $finalKingmakerExited')) 'the completion record does not use the final observation made under the reservation'
        $deploy = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Deploy-Local.ps1'))
        $deployReserve = $deploy.IndexOf('$deployLease = Enter-KbpNativeReservation')
        Assert-That ($deployReserve -gt $deploy.IndexOf('if (-not $requestedWhatIf -and [string]::IsNullOrEmpty($ReservationToken)) {') -and
            $deployReserve -lt $deploy.IndexOf("validate-package.ps1") -and $deployReserve -lt $deploy.IndexOf('Assert-KbpInstallationPreflight')) 'standalone deployment reads before reserving'
    }

    Test-Case 'Test-KbpRunSettled requires affirmative, typed evidence' {
        param($case)
        $state = Join-Path $case 'state'
        New-Item -ItemType Directory -Path (Join-Path $state 'transactions\r') -Force | Out-Null
        Assert-That (Test-KbpRunSettled $state 'none') 'a run with no work is unsettled'
        $path = Join-Path $state 'transactions\r\transaction.json'
        foreach ($case2 in @(@{ status = 'Active'; restorationVerified = $false; expect = $false }, @{ status = 'Restored'; restorationVerified = 'true'; expect = $false },
                @{ status = 'Restored'; restorationVerified = $true; expect = $true })) {
            Write-KbpJsonAtomic $path ([ordered]@{ status = $case2.status; restorationVerified = $case2.restorationVerified })
            Assert-That ((Test-KbpRunSettled $state 'r') -eq $case2.expect) "settled for $($case2 | ConvertTo-Json -Compress)"
        }
        New-KbpOwnedLock (Join-Path $state 'deployment.lock') 'r' 't'
        Assert-That (-not (Test-KbpRunSettled $state 'r')) 'a run with its own lock is settled'
    }

    Test-Case 'the gate verdict is incomplete when a required case is not run, and failing on any failure (R1-F13)' {
        param($case)
        Assert-That ((Get-GateVerdict @('PASS a', 'PASS b')) -ceq 'pass') 'complete run'
        Assert-That ((Get-GateVerdict @('PASS a', 'NOT RUN b: a Kingmaker process is running')) -ceq 'incomplete') 'a skipped required case passed'
        Assert-That ((Get-GateVerdict @('PASS a', 'NOT RUN b: x', 'FAIL c: y')) -ceq 'fail') 'a failure was not failing'
        Assert-That ((Get-GateVerdict @()) -ceq 'incomplete') 'an empty run passed'
    }

    # --- the directly affected existing checks, each in its own process and private boundary, with their output in
    #     this log and a commit-bound copy under artifacts\review-logs.
    New-Item -ItemType Directory -Path $LogRoot -Force | Out-Null
    $package = Join-Path $repo ('artifacts\local-runtime\' + (Get-KbpVersion) + '\KingmakerBuffPlanner-' + (Get-KbpVersion) + '-local-runtime.zip')
    foreach ($check in @(@{ Name = 'Test-DisplayModsRecovery'; Pass = 'Display Mods recovery: PASS=16 FAIL=0'; Needs = $null },
            @{ Name = 'Test-RuntimeHarness'; Pass = 'Runtime harness filesystem tests: PASS='; Needs = $package })) {
        if ($check.Needs -and -not (Test-Path -LiteralPath $check.Needs -PathType Leaf)) {
            $results.Add("NOT RUN affected existing check $($check.Name): its local package is not built ($($check.Needs))")
            continue
        }
        $log = Join-Path $LogRoot ("$($check.Name)-$($commit.Substring(0, 12)).log")
        $output = @(& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "$($check.Name).ps1") 2>&1 | ForEach-Object { "$_" })
        $exit = $LASTEXITCODE
        [IO.File]::WriteAllLines($log, [string[]](@("commit=$commit dirty=$dirty check=$($check.Name)") + $output + @("exit=$exit")))
        $summary = @($output | Where-Object { $_ -like "$($check.Pass)*" }) | Select-Object -Last 1
        if ($exit -eq 0 -and $summary) { $results.Add("PASS affected existing check $($check.Name): $summary") }
        else { $results.Add("FAIL affected existing check $($check.Name): exit $exit (log $log)") }
    }
    # Loader-baseline (R1-F15): it tests the committed HEAD through a clone; the transaction cases are this change's
    # affected cases and must all pass. Its other cases (package and installed-loader checks) are reported as they are.
    if ($dirty) { $results.Add('NOT RUN affected loader-baseline transaction cases: the worktree is not clean (the suite tests committed HEAD)') }
    else {
        $report = Join-Path ([IO.Path]::GetTempPath()) ('kbp-loader-report-' + [Guid]::NewGuid().ToString('N') + '.json')
        $loaderLog = [IO.Path]::GetTempFileName()
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Test-LoaderBaseline.ps1') -ReportPath $report *> $loaderLog
        $loaderExit = $LASTEXITCODE
        $cases = @()
        if (Test-Path -LiteralPath $report) { $cases = @((Get-Content -LiteralPath $report -Raw | ConvertFrom-Json).cases); Remove-Item -LiteralPath $report -Force }
        Copy-Item -LiteralPath $loaderLog -Destination (Join-Path $LogRoot ("Test-LoaderBaseline-$($commit.Substring(0, 12)).log")) -Force
        Remove-Item -LiteralPath $loaderLog -Force
        foreach ($loaderCase in $cases) { Write-Host ("LOADER {0} {1} :: {2}" -f $(if ($loaderCase.passed) { 'PASS' } else { 'FAIL' }), $loaderCase.name, $loaderCase.detail) }
        $transactionCases = @($cases | Where-Object { [string]$_.name -like 'transaction-*' })
        $transactionFailed = @($transactionCases | Where-Object { -not $_.passed })
        $otherFailed = @($cases | Where-Object { [string]$_.name -notlike 'transaction-*' -and -not $_.passed })
        if ($transactionCases.Count -ge 6 -and $transactionFailed.Count -eq 0) {
            $results.Add("PASS affected loader-baseline transaction cases: $($transactionCases.Count)/$($transactionCases.Count) (suite exit $loaderExit; other failing cases: $(@($otherFailed | ForEach-Object { $_.name }) -join ', '))")
        }
        else { $results.Add("FAIL affected loader-baseline transaction cases: $($transactionFailed.Count) failed of $($transactionCases.Count) (suite exit $loaderExit)") }
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
