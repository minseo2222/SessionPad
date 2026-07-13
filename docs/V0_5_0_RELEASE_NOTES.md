# SessionPad v0.5.0-beta

## Summary

Hardening release preparing the paid beta: storage and hotkey robustness, a
centralized window-target policy, settings-layer refactoring with view-model
tests, accessibility and first-run polish, and the first distribution packaging
(license, privacy statement, portable zip with checksum). No data format changes.

## What's New

- **Storage hardening.** Note paths are validated against `%APPDATA%\SessionPad`;
  malicious or traversal index entries are skipped instead of followed, and a
  valid session still loads when another index entry is malformed.
- **Hotkey registration fallback.** Applying a shortcut that cannot be registered
  keeps the previous working shortcut with a clear, non-technical message; when
  none can be registered the status says so and suggests picking another.
- **Centralized window target policy.** All external-window acceptance rules
  (shell/taskbar/desktop rejection, own-window rejection, ambiguous explorer
  windows, minimized/invisible/zero-size windows) live in one tested policy.
- **Settings view-model extraction.** Theme, start-on-login, and hotkey flows are
  now in `SettingsPanelViewModel` with unit tests, including failure-revert
  behavior for start-on-login.
- **Accessibility & first-run polish.** Icon-only controls expose accessible
  names; tabs, search, rename, and input fields announce names; empty-state copy
  reads well on a fresh install.
- **Distribution packaging.** `scripts/package-release.ps1` produces a versioned
  self-contained portable zip under `artifacts/` with `LICENSE.md` and
  `PRIVACY.md` bundled and a SHA256 checksum file.
- **Legal/privacy docs.** Proprietary license draft (`LICENSE.md`, marked as
  needing legal review) and a full local-only privacy statement (`PRIVACY.md`).

## Why

v0.5 is the first release intended for paying beta users. That raises the bar on
data safety (never follow a hostile index entry), failure behavior (a hotkey
conflict must not strand the user), and trust (explicit license and privacy
terms in the box).

## Tests

107 unit tests total, including deterministic coverage for window placement
boundaries and attachment-result state invariants, plus path traversal rejection,
hotkey registration fallback, window target policy, and the settings view-model.

## Known Limitations / Privacy

Unchanged product behavior: local-first; window title only; clipboard written
only on Copy. Distribution is a portable zip — no installer or MSIX yet, and the
executable is unsigned, so Windows SmartScreen may warn on first run.

## How To Run

```powershell
dotnet run --project src/SessionPad.App/SessionPad.App.csproj
```

```powershell
dotnet test
```
