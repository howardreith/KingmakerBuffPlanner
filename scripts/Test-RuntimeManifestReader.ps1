[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
# Keep the prior reader here as the behavioral oracle, including ordering,
# literal paths, zero-byte files and UTC timestamp serialization.
function Get-PriorManifest([string]$Path) {
    $root = (Resolve-Path -LiteralPath $Path).Path
    $directories = @(Get-ChildItem -LiteralPath $root -Directory -Recurse -Force | Sort-Object FullName | ForEach-Object {
        [ordered]@{ path = Get-KbpRelativePath $root $_.FullName; kind = 'directory' } })
    $files = @(Get-ChildItem -LiteralPath $root -File -Recurse -Force | Sort-Object FullName | ForEach-Object {
        [ordered]@{ path = Get-KbpRelativePath $root $_.FullName; kind = 'file'; length = $_.Length
            lastWriteTimeUtc = $_.LastWriteTimeUtc.ToString('o'); sha256 = Get-KbpSha256 $_.FullName } })
    return @([ordered]@{ path = '.'; kind = 'directory' }) + $directories + $files
}
$directory = Join-Path (Get-KbpRepositoryRoot) ('artifacts\manifest-reader-tests-' + [Guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path (Join-Path $directory 'empty') -Force)
[void](New-Item -ItemType Directory -Path (Join-Path $directory 'nested space') -Force)
foreach ($name in @('zero.txt', 'nested space\percent%23.txt', '[literal].txt', 'nested space\hash#.txt', 'hidden.txt')) {
    [IO.File]::WriteAllText((Join-Path $directory $name), $(if ($name -eq 'zero.txt') { '' } else { 'abc' }))
}
$hidden = Join-Path $directory 'hidden.txt'
[IO.File]::SetAttributes($hidden, [IO.FileAttributes]::Hidden)
$prior = @(Get-PriorManifest $directory)
$current = @(Get-KbpDirectoryManifest $directory)
if (-not (Test-KbpManifestEqual $prior $current)) { throw 'Reader differs from the complete prior manifest.' }
$passed = 1
function Assert-Changed([scriptblock]$Change) {
    $before = @(Get-KbpDirectoryManifest $directory)
    & $Change
    $after = @(Get-KbpDirectoryManifest $directory)
    if (Test-KbpManifestEqual $before $after) { throw 'A mutation escaped the complete manifest.' }
    if (-not (Test-KbpManifestEqual @(Get-PriorManifest $directory) $after)) { throw 'Mutation manifest differs from prior reader.' }
}
Assert-Changed { [IO.File]::WriteAllText((Join-Path $directory 'added.txt'), 'added') }; $passed++
Assert-Changed { [IO.File]::WriteAllText((Join-Path $directory '[literal].txt'), 'xyz') }; $passed++
Assert-Changed { [IO.File]::SetLastWriteTimeUtc((Join-Path $directory 'zero.txt'), [DateTime]::UtcNow.AddMinutes(-10)) }; $passed++
Assert-Changed { [IO.File]::Delete((Join-Path $directory 'added.txt')) }; $passed++
Assert-Changed { [void](New-Item -ItemType Directory -Path (Join-Path $directory 'added-directory')) }; $passed++
# A missing or locked file must throw, never become an omitted manifest row.
$file = [IO.FileInfo](Join-Path $directory '[literal].txt')
$lock = [IO.File]::Open($file.FullName, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
$refused = $false
try { [void](Get-KbpDirectoryManifest $directory) } catch { $refused = $true } finally { $lock.Dispose() }
if (-not $refused) { throw 'An unreadable file did not refuse the manifest.' }; $passed++
$refused = $false
try { [void](Get-KbpDirectoryManifest (Join-Path $directory 'missing')) } catch { $refused = $true }
if (-not $refused) { throw 'A missing root did not refuse the manifest.' }; $passed++
# WhatIf still reads real bytes, and causes no filesystem mutation.
$before = @(Get-KbpDirectoryManifest $directory)
$WhatIfPreference = $true
try { $during = @(Get-KbpDirectoryManifest $directory) } finally { $WhatIfPreference = $false }
if (-not (Test-KbpManifestEqual $before $during)) { throw 'WhatIf changed manifest semantics.' }; $passed++
Write-Host "Runtime manifest reader: PASS=$passed FAIL=0 (same prior manifest and mutation refusals)."
