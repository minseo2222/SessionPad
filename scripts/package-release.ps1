param(
    [Parameter(Mandatory = $true)][string]$Version,
    [switch]$FrameworkDependent,
    [string]$Runtime = "win-x64",
    [switch]$AllowDirty
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

. (Join-Path $PSScriptRoot "release-common.ps1")

$versionInfo = ConvertTo-ReleaseVersionInfo $Version
$Runtime = Assert-SupportedRuntime $Runtime
$stagePath = Join-Path $repoRoot "artifacts/SessionPad-v$Version"
$stagePath = Assert-SafeArtifactChildPath $repoRoot $stagePath
$gitDirty = Assert-CleanGitWorkingTree $repoRoot -AllowDirty:$AllowDirty

$gitCommit = (& git -C $repoRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $gitCommit -notmatch '^[0-9a-fA-F]{40}$') {
    throw "Unable to determine the current Git commit SHA for the release manifest."
}

$publishArgs = @{
    Version = $Version
    Runtime = $Runtime
}
if (-not $FrameworkDependent) {
    $publishArgs.SelfContained = $true
}
& (Join-Path $PSScriptRoot "publish-release.ps1") @publishArgs

$licensePath = Join-Path $repoRoot "LICENSE.md"
$privacyPath = Join-Path $repoRoot "PRIVACY.md"
Copy-Item -LiteralPath $licensePath -Destination $stagePath
Copy-Item -LiteralPath $privacyPath -Destination $stagePath

$manifest = [ordered]@{
    product = "SessionPad"
    version = $versionInfo.FullVersion
    runtime = $Runtime
    selfContained = -not $FrameworkDependent
    gitCommit = $gitCommit
    gitDirty = $gitDirty
}
$manifestPath = Join-Path $stagePath "release-manifest.json"
$manifest | ConvertTo-Json | Set-Content -LiteralPath $manifestPath -Encoding utf8

$requiredStageFiles = @("SessionPad.App.exe", "LICENSE.md", "PRIVACY.md", "release-manifest.json")
foreach ($fileName in $requiredStageFiles) {
    $requiredPath = Join-Path $stagePath $fileName
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Package staging validation failed: required file '$fileName' is missing from '$stagePath'."
    }
}

$exePath = Join-Path $stagePath "SessionPad.App.exe"
$exeVersion = (Get-Item -LiteralPath $exePath).VersionInfo
if ($exeVersion.FileVersion -ne $versionInfo.NumericVersion) {
    throw "Package staging validation failed: SessionPad.App.exe FileVersion '$($exeVersion.FileVersion)' does not match expected '$($versionInfo.NumericVersion)'."
}
if ($exeVersion.ProductVersion -ne $versionInfo.FullVersion) {
    throw "Package staging validation failed: SessionPad.App.exe ProductVersion '$($exeVersion.ProductVersion)' does not match requested version '$($versionInfo.FullVersion)'."
}

$zipPath = Assert-SafeArtifactChildPath $repoRoot (Join-Path $repoRoot "artifacts/SessionPad-v$Version-$Runtime.zip")
if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}
$hashPath = Assert-SafeArtifactChildPath $repoRoot "$zipPath.sha256"
if (Test-Path -LiteralPath $hashPath) {
    Remove-Item -LiteralPath $hashPath -Force
}
Compress-Archive -Path (Join-Path $stagePath "*") -DestinationPath $zipPath

if (-not (Test-Path -LiteralPath $zipPath -PathType Leaf)) {
    throw "Package validation failed: zip archive '$zipPath' was not created."
}

$hash = (Get-FileHash $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $(Split-Path $zipPath -Leaf)" | Out-File $hashPath -Encoding ascii

if (-not (Test-Path -LiteralPath $hashPath -PathType Leaf)) {
    throw "Package validation failed: checksum file '$hashPath' was not created."
}
$recordedHash = ((Get-Content -LiteralPath $hashPath -Raw).Trim() -split '\s+')[0]
$actualHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($recordedHash -ne $actualHash) {
    throw "Package validation failed: checksum '$recordedHash' does not match actual zip SHA256 '$actualHash'."
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $archiveFiles = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
    foreach ($fileName in $requiredStageFiles) {
        if ($archiveFiles -notcontains $fileName) {
            throw "Package validation failed: required file '$fileName' is missing from '$zipPath'."
        }
    }
} finally {
    $archive.Dispose()
}

Write-Host "Package: $zipPath"
Write-Host "SHA256:  $hash ($hashPath)"
Write-Host "Validated: executable versions, required files, archive contents, and checksum."
