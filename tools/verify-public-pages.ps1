param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$docsRoot = Join-Path $projectRoot 'docs'
$pages = @(Get-ChildItem -LiteralPath $docsRoot -File | Where-Object { $_.Name -like 'index*.html' })
$expectedLanguages = @('index.html', 'index.en.html', 'index.es.html', 'index.fr.html', 'index.de.html', 'index.pt.html', 'index.hu.html', 'index.it.html')
$requiredFragments = @(
    '<meta name="description"',
    '<meta name="robots" content="index,follow">',
    '<link rel="canonical"',
    'application/ld+json',
    'class="download-button"',
    'releases/latest/download/OrizontSetup.exe',
    'releases/latest/download/Orizont-RSS-1.6.0-win-x64.zip',
    'assets/screenshots/main-window.png',
    'assets/screenshots/reader.png',
    'assets/screenshots/settings.png'
)

if ($pages.Count -ne $expectedLanguages.Count) { throw "Se așteptau $($expectedLanguages.Count) pagini localizate, dar au fost găsite $($pages.Count)." }
$missingPages = @($expectedLanguages | Where-Object { -not (Test-Path -LiteralPath (Join-Path $docsRoot $_) -PathType Leaf) })
if ($missingPages.Count -gt 0) { throw "Lipsesc paginile publice: $($missingPages -join ', ')." }

foreach ($page in $pages)
{
    $html = Get-Content -LiteralPath $page.FullName -Raw
    foreach ($fragment in $requiredFragments)
    {
        if ($html.IndexOf($fragment, [StringComparison]::OrdinalIgnoreCase) -lt 0)
        {
            throw "Pagina $($page.Name) nu conține elementul public obligatoriu: $fragment"
        }
    }
    if ($html -match 'v1\.5\.[0-9]') { throw "Pagina $($page.Name) conține o referință veche la versiunea 1.5." }
}

$robotsPath = Join-Path $docsRoot 'robots.txt'
$sitemapPath = Join-Path $docsRoot 'sitemap.xml'
if (-not (Test-Path -LiteralPath $robotsPath -PathType Leaf)) { throw 'Lipsește robots.txt.' }
if (-not (Test-Path -LiteralPath $sitemapPath -PathType Leaf)) { throw 'Lipsește sitemap.xml.' }
$robots = Get-Content -LiteralPath $robotsPath -Raw
if ($robots -notmatch '(?im)^Sitemap:\s*https://grifnas\.github\.io/OrizontRSS/sitemap\.xml\s*$') { throw 'robots.txt nu indică sitemap-ul public.' }
[xml]$sitemap = Get-Content -LiteralPath $sitemapPath -Raw
$sitemapLocations = @($sitemap.urlset.url.loc | ForEach-Object { $_.'#text' })
if ($sitemapLocations.Count -lt $expectedLanguages.Count) { throw 'sitemap.xml nu conține toate paginile localizate.' }

Write-Output "Public pages check passed: $($pages.Count) localized pages, download links, SEO metadata, screenshots, robots.txt and sitemap.xml. No network accessed."
