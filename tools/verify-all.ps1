param(
    [Parameter(Mandatory = $true)]
    [string]$DistributionPath
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$cliHome = Join-Path (Split-Path -Parent $projectRoot) '.dotnet-cli'
$env:DOTNET_CLI_HOME = $cliHome
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

Push-Location $projectRoot
try
{
    powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-source.ps1 -Mode Full
    if ($LASTEXITCODE -ne 0) { throw "Source validation failed." }
    powershell -ExecutionPolicy Bypass -File .\tools\verify-distribution.ps1 -DistributionPath $DistributionPath -ExpectedFileVersion '1.6.0.0' -ExpectedProductVersion '1.6.0'
    if ($LASTEXITCODE -ne 0) { throw "Distribution verification failed." }
    Write-Output 'FULL VALIDATION PASSED.'
}
finally
{
    Pop-Location
}
