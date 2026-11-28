[CmdletBinding()]
param()

# Focused self-test of the Kingmaker native-test reservation (protocol 1,
# docs/NATIVE-RESERVATION.md) as this lab implements it. Two stand-in labs
# under artifacts\runtime-harness-tests share only a private reservation
# convention (never the machine-wide one); separate Windows PowerShell
# contender processes (scripts\NativeReservationContender.ps1) run the actual
# Enter-KbpRuntimeTransaction / Restore-KbpRuntimeTransaction in FixtureMode.
# No game, live Mods folder, save, lab root or other lab is touched. Emits
# PASS/FAIL lines and a RESULT line; exits 1 on any failure.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')

$repo = Get-KbpRepositoryRoot
$root = Join-Path $repo ('artifacts\runtime-harness-tests\reservation-' + [Guid]::NewGuid().ToString('N'))
$results = New-Object System.Collections.Generic.List[string]
$processes = New-Object System.Collections.Generic.List[System.Diagnostics.Process]
$handles = New-Object System.Collections.Generic.List[System.IDisposable]
$contender = Join-Path $PSScriptRoot 'NativeReservationContender.ps1'
$testMutex = 'Global\KbpReservationSelfTest-' + [Guid]::NewGuid().ToString('N')
$script:caseIndex = 0

function Test-Case([string]$Name, [scriptblock]$Body) {
    $script:caseIndex++
    $case = Join-Path $root ('case' + $script:caseIndex)
    New-Item -ItemType Directory -Path (Join-Path $case 'signals') -Force | Out-Null
    $script:KbpReservationOverride = [pscustomobject]@{
        MutexName = $testMutex + '-' + $script:caseIndex
        RecordPath = Join-Path $case 'shared\KingmakerNativeTest\reservation.v1.json'
    }
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
        $script:KbpReservationOverride = $null
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

# A stand-in lab: its own state, staging and backup roots and a game folder
# with an existing Mods tree; plus the minimal package Enter stages.
function New-StandInLab([string]$Case, [string]$Name) {
    $lab = Join-Path $Case $Name
    foreach ($folder in @('state', 'staging', 'backups', 'game\Mods\Existing')) {
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

function Start-Contender([string]$Case, [string]$Action, [string]$Name, [string]$Lab, [string]$RunId, [switch]$Barrier) {
    $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', ('"' + $contender + '"'), '-Action', $Action,
        '-Name', $Name, '-RunId', $RunId, '-Signals', ('"' + (Join-Path $Case 'signals') + '"'), '-LabRoot', ('"' + $Lab + '"'),
        '-PackagePath', ('"' + (Get-StandInPackage $Case) + '"'), '-MutexName', $script:KbpReservationOverride.MutexName,
        '-RecordPath', ('"' + $script:KbpReservationOverride.RecordPath + '"'))
    if ($Barrier) { $arguments += '-Barrier' }
    $process = Start-Process -FilePath 'powershell.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $processes.Add($process)
    return $process
}

function Wait-Signal([string]$Case, [string]$Name, [string[]]$What, [int]$Seconds = 90) {
    $until = (Get-Date).AddSeconds($Seconds)
    while ((Get-Date) -lt $until) {
        foreach ($candidate in $What) {
            $path = Join-Path $Case "signals\$Name.$candidate"
            if (Test-Path -LiteralPath $path) { return [pscustomobject]@{ What = $candidate; Text = [IO.File]::ReadAllText($path) } }
        }
        Start-Sleep -Milliseconds 100
    }
    throw "contender $Name reached none of: $($What -join ', ')"
}

function Wait-Status([string]$Lab, [string]$RunId, [string]$Status, [int]$Seconds = 60) {
    $statePath = Join-Path $Lab "state\transactions\$RunId\transaction.json"
    $until = (Get-Date).AddSeconds($Seconds)
    while ((Get-Date) -lt $until) {
        if (Test-Path -LiteralPath $statePath) {
            try { if ([string](Read-KbpJson $statePath).status -ceq $Status) { return } } catch { }
        }
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

function Get-ModsManifestText([string]$Lab) {
    return (@(Get-KbpDirectoryManifest (Join-Path $Lab 'game\Mods')) | ConvertTo-Json -Depth 5 -Compress)
}

# A loser arriving while the winner holds the reservation: refused by it, with
# no lock, no run folders and no Mods change in its own lab, and the winner's
# record unchanged.
function Assert-LoserRefused([string]$Case, [string]$Loser, [string]$LoserLab, [string]$WinnerRunId) {
    $modsBefore = Get-ModsManifestText $LoserLab
    [void](Start-Contender $Case 'Enter' $Loser $LoserLab $Loser)
    $outcome = Wait-Signal $Case $Loser @('refused', 'active')
    Assert-That ($outcome.What -ceq 'refused') "loser $Loser entered while the winner held the reservation"
    Assert-That ($outcome.Text -like "*reserved by KingmakerBuffPlanner run $WinnerRunId*") "loser $Loser was refused for another reason: $($outcome.Text)"
    Assert-That (-not (Test-Path -LiteralPath (Join-Path $LoserLab 'state\deployment.lock'))) "loser $Loser took its lock"
    foreach ($folder in @("state\transactions\$Loser", "staging\$Loser", "backups\$Loser")) {
        Assert-That (-not (Test-Path -LiteralPath (Join-Path $LoserLab $folder))) "loser $Loser created $folder"
    }
    Assert-That ((Get-ModsManifestText $LoserLab) -ceq $modsBefore) "loser $Loser changed its Mods folder"
    $record = Read-KbpReservationRecord $script:KbpReservationOverride
    Assert-That ($null -ne $record -and [string]$record.runId -ceq $WinnerRunId) "the winner's reservation changed while $Loser was refused"
}

# The winner is held inside its preparation (Prepared, original Mods move
# retrying), then holds the Active transaction after its process exited, then
# is held inside its restoration (Mods move retrying); a loser is refused in
# each. After the verified restoration the loser enters and restores.
function Invoke-ArrivalOrder([string]$Case, [string]$Winner, [string]$WinnerLab, [string]$Loser, [string]$LoserLab) {
    $hold = Hold-File (Join-Path $WinnerLab 'game\Mods\Existing\Info.json')
    $winnerProcess = Start-Contender $Case 'Enter' $Winner $WinnerLab $Winner
    Wait-Status $WinnerLab $Winner 'Prepared'
    Assert-LoserRefused $Case "$Loser-prepared" $LoserLab $Winner
    $hold.Dispose()
    $outcome = Wait-Signal $Case $Winner @('active', 'refused')
    Assert-That ($outcome.What -ceq 'active') "winner $Winner failed: $($outcome.Text)"
    Assert-That ($winnerProcess.WaitForExit(30000)) "winner $Winner's entry process did not exit"
    Assert-LoserRefused $Case "$Loser-active" $LoserLab $Winner

    $hold = Hold-File (Join-Path $WinnerLab 'game\Mods\KingmakerBuffPlanner\Info.json')
    [void](Start-Contender $Case 'Restore' "$Winner-restore" $WinnerLab $Winner)
    [void](Wait-Signal $Case "$Winner-restore" @('restoring'))
    Start-Sleep -Milliseconds 1500
    Assert-That (-not (Test-Path -LiteralPath (Join-Path $Case "signals\$Winner-restore.restored"))) 'the held restoration finished early'
    Assert-LoserRefused $Case "$Loser-restoring" $LoserLab $Winner
    $hold.Dispose()
    $outcome = Wait-Signal $Case "$Winner-restore" @('restored', 'refused')
    Assert-That ($outcome.What -ceq 'restored') "winner restoration failed: $($outcome.Text)"
    Assert-That (-not (Test-Path -LiteralPath $script:KbpReservationOverride.RecordPath)) "winner $Winner left its reservation"

    $afterProcess = Start-Contender $Case 'Enter' "$Loser-after" $LoserLab "$Loser-after"
    $outcome = Wait-Signal $Case "$Loser-after" @('active', 'refused')
    Assert-That ($outcome.What -ceq 'active') "$Loser-after was refused after the winner released: $($outcome.Text)"
    Assert-That ($afterProcess.WaitForExit(30000)) "$Loser-after's entry process did not exit"
    [void](Start-Contender $Case 'Restore' "$Loser-after-restore" $LoserLab "$Loser-after")
    $outcome = Wait-Signal $Case "$Loser-after-restore" @('restored', 'refused')
    Assert-That ($outcome.What -ceq 'restored') "$Loser-after restoration failed: $($outcome.Text)"
    Assert-That (-not (Test-Path -LiteralPath $script:KbpReservationOverride.RecordPath)) "$Loser-after left its reservation"
}

function Enter-InProcess([string]$Case, [string]$Lab, [string]$RunId) {
    return Enter-KbpRuntimeTransaction -PackagePath (Get-StandInPackage $Case) -KingmakerInstallDir (Join-Path $Lab 'game') `
        -StateRoot (Join-Path $Lab 'state') -StagingRoot (Join-Path $Lab 'staging') -BackupRoot (Join-Path $Lab 'backups') `
        -RunId $RunId -FixtureMode -KnownKingmakerProcessIds @() -MoveAttempts 1 -MoveDelayMilliseconds 0
}

function Restore-InProcess([string]$Lab, [string]$RunId) {
    return Restore-KbpRuntimeTransaction -RunId $RunId -StateRoot (Join-Path $Lab 'state') -FixtureMode -KnownKingmakerProcessIds @()
}

# A record as another writer would leave it.
function Set-ReservationRecord([string]$Holder, [int]$HolderProcessId, [string]$RunId, [string]$StateRoot = '',
    [string]$HolderStartUtc = '2020-01-01T00:00:00.0000000Z') {
    $path = $script:KbpReservationOverride.RecordPath
    New-Item -ItemType Directory -Path (Split-Path -Parent $path) -Force | Out-Null
    $record = [ordered]@{ protocol = 'kingmaker-native-test-reservation/1'; lab = $Holder; runId = $RunId; purpose = 'stand-in'
        token = [Guid]::NewGuid().ToString('N'); holderProcessId = $HolderProcessId; holderStartUtc = $HolderStartUtc
        host = $env:COMPUTERNAME; createdUtc = [DateTime]::UtcNow.ToString('o') }
    if ($StateRoot) { $record.stateRoot = [IO.Path]::GetFullPath($StateRoot).TrimEnd('\') }
    [IO.File]::WriteAllText($path, ($record | ConvertTo-Json))
}

function Get-DeadProcessId {
    $process = Start-Process -FilePath (Join-Path $env:WINDIR 'System32\PING.EXE') -ArgumentList @('-n', '1', '127.0.0.1') -WindowStyle Hidden -PassThru
    [void]$process.WaitForExit(10000)
    return $process.Id
}

function Get-RecordHash { return Get-KbpSha256 $script:KbpReservationOverride.RecordPath }

try {
    New-Item -ItemType Directory -Path $root -Force | Out-Null

    Test-Case 'the convention is the neutral machine-wide one for the lab and private for harness test roots' {
        param($case)
        $saved = $script:KbpReservationOverride
        $script:KbpReservationOverride = $null
        try {
            $production = Get-KbpReservationConvention $script:KbpRuntimeStateRoot
            Assert-That ($production.MutexName -ceq 'Global\KingmakerNativeTest.Reservation.v1') "mutex $($production.MutexName)"
            Assert-That ($production.RecordPath -ceq (Join-Path $env:ProgramData 'KingmakerNativeTest\reservation.v1.json')) "record $($production.RecordPath)"
            Assert-That (-not $production.RecordPath.StartsWith($script:KbpLabRoot, [StringComparison]::OrdinalIgnoreCase)) 'the record lives in the lab'
            $private = Get-KbpReservationConvention (Join-Path $case 'state')
            Assert-That ($private.MutexName -cne $production.MutexName -and $private.RecordPath -cne $production.RecordPath) 'a harness test root uses the machine-wide reservation'
        }
        finally { $script:KbpReservationOverride = $saved }
        Assert-Throws { Get-KbpReservationConvention $script:KbpRuntimeStateRoot } 'refused outside a harness test root' 'a test convention with the lab state root'
    }

    Test-Case 'a lab alone (no peer, no reservation folder yet) enters, holds across the transaction and releases' {
        param($case)
        $lab = New-StandInLab $case 'alone'
        Assert-That (-not (Test-Path -LiteralPath (Split-Path -Parent $script:KbpReservationOverride.RecordPath))) 'the reservation folder pre-exists'
        $statePath = Enter-InProcess $case $lab 'alone-run'
        $record = Read-KbpReservationRecord $script:KbpReservationOverride
        $state = Read-KbpJson $statePath
        Assert-That ($record.lab -ceq 'KingmakerBuffPlanner' -and $record.runId -ceq 'alone-run' -and $record.token -ceq $state.reservationToken) 'the transaction does not record its reservation'
        $restored = Restore-InProcess $lab 'alone-run'
        Assert-That ($restored.restorationVerified) 'not restored'
        Assert-That (-not (Test-Path -LiteralPath $script:KbpReservationOverride.RecordPath)) 'the reservation was not released after verified restoration'
    }

    Test-Case 'two labs exclude each other through the reservation alone, in either arrival order' {
        param($case)
        $first = New-StandInLab $case 'lab-one'
        $second = New-StandInLab $case 'lab-two'
        Invoke-ArrivalOrder $case 'A' $first 'B' $second
        Invoke-ArrivalOrder $case 'B2' $second 'A2' $first
    }

    Test-Case 'simultaneous arrivals admit exactly one contender' {
        param($case)
        $first = New-StandInLab $case 'lab-one'
        $second = New-StandInLab $case 'lab-two'
        foreach ($round in 1..3) {
            $start = Join-Path $case 'signals\start'
            if (Test-Path -LiteralPath $start) { Remove-Item -LiteralPath $start -Force }
            $xProcess = Start-Contender $case 'Enter' "X$round" $first "X$round" -Barrier
            $yProcess = Start-Contender $case 'Enter' "Y$round" $second "Y$round" -Barrier
            [void](Wait-Signal $case "X$round" @('ready'))
            [void](Wait-Signal $case "Y$round" @('ready'))
            [IO.File]::WriteAllText($start, 'go')
            $x = Wait-Signal $case "X$round" @('active', 'refused')
            $y = Wait-Signal $case "Y$round" @('active', 'refused')
            Assert-That ($xProcess.WaitForExit(30000) -and $yProcess.WaitForExit(30000)) "round ${round}: a contender process did not exit"
            Assert-That (@(@($x.What, $y.What) | Where-Object { $_ -ceq 'active' }).Count -eq 1) "round ${round}: $($x.What) / $($y.What)"
            $loserText = if ($x.What -ceq 'refused') { $x.Text } else { $y.Text }
            $loserLab = if ($x.What -ceq 'refused') { $first } else { $second }
            $loserRun = if ($x.What -ceq 'refused') { "X$round" } else { "Y$round" }
            Assert-That ($loserText -like '*reserved by KingmakerBuffPlanner*') "round ${round} loser refused for another reason: $loserText"
            Assert-That (-not (Test-Path -LiteralPath (Join-Path $loserLab 'state\deployment.lock'))) "round ${round}: the loser took its lock"
            foreach ($folder in @("state\transactions\$loserRun", "staging\$loserRun", "backups\$loserRun")) {
                Assert-That (-not (Test-Path -LiteralPath (Join-Path $loserLab $folder))) "round ${round}: the loser created $folder"
            }
            Assert-That (-not (Test-Path -LiteralPath (Join-Path $loserLab 'game\Mods\KingmakerBuffPlanner'))) "round ${round}: the loser staged its Mods"
            if ($x.What -ceq 'active') { [void](Restore-InProcess $first "X$round") } else { [void](Restore-InProcess $second "Y$round") }
            Assert-That (-not (Test-Path -LiteralPath $script:KbpReservationOverride.RecordPath)) "round ${round}: reservation kept"
        }
    }

    Test-Case 'another lab''s or an unreadable record refuses entry and restoration and is left byte-identical' {
        param($case)
        $lab = New-StandInLab $case 'lab'
        $statePath = Enter-InProcess $case $lab 'mine'
        $token = (Read-KbpJson $statePath).reservationToken
        Set-ReservationRecord 'KingmakerFlight' (Get-DeadProcessId) 'flight-run'
        $hash = Get-RecordHash
        Assert-Throws { Enter-InProcess $case (New-StandInLab $case 'other') 'second' } 'reserved by KingmakerFlight run flight-run' 'entry under a foreign record'
        Assert-Throws { Restore-InProcess $lab 'mine' } 'lost its Kingmaker native-test reservation' 'restoration under a foreign record'
        Assert-That ((Get-RecordHash) -ceq $hash) 'the foreign record changed'
        Assert-That ([string](Read-KbpJson $statePath).status -ceq 'Active') 'a lost reservation changed the transaction status'
        Assert-That (Test-Path -LiteralPath (Join-Path $lab 'state\deployment.lock')) 'a lost reservation released the lab lock'
        [IO.File]::WriteAllText($script:KbpReservationOverride.RecordPath, 'not json')
        $hash = Get-RecordHash
        Assert-Throws { Enter-InProcess $case (New-StandInLab $case 'third') 'third' } 'malformed or of an unknown protocol' 'entry over an unreadable record'
        Assert-Throws { Restore-InProcess $lab 'mine' } 'now: an unreadable record' 'restoration over an unreadable record'
        Assert-That ((Get-RecordHash) -ceq $hash) 'the unreadable record changed'
        Assert-That (-not [string]::IsNullOrEmpty([string]$token)) 'no token recorded'
    }

    Test-Case 'recovery admission: an orphan, live, foreign-root or malformed record is never adopted; only the stored token is' {
        param($case)
        $lab = New-StandInLab $case 'lab'
        $peer = New-StandInLab $case 'peer'
        $labState = Join-Path $lab 'state'
        # An orphan of an entry that died (writer gone, no transaction): refused, left for the owner.
        Set-ReservationRecord 'KingmakerBuffPlanner' (Get-DeadProcessId) 'crashed' $labState
        $hash = Get-RecordHash
        Assert-Throws { Enter-InProcess $case $lab 'after-crash' } 'reserved by KingmakerBuffPlanner run crashed' 'entry over an orphan record'
        Assert-That ((Get-RecordHash) -ceq $hash) 'the orphan record changed'
        # The same lab's record with a live writer, and another state root's record, are refused.
        Set-ReservationRecord 'KingmakerBuffPlanner' $PID 'live' $labState ((Get-Process -Id $PID).StartTime.ToUniversalTime().ToString('o'))
        Assert-Throws { Enter-InProcess $case $lab 'over-live' } 'reserved by KingmakerBuffPlanner run live' 'entry over a live writer'
        Set-ReservationRecord 'KingmakerBuffPlanner' (Get-DeadProcessId) 'elsewhere' (Join-Path $peer 'state')
        $hash = Get-RecordHash
        Assert-Throws { Enter-InProcess $case $lab 'over-peer' } 'reserved by KingmakerBuffPlanner run elsewhere' 'entry over another state root''s record'
        Assert-That ((Get-RecordHash) -ceq $hash) 'another state root''s record changed'
        # Adoption by the durably stored token (the fixture recovery's rule), and only for the same state root.
        $stored = (Read-KbpReservationRecord $script:KbpReservationOverride).token
        Assert-Throws { [void](Enter-KbpNativeReservation -RunId 'adopt' -Purpose 'test' -StateRoot $labState -AdoptToken $stored) } 'reserved by KingmakerBuffPlanner run elsewhere' 'token adoption from another state root'
        Set-ReservationRecord 'KingmakerBuffPlanner' (Get-DeadProcessId) 'interrupted' $labState
        $stored = (Read-KbpReservationRecord $script:KbpReservationOverride).token
        Assert-Throws { [void](Enter-KbpNativeReservation -RunId 'adopt' -Purpose 'test' -StateRoot $labState -AdoptToken ($stored.ToUpperInvariant())) } 'reserved by' 'adoption with a case-changed token'
        $adopted = Enter-KbpNativeReservation -RunId 'adopt' -Purpose 'test' -StateRoot $labState -AdoptToken $stored
        Assert-That ($adopted.adopted.token -ceq $stored -and (Read-KbpReservationRecord $script:KbpReservationOverride).token -ceq $adopted.token) 'the stored token was not adopted'
        # Malformed or case-changed records refuse and keep their bytes.
        $path = $script:KbpReservationOverride.RecordPath
        $good = '{"protocol":"kingmaker-native-test-reservation/1","lab":"KingmakerFlight","runId":"r","token":"0123456789abcdef0123456789abcdef","holderProcessId":4,"holderStartUtc":"2020-01-01T00:00:00.0000000Z"}'
        foreach ($bad in @(
                $good.Replace('kingmaker-native-test-reservation/1', 'Kingmaker-Native-Test-Reservation/1'),
                $good.Replace('0123456789abcdef0123456789abcdef', '0123456789ABCDEF0123456789ABCDEF'),
                $good.Replace('"holderProcessId":4', '"holderProcessId":0'),
                $good.Replace('"holderProcessId":4', '"holderProcessId":"4"'),
                $good.Replace(',"holderStartUtc":"2020-01-01T00:00:00.0000000Z"', ''),
                $good.Replace('"lab":"KingmakerFlight"', '"lab":""'),
                '[1,2]')) {
            [IO.File]::WriteAllText($path, $bad)
            $hash = Get-RecordHash
            Assert-Throws { Enter-InProcess $case (New-StandInLab $case ('m' + [Guid]::NewGuid().ToString('N').Substring(0, 8))) 'malformed' } 'malformed or of an unknown protocol' "entry over a malformed record ($bad)"
            Assert-That ((Get-RecordHash) -ceq $hash) "a malformed record changed ($bad)"
        }
        [IO.File]::WriteAllText($path, $good)
        Assert-Throws { Enter-InProcess $case (New-StandInLab $case 'wellformed') 'wellformed' } 'reserved by KingmakerFlight run r' 'entry over the well-formed control record'
    }

    Test-Case 'a restoration from another process waits while the entry''s writer still runs' {
        param($case)
        $lab = New-StandInLab $case 'lab'
        [void](Enter-InProcess $case $lab 'live-entry')
        $restorer = Start-Contender $case 'Restore' 'other-process' $lab 'live-entry'
        $outcome = Wait-Signal $case 'other-process' @('restored', 'refused')
        Assert-That ($outcome.What -ceq 'refused' -and $outcome.Text -like "*still held by its live process $PID*") "a second process restored under a live entry: $($outcome.Text)"
        Assert-That ($restorer.WaitForExit(30000)) 'the refused restorer did not exit'
        Assert-That ([string](Read-KbpJson (Join-Path $lab 'state\transactions\live-entry\transaction.json')).status -ceq 'Active') 'the refused restorer changed the transaction'
        $restored = Restore-InProcess $lab 'live-entry'
        Assert-That ($restored.restorationVerified -and -not (Test-Path -LiteralPath $script:KbpReservationOverride.RecordPath)) 'the writer''s own restoration did not release'
    }

    Test-Case 'a lost reservation keeps its first incident record; an unresolved save violation keeps the reservation' {
        param($case)
        $lab = New-StandInLab $case 'lab'
        $labState = Join-Path $lab 'state'
        $statePath = Enter-InProcess $case $lab 'lost'
        $token = (Read-KbpJson $statePath).reservationToken
        Set-ReservationRecord 'KingmakerMountedCombat' 4 'kmc-first'
        Assert-Throws { Restore-InProcess $lab 'lost' } 'lost its Kingmaker native-test reservation' 'restoration after loss'
        $incidentPath = Join-Path $labState 'transactions\lost\reservation-incident.json'
        Assert-That (Test-Path -LiteralPath $incidentPath) 'no incident record'
        $incidentHash = Get-KbpSha256 $incidentPath
        Set-ReservationRecord 'KingmakerWakingRune' 4 'wr-second'
        Assert-Throws { Restore-InProcess $lab 'lost' } 'lost its Kingmaker native-test reservation' 'second restoration after loss'
        Assert-That ((Get-KbpSha256 $incidentPath) -ceq $incidentHash -and (Get-Content -LiteralPath $incidentPath -Raw) -like '*kmc-first*') 'the first incident was replaced'
        # Put the lease back (owner resolution stand-in) and restore with an unacknowledged violation present.
        Set-ReservationRecord 'KingmakerBuffPlanner' $PID 'lost' $labState ((Get-Process -Id $PID).StartTime.ToUniversalTime().ToString('o'))
        $record = Get-Content -LiteralPath $script:KbpReservationOverride.RecordPath -Raw | ConvertFrom-Json
        $record.token = $token
        [IO.File]::WriteAllText($script:KbpReservationOverride.RecordPath, ($record | ConvertTo-Json))
        New-Item -ItemType Directory -Path (Join-Path $labState 'protected-save-violations') -Force | Out-Null
        Write-KbpJsonAtomic (Join-Path $labState 'protected-save-violations\earlier-run.json') ([ordered]@{ runId = 'earlier-run'; blocking = @('changed:Manual_1.zks') })
        $restored = Restore-InProcess $lab 'lost'
        Assert-That ($restored.restorationVerified) 'not restored'
        Assert-That ((Read-KbpReservationRecord $script:KbpReservationOverride).token -ceq $token) 'the reservation was released over an unacknowledged save violation'
        Remove-Item -LiteralPath (Join-Path $labState 'protected-save-violations\earlier-run.json') -Force
        [void](Restore-InProcess $lab 'lost')
        Assert-That (-not (Test-Path -LiteralPath $script:KbpReservationOverride.RecordPath)) 'the resolved run did not release its reservation'
    }

    Test-Case 'a failed restoration keeps the reservation; the retried restoration releases it' {
        param($case)
        $lab = New-StandInLab $case 'lab'
        $statePath = Enter-InProcess $case $lab 'retry'
        $hold = Hold-File (Join-Path $lab 'game\Mods\KingmakerBuffPlanner\Info.json')
        # Held through every bounded move attempt, so the restoration fails closed.
        $failure = $null
        try { [void](Restore-InProcess $lab 'retry') } catch { $failure = $_.Exception.Message }
        $hold.Dispose()
        Assert-That ($failure -like '*failed closed*') "restoration did not fail: $failure"
        $record = Read-KbpReservationRecord $script:KbpReservationOverride
        Assert-That ($null -ne $record -and $record.token -ceq (Read-KbpJson $statePath).reservationToken) 'a failed restoration released the reservation'
        Assert-Throws { Enter-InProcess $case (New-StandInLab $case 'peer') 'peer' } 'reserved by KingmakerBuffPlanner run retry' 'entry over an unresolved restoration'
        $restored = Restore-InProcess $lab 'retry'
        Assert-That ($restored.restorationVerified -and -not (Test-Path -LiteralPath $script:KbpReservationOverride.RecordPath)) 'the retried restoration did not release the reservation'
    }

    Test-Case 'a transaction entered before this lab took part reserves before restoring; an interrupted release finishes' {
        param($case)
        $lab = New-StandInLab $case 'lab'
        $statePath = Enter-InProcess $case $lab 'legacy'
        $state = Read-KbpJson $statePath
        [void](Exit-KbpNativeReservation -StateRoot (Join-Path $lab 'state') -Token $state.reservationToken)
        $state.PSObject.Properties.Remove('reservationToken')
        Write-KbpJsonAtomic $statePath $state
        Set-ReservationRecord 'KingmakerMountedCombat' (Get-DeadProcessId) 'kmc-run'
        Assert-Throws { Restore-InProcess $lab 'legacy' } 'reserved by KingmakerMountedCombat' 'a legacy restoration under a foreign record'
        Assert-That ([string](Read-KbpJson $statePath).status -ceq 'Active') 'the refused legacy restoration changed the transaction'
        Remove-Item -LiteralPath $script:KbpReservationOverride.RecordPath -Force
        $restored = Restore-InProcess $lab 'legacy'
        Assert-That ($restored.restorationVerified -and -not (Test-Path -LiteralPath $script:KbpReservationOverride.RecordPath)) 'the legacy restoration did not reserve and release'
        # Interrupted release: a restored transaction whose record is still its own.
        $statePath = Enter-InProcess $case $lab 'interrupted'
        $restored = Restore-InProcess $lab 'interrupted'
        $token = [string]$restored.reservationToken
        Set-ReservationRecord 'KingmakerBuffPlanner' $PID 'interrupted' (Join-Path $lab 'state')
        $record = Get-Content -LiteralPath $script:KbpReservationOverride.RecordPath -Raw | ConvertFrom-Json
        $record.token = $token
        [IO.File]::WriteAllText($script:KbpReservationOverride.RecordPath, ($record | ConvertTo-Json))
        [void](Restore-InProcess $lab 'interrupted')
        Assert-That (-not (Test-Path -LiteralPath $script:KbpReservationOverride.RecordPath)) 'an interrupted release was not finished'
        # ...and a restored transaction never removes a record that is no longer its own.
        Set-ReservationRecord 'KingmakerWakingRune' 4 'wr-run'
        $hash = Get-RecordHash
        [void](Restore-InProcess $lab 'interrupted')
        Assert-That ((Get-RecordHash) -ceq $hash) 'a restored transaction removed another lab''s record'
    }

    # The fixture bootstrap/teardown script participates through its own state
    # root's (here: private test) convention; it really checks for a running
    # game, so this case is not run while one exists.
    if (@(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue).Count -ne 0) {
        $results.Add('NOT RUN the fixture script reserves before its lock: a Kingmaker process is running')
    }
    else {
    Test-Case 'the fixture script reserves before its lock, refuses a held reservation and releases after bootstrap and teardown' {
        param($case)
        $saveRoot = Join-Path $case 'saves'
        $fixtureState = Join-Path $case 'fixture-state'
        New-Item -ItemType Directory -Path $saveRoot, $fixtureState -Force | Out-Null
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        foreach ($save in @(@{ File = 'Manual_400_PlayerCampaign.zks'; Name = 'PlayerCampaign' }, @{ File = 'Manual_401_KBP_AUTOMATION_SEED.zks'; Name = 'KBP_AUTOMATION_SEED' })) {
            $archive = [IO.Compression.ZipFile]::Open((Join-Path $saveRoot $save.File), [IO.Compression.ZipArchiveMode]::Create, [Text.Encoding]::UTF8)
            try {
                $writer = [IO.StreamWriter]::new($archive.CreateEntry('header.json').Open(), [Text.Encoding]::UTF8)
                try { $writer.Write('{"Name":"' + $save.Name + '","GameName":"Disposable Automation Campaign","GameId":"11111111-2222-3333-4444-555555555555","Area":"Test Area"}') }
                finally { $writer.Dispose() }
            } finally { $archive.Dispose() }
        }
        $saved = $script:KbpReservationOverride
        $script:KbpReservationOverride = $null
        try { $fixtureConvention = Get-KbpReservationConvention $fixtureState } finally { $script:KbpReservationOverride = $saved }
        $script:KbpReservationOverride = $fixtureConvention
        $bootstrap = Join-Path $PSScriptRoot 'New-KbpAutomationFixture.ps1'
        $archiveRoot = Join-Path $case 'archive'
        Set-ReservationRecord 'KingmakerWakingRune' 4 'wr-run'
        $hash = Get-RecordHash
        Assert-Throws { & $bootstrap -RunId 'held' -SaveRoot $saveRoot -StateRoot $fixtureState -ArchiveRoot $archiveRoot -Confirm:$false | Out-Null } 'reserved by KingmakerWakingRune run wr-run' 'bootstrap under a held reservation'
        Assert-That (-not (Test-Path -LiteralPath (Join-Path $fixtureState 'fixture.lock'))) 'the refused bootstrap took the fixture lock'
        Assert-That (@(Get-ChildItem -LiteralPath $saveRoot -Filter '*KBP_AUTOMATION_BASELINE*').Count -eq 0) 'the refused bootstrap wrote a fixture save'
        Assert-That ((Get-RecordHash) -ceq $hash) 'the held record changed'
        Remove-Item -LiteralPath $fixtureConvention.RecordPath -Force
        & $bootstrap -RunId 'boot' -SaveRoot $saveRoot -StateRoot $fixtureState -ArchiveRoot $archiveRoot -Confirm:$false | Out-Null
        Assert-That ([string](Read-KbpJson (Join-Path $fixtureState 'boot\transaction.json')).reservationToken -ne '') 'the bootstrap did not record its reservation'
        Assert-That (-not (Test-Path -LiteralPath $fixtureConvention.RecordPath)) 'the completed bootstrap kept its reservation'
        & $bootstrap -RunId 'teardown' -Teardown -SaveRoot $saveRoot -StateRoot $fixtureState -ArchiveRoot $archiveRoot -Confirm:$false | Out-Null
        Assert-That (-not (Test-Path -LiteralPath $fixtureConvention.RecordPath) -and -not (Test-Path -LiteralPath (Join-Path $fixtureState 'fixture.lock'))) 'the teardown kept its reservation or lock'
    }
    }

    Test-Case 'an entry refused by this lab''s own checks releases the reservation it took' {
        param($case)
        $lab = New-StandInLab $case 'lab'
        New-KbpOwnedLock (Join-Path $lab 'state\deployment.lock') 'stale' 'token'
        Assert-Throws { Enter-InProcess $case $lab 'refused' } 'Unresolved runtime deployment lock' 'entry over an unresolved lock'
        Assert-That (-not (Test-Path -LiteralPath $script:KbpReservationOverride.RecordPath)) 'the refused entry kept the reservation'
        # A failed entry (the package copy is refused after the lock) also releases it.
        Assert-Throws { Enter-KbpRuntimeTransaction -PackagePath (Get-StandInPackage $case) -KingmakerInstallDir (Join-Path (New-StandInLab $case 'failing') 'game') `
            -StateRoot (Join-Path $case 'failing\state') -StagingRoot (Join-Path $case 'failing\staging') -BackupRoot (Join-Path $case 'failing\backups') `
            -RunId 'failing' -FixtureMode -KnownKingmakerProcessIds @() -ExpectedPackageSha256 ('0' * 64) } 'changed after admission' 'entry with a refused package copy'
        Assert-That (-not (Test-Path -LiteralPath $script:KbpReservationOverride.RecordPath)) 'the failed entry kept the reservation'
        # ...so another contender process is admitted afterwards.
        $peer = New-StandInLab $case 'peer'
        $peerProcess = Start-Contender $case 'Enter' 'after-refusal' $peer 'after-refusal'
        $outcome = Wait-Signal $case 'after-refusal' @('active', 'refused')
        Assert-That ($outcome.What -ceq 'active') "a contender was refused after the released entries: $($outcome.Text)"
        Assert-That ($peerProcess.WaitForExit(30000)) 'the admitted contender did not exit'
        [void](Restore-InProcess $peer 'after-refusal')
    }
    # The directly affected existing display-recovery check (offline, bounded).
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Test-DisplayModsRecovery.ps1') | Out-Null
    if ($LASTEXITCODE -eq 0) { $results.Add('PASS affected existing check: Test-DisplayModsRecovery.ps1') }
    else { $results.Add("FAIL affected existing check: Test-DisplayModsRecovery.ps1 exited $LASTEXITCODE") }
}
finally {
    $script:KbpReservationOverride = $null
    if (Test-Path -LiteralPath $root) {
        $resolved = [IO.Path]::GetFullPath($root)
        [void](Assert-KbpPathWithin -Path $resolved -Root (Join-Path $repo 'artifacts\runtime-harness-tests'))
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}

$results | ForEach-Object { Write-Host $_ }
$failed = @($results | Where-Object { $_ -like 'FAIL*' }).Count
$passedCount = @($results | Where-Object { $_ -like 'PASS*' }).Count
$notRun = @($results | Where-Object { $_ -like 'NOT RUN*' }).Count
Write-Host "RESULT total=$($results.Count) passed=$passedCount failed=$failed notrun=$notRun"
if ($failed -gt 0 -or $passedCount -eq 0) { exit 1 }
