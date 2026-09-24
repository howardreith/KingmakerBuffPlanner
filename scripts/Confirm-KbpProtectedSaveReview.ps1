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

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
if ([string]::IsNullOrEmpty($StateRoot)) { $StateRoot = $script:KbpRuntimeStateRoot }
$folder = Join-Path $StateRoot 'protected-save-violations'
$recordPath = Join-Path $folder ($RunId + '.json')
if (-not (Test-Path -LiteralPath $recordPath -PathType Leaf)) { throw "No protected-save violation is recorded for run $RunId." }
$acknowledgementPath = Join-Path $folder ($RunId + '.acknowledged.json')
if (Test-Path -LiteralPath $acknowledgementPath) { throw "The violation of run $RunId is already acknowledged." }
if ([string]::IsNullOrWhiteSpace($ReviewedBy) -or [string]::IsNullOrWhiteSpace($Note)) {
    throw 'Name the reviewer and say what was reviewed.'
}
$violation = Read-KbpJson $recordPath
if (-not $PSCmdlet.ShouldProcess($RunId, 'acknowledge the reviewed protected-save violation (' +
        (@($violation.blocking) -join ', ') + ')')) { return }
$body = [ordered]@{
    schemaVersion = 1; runId = $RunId; blocking = @($violation.blocking)
    violationRecordSha256 = Get-KbpSha256 $recordPath
    reviewedBy = $ReviewedBy; note = $Note; acknowledgedAtUtc = [DateTime]::UtcNow.ToString('o')
}
$stream = [IO.File]::Open($acknowledgementPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
try {
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes(($body | ConvertTo-Json -Compress) + [Environment]::NewLine)
    $stream.Write($bytes, 0, $bytes.Length)
}
finally { $stream.Dispose() }
Write-Host "Acknowledged: $acknowledgementPath"
