param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$basePath = Join-Path $projectRoot 'Resources\UiStrings.resx'
$document = [System.Xml.XmlDocument]::new()
$document.PreserveWhitespace = $true
$document.Load($basePath)
$existing = @{}
foreach ($node in $document.SelectNodes('/root/data'))
{
    $existing[[string]$node.GetAttribute('name')] = $true
}

$pattern = '(?:UiText\.(?:Translate|Format)|\bT|\bF|\bSay)\(\s*"((?:\\.|[^"\\])*)"'
$found = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($file in Get-ChildItem -LiteralPath $projectRoot -Filter '*.cs' -Recurse)
{
    if ($file.FullName -match '\\(?:bin|obj|Publicare[^\\]*)\\') { continue }
    $source = Get-Content -LiteralPath $file.FullName -Raw -Encoding utf8
    foreach ($match in [regex]::Matches($source, $pattern))
    {
        [void]$found.Add([regex]::Unescape($match.Groups[1].Value))
    }
}

$added = 0
foreach ($key in $found | Sort-Object)
{
    if ($existing.ContainsKey($key)) { continue }
    $data = $document.CreateElement('data')
    [void]$data.SetAttribute('name', $key)
    [void]$data.SetAttribute('space', 'http://www.w3.org/XML/1998/namespace', 'preserve')
    $value = $document.CreateElement('value')
    $value.InnerText = $key
    [void]$data.AppendChild($value)
    [void]$document.DocumentElement.AppendChild($data)
    $added++
}

if ($added -gt 0)
{
    $temporaryPath = "$basePath.tmp"
    $settings = [System.Xml.XmlWriterSettings]::new()
    $settings.Encoding = [System.Text.UTF8Encoding]::new($false)
    $settings.Indent = $true
    $settings.NewLineChars = "`r`n"
    $writer = [System.Xml.XmlWriter]::Create($temporaryPath, $settings)
    try { $document.Save($writer) } finally { $writer.Dispose() }
    Move-Item -LiteralPath $temporaryPath -Destination $basePath -Force
}

Write-Host "$added chei noi adăugate; $($found.Count) referințe localizabile găsite în cod."
