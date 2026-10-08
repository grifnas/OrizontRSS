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
    throw "Distribution directory not found: $path"
}

$executablePath = Join-Path $path 'Orizont.exe'
if (-not (Test-Path -LiteralPath $executablePath -PathType Leaf))
{
    throw 'Distribution is missing Orizont.exe.'
}
$version = (Get-Item -LiteralPath $executablePath).VersionInfo
if ([string]::IsNullOrWhiteSpace($ExpectedFileVersion)) { $ExpectedFileVersion = $version.FileVersion }
if ([string]::IsNullOrWhiteSpace($ExpectedProductVersion)) { $ExpectedProductVersion = $version.ProductVersion }
$releaseNotesMatch = [regex]::Match($ExpectedProductVersion, '^(?<version>\d+\.\d+\.\d+)')
if (-not $releaseNotesMatch.Success) { throw "Product version is not a valid release version: $ExpectedProductVersion" }
$requiredReleaseNotes = "RELEASE-NOTES-$($releaseNotesMatch.Groups['version'].Value).md"

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
    'docs\user-guides\Ghid-utilizator-Orizont-RSS.html',
    'docs\user-guides\Ghid-utilizator-Orizont-RSS.en.html',
    'docs\user-guides\Ghid-utilizator-Orizont-RSS.es.html',
    'docs\user-guides\Ghid-utilizator-Orizont-RSS.fr.html',
    'docs\user-guides\Ghid-utilizator-Orizont-RSS.de.html',
    'docs\user-guides\Ghid-utilizator-Orizont-RSS.pt.html',
    'docs\user-guides\Ghid-utilizator-Orizont-RSS.hu.html',
    'docs\user-guides\Ghid-utilizator-Orizont-RSS.it.html',
    'en-US\Orizont.resources.dll',
    'es-ES\Orizont.resources.dll',
    'fr-FR\Orizont.resources.dll',
    'de-DE\Orizont.resources.dll',
    'pt-BR\Orizont.resources.dll',
    'hu-HU\Orizont.resources.dll',
    'it-IT\Orizont.resources.dll',
    $requiredReleaseNotes
)

$missing = @($requiredFiles | Where-Object { -not (Test-Path -LiteralPath (Join-Path $path $_) -PathType Leaf) })
if ($missing.Count -gt 0)
{
    Write-Error ("Distribution is incomplete. Missing: " + ($missing -join ', '))
    exit 1
}

$dataFiles = @(Get-ChildItem -LiteralPath (Join-Path $path 'SpeechEngines\eSpeakNG\espeak-ng-data') -Recurse -File)
if ($dataFiles.Count -lt 400)
{
    Write-Error "eSpeak NG data is incomplete: only $($dataFiles.Count) files."
    exit 1
}

$forbiddenNames = @('settings.json', 'feeds.json')
$forbiddenExtensions = @('.pdb', '.cs', '.xaml', '.csproj', '.zip', '.7z', '.rar')
$forbidden = @(Get-ChildItem -LiteralPath $path -Recurse -File | Where-Object {
    $_.Extension -in $forbiddenExtensions -or $_.Name -in $forbiddenNames -or $_.Name -like '*.backup.json' -or $_.DirectoryName -match '[\\/]diagnostic(?:[\\/]|$)'
})
if ($forbidden.Count -gt 0)
{
    Write-Error ("Distribution contains forbidden files or local data: " + (($forbidden | ForEach-Object Name) -join ', '))
    exit 1
}

if ($version.FileVersion -ne $ExpectedFileVersion)
{
    Write-Error "File version is $($version.FileVersion); expected $ExpectedFileVersion."
    exit 1
}
if ($version.ProductVersion -ne $ExpectedProductVersion)
{
    Write-Error "Product version is $($version.ProductVersion); expected $ExpectedProductVersion."
    exit 1
}
[pscustomobject]@{
    Path = $path
    FileVersion = $version.FileVersion
    ProductVersion = $version.ProductVersion
    EspeakDataFiles = $dataFiles.Count
    Status = 'complete'
}
