<#
.SYNOPSIS
Removes build output (bin/, obj/, artifacts/) from the SessionPad repository.

.DESCRIPTION
Deletes every bin/ and obj/ directory beneath the repository root and the top-level
artifacts/ folder. Source files are never touched. The script anchors on its own
location and refuses to run unless SessionPad.sln sits at the resolved repo root, so it
cannot delete directories in an unrelated working directory.

.PARAMETER WhatIf
List what would be removed without deleting anything.
#>
param(
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

# Safety guard: only ever operate on the SessionPad repo root.
if (-not (Test-Path (Join-Path $repoRoot "SessionPad.sln"))) {
    throw "Refusing to clean: SessionPad.sln not found at '$repoRoot'."
}

$targets = New-Object System.Collections.Generic.List[string]

# Top-level artifacts/.
$artifacts = Join-Path $repoRoot "artifacts"
if (Test-Path $artifacts) { $targets.Add((Resolve-Path $artifacts).Path) }

# Every bin/ and obj/ directory under the repo root (excluding .git).
Get-ChildItem -Path $repoRoot -Directory -Recurse -Force -Include "bin", "obj" |
    Where-Object { $_.FullName -notmatch '[\\/]\.git[\\/]' } |
    ForEach-Object { $targets.Add($_.FullName) }

if ($targets.Count -eq 0) {
    Write-Host "Nothing to clean."
    return
}

foreach ($path in $targets) {
    # A parent removal may have already deleted a nested bin/obj.
    if (-not (Test-Path -LiteralPath $path)) { continue }

    if ($WhatIf) {
        Write-Host "[WhatIf] Would remove: $path"
    } else {
        Write-Host "Removing: $path"
        Remove-Item -LiteralPath $path -Recurse -Force
    }
}

if (-not $WhatIf) {
    Write-Host "Clean complete."
}
