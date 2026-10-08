param(
    [ValidateSet('Quick', 'Full')][string]$Mode = 'Full',
    [switch]$NoRestore,
    [switch]$ListOnly
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$steps = [System.Collections.Generic.List[object]]::new()
function Add-Step([string]$Name, [string]$Command, [string[]]$Arguments) {
    $steps.Add([pscustomobject]@{ Name = $Name; Command = $Command; Arguments = $Arguments })
}
function Add-Build([string]$Name, [string]$Project) {
    $arguments = @('build', $Project, '--configuration', 'Release')
    if ($NoRestore) { $arguments += '--no-restore' } else { $arguments += '--ignore-failed-sources' }
    Add-Step $Name 'dotnet' $arguments
}

Add-Build 'Application build' '.\CititorRSS.Jaws.csproj'
if ($Mode -eq 'Full') { Add-Build 'Installer build' '.\packaging\installer\OrizontSetup.csproj' }
foreach ($suite in @('CoreSmoke', 'WorkflowSmoke', 'RulesSmoke', 'ShortcutsSmoke', 'OpenAiSmoke', 'LocalizationSmoke', 'EspeakSmoke', 'ScaleSmoke')) {
    if ($Mode -eq 'Quick' -and $suite -eq 'ScaleSmoke') { continue }
    $arguments = @('run', '--project', ".\tests\$suite\$suite.csproj", '--configuration', 'Release')
    if ($NoRestore) { $arguments += '--no-restore' }
    Add-Step $suite 'dotnet' $arguments
}
foreach ($validator in @('verify-localization', 'verify-user-guides', 'verify-text-encoding', 'verify-public-pages', 'verify-release-consistency', 'test-validation-runner')) {
    if ($Mode -eq 'Quick' -and $validator -eq 'verify-public-pages') { continue }
    $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', ".\tools\$validator.ps1")
    if ($validator -eq 'verify-localization') { $arguments += '-RequireComplete' }
    Add-Step $validator 'powershell' $arguments
}
if ($ListOnly) { $steps; return }

Push-Location $projectRoot
try {
    foreach ($step in $steps) {
        Write-Output ("Running: {0}" -f $step.Name)
        $arguments = $step.Arguments
        & $step.Command @arguments
        if ($LASTEXITCODE -ne 0) { throw "Validation failed: $($step.Name) (exit $LASTEXITCODE)." }
    }
    Write-Output "SOURCE VALIDATION PASSED ($Mode). No distribution created or validated."
}
finally { Pop-Location }
