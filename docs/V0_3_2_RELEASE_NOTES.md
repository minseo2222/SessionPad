# SessionPad v0.3.2

## Summary

A small patch that makes important local failures visible instead of silent. No data
format changes; everything stays local.

## What's New

- **Visible local errors.** Previously some failures were only written to a debug trace
  or buried in the developer info panel. Now:
  - If the global attach shortcut cannot be registered at startup (for example another
    app already holds Ctrl+Alt+N), SessionPad shows a status toast and notes it in the
    shortcut status under Settings. The app keeps running normally.
  - If a note fails to save (for example the data folder is not writable), a status
    toast tells you, instead of the failure being hidden.

These messages are shown on screen only. Nothing is reported or sent anywhere — no
telemetry, no logging to any server. The existing copy confirmation toast and all
normal behavior are unchanged.

## Known Limitations

Unchanged from v0.3.1.

## Privacy And Local-only Notes

Unchanged. Only user-entered notes are stored, locally. Window title only for matching.
Clipboard is written only on Copy and never read. Local errors are displayed on screen
and never transmitted.

## How To Run

```powershell
dotnet run --project src/SessionPad.App/SessionPad.App.csproj
```

```powershell
dotnet test
```
