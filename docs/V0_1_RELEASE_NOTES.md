# SessionPad v0.1.0

## Summary

SessionPad v0.1.0 is the first local Windows release candidate. It provides a lightweight note pad that can attach to app windows, follow their position, and restore notes by basic window identity.

This release is focused on local-first MVP behavior. It does not include an installer, cloud sync, login, telemetry, AI features, UI Automation, or app-specific deep integrations.

## Features

- Attach SessionPad to the current foreground window with `Ctrl+Alt+N`.
- Manually attach by dragging SessionPad near a valid external app window.
- Restore per-window notes using `processName + normalizedWindowTitle`.
- Follow the attached window when it moves or resizes.
- Hide SessionPad when the attached target is minimized.
- Restore and reposition SessionPad when the attached target is restored.
- Edit Pinned, Todo, Commands, and Notes sections.
- Check and uncheck TODO items.
- Copy user-entered command snippets with an explicit Copy button.
- Store notes as local JSON files.
- View the local data path from inside the app.
- Open the local data folder from inside the app.
- Delete all local SessionPad data with confirmation.

## Privacy And Local-only Notes

SessionPad v0.1.0 stores only user-entered notes on the local machine.

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

1. Extract `SessionPad-v0.1.0.zip`.
2. Run `SessionPad.App.exe`.

The release package is framework-dependent and requires the .NET Desktop Runtime compatible with `net10.0-windows`.

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
artifacts/SessionPad-v0.1.0.zip
```

Generated `artifacts/`, `publish/`, `bin/`, and `obj/` outputs are ignored by Git and should not be committed.
