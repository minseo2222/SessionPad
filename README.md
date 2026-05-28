# SessionPad

SessionPad is a Windows-first local desktop utility that attaches a lightweight note pad to an application window such as VS Code, Cursor, Windsurf, or Windows Terminal.

The product goal is simple:

> When the user returns to a work window, the note associated with that work context should return with it.

SessionPad is not a knowledge management app, not an AI assistant, not a cloud app, and not a collaboration tool.

## MVP Direction

The MVP is a local Windows app with a small floating note window that can later attach to external application windows.

Initial target apps:

- Visual Studio Code
- Cursor
- Windsurf
- Windows Terminal
- PowerShell / terminal-style workflows

## Technology Direction

Preferred stack:

- WPF
- .NET, targeting `net10.0-windows` if the SDK is available
- Fallback to `net8.0-windows` only if .NET 10 SDK is not installed locally
- C#
- Local JSON storage
- Later Win32 interop through P/Invoke

The first implementation slice should not implement external window attachment yet. It should only create a minimal runnable WPF app with the basic SessionPad UI.

## Core Product Principles

- Windows first
- Local first
- No login
- No cloud sync
- No telemetry in MVP
- No AI features
- No screen scraping
- No terminal output scraping
- Store only user-entered notes
- Keep the app lightweight and focused

## Planned Source Layout
src/
  SessionPad.App/
    App.xaml
    MainWindow.xaml
    Views/
    ViewModels/
    Models/
    Services/
    Native/
docs/

## MVP User Experience
The final MVP should support:

1. User presses a hotkey while focused on a work window.
2. SessionPad creates or restores a note associated with that window.
3. The note appears as a small docked tab beside the target window.
4. User expands it into a compact note.
5. User writes pinned notes, TODOs, commands, and plain notes.
6. The note hides when the target window is minimized.
7. The note returns when the target window is restored or reactivated.
8. Notes are saved locally.


## Current Status
Empty repository / pre-implementation.
Start with Slice 1 from docs/05_CODEX_SLICES.md.