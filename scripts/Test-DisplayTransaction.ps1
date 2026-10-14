[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeAutomation.Common.ps1')

$boundary = [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetTempPath()) `
    ('kbp-display-transaction-' + [Guid]::NewGuid().ToString('N'))))
New-Item -ItemType Directory -Path $boundary | Out-Null
$passed = 0

function Assert-Test([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function New-TestDisplay([int]$Width, [int]$Height, [int]$Monitors = 1) {
    [pscustomobject][ordered]@{
        primarySize = [string]$Width + 'x' + [string]$Height
        primaryWidth = $Width; primaryHeight = $Height
        virtualLeft = 0; virtualTop = 0; virtualWidth = $Width
        virtualHeight = $Height; monitorCount = $Monitors
    }
}

function Write-TestPngHeader([string]$Path, [int]$Width, [int]$Height) {
    $bytes = New-Object byte[] 24
    [byte[]]$signature = 137, 80, 78, 71, 13, 10, 26, 10
    [Array]::Copy($signature, 0, $bytes, 0, $signature.Length)
    $bytes[11] = 13
    [Array]::Copy([Text.Encoding]::ASCII.GetBytes('IHDR'), 0, $bytes, 12, 4)
    $bytes[16] = [byte](($Width -shr 24) -band 255)
    $bytes[17] = [byte](($Width -shr 16) -band 255)
    $bytes[18] = [byte](($Width -shr 8) -band 255)
    $bytes[19] = [byte]($Width -band 255)
    $bytes[20] = [byte](($Height -shr 24) -band 255)
    $bytes[21] = [byte](($Height -shr 16) -band 255)
    $bytes[22] = [byte](($Height -shr 8) -band 255)
    $bytes[23] = [byte]($Height -band 255)
    [IO.File]::WriteAllBytes($Path, $bytes)
}

function New-ScreenCase([string]$Directory, [int]$PngWidth = 1920,
    [int]$RecordWidth = 1920, [string]$AssertionObserved = '1920x1200;fullScreen=False') {
    New-Item -ItemType Directory -Path $Directory -Force | Out-Null
    Write-TestPngHeader (Join-Path $Directory 'nui-test.png') $PngWidth 1200
    Write-KbpJsonAtomic (Join-Path $Directory 'native-ui.json') ([ordered]@{
        ScreenWidth = $RecordWidth; ScreenHeight = 1200; Captures = @('nui-test.png')
    })
    $request = [pscustomobject]@{
        runId = 'display-test'; scenario = 'live-workspace-native-ui'
        evidenceDirectory = $Directory; parameters = @{ expectedScreen = '1920x1200' }
    }
    $result = [pscustomobject]@{ assertions = @([pscustomobject]@{
        id = 'screen-size'; expected = '1920x1200'; observed = $AssertionObserved; status = 'PASS'
    }) }
    return [pscustomobject]@{ request = $request; result = $result }
}

try {
    Assert-Test ((Test-KbpDisplayModeSupported -Size '1920x1200' -DisplaySize '1920x1200') -and
        -not (Test-KbpDisplayModeSupported -Size '1920x1201' -DisplaySize '1920x1200') -and
        ((Get-KbpDisplayModeArguments -Size '1920x1200') -join ' ') -ceq
            '-screen-fullscreen 0 -popupwindow -screen-width 1920 -screen-height 1200') `
        'Explicit 1920x1200 windowed admission is incorrect.'
    Write-Host 'PASS explicit-size-admission'
    $passed++

    $stateRoot = Join-Path $boundary 'state'
    $snapshotDirectory = Join-Path $stateRoot 'transactions\display-good'
    New-Item -ItemType Directory -Path $snapshotDirectory -Force | Out-Null
    $snapshotPath = Join-Path $snapshotDirectory 'display-physical.json'
    $original = New-TestDisplay 1920 1200
    Save-KbpPhysicalDisplaySnapshotFile -Path $snapshotPath -Snapshot $original `
        -RunId 'display-good' -ExpectedWindowSize '1920x1200'
    $blocked = $false
    try { Assert-KbpNoUnresolvedTransaction -StateRoot $stateRoot `
            -FixtureLockPath (Join-Path $stateRoot 'absent-fixture.lock') }
    catch { $blocked = $_.Exception.Message -like '*Unverified physical display snapshot*' }
    Assert-Test $blocked 'A pending physical display snapshot did not block later work.'
    Write-Host 'PASS pending-snapshot-blocks'
    $passed++

    $verified = Confirm-KbpPhysicalDisplaySnapshotFile -Path $snapshotPath `
        -CurrentSnapshot (New-TestDisplay 1920 1200)
    Assert-KbpNoUnresolvedTransaction -StateRoot $stateRoot `
        -FixtureLockPath (Join-Path $stateRoot 'absent-fixture.lock')
    Assert-Test ([bool]$verified.restored -and
        [string]$verified.original.primarySize -ceq '1920x1200' -and
        [string]$verified.observedAfter.primarySize -ceq '1920x1200') `
        'An unchanged physical display was not verified durably.'
    Write-Host 'PASS unchanged-display-verifies'
    $passed++

    $reverifyFailure = $null
    try { Confirm-KbpPhysicalDisplaySnapshotFile -Path $snapshotPath `
            -CurrentSnapshot (New-TestDisplay 1920 1080) -Reverify | Out-Null }
    catch { $reverifyFailure = $_.Exception.Message }
    Assert-Test ($reverifyFailure -like '*Refusing to overwrite a potentially newer user change*') `
        'An active transaction did not reverify the physical display before releasing Mods.'
    [void](Confirm-KbpPhysicalDisplaySnapshotFile -Path $snapshotPath `
        -CurrentSnapshot $original -Reverify)
    Write-Host 'PASS active-transaction-reverifies-display'
    $passed++

    $mismatchDirectory = Join-Path $stateRoot 'transactions\display-mismatch'
    New-Item -ItemType Directory -Path $mismatchDirectory -Force | Out-Null
    $mismatchPath = Join-Path $mismatchDirectory 'display-physical.json'
    Save-KbpPhysicalDisplaySnapshotFile -Path $mismatchPath -Snapshot $original `
        -RunId 'display-mismatch' -ExpectedWindowSize '1920x1200'
    $mismatch = $null
    try { Confirm-KbpPhysicalDisplaySnapshotFile -Path $mismatchPath `
            -CurrentSnapshot (New-TestDisplay 1920 1080) | Out-Null }
    catch { $mismatch = $_.Exception.Message }
    Assert-Test ($mismatch -like '*Refusing to overwrite a potentially newer user change*' -and
        -not [bool](Read-KbpJson $mismatchPath).restored) `
        'A changed display was overwritten or marked restored.'
    Write-Host 'PASS changed-display-fails-closed'
    $passed++

    $stillBlocked = $false
    try { Assert-KbpNoUnresolvedTransaction -StateRoot $stateRoot `
            -FixtureLockPath (Join-Path $stateRoot 'absent-fixture.lock') }
    catch { $stillBlocked = $_.Exception.Message -like '*display-mismatch*' }
    Assert-Test $stillBlocked 'A mismatched display did not keep later runs blocked.'
    [void](Confirm-KbpPhysicalDisplaySnapshotFile -Path $mismatchPath -CurrentSnapshot $original)
    Write-Host 'PASS mismatch-recovery-remains-guarded'
    $passed++

    $restoreText = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Restore-Local.ps1') -Raw
    $physicalAt = $restoreText.IndexOf('Confirm-KbpPhysicalDisplaySnapshotFile')
    $modsAt = $restoreText.IndexOf('Restore-KbpRuntimeTransaction')
    Assert-Test ($physicalAt -ge 0 -and $modsAt -gt $physicalAt) `
        'Restore-Local can release the Mods transaction before display verification.'
    Write-Host 'PASS recovery-verifies-display-before-mods'
    $passed++

    $goodCase = New-ScreenCase (Join-Path $boundary 'screen-good')
    $screen = Assert-KbpExpectedScreenEvidence -Request $goodCase.request -Result $goodCase.result
    Assert-Test ([string]$screen.requested -ceq '1920x1200' -and
        [int]$screen.pngCount -eq 1 -and
        (Test-Path -LiteralPath (Join-Path $goodCase.request.evidenceDirectory 'screen-validation.json'))) `
        'Matching Unity, record and PNG dimensions were not correlated.'
    Write-Host 'PASS requested-screen-correlates-record-and-png'
    $passed++

    $badPng = New-ScreenCase (Join-Path $boundary 'screen-bad-png') -PngWidth 1280
    $badPngFailure = $null
    try { Assert-KbpExpectedScreenEvidence -Request $badPng.request -Result $badPng.result | Out-Null }
    catch { $badPngFailure = $_.Exception.Message }
    Assert-Test ($badPngFailure -like '*PNG nui-test.png is 1280x1200, expected 1920x1200*') `
        'A wrong-size PNG was accepted.'
    Write-Host 'PASS wrong-png-size-rejected'
    $passed++

    $badUnity = New-ScreenCase (Join-Path $boundary 'screen-bad-unity') `
        -AssertionObserved '1920x1080;fullScreen=False'
    $badUnityFailure = $null
    try { Assert-KbpExpectedScreenEvidence -Request $badUnity.request -Result $badUnity.result | Out-Null }
    catch { $badUnityFailure = $_.Exception.Message }
    Assert-Test ($badUnityFailure -like '*Unity screen-size assertion does not prove 1920x1200*') `
        'A wrong-size Unity assertion was accepted.'
    Write-Host 'PASS wrong-unity-size-rejected'
    $passed++

    Write-Host "Display transaction: PASS=$passed FAIL=0; no display, registry, game, mod, or save mutation."
}
finally {
    $temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
    if (-not $boundary.StartsWith($temporaryRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Unsafe display-test cleanup path.'
    }
    Remove-Item -LiteralPath $boundary -Recurse -Force
}
