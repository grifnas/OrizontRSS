#requires -Version 5.1
[CmdletBinding()]
param(
    [switch]$NoSave
)

$script:ApiUri = 'https://api.github.com/repos/grifnas/OrizontRSS/releases?per_page=100'
$script:HistoryPath = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'OrizontRSS-DownloadMonitor\history.json'

function Get-OrizontDownloadRows {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [object[]]$Releases
    )

    $rows = [System.Collections.Generic.List[object]]::new()
    foreach ($release in $Releases) {
        $tag = [string]$release.tag_name
        foreach ($asset in @($release.assets)) {
            $name = [string]$asset.name
            $isInstaller = $name -match '^OrizontSetup(?:-[0-9.]+)?\.exe$'
            $isPortable = $name -match '^Orizont-RSS-[0-9.]+-win-x64\.zip$'
            if (-not ($isInstaller -or $isPortable)) { continue }

            $count = 0L
            if ($null -ne $asset.download_count) { $count = [long]$asset.download_count }
            $rows.Add([pscustomobject]@{
                Release = $tag
                File = $name
                Downloads = $count
            })
        }
    }

    return @($rows | Sort-Object -Property @{ Expression = 'Release'; Descending = $true }, @{ Expression = 'File'; Descending = $false })
}

function Get-OrizontDownloadTotal {
    [CmdletBinding()]
    param(
        [AllowEmptyCollection()]
        [object[]]$Rows = @()
    )

    $total = 0L
    foreach ($row in $Rows) { $total += [long]$row.Downloads }
    return $total
}

function Write-OrizontDownloadReport {
    [CmdletBinding()]
    param(
        [AllowEmptyCollection()]
        [object[]]$Rows = @(),
        [object]$Previous
    )

    Write-Output 'GitHub downloads for Orizont RSS'
    Write-Output 'Counts include installer EXE and portable win-x64 ZIP assets; source archives and SHA-256 files are excluded.'

    $releaseGroups = @($Rows | Group-Object -Property Release | Sort-Object -Property Name -Descending)
    foreach ($releaseGroup in $releaseGroups) {
        $tag = [string]$releaseGroup.Name
        $releaseRows = @($releaseGroup.Group | Sort-Object -Property File)
        Write-Output ("Release {0}: {1} downloads" -f $tag, (Get-OrizontDownloadTotal -Rows $releaseRows))
        foreach ($row in $releaseRows) {
            Write-Output ("  {0}: {1}" -f $row.File, $row.Downloads)
        }
    }

    $currentTotal = Get-OrizontDownloadTotal -Rows $Rows
    Write-Output ("Cumulative application-package downloads: {0}" -f $currentTotal)

    if ($null -eq $Previous) {
        Write-Output 'No previous local check is saved. This run will establish the comparison baseline.'
        return
    }

    $previousRows = @($Previous.items)
    $previousTotal = Get-OrizontDownloadTotal -Rows $previousRows
    $change = $currentTotal - $previousTotal
    $previousDate = [string]$Previous.checkedAt
    Write-Output ("Change since {0}: {1}{2}" -f $previousDate, $(if ($change -gt 0) { '+' } else { '' }), $change)

    $previousByKey = @{}
    foreach ($row in $previousRows) {
        $key = '{0}|{1}' -f $row.Release, $row.File
        $previousByKey[$key] = [long]$row.Downloads
    }

    $currentByKey = @{}
    $changes = [System.Collections.Generic.List[string]]::new()
    foreach ($row in $Rows) {
        $key = '{0}|{1}' -f $row.Release, $row.File
        $currentByKey[$key] = [long]$row.Downloads
        if (-not $previousByKey.ContainsKey($key)) {
            $changes.Add(("New asset {0} ({1}): {2}" -f $row.Release, $row.File, $row.Downloads))
            continue
        }

        $assetChange = [long]$row.Downloads - [long]$previousByKey[$key]
        if ($assetChange -ne 0) {
            $prefix = if ($assetChange -gt 0) { '+' } else { '' }
            $changes.Add(("{0} / {1}: {2}{3}" -f $row.Release, $row.File, $prefix, $assetChange))
        }
    }

    foreach ($key in $previousByKey.Keys) {
        if (-not $currentByKey.ContainsKey($key)) {
            $changes.Add(("Previously listed asset is no longer present: {0}" -f $key))
        }
    }

    if ($changes.Count -eq 0) {
        Write-Output 'No per-file changes since the previous check.'
    }
    else {
        Write-Output 'Per-file changes:'
        foreach ($changeLine in $changes) { Write-Output ("  {0}" -f $changeLine) }
    }
}

function Read-OrizontDownloadHistory {
    if (-not (Test-Path -LiteralPath $script:HistoryPath -PathType Leaf)) { return $null }

    try {
        $history = Get-Content -LiteralPath $script:HistoryPath -Raw -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop
        if ($history.schemaVersion -ne 1 -or $null -eq $history.items) {
            throw 'The history file has an unsupported structure.'
        }
        return $history
    }
    catch {
        throw ("The saved comparison history could not be read, so it was not replaced. Details: {0}" -f $_.Exception.Message)
    }
}

function Save-OrizontDownloadHistory {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Rows
    )

    $folder = Split-Path -Parent $script:HistoryPath
    [void](New-Item -ItemType Directory -Path $folder -Force)
    $snapshot = [pscustomobject]@{
        schemaVersion = 1
        checkedAt = [DateTime]::UtcNow.ToString('o')
        items = @($Rows)
    }
    $temporaryPath = '{0}.{1}.tmp' -f $script:HistoryPath, [Guid]::NewGuid().ToString('N')
    try {
        $json = ConvertTo-Json -InputObject $snapshot -Depth 6
        Set-Content -LiteralPath $temporaryPath -Value $json -Encoding UTF8 -ErrorAction Stop
        Move-Item -LiteralPath $temporaryPath -Destination $script:HistoryPath -Force -ErrorAction Stop
    }
    finally {
        if (Test-Path -LiteralPath $temporaryPath -PathType Leaf) {
            Remove-Item -LiteralPath $temporaryPath -Force -ErrorAction SilentlyContinue
        }
    }
}

function Invoke-OrizontDownloadCheck {
    [CmdletBinding()]
    param([switch]$SkipSave)

    try {
        if ($PSVersionTable.PSVersion.Major -le 5) {
            [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        }

        $headers = @{
            Accept = 'application/vnd.github+json'
            'User-Agent' = 'OrizontRSS-DownloadMonitor'
        }
        if ($PSVersionTable.PSVersion.Major -le 5) {
            $response = Invoke-WebRequest -Uri $script:ApiUri -Headers $headers -TimeoutSec 30 -UseBasicParsing -ErrorAction Stop
        }
        else {
            $response = Invoke-WebRequest -Uri $script:ApiUri -Headers $headers -TimeoutSec 30 -ErrorAction Stop
        }

        # Parse the JSON body explicitly. Invoke-RestMethod can enumerate or
        # reshape top-level arrays differently across PowerShell versions.
        $releases = @(ConvertFrom-Json -InputObject ([string]$response.Content) -ErrorAction Stop)
        if ($releases.Count -eq 0) { throw 'GitHub returned no published releases.' }

        $rows = @(Get-OrizontDownloadRows -Releases $releases)
        if ($rows.Count -eq 0) { throw 'No installer or portable package assets were found in the release list.' }

        $previous = Read-OrizontDownloadHistory
        Write-OrizontDownloadReport -Rows $rows -Previous $previous

        if ($SkipSave) {
            Write-Output 'NoSave mode: the local comparison history was not changed.'
        }
        else {
            Save-OrizontDownloadHistory -Rows $rows
            Write-Output ("Comparison history saved locally at: {0}" -f $script:HistoryPath)
        }
    }
    catch {
        [Console]::Error.WriteLine(("Could not retrieve the GitHub download counts. The saved history was not changed. Details: {0}" -f $_.Exception.Message))
        exit 1
    }
}

if ($MyInvocation.InvocationName -ne '.') {
    Invoke-OrizontDownloadCheck -SkipSave:$NoSave
}
