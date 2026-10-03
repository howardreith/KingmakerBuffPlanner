[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,100}$')][string]$RunId,
    [Parameter(Mandatory = $true)][string]$ReviewedBy,
    [Parameter(Mandatory = $true)][string]$Note,
    [string]$StateRoot
)

# Owner-only (final review C1): records that the owner has reviewed a run's
# protected-save violation (what changed, and that the save folder is as
# they want it). Until this record exists, no runtime run starts. It
# changes no save and acknowledges exactly one recorded violation, once.
# Re-review (harness): on the lab's own state root the owner types the run
# id at the keyboard (a non-interactive shell cannot); the acknowledgement
# names the run and the exact record it acknowledges, in its own folder.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
if ([string]::IsNullOrEmpty($StateRoot)) { $StateRoot = $script:KbpRuntimeStateRoot }
# Focused re-review: only a harness test root skips the typed
# confirmation; any other root (a junction or another drive letter for the
# lab's included) is treated as the lab's own.
$production = -not (Test-KbpHarnessTestPath $StateRoot)
$folder = Join-Path $StateRoot 'protected-save-violations'
$recordPath = Join-Path $folder ($RunId + '.json')
if (-not (Test-Path -LiteralPath $recordPath -PathType Leaf)) { throw "No protected-save violation is recorded for run $RunId." }
$acknowledgedFolder = Join-Path $folder 'acknowledged'
$acknowledgementPath = Join-Path $acknowledgedFolder ($RunId + '.json')
if ([string]::IsNullOrWhiteSpace($ReviewedBy) -or [string]::IsNullOrWhiteSpace($Note)) {
    throw 'Name the reviewer and say what was reviewed.'
}
$violation = Read-KbpJson $recordPath
$violationRun = if ($null -ne $violation.PSObject.Properties['runId']) { [string]$violation.runId } else { '' }
$violationBlocking = if ($null -ne $violation.PSObject.Properties['blocking']) { @($violation.blocking) } else { @('unknown') }
if ($violationRun -cne $RunId) {
    throw "The violation record for $RunId is malformed (it names run '$violationRun'); inspect and resolve it by hand."
}
$recordSha256 = Get-KbpSha256 $recordPath
# Focused re-review: an acknowledgement of this exact record ends here; one
# of an earlier version of the record is superseded by a new review.
$superseded = $null
if (Test-Path -LiteralPath $acknowledgementPath) {
    # An unreadable acknowledgement is superseded like a stale one.
    $existing = $null
    try { $existing = Read-KbpJson $acknowledgementPath } catch { }
    $existingSha = if ($null -ne $existing -and $null -ne $existing.PSObject.Properties['violationRecordSha256']) {
        [string]$existing.violationRecordSha256 } else { '' }
    if ($existingSha -ceq $recordSha256) { throw "The violation of run $RunId is already acknowledged." }
    $superseded = Join-Path $acknowledgedFolder ($RunId + '.superseded-' +
        [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ') + '.json')
}
if (-not $PSCmdlet.ShouldProcess($RunId, 'acknowledge the reviewed protected-save violation (' +
        ($violationBlocking -join ', ') + ')')) { return }
if ($production) {
    if ([Console]::IsInputRedirected) {
        throw 'The acknowledgement is typed by the owner at the keyboard; redirected input is refused.'
    }
    $typed = Read-Host ('Type the run id ' + $RunId + ' to confirm you reviewed what changed (' +
        ($violationBlocking -join ', ') + ')')
    if ([string]$typed -cne $RunId) { throw 'The typed run id does not match; nothing was acknowledged.' }
}
$body = [ordered]@{
    schemaVersion = 1; runId = $RunId; blocking = $violationBlocking
    violationRecordSha256 = $recordSha256
    reviewedBy = $ReviewedBy; note = $Note; acknowledgedAtUtc = [DateTime]::UtcNow.ToString('o')
}
New-Item -ItemType Directory -Path $acknowledgedFolder -Force | Out-Null
if ($null -ne $superseded) { Move-Item -LiteralPath $acknowledgementPath -Destination $superseded }
$stream = [IO.File]::Open($acknowledgementPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
try {
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes(($body | ConvertTo-Json -Compress) + [Environment]::NewLine)
    $stream.Write($bytes, 0, $bytes.Length)
}
finally { $stream.Dispose() }
Write-Host "Acknowledged: $acknowledgementPath"
