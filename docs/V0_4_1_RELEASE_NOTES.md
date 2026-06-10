# SessionPad v0.4.1

## Summary

Adds a session manager so the saved-session list no longer grows unbounded with no way
to clean it up. No data format changes.

## What's New

- **Sessions list in Settings.** Shows every saved session (most recently used first)
  with its name, process, last-used time, and a pin indicator.
- **Open.** Jump straight to a session's note from the list.
- **Delete one session.** Removes the session from the index along with its note file
  and its backups, after an inline two-step confirm in the row. Deleting the currently
  loaded session safely returns the pad to the default note. The default note itself is
  never deletable from this list.
- Nothing is ever deleted automatically — cleanup is always an explicit user action.

## Why

Every distinct window title creates a session, and auto-track can create one for each
app you focus. Until now the only cleanup was "Delete All Local Data". This release adds
the missing granular control.

## Tests

9 new unit tests (34 total): storage-level delete behavior (index/note/backups isolation,
no-op and missing-file tolerance) and the first ViewModel-layer tests (list order,
confirm/cancel flow, current-session deletion returning to the default note).

## Known Limitations / Privacy

Unchanged from v0.4.0. Local-first; window title only; clipboard written only on Copy.

## How To Run

```powershell
dotnet run --project src/SessionPad.App/SessionPad.App.csproj
```

```powershell
dotnet test
```
