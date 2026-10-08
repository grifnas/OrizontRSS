param(
    [Parameter(Mandatory = $true)]
    [string]$DistributionPath,
    [string]$ExpectedProductVersion
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$releaseStatus = Get-Content -LiteralPath (Join-Path $projectRoot 'docs\RELEASE-STATUS.json') -Raw -Encoding utf8 | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($ExpectedProductVersion)) {
    $ExpectedProductVersion = [string]$releaseStatus.nextCandidateVersion
    if ([string]::IsNullOrWhiteSpace($ExpectedProductVersion)) { $ExpectedProductVersion = [string]$releaseStatus.latestPublishedVersion }
}
$versionMatch = [regex]::Match($ExpectedProductVersion, '^(?<version>\d+\.\d+\.\d+)')
if (-not $versionMatch.Success) { throw "Unsupported expected product version: $ExpectedProductVersion" }
$ExpectedFileVersion = "$($versionMatch.Groups['version'].Value).0"
$cliHome = Join-Path (Split-Path -Parent $projectRoot) '.dotnet-cli'
$env:DOTNET_CLI_HOME = $cliHome
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

Push-Location $projectRoot
try
{
    powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-source.ps1 -Mode Full
    if ($LASTEXITCODE -ne 0) { throw "Source validation failed." }
    powershell -ExecutionPolicy Bypass -File .\tools\verify-distribution.ps1 -DistributionPath $DistributionPath -ExpectedFileVersion $ExpectedFileVersion -ExpectedProductVersion $ExpectedProductVersion
    if ($LASTEXITCODE -ne 0) { throw "Distribution verification failed." }
    Write-Output 'FULL VALIDATION PASSED.'
}
finally
{
    Pop-Location
}
