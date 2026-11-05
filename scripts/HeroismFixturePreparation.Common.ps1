Set-StrictMode -Version Latest

function Save-KbpHeroismWorkingBackup {
    param([string]$WorkingPath, [string]$EvidenceDirectory, [string]$ExpectedSha256, [string]$RunId)
    if ((Split-Path -Leaf $WorkingPath) -cnotmatch '^Manual_[0-9]+_KBP_AUTOMATION_WORKING\.zks$') {
        throw 'Heroism preparation can back up only the exact Automation working save.'
    }
    if ((Get-KbpSha256 $WorkingPath) -cne $ExpectedSha256) { throw 'Heroism working-save hash changed.' }
    $backupPath = Join-Path $EvidenceDirectory 'heroism-working-before.zks'
    if (Test-Path -LiteralPath $backupPath) { throw 'Heroism preparation backup already exists.' }
    [IO.File]::Copy($WorkingPath, $backupPath, $false)
    if ((Get-KbpSha256 $backupPath) -cne $ExpectedSha256 -or (Get-KbpSha256 $WorkingPath) -cne $ExpectedSha256) {
        throw 'Heroism preparation backup did not preserve the exact bound working save.'
    }
    Write-KbpJsonAtomic (Join-Path $EvidenceDirectory 'heroism-preparation-transaction.json') ([ordered]@{
        schemaVersion = 1; runId = $RunId; status = 'BackedUp'; workingPath = [IO.Path]::GetFullPath($WorkingPath)
        backupPath = [IO.Path]::GetFullPath($backupPath); originalSha256 = $ExpectedSha256
    })
}

function Restore-KbpHeroismWorkingBackup {
    param([string]$WorkingPath, [string]$EvidenceDirectory, [string]$ExpectedSha256, [string]$RunId, $BeforeSnapshot = $null)
    Assert-KbpNotRunning
    $recordPath = Join-Path $EvidenceDirectory 'heroism-preparation-transaction.json'
    $record = Read-KbpJson $recordPath
    $backupPath = Join-Path $EvidenceDirectory 'heroism-working-before.zks'
    if ([string]$record.runId -cne $RunId -or [int]$record.schemaVersion -ne 1 -or
        [string]$record.workingPath -cne [IO.Path]::GetFullPath($WorkingPath) -or
        [string]$record.backupPath -cne [IO.Path]::GetFullPath($backupPath) -or
        [string]$record.originalSha256 -cne $ExpectedSha256 -or
        (Split-Path -Leaf $WorkingPath) -cnotmatch '^Manual_[0-9]+_KBP_AUTOMATION_WORKING\.zks$' -or
        (Get-KbpSha256 $backupPath) -cne $ExpectedSha256) { throw 'Heroism rollback ownership or backup hash mismatch.' }
    if (-not (Test-Path -LiteralPath $WorkingPath -PathType Leaf) -or (Get-KbpSha256 $WorkingPath) -cne $ExpectedSha256) {
        $staged = $WorkingPath + '.kbp-restore-' + [Guid]::NewGuid().ToString('N')
        $replaced = $WorkingPath + '.kbp-replaced-' + [Guid]::NewGuid().ToString('N')
        try {
            [IO.File]::Copy($backupPath, $staged, $false)
            Assert-KbpNotRunning
            if (Test-Path -LiteralPath $WorkingPath -PathType Leaf) {
                [IO.File]::Replace($staged, $WorkingPath, $replaced)
                [IO.File]::Copy($replaced, (Join-Path $EvidenceDirectory ('heroism-failed-working-' + [Guid]::NewGuid().ToString('N') + '.zks')), $false)
                Remove-Item -LiteralPath $replaced
            } else { [IO.File]::Move($staged, $WorkingPath) }
        } finally { if (Test-Path -LiteralPath $staged) { Remove-Item -LiteralPath $staged } }
    }
    if ((Get-KbpSha256 $WorkingPath) -cne $ExpectedSha256) { throw 'Heroism working-save rollback verification failed.' }
    $intentPath = Join-Path $EvidenceDirectory 'heroism-fixture-preparation.json'
    if (Test-Path -LiteralPath $intentPath) {
        $intent = Read-KbpJson $intentPath
        if ($intent.PSObject.Properties.Name -contains 'nativeTemporaryFileName') {
            $temporaryName = [string]$intent.nativeTemporaryFileName
            if ([int]$intent.schemaVersion -ne 1 -or [string]$intent.runId -cne $RunId -or
                [string]$intent.workingSha256Before -cne $ExpectedSha256 -or
                $temporaryName -cnotmatch '^Manual_[0-9]+_KBP_AUTOMATION_WORKING\.zks$' -or
                $temporaryName -ceq (Split-Path -Leaf $WorkingPath) -or $null -eq $BeforeSnapshot -or
                $BeforeSnapshot.Contains($temporaryName)) { throw 'Native temporary save ownership is unproven; refusing cleanup.' }
            $nativeTemporary = Join-Path (Split-Path -Parent $WorkingPath) $temporaryName
            if (Test-Path -LiteralPath $nativeTemporary -PathType Leaf) {
                [IO.File]::Copy($nativeTemporary, (Join-Path $EvidenceDirectory ('heroism-failed-native-temporary-' + [Guid]::NewGuid().ToString('N') + '.zks')), $false)
                Remove-Item -LiteralPath $nativeTemporary
            }
        }
    }
    $record.status = 'RolledBack'
    Write-KbpJsonAtomic $recordPath $record
}

function Assert-KbpHeroismPreparationOutcome {
    param($Request)
    $record = Read-KbpJson (Join-Path $Request.evidenceDirectory 'heroism-fixture-preparation.json')
    $before = Join-Path $Request.evidenceDirectory 'heroism-working-before.zks'
    if ([int]$record.schemaVersion -ne 1 -or [string]$record.runId -cne [string]$Request.runId -or
        -not [bool]$record.prepared -or $null -ne $record.failure -or [int]$record.castsSubmitted -ne 0 -or
        [string]$record.workingSha256Before -cne [string]$Request.parameters.workingSha256 -or
        (Get-KbpSha256 $before) -cne [string]$Request.parameters.workingSha256 -or
        [string]$record.workingSha256After -cnotmatch '^[a-f0-9]{64}$' -or
        [int]$record.spellbookCasterLevel -ne 10 -or [bool]$record.classProgressionChanged -or
        [int]$record.levelTwoCapacity -lt 5 -or [int]$record.levelTwoRemaining -ne [int]$record.levelTwoCapacity -or
        [bool]$record.infectiousCharmsPresent -ne ([string]$Request.parameters.heroismCondition -ceq 'present') -or
        [string]$record.heroism -cne '5ab0d42fb68c9e34abae4921822b9d63' -or
        [string]$record.successor -cne '3e4ab69ada402d145a5e0ad3ad4b8564') {
        throw 'Heroism fixture preparation evidence is incomplete or inconsistent.'
    }
}
