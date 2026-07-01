# Release Readiness — v0.5 Paid Beta

A short go/no-go checklist before tagging the v0.5 paid beta. Every box should be
checked, or the gap explicitly accepted, before release.

## Build & Test

- [ ] `dotnet build SessionPad.sln -c Release -warnaserror` passes with no warnings.
- [ ] `dotnet test SessionPad.sln -c Release --no-build --verbosity normal` is green.
- [ ] CI on `main` is green for the release commit.

## Product Scope (no regressions, no scope creep)

- [ ] Still Windows-first WPF, local-only. No cloud, login, telemetry, AI, or sync.
- [ ] No browser extension, IDE extension, or screen/terminal/editor scraping introduced.
- [ ] Only user-entered notes are stored, under `%APPDATA%\SessionPad`.

## Core Behavior

- [ ] Global attach shortcut (default `Ctrl+Alt+N`) attaches to the focused window.
- [ ] Per-window notes restore correctly via `processName + normalizedWindowTitle`.
- [ ] Window following (WinEvent hook + polling fallback) tracks move/resize.
- [ ] Hide-on-minimize and show-on-restore work for the attached target.
- [ ] Persistence survives restart; atomic writes and per-note backups intact.
- [ ] Session manager (list / open / delete) and cross-session search work.
- [ ] Command Copy writes only on explicit click; clipboard is never read.

## Data Safety

- [ ] Corrupt/partial note JSON degrades gracefully (no crash, recreates default).
- [ ] Schema/compatibility tests pass (`CompatibilityTests`, `SessionMatcherTests`).
- [ ] Delete All Local Data removes notes and backups and resets to a safe default.

## Docs & Hygiene

- [ ] `CLAUDE.md`, `README.md`, and `docs/` describe the shipped behavior accurately.
- [ ] All markdown code fences are balanced (`git diff --check` is clean).
- [ ] Release notes for v0.5 are written and linked from the README.
- [ ] No `bin/`, `obj/`, `artifacts/`, secrets, or machine-specific files are committed.

## Manual Smoke (per `docs/06_QA_CHECKLIST.md`)

- [ ] Attach to VS Code, Windows Terminal, and a browser.
- [ ] Multi-monitor and 100% / 125% / 150% DPI sanity checks.
- [ ] No unexpected network calls during a normal session.

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
