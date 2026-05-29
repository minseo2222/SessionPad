# SessionPad v0.2.1

## Summary

SessionPad v0.2.1 adds automatic session switching. When the window you are attached to changes what it is showing, SessionPad now follows along and shows the matching pad on its own, without a manual re-attach.

This is a focused, additive release on top of v0.2.0. There are no data format changes, and the local-first, no-scraping foundation is unchanged.

## What's New

- Automatic session switching on title change: while SessionPad is attached to a window, it watches that window's title and switches to the matching pad when the title changes.
  - Windows Terminal: switching between tabs that have different titles (for example a renamed profile tab versus another) switches the pad automatically.
  - VS Code, Cursor, and Windsurf: moving between files in the same project keeps the same pad (project-level matching); moving to a different project switches the pad.
- Notes are saved before switching, so moving between contexts never loses what you wrote. Returning to a previous context restores its pad.

## How It Works

SessionPad already matches a pad to a window using the window title only. Previously this happened once, at attach time. Now the follow loop also re-reads the attached window's current title and, only when it changes, re-matches and loads the corresponding pad. Matching still uses the window title only; nothing inside the editor, terminal, or browser is read.

Switching is skipped while dragging to attach, while hidden to the tray, while the target window is minimized, and while SessionPad is not visible. A pad switch only occurs when the title actually changes, so unchanged tabs and steady titles do not cause repeated switching.

## Known Limitations

- Two tabs or windows with the same title (for example two default "Windows PowerShell" tabs) cannot be told apart and will share one pad. Giving tabs distinct titles makes switching work.
- A title that changes constantly (for example a shell that puts the running command or path in the title) can cause the pad to switch more than intended. Stable, distinct titles work best.
- Matching uses the window title only. There is no VS Code workspace detection, Windows Terminal tab-id detection, or browser URL detection.
- Windows only. No installer or MSIX package yet. No cloud sync, login, telemetry, AI, or UI Automation.
- Attached-window following uses polling, not WinEvent hooks.
- Multi-monitor and DPI behavior should still be manually verified.

## Privacy And Local-only Notes

Unchanged from v0.2.0. SessionPad stores only user-entered notes on the local machine.

- No login, cloud sync, telemetry, or AI.
- No screen scraping or terminal scraping.
- No automatic reading of editor, browser, terminal, or project contents. Only the window title is used for matching.
- Clipboard is only written when you click Copy. Clipboard is not read.
- Commands are not executed or pasted automatically.

## How To Run

From source:

```powershell
dotnet run --project src/SessionPad.App/SessionPad.App.csproj
```

From the release package:

1. Extract `SessionPad-v0.2.1.zip`.
2. Run `SessionPad.App.exe`.

The release package is framework-dependent and requires the .NET Desktop Runtime compatible with `net10.0-windows`.

To quit SessionPad fully, right-click the tray icon and choose Exit. Closing the window only hides it to the tray.

## Storage Location

```text
%APPDATA%\SessionPad\
  settings.json
  sessions.index.json
  notes\
    default.json
    <sessionId>.json
```

## Build And Publish Information

```powershell
dotnet build
dotnet build -c Release
powershell -ExecutionPolicy Bypass -File scripts/publish-release.ps1
```

Default publish output:

```text
artifacts/SessionPad-v0.1
```

Release zip:

```text
artifacts/SessionPad-v0.2.1.zip
```

Generated `artifacts/`, `publish/`, `bin/`, and `obj/` outputs are ignored by Git and should not be committed.
