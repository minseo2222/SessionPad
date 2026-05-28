# SessionPad v0.1.2

## Summary

SessionPad v0.1.2 adds manual session control on top of v0.1.1. You can now rename the pad for a window, and pin a pad to an app so it stays the same even when the window title changes (for example, when a browser navigates to a different page).

This release does not change how data is stored. It remains local-first, with no installer, cloud sync, login, telemetry, AI, UI Automation, or app-specific deep integrations.

## New In v0.1.2

- Rename session: give the current window's pad a custom name. The name is kept even when you return to the same window, so automatic title-based naming no longer overwrites it.
- Pin to this app: pin the current pad to its app. While pinned, the pad is matched by application (process) and ignores the window title, so it stays the same across page changes, tabs, and other windows of that app.
- Unpin: clears the pin and returns to title-based matching.

## Notes On Pinning

- Pinning matches by application, so all windows of a pinned app share one pad. Distinguishing individual windows or tabs of the same app is not possible from the window title alone, and SessionPad does not read app internals.
- Pinning is most useful for apps whose window title changes constantly, such as browsers and terminals, where title-based matching cannot restore a pad reliably.

## Features

- Attach SessionPad to the current foreground window with `Ctrl+Alt+N`.
- Manually attach by dragging SessionPad near a valid external app window.
- Restore per-window notes using `processName + normalizedWindowTitle`.
- Rename the current session and keep the custom name across re-matches.
- Pin the current session to an app to match by process and ignore the window title.
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

SessionPad v0.1.2 stores only user-entered notes on the local machine.

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
- Window identity is currently `processName + normalizedWindowTitle`, except for pinned sessions, which match by process only.
- Individual tabs or windows of the same app cannot be tracked separately from the window title alone.
- Attached-window following uses polling, not WinEvent hooks.
- Multi-monitor and DPI behavior should still be manually verified.

## How To Run

From source:

```powershell
dotnet run --project src/SessionPad.App/SessionPad.App.csproj
```

From the release package:

1. Extract `SessionPad-v0.1.2.zip`.
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
artifacts/SessionPad-v0.1.2.zip
```

Generated `artifacts/`, `publish/`, `bin/`, and `obj/` outputs are ignored by Git and should not be committed.
