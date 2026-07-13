# MVP Scope

## MVP Goal

Build a Windows desktop app that can eventually attach a compact local note to app windows.

For the first development phase, focus on the UI shell and local app foundation.

The full MVP should later support:

- Attaching a note to the current active window
- Restoring notes by window identity
- Following target window movement
- Hiding when the target window is minimized
- Local JSON persistence

## Slice 1 Goal

> Historical: Slice 1 is complete. Persistence, hotkeys, and Win32 tracking — listed
> below as "must not include" — have since shipped and are core features. This section
> is kept as a record of the original scaffold scope, not as an active constraint.

Slice 1 is only the app scaffold and UI placeholder.

Slice 1 must produce:

1. A buildable WPF desktop app.
2. A minimal SessionPad window.
3. A collapsed Docked Tab state.
4. An expanded Compact Note state.
5. Placeholder sections:
   - Pinned
   - Todo
   - Commands
   - Notes

Slice 1 must not include:

- External window attachment
- Global hotkeys
- Win32 event hooks
- UI Automation
- Persistence
- Settings
- Tray icon
- Installer
- AI
- Cloud sync

## MVP Must-have Features

These are for the complete MVP, not necessarily Slice 1.

| Feature | Description |
|---|---|
| Attach to active window | User triggers attach while another app is focused |
| Docked Tab | Small collapsed tab beside target window |
| Compact Note | Expanded note UI |
| Pinned section | Short persistent reminders |
| Todo section | Checkable TODO items |
| Commands section | User-entered command snippets |
| Notes section | Freeform notes |
| Local storage | JSON files under app data directory |
| Window movement tracking | Note follows attached target window |
| Minimize handling | Note hides when target window is minimized |
| Identity matching | Process name + normalized window title |

## Should-have Features

| Feature | Description |
|---|---|
| Hotkey customization | User can change default attach hotkey |
| Search | Search local notes |
| Backup | Keep simple backup copies of note files |
| Manual session name | User can rename unstable window sessions |
| Multi-monitor support | Proper positioning across monitors and DPI settings |

## Later Features

| Feature | Description |
|---|---|
| VS Code project detection | Detect workspace/project more precisely |
| Windows Terminal tab detection | Detect terminal tab context |
| UI Automation smart binding | Detect app-internal tabs or documents |
| Markdown export | Export notes as markdown |
| Optional sync | Explicit opt-in only |
| macOS version | Separate future product line |

## Default UI States

### Docked Tab

A small collapsed state.

Example content:

```text
SP
3
```

Where 3 may later mean open TODO count.

### Compact Note

A small expanded note panel.

Sections:

- Pinned
- Todo
- Commands
- Notes

## Design Tone

The UI should be simple, utility-like, and unobtrusive.

Avoid heavy animations, large dashboards, and complex document editing.