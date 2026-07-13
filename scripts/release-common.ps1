Set-StrictMode -Version Latest

function ConvertTo-ReleaseVersionInfo {
    param(
        [Parameter(Mandatory = $true)][string]$Version
    )

    $semVerPattern = '^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<prerelease>(?:0|[1-9]\d*|[0-9A-Za-z-]*[A-Za-z-][0-9A-Za-z-]*)(?:\.(?:0|[1-9]\d*|[0-9A-Za-z-]*[A-Za-z-][0-9A-Za-z-]*))*))?(?:\+(?<metadata>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$'
    if ($Version -cnotmatch $semVerPattern) {
        throw "Invalid release version '$Version'. Expected SemVer such as 0.5.0 or 0.5.0-beta.99-validation."
    }

    $maximumAssemblyVersionComponent = 65534
    foreach ($componentName in @("major", "minor", "patch")) {
        $component = $Matches[$componentName]
        if ($component.Length -gt 5 -or [int]$component -gt $maximumAssemblyVersionComponent) {
            throw "Invalid release version '$Version'. The $componentName component must be between 0 and $maximumAssemblyVersionComponent for .NET AssemblyVersion metadata."
        }
    }

    [pscustomobject]@{
        FullVersion = $Version
        NumericVersion = "$($Matches.major).$($Matches.minor).$($Matches.patch).0"
    }
}

function Assert-SupportedRuntime {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Runtime
    )

    if ($Runtime -cne "win-x64") {
        throw "Unsupported release runtime '$Runtime'. SessionPad releases currently support only 'win-x64'."
    }

    return $Runtime
}

function Assert-SafeArtifactChildPath {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $true)][string]$Path
    )

    $artifactsRoot = [IO.Path]::GetFullPath((Join-Path $RepoRoot "artifacts"))
    $candidate = [IO.Path]::GetFullPath($Path)
    $artifactsPrefix = $artifactsRoot.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar

    if (-not $candidate.StartsWith($artifactsPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Unsafe artifact path '$candidate'. The path must be a child of '$artifactsRoot'."
    }

    return $candidate
}

function Assert-CleanGitWorkingTree {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [switch]$AllowDirty
    )

    $statusLines = @(& git -C $RepoRoot status --porcelain --untracked-files=all)
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to inspect the Git working tree. 'git status' exited with code $LASTEXITCODE."
    }

    if ($statusLines.Count -gt 0 -and -not $AllowDirty) {
        $details = $statusLines -join [Environment]::NewLine
        throw "Release packaging requires a clean Git working tree. Review these changes or rerun with -AllowDirty for an explicit local validation override:$([Environment]::NewLine)$details"
    }

    return $statusLines.Count -gt 0
}
