param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$statusPath = Join-Path $projectRoot 'docs\RELEASE-STATUS.json'
$status = Get-Content -LiteralPath $statusPath -Raw -Encoding utf8 | ConvertFrom-Json
$version = [version]$status.latestPublishedVersion
$tag = [string]$status.latestPublishedTag
$candidateVersion = [string]$status.nextCandidateVersion
if ($tag -ne "v$version") { throw "Tagul public ($tag) nu corespunde versiunii $version." }
if ($status.workingTreeStatus -ne 'unreleased-post-release-changes') { throw 'Working branch must be marked as containing unpublished changes.' }
if ($status.publishedBaselinePackageReplaced -ne $false) { throw 'The published baseline package must remain unchanged.' }

$sourceVersion = $version.ToString()
if (-not [string]::IsNullOrWhiteSpace($candidateVersion)) {
    $candidateStatus = [string]$status.candidateStatus
    $distributionCreated = [bool]$status.candidateDistributionCreated
    $validCandidateState =
        ($candidateStatus -eq 'preparation-in-progress' -and -not $distributionCreated) -or
        ($candidateStatus -eq 'distribution-prepared' -and $distributionCreated)
    if (-not $validCandidateState -or $status.candidateReleasePublished -ne $false) {
        throw 'Candidate must be either in preparation or locally packaged, and must remain unpublished.'
    }
    if ($candidateVersion -notmatch '^(?<core>\d+\.\d+\.\d+)(?:-preview\.\d+)?$') {
        throw "Candidate version has an unsupported format: $candidateVersion."
    }
    $sourceVersion = $Matches.core
    if ([version]$sourceVersion -le $version) { throw 'Candidate version must be newer than the latest public release.' }
}

$projectFile = Join-Path $projectRoot 'CititorRSS.Jaws.csproj'
$projectText = Get-Content -LiteralPath $projectFile -Raw -Encoding utf8
$projectVersionMatch = [regex]::Match($projectText, '<Version>(?<version>[^<]+)</Version>')
$expectedProjectVersion = if ([string]::IsNullOrWhiteSpace($candidateVersion)) { $version.ToString() } else { $candidateVersion }
if (-not $projectVersionMatch.Success -or $projectVersionMatch.Groups['version'].Value -ne $expectedProjectVersion) {
    throw "CititorRSS.Jaws.csproj version does not match expected source version $expectedProjectVersion."
}
$expectedFileVersion = "$sourceVersion.0"
foreach ($metadata in @(
    @{ Name = 'AssemblyVersion'; Expected = $expectedFileVersion },
    @{ Name = 'FileVersion'; Expected = $expectedFileVersion },
    @{ Name = 'InformationalVersion'; Expected = $expectedProjectVersion }
)) {
    $match = [regex]::Match($projectText, "<$($metadata.Name)>(?<value>[^<]+)</$($metadata.Name)>")
    if (-not $match.Success -or $match.Groups['value'].Value -ne $metadata.Expected) {
        throw "$($metadata.Name) in CititorRSS.Jaws.csproj does not match expected source version $expectedProjectVersion."
    }
}

$installerProject = Get-Content -LiteralPath (Join-Path $projectRoot 'packaging\installer\OrizontSetup.csproj') -Raw -Encoding utf8
foreach ($metadata in @(
    @{ Name = 'Version'; Expected = $expectedProjectVersion },
    @{ Name = 'AssemblyVersion'; Expected = $expectedFileVersion },
    @{ Name = 'FileVersion'; Expected = $expectedFileVersion },
    @{ Name = 'InformationalVersion'; Expected = $expectedProjectVersion }
)) {
    $match = [regex]::Match($installerProject, "<$($metadata.Name)>(?<value>[^<]+)</$($metadata.Name)>")
    if (-not $match.Success -or $match.Groups['value'].Value -ne $metadata.Expected) {
        throw "$($metadata.Name) in installer project does not match expected source version $expectedProjectVersion."
    }
}

$releaseNotesPath = Join-Path $projectRoot "RELEASE-NOTES-$version.md"
if (-not (Test-Path -LiteralPath $releaseNotesPath -PathType Leaf)) { throw "Lipsesc notele release-ului $tag." }
if (-not [string]::IsNullOrWhiteSpace($candidateVersion)) {
    $candidateCore = [regex]::Match($candidateVersion, '^(?<core>\d+\.\d+\.\d+)(?:-preview\.\d+)?$').Groups['core'].Value
    $candidateNotesPath = Join-Path $projectRoot "RELEASE-NOTES-$candidateCore.md"
    if (-not (Test-Path -LiteralPath $candidateNotesPath -PathType Leaf)) { throw "Lipsesc notele de lucru ale candidatului $candidateVersion." }
    $candidateNotes = Get-Content -LiteralPath $candidateNotesPath -Raw -Encoding utf8
    if ($candidateNotes -notmatch [regex]::Escape($candidateVersion)) {
        throw 'Candidate notes must identify the candidate version.'
    }
    if ($candidateStatus -eq 'distribution-prepared') {
        $artifactNames = @(
            [string]$status.candidatePortableArchive,
            [string]$status.candidateSourceArchive,
            [string]$status.candidateInstaller,
            [string]$status.candidatePortableHash,
            [string]$status.candidateSourceHash,
            [string]$status.candidateInstallerHash
        )
        if ($artifactNames | Where-Object { [string]::IsNullOrWhiteSpace($_) }) {
            throw 'Prepared candidate registry is missing one or more artifact names.'
        }
        foreach ($artifact in $artifactNames) {
            $artifactPath = Join-Path $projectRoot (Join-Path 'bin\Release' $artifact)
            if (-not (Test-Path -LiteralPath $artifactPath -PathType Leaf)) {
                throw "Prepared candidate artifact is missing: $artifact."
            }
        }
    }
}

$readme = Get-Content -LiteralPath (Join-Path $projectRoot 'README.md') -Raw -Encoding utf8
if ($readme -notmatch [regex]::Escape("releases/tag/$tag") -or $readme -notmatch [regex]::Escape("tree/$tag")) {
    throw "README.md must identify both the published release and exact source tag $tag."
}
foreach ($asset in @("OrizontSetup-$version.exe", "Orizont-RSS-$version-win-x64.zip")) {
    if ($readme -notmatch [regex]::Escape("releases/download/$tag/$asset")) {
        throw "README.md is missing the published asset $asset for release $tag."
    }
}
if ($readme -notmatch '(?i)nu fac parte din pachetele publice' -or $readme -notmatch '(?i)nu este binarul public') {
    throw 'README.md must distinguish the local candidate from the public package.'
}

$changelog = Get-Content -LiteralPath (Join-Path $projectRoot 'CHANGELOG.md') -Raw -Encoding utf8
if ($changelog -notmatch "(?i)Versiunea.{0,20}$([regex]::Escape($tag))" -or $changelog -notmatch '(?i)nepublicate') {
    throw 'CHANGELOG.md must separate the published release from unpublished work.'
}

$publication = Get-Content -LiteralPath (Join-Path $projectRoot 'PUBLICATION.md') -Raw -Encoding utf8
if ($publication -notmatch [regex]::Escape("releases/tag/$tag") -or $publication -notmatch '(?i)nu este pachetul public') {
    throw 'PUBLICATION.md must distinguish the published release from the local executable.'
}

$releaseNotes = @(Get-ChildItem -LiteralPath $projectRoot -File -Filter 'RELEASE-NOTES-*.md')
foreach ($file in $releaseNotes) {
    if ($file.BaseName -match '^RELEASE-NOTES-(?<version>\d+\.\d+\.\d+)$' -and [version]$Matches.version -lt $version) {
        throw "An obsolete release note remains in the tree: $($file.Name)."
    }
}

$manifestRoot = Join-Path $projectRoot 'packaging\winget\Grifnas.OrizontRSS'
$manifestDirectories = @(Get-ChildItem -LiteralPath $manifestRoot -Directory)
foreach ($directory in $manifestDirectories) {
    if ([version]$directory.Name -lt $version) { throw "An obsolete WinGet manifest remains in the tree: $($directory.Name)." }
}
$currentManifestDirectory = Join-Path $manifestRoot $version.ToString()
$expectedManifests = @(
    'Grifnas.OrizontRSS.yaml',
    'Grifnas.OrizontRSS.installer.yaml',
    'Grifnas.OrizontRSS.locale.en-US.yaml',
    'Grifnas.OrizontRSS.locale.ro-RO.yaml'
)
foreach ($name in $expectedManifests) {
    $path = Join-Path $currentManifestDirectory $name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Current manifest is missing: $name." }
    $manifest = Get-Content -LiteralPath $path -Raw -Encoding utf8
    if ($manifest -notmatch "(?m)^PackageVersion:\s*$([regex]::Escape($version.ToString()))\s*$") {
        throw "Versiunea din manifestul $name nu corespunde versiunii publicate $version."
    }
}

$candidateSummary = if ([string]::IsNullOrWhiteSpace($candidateVersion)) { 'no local candidate' } else { "local candidate $candidateVersion is $([string]$status.candidateStatus) and unpublished" }
Write-Output "Release consistency passed: $tag is the published baseline; $candidateSummary; old local release notes and WinGet snapshots are absent."
