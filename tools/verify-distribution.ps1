param(
    [Parameter(Mandatory = $true)]
    [string]$DistributionPath,
    [string]$ExpectedFileVersion,
    [string]$ExpectedProductVersion
)

$ErrorActionPreference = 'Stop'
$path = [System.IO.Path]::GetFullPath($DistributionPath)
if (-not (Test-Path -LiteralPath $path -PathType Container))
{
    throw "Directorul de distribuție nu există: $path"
}

$requiredFiles = @(
    'Orizont.exe',
    'Orizont.dll',
    'Orizont.deps.json',
    'Orizont.runtimeconfig.json',
    'PresentationCore.dll',
    'PresentationFramework.dll',
    'WindowsBase.dll',
    'libespeak-ng.dll',
    'SpeechEngines\eSpeakNG\espeak-ng-data\ro_dict',
    'Licenses\eSpeakNG\COPYING',
    'Ghid-utilizator-Orizont-RSS.html',
    'Ghid-utilizator-Orizont-RSS.en.html',
    'Ghid-utilizator-Orizont-RSS.es.html',
    'Ghid-utilizator-Orizont-RSS.fr.html',
    'Ghid-utilizator-Orizont-RSS.de.html',
    'en-US\Orizont.resources.dll',
    'es-ES\Orizont.resources.dll',
    'fr-FR\Orizont.resources.dll',
    'de-DE\Orizont.resources.dll',
    'pt-BR\Orizont.resources.dll',
    'RELEASE-NOTES-1.5.3.md'
)

$missing = @($requiredFiles | Where-Object { -not (Test-Path -LiteralPath (Join-Path $path $_) -PathType Leaf) })
if ($missing.Count -gt 0)
{
    Write-Error ("Distribuția este incompletă. Lipsesc: " + ($missing -join ', '))
    exit 1
}

$dataFiles = @(Get-ChildItem -LiteralPath (Join-Path $path 'SpeechEngines\eSpeakNG\espeak-ng-data') -Recurse -File)
if ($dataFiles.Count -lt 400)
{
    Write-Error "Datele eSpeak NG sunt incomplete: numai $($dataFiles.Count) fișiere."
    exit 1
}

$forbiddenNames = @('settings.json', 'feeds.json')
$forbiddenExtensions = @('.pdb', '.cs', '.xaml', '.csproj', '.zip', '.7z', '.rar')
$forbidden = @(Get-ChildItem -LiteralPath $path -Recurse -File | Where-Object {
    $_.Extension -in $forbiddenExtensions -or $_.Name -in $forbiddenNames -or $_.Name -like '*.backup.json' -or $_.DirectoryName -match '[\\/]diagnostic(?:[\\/]|$)'
})
if ($forbidden.Count -gt 0)
{
    Write-Error ("Distribuția conține fișiere interzise sau date locale: " + (($forbidden | ForEach-Object Name) -join ', '))
    exit 1
}

$version = (Get-Item -LiteralPath (Join-Path $path 'Orizont.exe')).VersionInfo
if (-not [string]::IsNullOrWhiteSpace($ExpectedFileVersion) -and $version.FileVersion -ne $ExpectedFileVersion)
{
    Write-Error "Versiunea fișierului este $($version.FileVersion), dar era așteptată $ExpectedFileVersion."
    exit 1
}
if (-not [string]::IsNullOrWhiteSpace($ExpectedProductVersion) -and $version.ProductVersion -ne $ExpectedProductVersion)
{
    Write-Error "Versiunea produsului este $($version.ProductVersion), dar era așteptată $ExpectedProductVersion."
    exit 1
}
[pscustomobject]@{
    Path = $path
    FileVersion = $version.FileVersion
    ProductVersion = $version.ProductVersion
    EspeakDataFiles = $dataFiles.Count
    Status = 'completă'
}
