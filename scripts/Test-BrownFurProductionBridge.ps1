[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ProductAssemblyPath,
    [Parameter(Mandatory = $true)][string]$ProviderAssemblyPath,
    [Parameter(Mandatory = $true)][string]$IncompatibleProviderAssemblyPath,
    [string]$ExpectedRejection = 'provider-direct-api-type-mismatch',
    [string]$KingmakerInstallDir,
    [switch]$RequireRoutingDiagnostics,
    [string]$OutputDirectory
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-KbpRepositoryRoot
if (-not $KingmakerInstallDir) { $KingmakerInstallDir = Get-KbpGamePath }
foreach ($path in @($ProductAssemblyPath, $ProviderAssemblyPath, $IncompatibleProviderAssemblyPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required actual product assembly is absent: $path"
    }
}
$ProductAssemblyPath = (Resolve-Path -LiteralPath $ProductAssemblyPath).Path
$ProviderAssemblyPath = (Resolve-Path -LiteralPath $ProviderAssemblyPath).Path
$IncompatibleProviderAssemblyPath = (Resolve-Path -LiteralPath $IncompatibleProviderAssemblyPath).Path
if ($ProviderAssemblyPath -eq $IncompatibleProviderAssemblyPath -or
    (Get-KbpSha256 $ProviderAssemblyPath) -eq (Get-KbpSha256 $IncompatibleProviderAssemblyPath)) {
    throw 'Acceptance and rejection require distinct actual provider binaries.'
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $root ('artifacts\production-bridge\' +
        [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))
}
$OutputDirectory = Assert-KbpPathWithin -Path $OutputDirectory -Root (Join-Path $root 'artifacts')
if (Test-Path -LiteralPath $OutputDirectory) { throw 'Probe evidence directory must be new.' }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$csc = Join-Path (Split-Path -Parent (Get-KbpMsBuild)) 'Roslyn\csc.exe'
$exe = Join-Path $OutputDirectory 'BrownFurProductionBridgeProbe.exe'
& $csc /nologo /target:exe /langversion:7.3 /warnaserror+ /optimize+ "/out:$exe" `
    "/reference:$ProductAssemblyPath" /reference:System.Core.dll `
    (Join-Path $root 'tests\ProductionBridge\BrownFurProductionBridgeProbe.cs')
if ($LASTEXITCODE -ne 0) { throw 'Production bridge probe compilation failed.' }
$cases = @(
    @{ name = 'intended-pair'; provider = $ProviderAssemblyPath; expected = 'accepted' },
    @{ name = 'incompatible-provider'; provider = $IncompatibleProviderAssemblyPath; expected = $ExpectedRejection },
    @{ name = 'missing-provider'; provider = 'missing'; expected = 'provider-direct-assembly-count-0' }
)
$diagnosticRequirement = if ($RequireRoutingDiagnostics) { "diagnostics" } else { "bridge-only" }
$failures = 0
foreach ($case in $cases) {
    & $exe $ProductAssemblyPath $case.provider $KingmakerInstallDir $case.expected $diagnosticRequirement 2>&1 |
        Tee-Object -FilePath (Join-Path $OutputDirectory ($case.name + '.log'))
    if ($LASTEXITCODE -ne 0) { $failures++ }
}
if ($failures -ne 0) { throw "Production bridge cases: PASS=$($cases.Count - $failures) FAIL=$failures; evidence=$OutputDirectory" }
Write-Host "Production bridge cases: PASS=3 FAIL=0; evidence=$OutputDirectory; gameplay=NOT VERIFIED"
