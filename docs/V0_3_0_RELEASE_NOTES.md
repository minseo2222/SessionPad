# SessionPad v0.3.0

## Summary

SessionPad v0.3.0 is a broad usability and reliability release. It completes the
note UI (reordering, empty states, keyboard cancel), adds opt-in automatic window
following, replaces follow polling with WinEvent hooks, protects notes with rolling
backups, adds cross-session search, and makes the attach shortcut configurable.

The local-first, no-scraping foundation is unchanged. There are no breaking data
format changes: existing `settings.json` and note JSON load as-is.

## What's New

### Note UI

- Reorder items in any section (Key, Todo, Commands, Notes) with hover-revealed
  up/down controls. Order persists across restarts.
- Inline note editing can be cancelled with `Esc` (closes without saving).
- Empty sections now show a short hint instead of a blank area.
- The footer attach indicator reflects the real attachment state instead of always
  reading "attached".
- `Enter` adds an item from each section's input box (already present, now consistent).

### Auto-track focused window (opt-in)

- New setting "Auto-track focused window" (default off). When on, SessionPad attaches
  to whichever window you focus and shows its matching note, without a manual attach.
- Matching still uses the window title only; nothing inside the window is read.
- Suppressed while SessionPad is hidden to the tray or being dragged to attach. The
  current note is saved before switching, and focus is never stolen from the window
  you switched to.

### WinEvent-based following

- Window move/resize, minimize/restore, title change, destruction, and foreground
  changes are now driven by WinEvent hooks instead of 60 ms polling.
- Polling remains only as a low-frequency safety net, and falls back to the original
  responsive intervals if hooks cannot be installed. Hooks are removed on exit.

### Note backups

- Every note save also writes a timestamped copy to `backups/<key>.<timestamp>.json`,
  keeping the 5 most recent per note. Backups are best-effort and never block the
  primary save. They are removed by Delete All Local Data.

### Local search

- Search across all session notes from Settings. Results list one matching session
  with a snippet and match count; selecting a result jumps to that session's note.
  Search runs only when you ask it to.

### Configurable attach shortcut

- Choose the global attach shortcut from a preset list in Settings (default
  `Ctrl + Alt + N`). The choice persists. If a shortcut is already taken by another
  app, SessionPad reports the failure and keeps the previous working shortcut.

## Known Limitations

- Matching still uses the window title only. Two windows/tabs with the same title
  share one note. No VS Code workspace, Windows Terminal tab-id, or browser URL
  detection.
- The attach shortcut is chosen from a preset list, not free-form key capture.
- Multi-monitor and DPI behavior should still be manually verified.
- Windows only. No installer or MSIX package, cloud sync, login, telemetry, or AI.

## Privacy And Local-only Notes

Unchanged. SessionPad stores only user-entered notes on the local machine.

- No login, cloud sync, telemetry, or AI.
- No screen, terminal, editor, browser, or project-file scraping. Only the window
  title is used for matching.
- Clipboard is only written when you click Copy. Clipboard is not read.

## Storage Location

```text
%APPDATA%\SessionPad\
  settings.json
  sessions.index.json
  notes\
    default.json
    <sessionId>.json
  backups\
    <key>.<timestamp>.json
```

## How To Run

```powershell
dotnet run --project src/SessionPad.App/SessionPad.App.csproj
```

To quit SessionPad fully, right-click the tray icon and choose Exit. Closing the
window only hides it to the tray.
