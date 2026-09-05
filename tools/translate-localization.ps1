param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('en-US', 'es-ES', 'fr-FR', 'de-DE', 'pt-BR')]
    [string]$Culture
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$resourcesRoot = Join-Path $projectRoot 'Resources'
$basePath = Join-Path $resourcesRoot 'UiStrings.resx'
$targetPath = Join-Path $resourcesRoot "UiStrings.$Culture.resx"
$targetLanguage = @{
    'en-US' = 'en'
    'es-ES' = 'es'
    'fr-FR' = 'fr'
    'de-DE' = 'de'
    'pt-BR' = 'pt'
}[$Culture]

function Read-Resx([string]$Path)
{
    $document = [System.Xml.XmlDocument]::new()
    $document.PreserveWhitespace = $true
    $document.Load($Path)
    return $document
}

function Invoke-Translation([string[]]$Texts)
{
    $protectedTerms = @(
        [pscustomobject]@{Term='Orizont';Marker='ZXQ001QXZ'},
        [pscustomobject]@{Term='orizont';Marker='ZXQ010QXZ'},
        [pscustomobject]@{Term='Gemini';Marker='ZXQ002QXZ'},
        [pscustomobject]@{Term='SAPI5';Marker='ZXQ003QXZ'},
        [pscustomobject]@{Term='OPML';Marker='ZXQ005QXZ'},
        [pscustomobject]@{Term='RSS';Marker='ZXQ006QXZ'},
        [pscustomobject]@{Term='Ctrl';Marker='ZXQ007QXZ'},
        [pscustomobject]@{Term='Shift';Marker='ZXQ008QXZ'},
        [pscustomobject]@{Term='Alt';Marker='ZXQ009QXZ'}
    )
    $parts = [System.Collections.Generic.List[string]]::new()
    for ($index = 0; $index -lt $Texts.Count; $index++)
    {
        $protectedText = $Texts[$index]
        foreach ($item in $protectedTerms)
        {
            $protectedText = $protectedText.Replace($item.Term, $item.Marker)
        }
        $parts.Add(('[[[ORZ_{0:D4}]]]' -f $index))
        $parts.Add($protectedText)
    }
    $parts.Add('[[[ORZ_END]]]')
    $query = $parts -join "`n"
    $uri = 'https://translate.googleapis.com/translate_a/single?client=gtx&sl=ro&tl=' +
        $targetLanguage + '&dt=t&q=' + [Uri]::EscapeDataString($query)
    $response = Invoke-RestMethod -Uri $uri -Method Get -TimeoutSec 45
    $translatedBlock = (($response[0] | ForEach-Object { $_[0] }) -join '')
    $results = [System.Collections.Generic.List[string]]::new()

    for ($index = 0; $index -lt $Texts.Count; $index++)
    {
        $startMarker = '[[[ORZ_{0:D4}]]]' -f $index
        $endMarker = if ($index + 1 -lt $Texts.Count) {
            '[[[ORZ_{0:D4}]]]' -f ($index + 1)
        } else {
            '[[[ORZ_END]]]'
        }
        $start = $translatedBlock.IndexOf($startMarker, [StringComparison]::Ordinal)
        $end = $translatedBlock.IndexOf($endMarker, [StringComparison]::Ordinal)
        if ($start -lt 0 -or $end -lt 0 -or $end -le $start)
        {
            throw "Serviciul de traducere nu a păstrat marcajele lotului ($index)."
        }
        $start += $startMarker.Length
        $translatedText = $translatedBlock.Substring($start, $end - $start).Trim()
        foreach ($item in $protectedTerms)
        {
            $translatedText = $translatedText.Replace($item.Marker, $item.Term)
        }
        if ($translatedText.Contains('ZXQ'))
        {
            throw "Un termen tehnic protejat nu a putut fi restaurat ($index)."
        }
        $results.Add($translatedText)
    }

    return $results.ToArray()
}

$baseDocument = Read-Resx $basePath
$targetDocument = Read-Resx $targetPath
$existing = @{}
foreach ($node in $targetDocument.SelectNodes('/root/data'))
{
    $existing[[string]$node.GetAttribute('name')] = $true
}

$missing = [System.Collections.Generic.List[object]]::new()
foreach ($node in $baseDocument.SelectNodes('/root/data'))
{
    $key = [string]$node.GetAttribute('name')
    if (-not $existing.ContainsKey($key))
    {
        $missing.Add([pscustomobject]@{ Key = $key; Text = [string]$node.SelectSingleNode('value').InnerText })
    }
}

if ($missing.Count -eq 0)
{
    Write-Host "$Culture este deja completă."
    exit 0
}

$batches = [System.Collections.Generic.List[object]]::new()
$current = [System.Collections.Generic.List[object]]::new()
$currentLength = 0
foreach ($item in $missing)
{
    $estimatedLength = $item.Text.Length + 30
    if ($current.Count -gt 0 -and $currentLength + $estimatedLength -gt 2800)
    {
        $batches.Add($current.ToArray())
        $current = [System.Collections.Generic.List[object]]::new()
        $currentLength = 0
    }
    $current.Add($item)
    $currentLength += $estimatedLength
}
if ($current.Count -gt 0) { $batches.Add($current.ToArray()) }

$completed = 0
foreach ($batch in $batches)
{
    $translated = Invoke-Translation @($batch | ForEach-Object { $_.Text })
    for ($index = 0; $index -lt $batch.Count; $index++)
    {
        if ([string]::IsNullOrWhiteSpace($translated[$index]))
        {
            throw "Traducere goală pentru cheia: $($batch[$index].Key)"
        }
        $data = $targetDocument.CreateElement('data')
        [void]$data.SetAttribute('name', $batch[$index].Key)
        [void]$data.SetAttribute('space', 'http://www.w3.org/XML/1998/namespace', 'preserve')
        $value = $targetDocument.CreateElement('value')
        $value.InnerText = $translated[$index]
        [void]$data.AppendChild($value)
        [void]$targetDocument.DocumentElement.AppendChild($data)
    }
    $completed += $batch.Count
    Write-Host "${Culture}: $completed din $($missing.Count) texte traduse."
}

$temporaryPath = "$targetPath.tmp"
$settings = [System.Xml.XmlWriterSettings]::new()
$settings.Encoding = [System.Text.UTF8Encoding]::new($false)
$settings.Indent = $true
$settings.NewLineChars = "`r`n"
$writer = [System.Xml.XmlWriter]::Create($temporaryPath, $settings)
try { $targetDocument.Save($writer) } finally { $writer.Dispose() }
Move-Item -LiteralPath $temporaryPath -Destination $targetPath -Force
Write-Host "${Culture}: resursa a fost completată în $targetPath"
