# SessionPad

[![CI](https://github.com/minseo2222/SessionPad/actions/workflows/ci.yml/badge.svg)](https://github.com/minseo2222/SessionPad/actions/workflows/ci.yml)

SessionPad is a Windows-first local desktop utility that attaches a lightweight note pad to the app window you are working in.

The product goal is simple:

> This note belongs to the work window I am using right now.

SessionPad is local-first. It is not a cloud app, AI assistant, team tool, IDE plugin, browser extension, or full knowledge-management system.

## Features

- WPF desktop app targeting `net10.0-windows`, with Per-Monitor V2 DPI awareness.
- Chromeless floating shell: native rounded corners and DWM dark frame on Windows 11.
- Compact Note and Docked Tab views; dark and light themes; system tray icon.
- Pinned, Todo, Commands, and Notes sections with item reordering and inline note editing.
- Local JSON persistence with atomic writes and rolling per-note backups.
- Per-window note restore using `processName + normalizedWindowTitle`; VS Code, Cursor, and Windsurf match at the project level.
- Global attach shortcut (default `Ctrl+Alt+N`, configurable from a preset list in Settings).
- Manual drag attach by dragging the SessionPad handle near another app window.
- Optional auto-track mode (off by default): SessionPad follows whichever window you focus.
- WinEvent-hook-based following with low-frequency polling as a fallback; rapid title changes are debounced.
- Hide when the attached target is minimized, then show again when it is restored.
- Search across all session notes from Settings.
- Session manager in Settings: list saved sessions, open one, or delete it (note and
  backups included) with an inline confirm.
- Command Copy button that writes a user-entered command to the clipboard only after an explicit click.
- Local Data section with storage path, Open Folder, and Delete All Local Data controls.
- Unit tests (`dotnet test`) run in CI on every push.

## Run From Source

Prerequisites:

- Windows
- .NET 10 SDK

Run:

```powershell
dotnet run --project src/SessionPad.App/SessionPad.App.csproj
```

## Build

Debug build:

```powershell
dotnet build
```

Release build:

```powershell
dotnet build -c Release
```

## Publish A Local Release Build

The publish script writes output under `artifacts/`, which is ignored by Git.

```powershell
powershell -ExecutionPolicy Bypass -File scripts/publish-release.ps1
```

Default output:

```text
artifacts/SessionPad
```

Pass `-Version 0.3.2` to produce a versioned folder such as `artifacts/SessionPad-v0.3.2`.

The script creates a framework-dependent publish by default. It does not create an installer, MSIX package, or signed build.

## Hotkey Attach

Press `Ctrl+Alt+N` while another app is focused.

SessionPad detects the foreground window first, then shows/restores itself, loads or creates the matching window session, attaches beside the target window, and starts following that target.

Supported MVP target examples:

- VS Code
- Notepad
- Windows Terminal
- PowerShell / pwsh / console windows
- Browsers

## Drag Attach

In Compact Note, use the `Drag` handle and release SessionPad near another application window.

If a valid external app window is within the attach threshold, SessionPad loads or creates that window's note session and attaches beside it. SessionPad rejects its own windows, desktop background windows, taskbar/shell windows, and ambiguous explorer shell windows.

If no valid target is nearby, SessionPad stays where it is and shows a safe status.

## Per-window Notes

SessionPad stores notes per matched window identity.

The window identity key is:

```text
lower(processName) + "|" + normalizedWindowTitle
```

When you return to the same process/title and press `Ctrl+Alt+N`, SessionPad restores that session's note. Runtime HWND values, detected window state, attach state, and follow state are not stored in note JSON.

## Command Copy

Each saved command has a Copy action.

Clicking Copy writes only that command's text to the clipboard. SessionPad does not read the clipboard, paste into terminals, send keystrokes, or execute commands.

## Local Data

SessionPad stores local data under:

```text
%APPDATA%\SessionPad
```

Expected files include:

```text
%APPDATA%\SessionPad\
  sessions.index.json
  notes\
    default.json
    <sessionId>.json
```

The app shows this path in the Local Data section. Use Open Folder to inspect the files.

To delete local data from inside the app:

1. Open SessionPad.
2. In the Local Data section, click Delete All Local Data.
3. Confirm the warning dialog.

This removes saved SessionPad notes and sessions from this device only. The app resets to a safe default note and recreates local files on future edits.

## Privacy Principles

SessionPad is local-first:

- No login.
- No cloud sync.
- No telemetry.
- No AI features.
- No screen scraping.
- No terminal scraping.
- No automatic reading of editor, browser, terminal, or project contents.
- Clipboard is only written when the user clicks Copy on a command.
- Clipboard is not read.
- Only user-entered notes are stored.
- Local errors (e.g. a failed save or an unavailable shortcut) are shown on screen
  only; nothing is reported or sent anywhere.

The full data-behavior statement is in [`PRIVACY.md`](PRIVACY.md).

## License

SessionPad is proprietary software distributed under the terms in
[`LICENSE.md`](LICENSE.md) (beta draft).

## Known Limitations

- Window identity is currently `processName + normalizedWindowTitle`. Two windows
  with the same title share one note.
- VS Code workspace/project detection is not implemented.
- Windows Terminal tab detection is not implemented.
- Browser URL/tab detection is not implemented.
- UI Automation is not implemented.
- Automatic foreground-change restore is available as an opt-in setting
  ("Auto-track focused window"), off by default.
- Window following uses WinEvent hooks, with low-frequency polling as a fallback.
- Rapid window-title changes (e.g. a shell that puts the running command in its title)
  are debounced, and title changes that still resolve to the same pad no longer switch.
- The attach shortcut is configurable from a preset list, not free-form key capture.
- Per-Monitor V2 DPI awareness is enabled, so the pad renders crisply across
  mixed-DPI monitors. Multi-monitor positioning still benefits from manual checking
  on your specific setup.
- No installer or MSIX package yet.

See `docs/V0_3_0_RELEASE_NOTES.md` for the latest additions (item reordering,
auto-track, WinEvent following, note backups, search, and a configurable shortcut).

## Project Layout

```text
src/
  SessionPad.App/
    App.xaml
    MainWindow.xaml
    Models/
    Native/
    Services/
    ViewModels/
    Views/
docs/
scripts/
```
