# SessionPad v0.2.0

## Summary

SessionPad v0.2.0 is a major step up from the v0.1.x line. It improves how notes are matched to your work context and gives the app a complete visual redesign: a warm, compact sticky-note panel built to live beside whatever you are working on.

This release keeps the same local-first foundation. There is no installer, cloud sync, login, telemetry, AI, UI Automation, or app-specific deep integration. Your notes still never leave the machine.

## Highlights

- A redesigned sticky-note interface with a calm off-white theme, a session color spine, underline tabs, quick-capture input, and light/dark themes.
- Smarter matching: rename a pad, pin a pad to an app, and one pad per project for VS Code-family editors.
- Each tab now has its own character: Key cards, Todo checklist, Command snippets, and Notes with inline editing.

## Matching Improvements

- Rename session: give the current window's pad a custom name. The name is preserved even when you return to the same window.
- Pin to app: pin a pad to its application so it stays the same even when the window title changes (useful for browsers and terminals). Pinned pads match by application.
- Project-level matching for VS Code, Cursor, and Windsurf: switching files within the same project keeps the same pad, instead of creating a separate pad per file.
- Existing pads from earlier versions are carried over automatically; no notes are lost.

## Interface Redesign

- Warm off-white panel with a subtle session color spine on the left edge and a light, soft depth.
- Light and dark themes with a toggle; the choice is remembered.
- Underline tabs for Key, Todo, Commands, and Notes, with only the active tab emphasized.
- Quick-capture input with a context-aware placeholder per tab; press Enter to save.
- Per-tab item styles:
  - Key: highlighted detail cards.
  - Todo: circular check with strikethrough when done, plus a done count.
  - Commands: code-snippet cards with a copy action on hover.
  - Notes: free-form cards that expand inline for full-text view, editing, and copy.
- Delete and copy actions appear quietly on hover instead of as large repeated buttons.
- Session rename, pin, local data controls, start-with-Windows, theme, and developer info are tucked into a settings panel, keeping the main view focused on notes.

## Resident Utility Behavior

- Lives in the system tray; closing the window hides it instead of quitting.
- Single instance; launching again brings the existing window forward.
- Optional start with Windows, launching silently to the tray.
- Global hotkey `Ctrl+Alt+N` to bring the pad to the current window.
- Smooth following of the attached window.

## Privacy And Local-only Notes

SessionPad v0.2.0 stores only user-entered notes on the local machine.

- No login, cloud sync, telemetry, or AI.
- No screen scraping or terminal scraping.
- No automatic reading of editor, browser, terminal, or project contents.
- Clipboard is only written when you click Copy. Clipboard is not read.
- Commands are not executed or pasted automatically.
- Start with Windows writes only the executable path to the current-user startup registry key.

## Known Limitations

- Windows only.
- No installer or MSIX package yet.
- No cloud sync, login, telemetry, AI, or UI Automation.
- No VS Code workspace detection, Windows Terminal tab detection, or browser URL detection (matching uses the window title only).
- Pinned pads match by application, so individual windows or tabs of the same app are not tracked separately.
- Project-level matching relies on the editor's window title and may not apply to heavily customized title formats; pinning is the fallback.
- Attached-window following uses polling, not WinEvent hooks.
- Multi-monitor and DPI behavior should still be manually verified.

## How To Run

From source:

```powershell
dotnet run --project src/SessionPad.App/SessionPad.App.csproj
```

From the release package:

1. Extract `SessionPad-v0.2.0.zip`.
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
  settings.json
  sessions.index.json
  notes\
    default.json
    <sessionId>.json
```

Runtime HWND values, detected window state, attach state, follow state, and drag status are not persisted.

## Delete Local Data

Open the settings panel, find the Local Data section, click Delete All Local Data, and confirm. This removes SessionPad notes and session data from this device only.

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
artifacts/SessionPad-v0.2.0.zip
```

Generated `artifacts/`, `publish/`, `bin/`, and `obj/` outputs are ignored by Git and should not be committed.
