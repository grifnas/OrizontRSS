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
    dotnet build .\CititorRSS.Jaws.csproj --configuration Release --ignore-failed-sources
    if ($LASTEXITCODE -ne 0) { throw "Application build failed." }
    dotnet run --project .\tests\CoreSmoke\CoreSmoke.csproj --configuration Release
    if ($LASTEXITCODE -ne 0) { throw "Core smoke test failed." }
    dotnet run --project .\tests\LocalizationSmoke\LocalizationSmoke.csproj --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Localization smoke test failed." }
    dotnet run --project .\tests\EspeakSmoke\EspeakSmoke.csproj --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "eSpeak NG smoke test failed." }
    powershell -ExecutionPolicy Bypass -File .\tools\verify-localization.ps1 -RequireComplete
    if ($LASTEXITCODE -ne 0) { throw "Localization verification failed." }
    powershell -ExecutionPolicy Bypass -File .\tools\verify-user-guides.ps1
    if ($LASTEXITCODE -ne 0) { throw "User guide verification failed." }
    powershell -ExecutionPolicy Bypass -File .\tools\verify-distribution.ps1 -DistributionPath $DistributionPath -ExpectedFileVersion '1.5.3.0' -ExpectedProductVersion '1.5.3'
    if ($LASTEXITCODE -ne 0) { throw "Distribution verification failed." }
    Write-Output 'FULL VALIDATION PASSED.'
}
finally
{
    Pop-Location
}
