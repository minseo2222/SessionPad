$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot "release-common.ps1")

function Assert-Equal {
    param([object]$Expected, [object]$Actual, [string]$Message)

    if ($Expected -ne $Actual) {
        throw "$Message Expected '$Expected', got '$Actual'."
    }
}

function Assert-Throws {
    param([scriptblock]$Action, [string]$Message)

    try {
        & $Action
    } catch {
        return
    }

    throw $Message
}

$stable = ConvertTo-ReleaseVersionInfo "0.5.0"
Assert-Equal "0.5.0" $stable.FullVersion "Stable SemVer was not preserved."
Assert-Equal "0.5.0.0" $stable.NumericVersion "Stable numeric version was incorrect."

$prerelease = ConvertTo-ReleaseVersionInfo "0.5.0-beta.99-validation"
Assert-Equal "0.5.0-beta.99-validation" $prerelease.FullVersion "Prerelease SemVer was not preserved."
Assert-Equal "0.5.0.0" $prerelease.NumericVersion "Prerelease numeric version was incorrect."

$metadata = ConvertTo-ReleaseVersionInfo "2.10.3+build.7"
Assert-Equal "2.10.3+build.7" $metadata.FullVersion "Build metadata SemVer was not preserved."
Assert-Equal "2.10.3.0" $metadata.NumericVersion "Build metadata numeric version was incorrect."

$maximum = ConvertTo-ReleaseVersionInfo "65534.65534.65534"
Assert-Equal "65534.65534.65534.0" $maximum.NumericVersion "Maximum assembly version was rejected or converted incorrectly."

Assert-Throws { ConvertTo-ReleaseVersionInfo "0.5" } "An incomplete SemVer was accepted."
Assert-Throws { ConvertTo-ReleaseVersionInfo "01.5.0" } "A SemVer with a leading zero was accepted."
Assert-Throws { ConvertTo-ReleaseVersionInfo "65535.0.0" } "An out-of-range major version was accepted."
Assert-Throws { ConvertTo-ReleaseVersionInfo "0.65535.0" } "An out-of-range minor version was accepted."
Assert-Throws { ConvertTo-ReleaseVersionInfo "0.0.65535" } "An out-of-range patch version was accepted."

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$safePath = Assert-SafeArtifactChildPath $repoRoot (Join-Path $repoRoot "artifacts/SessionPad-v0.5.0")
Assert-Equal (Join-Path $repoRoot "artifacts/SessionPad-v0.5.0") $safePath "A valid staging path changed unexpectedly."
Assert-Throws { Assert-SafeArtifactChildPath $repoRoot $repoRoot } "The repository root was accepted as a staging path."
Assert-Throws { Assert-SafeArtifactChildPath $repoRoot (Join-Path $repoRoot "artifacts/..") } "The artifacts parent was accepted as a staging path."
Assert-Throws { Assert-SafeArtifactChildPath $repoRoot (Join-Path $repoRoot "outside") } "A path outside artifacts was accepted."

Assert-Equal "win-x64" (Assert-SupportedRuntime "win-x64") "The supported runtime was rejected."
Assert-Throws { Assert-SupportedRuntime "" } "An empty runtime was accepted."
Assert-Throws { Assert-SupportedRuntime ".." } "A parent traversal runtime was accepted."
Assert-Throws { Assert-SupportedRuntime "win/x64" } "A runtime containing a forward slash was accepted."
Assert-Throws { Assert-SupportedRuntime 'win\x64' } "A runtime containing a backslash was accepted."
Assert-Throws { Assert-SupportedRuntime 'C:\win-x64' } "An absolute Windows path runtime was accepted."
Assert-Throws { Assert-SupportedRuntime "win:x64" } "A runtime containing a forbidden Windows filename character was accepted."
Assert-Throws { Assert-SupportedRuntime "win-*" } "A runtime containing a wildcard was accepted."

Write-Host "Release script tests passed: SemVer bounds, artifact paths, and runtime validation."
