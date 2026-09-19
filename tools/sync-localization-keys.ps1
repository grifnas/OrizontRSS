param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$basePath = Join-Path $projectRoot 'Resources\UiStrings.resx'
$supplementalPath = Join-Path $projectRoot 'Resources\UiStringsSupplement.resx'
$document = [System.Xml.XmlDocument]::new()
$document.PreserveWhitespace = $true
$document.Load($basePath)
$existing = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($node in $document.SelectNodes('/root/data'))
{
    [void]$existing.Add([string]$node.GetAttribute('name'))
}

$pattern = '(?:UiText\.(?:Translate|Format)|\bT|\bF|\bSay)\(\s*"((?:\\.|[^"\\])*)"'
$found = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($file in Get-ChildItem -LiteralPath $projectRoot -Filter '*.cs' -Recurse)
{
    if ($file.FullName -match '\\(?:bin|obj|Publicare[^\\]*)\\') { continue }
    $source = Get-Content -LiteralPath $file.FullName -Raw -Encoding utf8
    foreach ($match in [regex]::Matches($source, $pattern))
    {
        $key = [regex]::Unescape($match.Groups[1].Value)
        if ($key -ceq 'toate folderele') { $key = 'Toate folderele' }
        [void]$found.Add($key)
    }
}

$xamlAttributes = 'Header', 'Text', 'Content', 'ToolTip', 'Title', 'AutomationProperties.Name', 'AutomationProperties.HelpText'
foreach ($file in Get-ChildItem -LiteralPath $projectRoot -Filter '*.xaml' -Recurse)
{
    if ($file.FullName -match '\\(?:bin|obj|tests|packaging)\\') { continue }
    $source = [System.Xml.XmlDocument]::new()
    $source.PreserveWhitespace = $true
    $source.Load($file.FullName)
    foreach ($element in $source.SelectNodes('//*'))
    {
        foreach ($attribute in $element.Attributes)
        {
            if ($attribute.Name -notin $xamlAttributes) { continue }
            $value = [string]$attribute.Value
            if ($value -match '^\s*\{') { continue }
            if ($value -ceq 'toate folderele') { $value = 'Toate folderele' }
            [void]$found.Add($value)
        }
    }
}

if (Test-Path -LiteralPath $supplementalPath)
{
    $supplemental = [System.Xml.XmlDocument]::new()
    $supplemental.PreserveWhitespace = $true
    $supplemental.Load($supplementalPath)
    foreach ($node in $supplemental.SelectNodes('/root/data'))
    {
        $key = [string]$node.GetAttribute('name')
        [void]$found.Add($key)
        if ($existing.Contains($key)) { continue }

        $data = $document.CreateElement('data')
        [void]$data.SetAttribute('name', $key)
        [void]$data.SetAttribute('space', 'http://www.w3.org/XML/1998/namespace', 'preserve')
        $value = $document.CreateElement('value')
        $value.InnerText = [string]$node.SelectSingleNode('value').InnerText
        [void]$data.AppendChild($value)
        [void]$document.DocumentElement.AppendChild($data)
        [void]$existing.Add($key)
    }
}

$added = 0
foreach ($key in $found | Sort-Object)
{
    if ($existing.Contains($key)) { continue }
    $data = $document.CreateElement('data')
    [void]$data.SetAttribute('name', $key)
    [void]$data.SetAttribute('space', 'http://www.w3.org/XML/1998/namespace', 'preserve')
    $value = $document.CreateElement('value')
    $value.InnerText = $key
    [void]$data.AppendChild($value)
    [void]$document.DocumentElement.AppendChild($data)
    [void]$existing.Add($key)
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

Write-Host "$added chei noi adăugate; $($found.Count) referințe localizabile găsite în cod și XAML."
