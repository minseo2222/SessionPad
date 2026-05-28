# SessionPad v0.1.1

## Summary

SessionPad v0.1.1 builds on the v0.1.0 foundation. It turns SessionPad into a proper resident utility: it now lives in the system tray, runs as a single instance, can optionally start with Windows, and follows attached windows more smoothly.

This release does not change how notes are matched to windows or how data is stored. It remains local-first, with no installer, cloud sync, login, telemetry, AI, UI Automation, or app-specific deep integrations.

## New In v0.1.1

- Single instance: launching SessionPad again brings the existing window forward instead of opening a second copy.
- System tray presence: SessionPad now lives in the notification area and keeps running in the background.
- Close-to-tray: closing the window (X or Alt+F4) hides SessionPad to the tray instead of quitting, so the hotkey keeps working.
- Tray menu: Open, Open data folder, and Exit. Exit fully quits the app.
- Start with Windows (optional): a toggle in the Local Data section. When enabled, SessionPad starts silently to the tray on login.
- Smoother window following: tighter polling so the pad tracks the attached window more closely.
- Fix: SessionPad no longer pops back up after being hidden to the tray while attached to a visible window.

## Features

- Attach SessionPad to the current foreground window with `Ctrl+Alt+N`.
- Manually attach by dragging SessionPad near a valid external app window.
- Restore per-window notes using `processName + normalizedWindowTitle`.
- Follow the attached window when it moves or resizes.
- Hide SessionPad when the attached target is minimized.
- Restore and reposition SessionPad when the attached target is restored.
- Stay resident in the system tray; close the window to hide, use the tray Exit to quit.
- Optionally start with Windows and launch silently to the tray.
- Edit Pinned, Todo, Commands, and Notes sections.
- Check and uncheck TODO items.
- Copy user-entered command snippets with an explicit Copy button.
- Store notes as local JSON files.
- View the local data path from inside the app.
- Open the local data folder from inside the app or the tray menu.
- Delete all local SessionPad data with confirmation.

## Privacy And Local-only Notes

SessionPad v0.1.1 stores only user-entered notes on the local machine.

- No login.
- No cloud sync.
- No telemetry.
- No AI features.
- No screen scraping.
- No terminal scraping.
- No automatic reading of editor, browser, terminal, or project contents.
- Clipboard is only written when the user clicks Copy on a command.
- Clipboard is not read.
- Commands are not executed or pasted automatically.
- Start with Windows writes only the SessionPad executable path to the current-user startup registry key. No user notes or data are written to the registry.

## Known Limitations

- Windows only.
- No installer or MSIX package yet.
- No cloud sync.
- No login.
- No telemetry.
- No AI.
- No UI Automation.
- No VS Code workspace/project detection.
- No Windows Terminal tab detection.
- No browser URL/tab detection.
- No automatic foreground-change restore.
- Window identity is currently `processName + normalizedWindowTitle`.
- Attached-window following uses polling, not WinEvent hooks.
- Multi-monitor and DPI behavior should still be manually verified.

## How To Run

From source:

```powershell
dotnet run --project src/SessionPad.App/SessionPad.App.csproj
```

From the release package:

1. Extract `SessionPad-v0.1.1.zip`.
2. Run `SessionPad.App.exe`.

The release package is framework-dependent and requires the .NET Desktop Runtime compatible with `net10.0-windows`.

To quit SessionPad fully, right-click the tray icon and choose Exit. Closing the window only hides it to the tray.

## Storage Location

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

Runtime HWND values, detected window state, attach state, follow state, and drag status are not persisted.

## Delete Local Data

Inside the app:

1. Open SessionPad.
2. Find the Local Data section.
3. Click Delete All Local Data.
4. Confirm the warning dialog.

This removes SessionPad notes and session data from this device only. The app resets to a safe default note and recreates local files on future edits.

## Build And Publish Information

Debug build:

```powershell
dotnet build
```

Release build:

```powershell
dotnet build -c Release
```

Publish:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/publish-release.ps1
```

Default publish output:

```text
artifacts/SessionPad-v0.1
```

Release zip:

```text
artifacts/SessionPad-v0.1.1.zip
```

Generated `artifacts/`, `publish/`, `bin/`, and `obj/` outputs are ignored by Git and should not be committed.
