param(
    [switch]$RequireComplete
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$resourcesRoot = Join-Path $projectRoot 'Resources'
$basePath = Join-Path $resourcesRoot 'UiStrings.resx'

function Read-ResourceMap([string]$Path)
{
    [xml]$document = Get-Content -LiteralPath $Path -Raw -Encoding utf8
    $map = [System.Collections.Generic.Dictionary[string, string]]::new([StringComparer]::Ordinal)
    $duplicates = 0
    foreach ($node in $document.SelectNodes('/root/data'))
    {
        $key = [string]$node.GetAttribute('name')
        if ($map.ContainsKey($key)) { $duplicates++; continue }
        $map[$key] = [string]$node.SelectSingleNode('value').InnerText
    }
    return [pscustomobject]@{ Map = $map; Duplicates = $duplicates }
}

$baseResult = Read-ResourceMap $basePath
$baseMap = $baseResult.Map
$baseKeys = @($baseMap.Keys)
$cultures = 'en-US', 'es-ES', 'fr-FR', 'de-DE', 'pt-BR', 'hu-HU', 'it-IT'
$failed = $baseResult.Duplicates -gt 0
$protectedTerms = 'Orizont', 'RSS', 'OPML', 'SAPI5', 'Gemini', 'Ctrl', 'Shift', 'Alt', 'https://', 'http://'
$mojibakePattern = '(?:Ã[^\x00-\x7F]|Â[^\x00-\x7F]|Äƒ|È™|È›|â€[\p{L}\p{N}\p{P}\p{S}]|â†[\p{L}\p{N}\p{P}\p{S}]|â€¦|ï¿½|\uFFFD)'
$obsoleteKeys = @(
    'FereastrÄƒ',
    'MaximizeazÄƒ/restabileÈ™te fereastra',
    'MaximizeazÄƒ sau restabileÈ™te fereastra.',
    'Deschide SetÄƒri voce.',
    'Fereastra a fost maximizatÄƒ.',
    'Fereastra a fost restabilitÄƒ.',
    'River of News: Toate nouta?ile din {0} feeduri. {1} articole afi?ate, cronologic.',
    'Notificări Toast pentru cuvinte-cheie',
    'Notificări Windows (Toast) pentru cuvinte-cheie',
    'Istoric stare și erori — Orizont RSS 1.3',
    'Orizont RSS 1.3'
)
$baseMojibake = @($baseMap.Keys + $baseMap.Values | Where-Object { [regex]::IsMatch($_, $mojibakePattern) })
$baseObsolete = @($obsoleteKeys | Where-Object { $baseMap.ContainsKey($_) })
if ($baseMojibake.Count -gt 0 -or $baseObsolete.Count -gt 0) { $failed = $true }
Write-Output ([pscustomobject]@{
    Inventory = 'base-resource'
    Keys = $baseMap.Count
    DuplicateKeys = $baseResult.Duplicates
    Mojibake = $baseMojibake.Count
    ObsoleteKeys = $baseObsolete.Count
})

$sourceKeys = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$pattern = '(?:UiText\.(?:Translate|Format)|\bT|\bF|\bSay)\(\s*"((?:\\.|[^"\\])*)"'
foreach ($file in Get-ChildItem -LiteralPath $projectRoot -Filter '*.cs' -Recurse)
{
    if ($file.FullName -match '\\(?:bin|obj|tests|packaging|Publicare[^\\]*)\\') { continue }
    $source = Get-Content -LiteralPath $file.FullName -Raw -Encoding utf8
    foreach ($match in [regex]::Matches($source, $pattern))
    {
        $key = [regex]::Unescape($match.Groups[1].Value)
        if ($key -ceq 'toate folderele') { $key = 'Toate folderele' }
        [void]$sourceKeys.Add($key)
    }
}

$xamlAttributes = 'Header', 'Text', 'Content', 'ToolTip', 'Title', 'AutomationProperties.Name', 'AutomationProperties.HelpText'
foreach ($file in Get-ChildItem -LiteralPath $projectRoot -Filter '*.xaml' -Recurse)
{
    if ($file.FullName -match '\\(?:bin|obj|tests|packaging|Publicare[^\\]*)\\') { continue }
    [xml]$document = Get-Content -LiteralPath $file.FullName -Raw -Encoding utf8
    foreach ($element in $document.SelectNodes('//*'))
    {
        foreach ($attribute in $element.Attributes)
        {
            if ($attribute.Name -notin $xamlAttributes) { continue }
            $value = [string]$attribute.Value
            if ($value -match '^\s*\{') { continue }
            if ($value -ceq 'toate folderele') { $value = 'Toate folderele' }
            [void]$sourceKeys.Add($value)
        }
    }
}

$missingFromBase = @($sourceKeys | Where-Object { -not $baseMap.ContainsKey($_) })
if ($missingFromBase.Count -gt 0 -or $baseResult.Duplicates -gt 0)
{
    $failed = $true
    Write-Output ([pscustomobject]@{
        Inventory = 'source-to-base'
        SourceKeys = $sourceKeys.Count
        Missing = $missingFromBase.Count
        DuplicateBaseKeys = $baseResult.Duplicates
        Examples = ($missingFromBase | Select-Object -First 5) -join ' | '
    })
}

foreach ($culture in $cultures)
{
    $path = Join-Path $resourcesRoot "UiStrings.$culture.resx"
    $cultureResult = Read-ResourceMap $path
    $map = $cultureResult.Map
    $missing = @($baseKeys | Where-Object { -not $map.ContainsKey($_) })
    $extra = @($map.Keys | Where-Object { -not $baseMap.ContainsKey($_) })
    $sourceMissing = @($sourceKeys | Where-Object { -not $map.ContainsKey($_) })
    $empty = @($map.Keys | Where-Object { [string]::IsNullOrWhiteSpace($map[$_]) })
    $formatErrors = 0
    $termErrors = 0
    $structureErrors = 0
    $untranslatedRomanian = 0

    foreach ($key in $baseKeys)
    {
        if (-not $map.ContainsKey($key)) { continue }
        $source = $baseMap[$key]
        $target = $map[$key]
        $sourceFormats = @([regex]::Matches($source, '\{\d+(?::[^}]+)?\}') | ForEach-Object { $_.Value } | Sort-Object)
        $targetFormats = @([regex]::Matches($target, '\{\d+(?::[^}]+)?\}') | ForEach-Object { $_.Value } | Sort-Object)
        if (($sourceFormats -join '|') -ne ($targetFormats -join '|')) { $formatErrors++ }
        if ($source.Contains('|'))
        {
            $sourcePipes = ($source.ToCharArray() | Where-Object { $_ -eq '|' }).Count
            $targetPipes = ($target.ToCharArray() | Where-Object { $_ -eq '|' }).Count
            $sourceWildcards = @([regex]::Matches($source, '\*\.[A-Za-z0-9*]+') | ForEach-Object { $_.Value } | Sort-Object)
            $targetWildcards = @([regex]::Matches($target, '\*\.[A-Za-z0-9*]+') | ForEach-Object { $_.Value } | Sort-Object)
            if ($sourcePipes -ne $targetPipes -or ($sourceWildcards -join '|') -ne ($targetWildcards -join '|')) { $structureErrors++ }
        }
        if ($source -match '[ăâîșțĂÂÎȘȚ]' -and $target -ceq $source) { $untranslatedRomanian++ }

        foreach ($term in $protectedTerms)
        {
            if (-not $source.Contains($term)) { continue }
            if ($term -eq 'Orizont')
            {
                $sourceCount = [regex]::Matches($source, '(?<!\p{L})Orizont(?!\p{L})').Count
                $targetCount = [regex]::Matches($target, '(?<!\p{L})Orizont(?!\p{L})').Count
                if ($sourceCount -ne $targetCount) { $termErrors++ }
            }
            elseif (-not $target.Contains($term)) { $termErrors++ }
        }
    }

    $internalMarkers = [regex]::Matches((Get-Content -LiteralPath $path -Raw -Encoding utf8), 'ZXQ|__[A-Z_]+__').Count
    $mojibake = @($map.Keys + $map.Values | Where-Object { [regex]::IsMatch($_, $mojibakePattern) }).Count
    $obsolete = @($obsoleteKeys | Where-Object { $map.ContainsKey($_) }).Count
    [pscustomobject]@{
        Culture = $culture
        Keys = $map.Count
        Missing = $missing.Count
        Extra = $extra.Count
        SourceKeysMissing = $sourceMissing.Count
        DuplicateKeys = $cultureResult.Duplicates
        Empty = $empty.Count
        FormatErrors = $formatErrors
        TermErrors = $termErrors
        StructureErrors = $structureErrors
        UntranslatedRomanian = $untranslatedRomanian
        InternalMarkers = $internalMarkers
        Mojibake = $mojibake
        ObsoleteKeys = $obsolete
    }

    if ($cultureResult.Duplicates -gt 0 -or $extra.Count -gt 0 -or $empty.Count -gt 0 -or $formatErrors -gt 0 -or $termErrors -gt 0 -or $structureErrors -gt 0 -or $untranslatedRomanian -gt 0 -or $internalMarkers -gt 0 -or $mojibake -gt 0 -or $obsolete -gt 0 -or $sourceMissing.Count -gt 0 -or ($RequireComplete -and $missing.Count -gt 0))
    {
        $failed = $true
    }
}

if ($failed) { exit 1 }
