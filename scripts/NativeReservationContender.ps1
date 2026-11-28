[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][ValidateSet('Enter', 'Restore')][string]$Action,
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,100}$')][string]$Name,
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,100}$')][string]$RunId,
    [Parameter(Mandatory = $true)][string]$Signals,
    [Parameter(Mandatory = $true)][string]$LabRoot,
    [Parameter(Mandatory = $true)][string]$PackagePath,
    # Announce <Name>.ready, then wait at the shared start barrier (<Signals>\start).
    [switch]$Barrier
)

# A disposable contender for scripts\Test-NativeReservation.ps1 only: a separate
# Windows PowerShell process running this lab's actual admission
# (Enter-KbpRuntimeTransaction) or restoration (Restore-KbpRuntimeTransaction)
# in FixtureMode against a stand-in lab. Its reservation is the private one the
# test names in KBP_TEST_RESERVATION_ROOT (inherited); every path it writes -
# the stand-in lab, the package, the signal folder and that reservation - must
# lie inside this checkout's artifacts\runtime-harness-tests, or it refuses
# before doing anything. It writes <Name>.active / .restoring / .restored, or
# <Name>.refused with the message, into the signal folder.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
foreach ($bounded in @($LabRoot, $PackagePath, $Signals, [string]$env:KBP_TEST_RESERVATION_ROOT)) {
    if ([string]::IsNullOrWhiteSpace($bounded) -or -not (Test-KbpHarnessTestPath $bounded)) {
        throw "A reservation contender runs only inside a harness test root (refused: '$bounded')."
    }
}

function Write-ContenderSignal([string]$What, [string]$Text = '') {
    [IO.File]::WriteAllText((Join-Path $Signals "$Name.$What"), $Text)
}

$stateRoot = Join-Path $LabRoot 'state'
if ($Barrier) {
    Write-ContenderSignal 'ready'
    $until = (Get-Date).AddSeconds(60)
    while (-not (Test-Path -LiteralPath (Join-Path $Signals 'start'))) {
        if ((Get-Date) -gt $until) { Write-ContenderSignal 'refused' 'no start signal'; exit 4 }
        Start-Sleep -Milliseconds 1
    }
}
try {
    if ($Action -ceq 'Enter') {
        [void](Enter-KbpRuntimeTransaction -PackagePath $PackagePath -KingmakerInstallDir (Join-Path $LabRoot 'game') `
            -StateRoot $stateRoot -StagingRoot (Join-Path $LabRoot 'staging') -BackupRoot (Join-Path $LabRoot 'backups') `
            -RunId $RunId -FixtureMode -KnownKingmakerProcessIds @() -MoveAttempts 20 -MoveDelayMilliseconds 1000)
        Write-ContenderSignal 'active'
    }
    else {
        Write-ContenderSignal 'restoring'
        [void](Restore-KbpRuntimeTransaction -RunId $RunId -StateRoot $stateRoot -FixtureMode -KnownKingmakerProcessIds @())
        Write-ContenderSignal 'restored'
    }
}
catch {
    Write-ContenderSignal 'refused' $_.Exception.Message
    exit 3
}
exit 0
