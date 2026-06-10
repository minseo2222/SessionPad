# SessionPad v0.4.0

## Summary

A visual modernization of the app shell. SessionPad now looks like the floating pad it
is, instead of a utility wrapped in a classic Windows title bar. No data format changes
and no behavior changes to attaching, following, or storage.

## What's New

- **Chromeless window.** The OS title bar is gone; the pad itself is the window, with
  native rounded corners and a DWM drop shadow on Windows 11 (square corners on older
  Windows, same behavior). The window frame follows the dark/light theme.
- **Window movement.** Drag the header (or the collapsed tab's edge) to move the pad.
  The `Drag` handle still performs attach-to-window, unchanged.
- **Close to tray.** A new ✕ button in the header hides the pad to the tray — the same
  behavior the old title-bar close button had. Exit remains in the tray menu.
- **Themed dropdown and expander.** The attach-shortcut picker and the Developer info
  panel now follow the app theme (no more white system dropdown in dark mode).
- **In-app delete confirmation.** "Delete All Local Data" now confirms inline inside
  Settings instead of a classic Win32 message box.
- **Subtle hover transitions.** Buttons fade their hover state (120 ms) instead of
  snapping.

Considered and not adopted: a Windows 11 Mica backdrop. The pad's content is opaque by
design for readability, so a backdrop would be invisible without making notes
translucent — not worth it for a note surface.

## Known Limitations

Unchanged from v0.3.x, plus: on Windows 10 the chromeless window has square corners
(cosmetic only).

## Privacy And Local-only Notes

Unchanged. Local-first; window title only; clipboard written only on Copy; local errors
shown on screen only.

## How To Run

```powershell
dotnet run --project src/SessionPad.App/SessionPad.App.csproj
```

```powershell
dotnet test
```
