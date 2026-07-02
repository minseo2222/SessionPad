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
```

Both must pass. CI (`.github/workflows/ci.yml`) runs the same on every push.

## 2. Package

```powershell
scripts/package-release.ps1 -Version 0.5.0-beta.1
```

This publishes a self-contained `win-x64` build, bundles `LICENSE.md` and
`PRIVACY.md`, and writes to `artifacts/` (git-ignored):

- `SessionPad-v<version>-win-x64.zip`
- `SessionPad-v<version>-win-x64.zip.sha256`

Pass `-FrameworkDependent` for a smaller zip that requires the .NET 10 Desktop
Runtime on the user's machine.

## 3. Smoke-test the artifact

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
