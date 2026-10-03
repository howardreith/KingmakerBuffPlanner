[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')

# Prospective fixture-seal evidence tests: a per-file inventory written at
# seal creation makes later drift exactly explainable (added/removed/changed
# paths with hashes and sizes), while an aggregate-only mismatch without a
# bound inventory stays blocked as insufficient historical evidence. These
# tests use isolated temp directories only; no real mod or game file is
# read or changed, and nothing here authorizes the current BagOfTricks
# fixture.
$boundary = Join-Path ([IO.Path]::GetTempPath()) ('KbpFixtureInventoryTest-' + [Guid]::NewGuid().ToString('N'))
$passed = 0
function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "FixtureInventory test failed: $Message" }
}

try {
    New-Item -ItemType Directory -Path (Join-Path $boundary 'mod') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $boundary 'mod\cfg') -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $boundary 'mod\a.dll') -Value 'assembly-bytes-a'
    Set-Content -LiteralPath (Join-Path $boundary 'mod\info.json') -Value '{"Version":"1.0.0"}'
    Set-Content -LiteralPath (Join-Path $boundary 'mod\cfg\settings.xml') -Value '<settings/>'

    # 1. A detailed seal explains exact additions, removals, and changes.
    $sealed = Get-KbpDirectoryContentIdentity (Join-Path $boundary 'mod')
    $inventoryPath = Write-KbpFixtureSealInventory -ModName 'TestMod' -Identity $sealed -DestinationRoot $boundary
    Assert-True (Test-Path -LiteralPath $inventoryPath -PathType Leaf) 'seal inventory was not written'
    $bound = Read-KbpJson $inventoryPath
    Assert-True ([int]$bound.fileCount -eq 3 -and
        [string]$bound.boundDirectoryManifestSha256 -ceq [string]$sealed.directoryManifestSha256) `
        'seal inventory is not bound to the sealed identity'
    # Mutate: add one file, remove one, change one.
    Set-Content -LiteralPath (Join-Path $boundary 'mod\extra.log') -Value 'x'
    Remove-Item -LiteralPath (Join-Path $boundary 'mod\cfg\settings.xml')
    Set-Content -LiteralPath (Join-Path $boundary 'mod\a.dll') -Value 'assembly-bytes-b'
    $drifted = Get-KbpDirectoryContentIdentity (Join-Path $boundary 'mod')
    $comparison = Compare-KbpFixtureInventory -SealInventory $bound -ActualIdentity $drifted
    Assert-True ($comparison.status -ceq 'differs') 'detailed drift was not detected'
    Assert-True ($comparison.added -ccontains 'extra.log') 'added path was not explained'
    Assert-True ($comparison.removed -ccontains 'cfg\settings.xml') 'removed path was not explained'
    Assert-True ($comparison.changed -ccontains 'a.dll') 'changed path was not explained'
    $passed++

    # 2. Matching live state reports an exact match.
    Remove-Item -LiteralPath (Join-Path $boundary 'mod\extra.log')
    Set-Content -LiteralPath (Join-Path $boundary 'mod\cfg\settings.xml') -Value '<settings/>'
    Set-Content -LiteralPath (Join-Path $boundary 'mod\a.dll') -Value 'assembly-bytes-a'
    $restored = Get-KbpDirectoryContentIdentity (Join-Path $boundary 'mod')
    Assert-True ((Compare-KbpFixtureInventory -SealInventory $bound -ActualIdentity $restored).status -ceq 'match') `
        'a matching detailed comparison did not report a match'
    $passed++

    # 3. An aggregate-only mismatch (no bound per-file inventory) is
    #    insufficient historical evidence and stays blocked — it is never a
    #    pass and never invents historical entries from today's directory.
    $lookup = Get-KbpFixtureSealInventory -InventoryRoot (Join-Path $boundary 'absent') `
        -ModName 'TestMod' -BoundManifestSha256 ([string]$sealed.directoryManifestSha256)
    Assert-True ($null -eq $lookup) 'an absent inventory root fabricated evidence'
    Set-Content -LiteralPath (Join-Path $boundary 'mod\a.dll') -Value 'assembly-bytes-z'
    $mismatch = Get-KbpDirectoryContentIdentity (Join-Path $boundary 'mod')
    Assert-True ([string]$mismatch.directoryManifestSha256 -cne [string]$sealed.directoryManifestSha256) `
        'fixture failed to drift for the aggregate-only case'
    # The existing guard semantics are unchanged: the identity comparison
    # still fails closed for the aggregate-only mismatch.
    $aggregateBlocked = $false
    try {
        [void](Assert-KbpCompatibilityModIdentity @{
            directoryName = 'TestMod'; assemblyName = 'a.dll';
            infoSha256 = 'ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff';
            assemblySha256 = 'ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff';
            directoryManifestSha256 = [string]$sealed.directoryManifestSha256;
            fileCount = [int]$sealed.fileCount; totalBytes = [long]$sealed.totalBytes
        } (Join-Path $boundary 'mod'))
    }
    catch { $aggregateBlocked = $true }
    Assert-True $aggregateBlocked 'an aggregate-only mismatch was not refused by the existing guard'
    $passed++
}
finally {
    if (Test-Path -LiteralPath $boundary) { Remove-Item -LiteralPath $boundary -Recurse -Force }
}

Write-Host "Fixture inventory evidence tests: PASS=$passed FAIL=0"
