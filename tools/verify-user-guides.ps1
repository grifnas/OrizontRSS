param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$sourcePath = Join-Path $projectRoot 'Ghid-utilizator-Orizont-RSS.html'
$source = Get-Content -LiteralPath $sourcePath -Raw -Encoding utf8
$failed = $false

function Values([string]$Html, [string]$Pattern)
{
    return @([regex]::Matches($Html, $Pattern) | ForEach-Object { $_.Groups[1].Value })
}

$sourceIds = Values $source 'id="([^"]+)"'
$sourceHrefs = Values $source 'href="([^"]+)"'
$sourceKeys = @(Values $source '(?s)<kbd>(.*?)</kbd>' | Sort-Object -Unique)
$sourceSections = [regex]::Matches($source, '<section\b').Count
$sourceHeadings = [regex]::Matches($source, '<h[1-6]\b').Count

foreach ($language in 'en', 'es', 'fr', 'de', 'pt')
{
    $path = Join-Path $projectRoot "Ghid-utilizator-Orizont-RSS.$language.html"
    if (-not (Test-Path -LiteralPath $path))
    {
        [pscustomobject]@{Language=$language;Exists=$false;Structure='missing';Markers='n/a'}
        $failed = $true
        continue
    }
    $html = Get-Content -LiteralPath $path -Raw -Encoding utf8
    $translatedKeys = @(Values $html '(?s)<kbd>(.*?)</kbd>' | Sort-Object -Unique)
    $same = ($sourceIds -join '|') -eq ((Values $html 'id="([^"]+)"') -join '|') -and
        ($sourceHrefs -join '|') -eq ((Values $html 'href="([^"]+)"') -join '|') -and
        ($sourceKeys -join '|') -eq ($translatedKeys -join '|') -and
        $sourceSections -eq [regex]::Matches($html, '<section\b').Count -and
        $sourceHeadings -eq [regex]::Matches($html, '<h[1-6]\b').Count -and
        $html.Contains("<html lang=`"$language`">") -and $html.Contains('Orizont RSS')
    $markers = [regex]::Matches($html, 'ZXH|\[\[\[ORZ_').Count
    [pscustomobject]@{Language=$language;Exists=$true;Structure=if($same){'pass'}else{'fail'};Markers=$markers}
    if (-not $same -or $markers -gt 0) { $failed = $true }
}

if ($failed) { exit 1 }
