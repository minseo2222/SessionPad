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

if ($SelfContained) {
    dotnet publish $projectPath -c Release -r $Runtime --self-contained true -o $publishPath
} else {
    dotnet publish $projectPath -c Release --self-contained false -o $publishPath
}

Write-Host "SessionPad publish output: $publishPath"
