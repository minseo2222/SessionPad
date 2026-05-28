# Technical Architecture

## Recommended Stack

Use WPF with C# for the Windows MVP.

Preferred framework:

- `net10.0-windows` if available
- `net8.0-windows` fallback only if .NET 10 SDK is not installed

Reasoning:

SessionPad needs to interact with Windows desktop concepts such as:

- HWND
- Foreground window
- Top-level windows
- Window bounds
- Window movement
- Minimize/restore events
- DPI-aware positioning
- Always-on-top floating windows

WPF gives a practical Windows-native foundation and can interoperate with Win32 APIs through P/Invoke.

## Planned Project Structure

```text
src/
  SessionPad.App/
    App.xaml
    MainWindow.xaml
    Views/
      FloatingNoteWindow.xaml
      DockedTabView.xaml
      CompactNoteView.xaml
    ViewModels/
      FloatingNoteViewModel.cs
      NoteViewModel.cs
    Models/
      SessionNote.cs
      NoteSections.cs
      NoteItems.cs
      WindowIdentity.cs
    Services/
      NoteStorageService.cs
      SessionMatcher.cs
      WindowDetectionService.cs
      WindowAttachmentService.cs
      HotkeyService.cs
      WinEventHookService.cs
      ClipboardService.cs
    Native/
      User32.cs
      DwmApi.cs
      NativeTypes.cs

For Slice 1, only create the pieces required for a runnable UI.

Do not create empty services unless they are immediately useful.

Future Runtime Components
WindowDetectionService

Later responsibility:

Read foreground window
Read window title
Read process name
Read window bounds
Ignore SessionPad’s own windows

Planned Win32 APIs:

GetForegroundWindow
GetWindowTextW
GetWindowThreadProcessId
GetWindowRect
IsIconic
WindowAttachmentService

Later responsibility:

Attach SessionPad note to a target HWND
Position note beside target bounds
Hide/show note based on target state
Reposition note when target moves
WinEventHookService

Later responsibility:

Listen to foreground change
Listen to move/resize
Listen to minimize/restore
Listen to window destroy/name change

Planned Win32 API:

SetWinEventHook
HotkeyService

Later responsibility:

Register global attach hotkey
Receive WM_HOTKEY
Handle hotkey conflicts

Planned Win32 API:

RegisterHotKey
UnregisterHotKey
NoteStorageService

Later responsibility:

Store notes under local app data
Use JSON
Atomic file writes
Basic backup

Planned path:

%APPDATA%/SessionPad/
  settings.json
  sessions.index.json
  notes/
  backups/
Window Identity Strategy

MVP matching should use:

processName + normalizedWindowTitle

Do not persist HWND.

HWND is runtime-only and may change when apps restart.

Potential fields:

processName
windowTitle
normalizedWindowTitle
windowClass
executablePathHash
userDefinedSessionName
recentTitleSamples
UI State Strategy

The note has these states:

DockedTab
CompactNote
ExpandedNote

For MVP, implement:

DockedTab
CompactNote

ExpandedNote is later.

Privacy and Security

Do not implement:

Screen capture
OCR
Terminal scraping
Editor text scraping
Reading project files automatically
Cloud upload
Telemetry

Any future feature that reads external app state must be explicit, local-first, and documented.