# Release Readiness — v0.5 Paid Beta

A short go/no-go checklist before tagging the v0.5 paid beta. Every box should be
checked, or the gap explicitly accepted, before release.

Checked boxes below were verified on 2026-07-02 on `feature/v0.5-beta-hardening`.
Items marked **(manual)** need a human pass on a real machine before tagging.

## Build & Test

- [x] `dotnet build SessionPad.sln -c Release -warnaserror` passes with no warnings.
- [x] `dotnet test SessionPad.sln -c Release --no-build --verbosity normal` is green (78/78).
- [x] `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\test-release-scripts.ps1`
      passes (SemVer bounds, safe artifact paths, and runtime validation).
- [x] CI green: latest `main` push and the v0.5 branch PR run both passed.
      Re-check CI for the final release commit after merge.

## Product Scope (no regressions, no scope creep)

- [x] Still Windows-first WPF, local-only. No cloud, login, telemetry, AI, or sync
      (no `HttpClient`/socket/network code exists anywhere in `src/`).
- [x] No browser extension, IDE extension, or screen/terminal/editor scraping introduced.
- [x] Only user-entered notes are stored, under `%APPDATA%\SessionPad`.

## Core Behavior

- [ ] **(manual)** Global attach shortcut (default `Ctrl+Alt+N`) attaches to the focused window.
- [ ] **(manual)** Per-window notes restore correctly via `processName + normalizedWindowTitle`.
- [ ] **(manual)** Window following (WinEvent hook + polling fallback) tracks move/resize.
- [ ] **(manual)** Hide-on-minimize and show-on-restore work for the attached target.
- [ ] **(manual)** Persistence survives restart; atomic writes and per-note backups intact
      (storage logic is unit-tested; the restart pass itself is manual).
- [ ] **(manual)** Session manager (list / open / delete) and cross-session search work.
- [ ] **(manual)** Command Copy writes only on explicit click; clipboard is never read.

## Data Safety

- [x] Corrupt/partial note JSON degrades gracefully (no crash, recreates default) —
      covered by `CompatibilityTests`, including traversal/malicious index entries.
- [x] Schema/compatibility tests pass (`CompatibilityTests`, `SessionMatcherTests`).
- [ ] **(manual)** Delete All Local Data removes notes and backups and resets to a safe default.

## Docs & Hygiene

- [x] `CLAUDE.md`, `README.md`, and `docs/` describe the shipped behavior accurately.
- [x] All markdown code fences are balanced (`git diff --check` is clean).
- [x] Release notes for v0.5 are written (`docs/V0_5_0_RELEASE_NOTES.md`) and linked from the README.
- [x] No `bin/`, `obj/`, `artifacts/`, secrets, or machine-specific files are committed.

## Distribution (added for the paid beta)

- [x] `LICENSE.md` (proprietary draft) and `PRIVACY.md` exist and ship inside the zip.
- [x] `scripts/package-release.ps1` produces and verifies a self-contained zip +
      SHA256 checksum (exe, LICENSE, PRIVACY, and release manifest present; executable
      versions and manifest Git commit/dirty state match the requested release inputs).
- [ ] Sales/distribution channel chosen (Microsoft Store vs direct sale) — **user decision**.
- [ ] License draft reviewed by a human (ideally a lawyer) before charging money.
- [ ] Gap accepted or resolved: unsigned exe (SmartScreen warning) and no installer/MSIX.

## Manual Smoke (per `docs/06_QA_CHECKLIST.md`)

- [ ] **(manual)** Attach to VS Code, Windows Terminal, and a browser.
- [ ] **(manual)** Multi-monitor and 100% / 125% / 150% DPI sanity checks.
- [ ] **(manual)** No unexpected network calls during a normal session
      (statically verified: no network code in `src/`; runtime pass is manual).

## Beta UX & Accessibility Polish

- [ ] Icon-only controls expose an accessible name (Narrator / Accessibility Insights):
      Settings, Collapse, Hide-to-tray, add-item (+/›) buttons, per-item Copy, Delete,
      Move up/down, note Expand, the to-do toggle, the Drag handle, and the docked tab.
- [ ] Tabs (Key / To-do / Commands / Notes), the search box, the rename box, the
      per-section input fields, and the Attach-shortcut combo announce a name.
- [ ] Empty-state copy reads well on a fresh install for every tab (Key, To-do,
      Commands, Notes) and for the empty Sessions list.
- [ ] Settings → Local Data clearly states: stored locally on this device, no account,
      no cloud sync, no telemetry.
- [ ] Hotkey failure messages are user-friendly: applying an in-use shortcut keeps the
      previous one with a clear message; when none can be registered the status says no
      shortcut is active and to pick another. No raw "Win32 error N" is shown to the user.
- [ ] Visual design and layout are unchanged (no new animations, no template redesign).
