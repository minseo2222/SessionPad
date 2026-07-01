# AGENTS.md

> **Active instruction file:** `CLAUDE.md` is the single source of truth for how to
> work in this repo. This `AGENTS.md` is retained for product identity and historical
> slice planning. Where it describes "Slice 1" constraints (e.g. "do not implement
> persistence / global hotkeys / Win32 tracking / UI Automation"), those are
> **historical** — persistence, hotkeys, and Win32 tracking now exist and are core
> features. See `CLAUDE.md` → Current State.

This repository contains SessionPad, a Windows-first local desktop utility.

## Product Identity

SessionPad is a lightweight local note pad that attaches to app windows.

The user should feel:

> “This note belongs to the work window I am using right now.”

SessionPad is not:

- A Notion replacement
- An Obsidian replacement
- A Sticky Notes clone
- An AI assistant
- A cloud sync app
- A team collaboration app
- A dashboard app
- A browser extension
- An IDE plugin

## Hard Constraints

Do not implement these in the MVP unless explicitly requested:

- AI features
- Cloud sync
- Login/account system
- Collaboration
- Telemetry
- Browser extension
- IDE extension
- Markdown/block editor
- UI Automation smart binding
- VS Code workspace detection
- Windows Terminal tab detection
- Screen capture
- OCR
- Terminal output scraping
- Automatic reading of another app’s internal content

SessionPad stores only user-entered notes.

## Preferred Stack

Use WPF and C# for the Windows MVP unless the repository already contains a different working stack.

Preferred target framework:

- `net10.0-windows`, if installed
- fallback to `net8.0-windows` only when .NET 10 SDK is unavailable

Do not add a `global.json` unless the installed SDK version is confirmed.

## Implementation Order

Follow the slices in `docs/05_CODEX_SLICES.md`.

The first slice is intentionally narrow:

- Create a buildable WPF app.
- Add a minimal floating note window.
- Add placeholder UI for:
  - Pinned
  - Todo
  - Commands
  - Notes
- Add collapsed Docked Tab and expanded Compact Note states.
- Do not implement persistence.
- Do not implement global hotkeys.
- Do not implement Win32 external window tracking.
- Do not implement UI Automation.

## Code Style

Use:

- C#
- Nullable reference types
- `ImplicitUsings`
- Simple MVVM-style separation where useful
- Clear service boundaries
- Small classes
- No premature abstractions

Prefer readable code over clever code.

## Project Layout

Expected layout:

```text
src/
  SessionPad.App/
    App.xaml
    MainWindow.xaml
    Views/
    ViewModels/
    Models/
    Services/
    Native/
docs/
```

`Native/` should be reserved for later Win32 P/Invoke declarations.

## Build and Verification

After implementation, run:

```
dotnet build
```

If tests are added later, run:

```
dotnet test
```

> Historical (Slice 1 only). For the original scaffold slice, manual verification was
> enough:
>
> - App launches.
> - A SessionPad window appears.
> - User can switch between Docked Tab and Compact Note states.
> - Compact Note shows Pinned, Todo, Commands, Notes sections.
> - No persistence, hotkey, or external window tracking was implemented yet.

## Documentation Discipline

When product or architecture decisions change, update the relevant file in `docs/`.

Do not silently expand MVP scope.

## Git Workflow

- After each completed implementation task or slice, run dotnet build.
- Only commit if the build passes.
- Use a clear commit message describing the slice or fix.
- Push to origin main after each successful commit.
- Never commit bin/, obj/, secrets, credentials, tokens, or machine-specific files.
- Never force push.
- If push/authentication fails, report the error clearly and stop.
- Do not auto-push unfinished or failing work.
