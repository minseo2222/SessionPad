# CLAUDE.md

This is the single active instruction file for Claude Code (and any other AI coding
agent) working on SessionPad. When this file conflicts with older guidance, **this
file wins**. `AGENTS.md` and the files under `docs/` are retained for product identity,
architecture context, and historical slice planning — not as live instructions.

## Product Identity

SessionPad is a **Windows-first WPF local desktop utility**. It attaches a lightweight
note pad to the app/window you are working in, so the user feels:

> "This note belongs to the work window I am using right now."

Non-negotiable product constraints (MVP and v0.5 beta):

- Local-first. **No cloud, no login/account, no telemetry, no AI, no sync.**
- No browser extension, no IDE extension, no screen/terminal/editor scraping.
- Stores only user-entered notes, as local JSON under `%APPDATA%\SessionPad`.

SessionPad is not a Notion/Obsidian/Sticky Notes replacement, not a team or
collaboration tool, and not a dashboard.

## Current State — Do Not Regress

These features are **already implemented and shipped** (v0.1 – v0.4). They are NOT
future work. Do not remove, disable, or reintroduce "do not implement" guidance for
them:

- Local JSON persistence with atomic writes and rolling per-note backups.
- Global attach shortcut (default `Ctrl+Alt+N`, configurable from a preset list).
- Win32 external window tracking: foreground detection, attach beside target,
  WinEvent-hook following with low-frequency polling fallback, hide-on-minimize.
- Per-window note restore via `processName + normalizedWindowTitle` (project-level
  matching for VS Code, Cursor, Windsurf).
- Compact Note / Docked Tab views, dark and light themes, system tray icon,
  cross-session search, session manager, command copy, and Local Data controls.

> **Historical note:** Earlier "Slice 1" guidance in `AGENTS.md` and `docs/` says
> "do not implement persistence / global hotkeys / Win32 tracking / UI Automation."
> That constraint applied **only to the original scaffold slice**. Persistence,
> hotkeys, and Win32 tracking now exist and are core features. Treat those old
> "do not implement" lines as historical, never as active instructions.

## Build & Test

Release build (warnings are errors):

```
dotnet build SessionPad.sln -c Release -warnaserror
```

Run the tests against that release build:

```
dotnet test SessionPad.sln -c Release --no-build --verbosity normal
```

Both the build and the tests must pass before you commit.

## Working Rules

- **Tests first for bug fixes.** Reproduce the bug with a failing test, then make it
  pass. (See `tests/SessionPad.Tests`.)
- **Small, focused diffs.** Every changed line should trace directly to the request.
  Do not refactor or "improve" unrelated code, comments, or formatting.
- **Do not touch** `bin/`, `obj/`, `artifacts/`, `.git/`, or local machine settings.
- **Do not introduce** cloud, AI, account, telemetry, browser extension, IDE
  extension, or screen/terminal scraping. These are out of scope by product design.
- Match the existing style and service boundaries; prefer readable code over clever
  code; no premature abstractions.

## Engineering Discipline

**Think before coding.** State assumptions explicitly; if uncertain, ask. If multiple
interpretations exist, surface them instead of silently picking one. If a simpler
approach exists, say so.

**Simplicity first.** Minimum code that solves the problem. No speculative features,
abstractions, configurability, or error handling for impossible scenarios.

**Surgical changes.** Touch only what you must. Remove only the imports/variables your
own change orphaned; mention pre-existing dead code rather than deleting it.

**Goal-driven execution.** Turn each task into a verifiable goal and loop until the
build and tests confirm it. For multi-step work, state a brief plan with a verify
check per step.
