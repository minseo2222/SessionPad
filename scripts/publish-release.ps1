param(
    [switch]$SelfContained,
    [string]$Runtime = "win-x64",
    [string]$Version = "",
    [string]$OutputPath = ""
)

if (-not $OutputPath) {
    if ($Version) {
        $OutputPath = "artifacts/SessionPad-v$Version"
    } else {
        $OutputPath = "artifacts/SessionPad"
    }
}

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "src/SessionPad.App/SessionPad.App.csproj"
$publishPath = Join-Path $repoRoot $OutputPath

. (Join-Path $PSScriptRoot "release-common.ps1")

$Runtime = Assert-SupportedRuntime $Runtime

$versionArgs = @()
if ($Version) {
    $versionInfo = ConvertTo-ReleaseVersionInfo $Version
    $publishPath = Assert-SafeArtifactChildPath $repoRoot $publishPath
    if (Test-Path -LiteralPath $publishPath) {
        Remove-Item -LiteralPath $publishPath -Recurse -Force
    }

    $versionArgs = @(
        "-p:Version=$($versionInfo.FullVersion)"
        "-p:AssemblyVersion=$($versionInfo.NumericVersion)"
        "-p:FileVersion=$($versionInfo.NumericVersion)"
        "-p:InformationalVersion=$($versionInfo.FullVersion)"
        "-p:IncludeSourceRevisionInInformationalVersion=false"
    )
}

if ($SelfContained) {
    & dotnet publish $projectPath -c Release -r $Runtime --self-contained true -o $publishPath @versionArgs
} else {
    & dotnet publish $projectPath -c Release --self-contained false -o $publishPath @versionArgs
}
if ($LASTEXITCODE -ne 0) {
    throw "SessionPad publish failed with exit code $LASTEXITCODE."
}

Write-Host "SessionPad publish output: $publishPath"
