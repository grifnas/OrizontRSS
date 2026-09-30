$ErrorActionPreference = 'Stop'
$runner = Join-Path $PSScriptRoot 'verify-source.ps1'
$full = @(& $runner -Mode Full -ListOnly)
$quick = @(& $runner -Mode Quick -ListOnly)
$checks = 0
function Check([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    $script:checks++
}
foreach ($suite in @('CoreSmoke', 'WorkflowSmoke', 'RulesSmoke', 'ShortcutsSmoke', 'OpenAiSmoke', 'LocalizationSmoke', 'EspeakSmoke', 'ScaleSmoke')) {
    Check (@($full | Where-Object Name -eq $suite).Count -eq 1) "Missing or duplicated suite: $suite"
}
Check (@($quick | Where-Object Name -eq 'ScaleSmoke').Count -eq 0) 'Quick must omit the scale benchmark.'
Check (@($full | Where-Object Name -eq 'verify-public-pages').Count -eq 1) 'Full must validate public pages.'
$projectRoot = Split-Path -Parent $PSScriptRoot
foreach ($project in Get-ChildItem -Path (Join-Path $projectRoot 'tests') -Filter '*.csproj' -Recurse) {
    Check (@($full | Where-Object Name -eq $project.BaseName).Count -eq 1) "Unregistered test project: $($project.BaseName)"
}
$ci = Get-Content (Join-Path $projectRoot '.github\workflows\ci.yml') -Raw -Encoding utf8
$distribution = Get-Content (Join-Path $PSScriptRoot 'verify-all.ps1') -Raw -Encoding utf8
Check ($ci.Contains('verify-source.ps1 -Mode Full')) 'CI must use the shared full runner.'
Check ($distribution.Contains('verify-source.ps1 -Mode Full')) 'Distribution validation must use the shared full runner.'

# Mock external processes in this child PowerShell only. No builds, validators,
# user profiles or recursive invocations are actually started by this test.
$global:OrizontValidationTestCalls = 0
$global:OrizontValidationTestFailAt = -1
function dotnet { $global:OrizontValidationTestCalls++; $global:LASTEXITCODE = $(if ($global:OrizontValidationTestCalls -eq $global:OrizontValidationTestFailAt) { 9 } else { 0 }) }
function powershell { $global:OrizontValidationTestCalls++; $global:LASTEXITCODE = $(if ($global:OrizontValidationTestCalls -eq $global:OrizontValidationTestFailAt) { 9 } else { 0 }) }
$output = @(& $runner -Mode Full)
Check ($global:OrizontValidationTestCalls -eq $full.Count) 'Success must execute every planned step.'
Check (@($output | Where-Object { $_ -like 'SOURCE VALIDATION PASSED*' }).Count -eq 1) 'Success marker missing.'
foreach ($failedStep in @(1, 4, $full.Count)) {
    $global:OrizontValidationTestCalls = 0; $global:OrizontValidationTestFailAt = $failedStep; $caught = $false
    try { & $runner -Mode Full | Out-Null } catch { $caught = $true }
    Check $caught "A failed step must fail the runner ($failedStep)."
    Check ($global:OrizontValidationTestCalls -eq $failedStep) "No steps may run after failure ($failedStep)."
}
Write-Output "Validation runner passed: $checks checks; all subprocesses simulated."
