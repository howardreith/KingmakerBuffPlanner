[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'Common.ps1')

function Assert-KbpHashTest([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    $script:passed++
}

$repo = Get-KbpRepositoryRoot
$testParent = Join-Path $repo 'artifacts\runtime-harness-tests'
$root = Join-Path $testParent ('common-hashing-' + [Guid]::NewGuid().ToString('N'))
$processDirectory = Join-Path $root 'process-directory'
$powershellLocation = Join-Path $root 'powershell-location'
$originalProcessDirectory = [Environment]::CurrentDirectory
$locationPushed = $false
$passed = 0

try {
    New-Item -ItemType Directory -Path $processDirectory, $powershellLocation -Force | Out-Null

    $sameName = 'same-name.bin'
    $processFile = Join-Path $processDirectory $sameName
    $locationFile = Join-Path $powershellLocation $sameName
    [IO.File]::WriteAllBytes($processFile, [Text.Encoding]::UTF8.GetBytes('process-directory-bytes'))
    [IO.File]::WriteAllBytes($locationFile, [Text.Encoding]::UTF8.GetBytes('powershell-location-bytes'))
    $expectedLocationHash = (Get-FileHash -LiteralPath $locationFile -Algorithm SHA256).Hash.ToLowerInvariant()
    $processHash = (Get-FileHash -LiteralPath $processFile -Algorithm SHA256).Hash.ToLowerInvariant()

    [Environment]::CurrentDirectory = $processDirectory
    Push-Location -LiteralPath $powershellLocation
    $locationPushed = $true

    $relativeHash = Get-KbpSha256 $sameName
    Assert-KbpHashTest ($relativeHash -ceq $expectedLocationHash -and $relativeHash -cne $processHash) `
        'A relative path was not resolved against the PowerShell filesystem location.'

    $absoluteHash = Get-KbpSha256 $processFile
    Assert-KbpHashTest ($absoluteHash -ceq $processHash) 'An absolute path did not hash its exact file.'

    $literalName = 'literal[1].bin'
    $literalPath = Join-Path $powershellLocation $literalName
    [IO.File]::WriteAllBytes($literalPath, [Text.Encoding]::UTF8.GetBytes('literal-special-name'))
    $literalExpected = (Get-FileHash -LiteralPath $literalPath -Algorithm SHA256).Hash.ToLowerInvariant()
    Assert-KbpHashTest ((Get-KbpSha256 $literalName) -ceq $literalExpected) `
        'A filename containing wildcard metacharacters was not treated literally.'

    $oldWhatIf = $WhatIfPreference
    try {
        $WhatIfPreference = $true
        $whatIfHash = Get-KbpSha256 $sameName
        $mutationPath = Join-Path $powershellLocation 'must-not-be-created.bin'
        Set-Content -LiteralPath $mutationPath -Value 'mutation' -Encoding ASCII
        Assert-KbpHashTest ($whatIfHash -ceq $expectedLocationHash) `
            'Inherited WhatIf did not return the real digest.'
        Assert-KbpHashTest (-not (Test-Path -LiteralPath $mutationPath)) `
            'Inherited WhatIf no longer protected a mutating command.'
    }
    finally {
        $WhatIfPreference = $oldWhatIf
    }

    $unsupportedFailed = $false
    try { [void](Get-KbpSha256 'Env:\PATH') }
    catch { $unsupportedFailed = $_.Exception.Message -like '*FileSystem*' }
    Assert-KbpHashTest $unsupportedFailed 'An unsupported PowerShell provider was not explicitly refused.'

    $missingFailed = $false
    try { [void](Get-KbpSha256 'missing.bin') }
    catch { $missingFailed = $true }
    Assert-KbpHashTest $missingFailed 'A missing input returned a digest instead of failing.'

    $lockedPath = Join-Path $powershellLocation 'locked.bin'
    [IO.File]::WriteAllBytes($lockedPath, [Text.Encoding]::UTF8.GetBytes('locked-input'))
    $exclusive = [IO.File]::Open($lockedPath, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    try {
        $unreadableFailed = $false
        try { [void](Get-KbpSha256 $lockedPath) }
        catch { $unreadableFailed = $true }
        Assert-KbpHashTest $unreadableFailed 'An unreadable input returned a digest instead of failing.'
    }
    finally {
        $exclusive.Dispose()
    }

    Write-Host "Common hashing tests: PASS=$passed FAIL=0"
}
finally {
    if ($locationPushed) { Pop-Location }
    [Environment]::CurrentDirectory = $originalProcessDirectory
    if (Test-Path -LiteralPath $root) {
        $resolved = Assert-KbpPathWithin -Path $root -Root $testParent
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
