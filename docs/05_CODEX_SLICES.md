# Codex Implementation Slices

> Historical: Slices 1–9 below were the original build plan and have all shipped
> (through v0.4). Their "Must Not Implement" lines — persistence, global hotkeys,
> Win32 hooks, etc. — described per-slice sequencing, not permanent product limits.
> Those features now exist. Keep this file as a record of how SessionPad was built.

Implement SessionPad in small slices.

Do not skip ahead.

## Slice 1: Project Scaffold + Minimal Floating Note UI

### Goal

Create a buildable WPF desktop app with a minimal SessionPad UI.

### Must Implement

- Solution file
- WPF app project
- App launch
- Minimal SessionPad window
- Docked Tab state
- Compact Note state
- Toggle between states
- Placeholder sections:
  - Pinned
  - Todo
  - Commands
  - Notes

### Must Not Implement

- Persistence
- Win32 external window attachment
- Global hotkeys
- UI Automation
- Settings
- Tray icon
- Installer
- AI
- Cloud sync

### Suggested Files

```text
src/
  SessionPad.App/
    SessionPad.App.csproj
    App.xaml
    App.xaml.cs
    MainWindow.xaml
    MainWindow.xaml.cs
    Views/
      DockedTabView.xaml
      DockedTabView.xaml.cs
      CompactNoteView.xaml
      CompactNoteView.xaml.cs
    ViewModels/
      FloatingNoteViewModel.cs
      RelayCommand.cs
```

### Acceptance Criteria

- dotnet build succeeds.
- App launches.
- Window title is SessionPad.
- User sees a small SessionPad note UI.
- User can switch between Docked Tab and Compact Note.
- Compact Note displays Pinned, Todo, Commands, Notes sections.
- No external app/window integration exists yet.

## Slice 2: UI Interaction Skeleton

### Goal

Make placeholder note UI feel editable locally in memory.

### Must Implement

- Add TODO text
- Toggle TODO checkbox
- Add command text
- Add note text
- Remove placeholder-only dead UI where needed

### Must Not Implement

- File persistence
- External window attachment
- Global hotkey

## Slice 3: Local Storage

### Goal

Save and load SessionPad notes locally.

### Must Implement

- JSON storage
- App data path
- Load default note
- Save on change with debounce
- Atomic write

### Must Not Implement

- Cloud
- Login
- Sync

## Slice 4: Global Hotkey

### Goal

Register a global hotkey for future attach behavior.

### Must Implement

- Ctrl+Alt+N
- Receive hotkey event
- Show or focus SessionPad window

### Must Not Implement

- External window attachment yet if it makes the slice too large

## Slice 5: Window Detection

### Goal

Detect current foreground window identity.

### Must Implement

- Get foreground HWND
- Read process name
- Read title
- Read basic bounds
- Ignore SessionPad’s own window

## Slice 6: Attach to Active Window

### Goal

Attach SessionPad note to the active foreground window.

### Must Implement

- Hotkey triggers attach
- Position note beside target window
- Store runtime target HWND
- Basic bounds update on attach

## Slice 7: Follow Window Movement

### Goal

Make SessionPad follow target window movement and minimization.

### Must Implement

- WinEvent hook or safe polling fallback
- Follow move/resize
- Hide when minimized
- Show when restored

## Slice 8: Identity Matching and Restore

### Goal

Restore notes based on process name and normalized window title.

### Must Implement

- Session index
- Window title normalization
- Find or create session
- Load matching note

## Slice 9: Polish and QA

### Goal

Make the MVP usable.

### Must Implement

- Multi-monitor sanity checks
- DPI sanity checks
- Better collapsed tab
- Basic settings or about window
- Delete local data option