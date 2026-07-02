param(
    [Parameter(Mandatory = $true)][string]$Version,
    [switch]$FrameworkDependent,
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$stagePath = Join-Path $repoRoot "artifacts/SessionPad-v$Version"

$publishArgs = @{
    Version = $Version
    Runtime = $Runtime
}
if (-not $FrameworkDependent) {
    $publishArgs.SelfContained = $true
}
& (Join-Path $PSScriptRoot "publish-release.ps1") @publishArgs

Copy-Item (Join-Path $repoRoot "LICENSE.md") $stagePath
Copy-Item (Join-Path $repoRoot "PRIVACY.md") $stagePath

$zipPath = Join-Path $repoRoot "artifacts/SessionPad-v$Version-$Runtime.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}
Compress-Archive -Path (Join-Path $stagePath "*") -DestinationPath $zipPath

$hash = (Get-FileHash $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
$hashPath = "$zipPath.sha256"
"$hash  $(Split-Path $zipPath -Leaf)" | Out-File $hashPath -Encoding ascii

Write-Host "Package: $zipPath"
Write-Host "SHA256:  $hash ($hashPath)"
