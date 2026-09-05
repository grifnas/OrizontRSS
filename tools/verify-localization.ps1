param(
    [switch]$RequireComplete
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$resourcesRoot = Join-Path $projectRoot 'Resources'
$basePath = Join-Path $resourcesRoot 'UiStrings.resx'

[xml]$baseDocument = Get-Content -LiteralPath $basePath -Raw -Encoding utf8
$baseKeys = @($baseDocument.root.data | ForEach-Object { [string]$_.name })
$cultures = 'en-US', 'es-ES', 'fr-FR', 'de-DE', 'pt-BR'
$failed = $false
$protectedTerms = 'Orizont', 'RSS', 'OPML', 'SAPI5', 'Gemini', 'Ctrl', 'Shift', 'Alt', 'https://', 'http://'

foreach ($culture in $cultures)
{
    $path = Join-Path $resourcesRoot "UiStrings.$culture.resx"
    [xml]$document = Get-Content -LiteralPath $path -Raw -Encoding utf8
    $keys = @($document.root.data | ForEach-Object { [string]$_.name })
    $missing = @($baseKeys | Where-Object { $_ -notin $keys })
    $extra = @($keys | Where-Object { $_ -notin $baseKeys })
    $empty = @($document.root.data | Where-Object { [string]::IsNullOrWhiteSpace([string]$_.value) })
    $formatErrors = 0
    $termErrors = 0
    $structureErrors = 0
    foreach ($baseNode in $baseDocument.root.data)
    {
        $source = [string]$baseNode.value
        $targetNode = $document.root.data | Where-Object { [string]$_.name -eq [string]$baseNode.name } | Select-Object -First 1
        if ($null -eq $targetNode) { continue }
        $target = [string]$targetNode.value
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

    [pscustomobject]@{
        Culture = $culture
        Translated = $keys.Count
        Missing = $missing.Count
        Extra = $extra.Count
        Empty = $empty.Count
        FormatErrors = $formatErrors
        TermErrors = $termErrors
        StructureErrors = $structureErrors
        InternalMarkers = $internalMarkers
    }

    if ($extra.Count -gt 0 -or $empty.Count -gt 0 -or $formatErrors -gt 0 -or $termErrors -gt 0 -or $structureErrors -gt 0 -or $internalMarkers -gt 0 -or ($RequireComplete -and $missing.Count -gt 0)) { $failed = $true }
}

if ($failed) { exit 1 }
