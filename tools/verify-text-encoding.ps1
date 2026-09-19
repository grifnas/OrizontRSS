param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$mojibakePattern = '[\uFFFD]'
$targets = @(
    Get-ChildItem -LiteralPath $projectRoot -File -Filter '*.md'
    Get-ChildItem -LiteralPath (Join-Path $projectRoot 'docs\user-guides') -File -Filter 'Ghid-utilizator-Orizont-RSS*.html'
    Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Resources') -File -Filter 'UiStrings*.resx'
    Get-ChildItem -LiteralPath (Join-Path $projectRoot 'docs') -File -Filter '*.md'
    Get-ChildItem -LiteralPath (Join-Path $projectRoot 'docs') -File -Filter 'index*.html'
)
$failed = $false
foreach ($file in $targets)
{
    $content = Get-Content -LiteralPath $file.FullName -Raw -Encoding utf8
    $matches = [regex]::Matches($content, $mojibakePattern)
    [pscustomobject]@{File=$file.FullName.Substring($projectRoot.Length + 1);Mojibake=$matches.Count}
    if ($matches.Count -gt 0) { $failed = $true }
}

if ($failed) { exit 1 }
