# SessionPad v0.3.1

## Summary

A small reliability and maintainability patch on top of v0.3.0. It reduces unwanted
pad switching when a window's title changes rapidly, and adds the project's first
automated test suite. No data format changes; existing notes and settings load as-is.

## What's New

- **Calmer title-change switching.** While attached to a window, a title change that
  still resolves to the same pad (for example moving between files in the same VS Code
  project, or a change that only differs in whitespace/case) no longer triggers a
  switch. Rapid title churn — such as a shell that puts the running command or path in
  its title — is now debounced, so the pad switches once the title settles instead of
  flickering between pads. Position following, minimize/restore, and auto-track stay
  immediate; only session switching is debounced.

- **Test seed (internal).** Added an xUnit test project (`SessionPad.Tests`) and made
  `NoteStorageService` accept an injected clock and base directory so storage and
  matching logic can be tested deterministically. 16 unit tests cover title
  normalization, match-key generation, IDE project-level matching, pinned matching,
  legacy v1→v2 migration, JSON round-trip / forward-compatible loading, atomic writes,
  and backup rolling. The default constructors are unchanged, so existing behavior and
  on-disk data are identical.

## Known Limitations

Unchanged from v0.3.0, except that rapid title changes are now debounced and same-pad
title changes no longer switch. Matching still uses the window title only.

## Privacy And Local-only Notes

Unchanged. SessionPad stores only user-entered notes locally; no login, cloud sync,
telemetry, AI, or scraping. Only the window title is used for matching.

## How To Run

```powershell
dotnet run --project src/SessionPad.App/SessionPad.App.csproj
```

Run tests:

```powershell
dotnet test
```
