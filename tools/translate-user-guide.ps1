param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('en-US', 'es-ES', 'fr-FR', 'de-DE', 'pt-BR')]
    [string]$Culture
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$sourcePath = Join-Path $projectRoot 'Ghid-utilizator-Orizont-RSS.html'
$language = @{'en-US'='en';'es-ES'='es';'fr-FR'='fr';'de-DE'='de';'pt-BR'='pt'}[$Culture]
$targetPath = Join-Path $projectRoot "Ghid-utilizator-Orizont-RSS.$language.html"
$protectedTerms = @(
    [pscustomobject]@{Term='Ghid-utilizator-Orizont-RSS.html';Marker='ZXH001HXZ'},
    [pscustomobject]@{Term='Orizont.exe';Marker='ZXH002HXZ'},
    [pscustomobject]@{Term='OpenAI Codex';Marker='ZXH003HXZ'},
    [pscustomobject]@{Term='Orizont';Marker='ZXH004HXZ'},
    [pscustomobject]@{Term='orizont';Marker='ZXH005HXZ'},
    [pscustomobject]@{Term='Gemini';Marker='ZXH006HXZ'},
    [pscustomobject]@{Term='SAPI5';Marker='ZXH007HXZ'},
    [pscustomobject]@{Term='OPML';Marker='ZXH009HXZ'},
    [pscustomobject]@{Term='RSS';Marker='ZXH010HXZ'},
    [pscustomobject]@{Term='Ctrl';Marker='ZXH011HXZ'},
    [pscustomobject]@{Term='Shift';Marker='ZXH012HXZ'},
    [pscustomobject]@{Term='Alt';Marker='ZXH013HXZ'},
    [pscustomobject]@{Term='Windows';Marker='ZXH014HXZ'},
    [pscustomobject]@{Term='GNU GPL';Marker='ZXH015HXZ'},
    [pscustomobject]@{Term='LICENSE';Marker='ZXH016HXZ'}
)

function Protect-Text([string]$Text)
{
    foreach ($item in $protectedTerms) { $Text = $Text.Replace($item.Term, $item.Marker) }
    return $Text
}

function Restore-Text([string]$Text)
{
    foreach ($item in $protectedTerms) { $Text = $Text.Replace($item.Marker, $item.Term) }
    if ($Text.Contains('ZXH')) { throw 'Un termen protejat nu a putut fi restaurat.' }
    return $Text
}

function Invoke-Translation([string[]]$Texts)
{
    $parts = [System.Collections.Generic.List[string]]::new()
    for ($index = 0; $index -lt $Texts.Count; $index++)
    {
        $parts.Add(('[[[ORZ_{0:D4}]]]' -f $index))
        $parts.Add((Protect-Text $Texts[$index]))
    }
    $parts.Add('[[[ORZ_END]]]')
    $query = $parts -join "`n"
    $uri = 'https://translate.googleapis.com/translate_a/single?client=gtx&sl=ro&tl=' + $language + '&dt=t&q=' + [Uri]::EscapeDataString($query)
    $response = Invoke-RestMethod -Uri $uri -Method Get -TimeoutSec 45
    $block = (($response[0] | ForEach-Object { $_[0] }) -join '')
    $results = [System.Collections.Generic.List[string]]::new()
    for ($index = 0; $index -lt $Texts.Count; $index++)
    {
        $startMarker = '[[[ORZ_{0:D4}]]]' -f $index
        $endMarker = if ($index + 1 -lt $Texts.Count) { '[[[ORZ_{0:D4}]]]' -f ($index + 1) } else { '[[[ORZ_END]]]' }
        $start = $block.IndexOf($startMarker, [StringComparison]::Ordinal)
        $end = $block.IndexOf($endMarker, [StringComparison]::Ordinal)
        if ($start -lt 0 -or $end -le $start) { throw "Marcajele lotului nu au fost păstrate la textul $index." }
        $start += $startMarker.Length
        $results.Add((Restore-Text $block.Substring($start, $end - $start).Trim()))
    }
    return $results.ToArray()
}

$html = Get-Content -LiteralPath $sourcePath -Raw -Encoding utf8
$matches = @([regex]::Matches($html, '(?s)>([^<>]+)<'))
$items = [System.Collections.Generic.List[object]]::new()
foreach ($match in $matches)
{
    $raw = $match.Groups[1].Value
    $text = $raw.Trim()
    if ([string]::IsNullOrWhiteSpace($text)) { continue }
    $before = $html.Substring(0, $match.Index + 1)
    $lastOpen = $before.LastIndexOf('<')
    $tag = if ($lastOpen -ge 0) { $before.Substring($lastOpen) } else { '' }
    if ($tag -match '^<(?:style|script|kbd)\b') { continue }
    $items.Add([pscustomobject]@{ Match = $match; Text = [Net.WebUtility]::HtmlDecode($text) })
}

$translations = @{}
$batch = [System.Collections.Generic.List[object]]::new()
$length = 0
foreach ($item in $items)
{
    $estimated = $item.Text.Length + 30
    if ($batch.Count -gt 0 -and $length + $estimated -gt 2800)
    {
        $translated = Invoke-Translation @($batch | ForEach-Object { $_.Text })
        for ($index = 0; $index -lt $batch.Count; $index++) { $translations[$batch[$index].Match.Index] = $translated[$index] }
        $batch.Clear(); $length = 0
    }
    $batch.Add($item); $length += $estimated
}
if ($batch.Count -gt 0)
{
    $translated = Invoke-Translation @($batch | ForEach-Object { $_.Text })
    for ($index = 0; $index -lt $batch.Count; $index++) { $translations[$batch[$index].Match.Index] = $translated[$index] }
}

$builder = [Text.StringBuilder]::new($html)
foreach ($item in @($items | Sort-Object { $_.Match.Index } -Descending))
{
    $raw = $item.Match.Groups[1].Value
    $leading = [regex]::Match($raw, '^\s*').Value
    $trailing = [regex]::Match($raw, '\s*$').Value
    $replacement = $leading + [Net.WebUtility]::HtmlEncode([string]$translations[$item.Match.Index]) + $trailing
    [void]$builder.Remove($item.Match.Groups[1].Index, $item.Match.Groups[1].Length)
    [void]$builder.Insert($item.Match.Groups[1].Index, $replacement)
}
$contentsLabel = @{'en'='Table of contents';'es'='Índice';'fr'='Table des matières';'de'='Inhaltsverzeichnis';'pt'='Índice'}[$language]
$result = $builder.ToString().Replace('<html lang="ro">', "<html lang=`"$language`">").Replace('aria-label="Cuprins"', "aria-label=`"$contentsLabel`")
if ($language -eq 'en') { $result = $result.Replace('<h2>Content</h2>', '<h2>Table of contents</h2>') }
if ($language -eq 'es') { $result = $result.Replace('<h1>Orizont RSS Gu&#237;a del usuario</h1>', '<h1>Gu&#237;a del usuario de Orizont RSS</h1>').Replace('<h2>Contenido</h2>', '<h2>&#205;ndice</h2>') }
if ($language -eq 'fr') { $result = $result.Replace('<h1>Orizont RSS Guide de l&#39;utilisateur</h1>', '<h1>Guide de l&#39;utilisateur d&#39;Orizont RSS</h1>').Replace('<h2>Contenu</h2>', '<h2>Table des mati&#232;res</h2>').Replace('>Licence et cotisations<', '>Licence et contributions<') }
if ($language -eq 'de') { $result = $result.Replace('<h2>Inhalt</h2>', '<h2>Inhaltsverzeichnis</h2>') }
[IO.File]::WriteAllText($targetPath, $result, [Text.UTF8Encoding]::new($false))
Write-Host "${Culture}: $($items.Count) fragmente traduse în $targetPath"
