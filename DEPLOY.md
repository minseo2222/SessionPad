# Deploying SessionPad

How to produce and verify a distributable SessionPad build. SessionPad is a
local-only desktop app: there is no server, no environment variables, and no
secrets involved in a release.

## Prerequisites

- Windows
- .NET 10 SDK
- PowerShell

## 1. Verify the source

```powershell
dotnet build SessionPad.sln -c Release -warnaserror
dotnet test SessionPad.sln -c Release --no-build --verbosity normal
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\test-release-scripts.ps1
```

All three commands must pass. Local script examples use Windows PowerShell with
an explicit process-only execution-policy bypass so they also work on machines
whose default policy blocks repository scripts. CI uses the preinstalled `pwsh`
shell instead; it runs the same helper tests and a framework-dependent packaging
smoke test. The smoke test exercises executable versions, manifest generation,
archive contents, and checksum validation without producing the much larger
self-contained package or uploading an artifact.

Packaging requires a clean Git working tree. The check includes tracked changes
and non-ignored untracked files; files ignored by Git, including `artifacts/` and
`.claude/settings.local.json`, do not make the tree dirty. Review `git status
--short --untracked-files=all` before packaging.

Run official packaging from a clean checkout or dedicated clean worktree. A
user-owned file such as `.claude/commands/goal.md` is intentionally not ignored by
the repository and can therefore block packaging in an existing worktree. If the
owner decides that file must remain local-only, they may manually add
`/.claude/commands/goal.md` to `.git/info/exclude`; release scripts do not make that
repository-local policy decision or modify Git excludes automatically.

## 2. Package

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\package-release.ps1 -Version 0.5.0-beta.1
```

This publishes a self-contained `win-x64` build, bundles `LICENSE.md` and
`PRIVACY.md`, writes `release-manifest.json`, and outputs to `artifacts/`
(git-ignored):

- `SessionPad-v<version>-win-x64.zip`
- `SessionPad-v<version>-win-x64.zip.sha256`

Pass `-FrameworkDependent` for a smaller zip that requires the .NET 10 Desktop
Runtime on the user's machine.

The script accepts SemVer, including prereleases such as
`0.5.0-beta.99-validation`. It maps the numeric core to four-part executable
versions (`0.5.0.0` for AssemblyVersion and FileVersion) and preserves the full
SemVer in ProductVersion/InformationalVersion. Each numeric component is limited
to `65534`, the CLR assembly metadata maximum (`UInt16.MaxValue - 1`) documented
for [AssemblyVersionAttribute](https://learn.microsoft.com/dotnet/api/system.reflection.assemblyversionattribute).
The only supported runtime is currently `win-x64`; unsupported or path-like
Runtime values fail before staging is removed or publish begins. The manifest
records the product, requested version, runtime, self-contained setting, and
current 40-character Git commit SHA. It also records whether the explicit
dirty-tree override was used, and intentionally omits a build timestamp.

Before publishing, the version-specific staging directory is resolved and
verified as a child of this repository's `artifacts/` directory, then removed and
recreated by `dotnet publish`. Other version directories and `artifacts/` itself
are not removed.

For a supervised local validation of intentionally uncommitted release-script
changes, use the explicit override:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\package-release.ps1 -Version 0.5.0-beta.99-validation -AllowDirty
```

Do not use `-AllowDirty` for an official release.

## 3. Smoke-test the artifact

Packaging fails with a non-zero exit before reporting success unless all of the
following automated checks pass:

- Staging contains `SessionPad.App.exe`, `LICENSE.md`, `PRIVACY.md`, and
  `release-manifest.json`.
- The executable FileVersion and ProductVersion match the requested SemVer policy.
- The zip and checksum file exist, the recorded SHA256 matches the zip, and all
  required files are present at the archive root.

To independently verify the checksum:

```powershell
$zip = "artifacts/SessionPad-v0.5.0-beta.1-win-x64.zip"
$recorded = ((Get-Content "$zip.sha256" -Raw).Trim() -split '\s+')[0]
$actual = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
$recorded -eq $actual
```

Extract the zip to a clean folder and run `SessionPad.App.exe`. Walk
`docs/06_QA_CHECKLIST.md` (at minimum: hotkey attach, per-window restore,
restart persistence) and check `docs/RELEASE_READINESS.md`.

## 4. Distribute

Upload the zip and the `.sha256` file to the chosen sales/distribution channel
and publish the checksum next to the download link.

Not yet in place (decide before/at launch):

- **Distribution channel** (Microsoft Store, direct sale, etc.) is not chosen.
- **Code signing.** The exe is unsigned; SmartScreen may warn on first run.
  A signing certificate or Store distribution removes this.
- **Installer/MSIX.** Only the portable zip exists today. Microsoft Store
  distribution would require MSIX packaging.
