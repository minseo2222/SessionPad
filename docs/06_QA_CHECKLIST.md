# QA Checklist

## Slice 1 QA

### Build

Run:

```bash
dotnet build

Expected:

Build succeeds.
No errors.
Launch

Run the app from IDE or command line.

Expected:

App starts.
Window title is SessionPad.
A small note-like window appears.
Docked Tab State

Expected:

Docked Tab state is visible.
It is compact.
It clearly represents SessionPad.
There is a way to expand it.
Compact Note State

Expected:

Compact Note state is visible after expanding.
It contains these sections:
Pinned
Todo
Commands
Notes
There is a way to collapse back to Docked Tab.
Scope Check

Confirm Slice 1 does not include:

Global hotkey
External window tracking
Win32 hooks
UI Automation
Persistence
Cloud
AI
Later MVP QA
Attach to VS Code

Expected:

Focus VS Code.
Trigger attach.
SessionPad appears beside VS Code.
Attach to Windows Terminal

Expected:

Focus Windows Terminal.
Trigger attach.
SessionPad appears beside Windows Terminal.
Move Target Window

Expected:

Move target window.
SessionPad follows.
Resize Target Window

Expected:

Resize target window.
SessionPad remains attached to the chosen edge.
Minimize Target Window

Expected:

Minimize target window.
SessionPad hides.
Restore Target Window

Expected:

Restore target window.
SessionPad returns.
Local Persistence

Expected:

Edit note.
Exit app.
Restart app.
Note content remains.
Multi-monitor

Expected:

Attach on primary monitor.
Attach on secondary monitor.
Move target between monitors.
SessionPad remains positioned correctly.
DPI

Expected:

Test with 100%, 125%, and 150% scaling where available.
SessionPad does not drift away from target window.
Privacy QA

Confirm:

No network calls.
No login.
No telemetry.
No screen capture.
No terminal output capture.
No automatic reading of editor files.

---

# 9. Codex에게 줄 첫 프롬프트 — 바로 구현용

빈 프로젝트라면 이 프롬프트를 주는 게 가장 좋습니다. Codex에게 **Slice 1만 구현**시키는 프롬프트입니다.

```text
You are working in an empty repository for a Windows desktop utility called SessionPad.

Read these files first:

- README.md
- AGENTS.md
- docs/01_PRODUCT_BRIEF.md
- docs/02_MVP_SCOPE.md
- docs/03_TECH_ARCHITECTURE.md
- docs/04_DATA_MODEL.md
- docs/05_CODEX_SLICES.md
- docs/06_QA_CHECKLIST.md

Your task is to implement Slice 1 only.

Slice 1 goal:
Create a buildable WPF desktop app with a minimal SessionPad UI.

Hard constraints:
- Do not implement external window attachment.
- Do not implement global hotkeys.
- Do not implement Win32 hooks.
- Do not implement UI Automation.
- Do not implement persistence.
- Do not implement settings.
- Do not implement tray icon.
- Do not implement AI.
- Do not implement cloud sync.
- Do not implement telemetry.
- Do not scrape screen contents or terminal output.

Preferred stack:
- WPF
- C#
- Use `net10.0-windows` if the installed SDK supports it.
- If .NET 10 SDK is not installed, use `net8.0-windows`.
- Do not create `global.json` unless you have confirmed the installed SDK version.

Before coding:
1. Inspect the repository.
2. Check available .NET SDKs using the appropriate command.
3. Decide the target framework based on installed SDKs.
4. Briefly state the plan.

Then implement Slice 1.

Expected project structure:

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

Required UI behavior:
1. The app launches a window titled `SessionPad`.
2. The initial state may be Docked Tab or Compact Note, but the user must be able to toggle between them.
3. Docked Tab is a small collapsed SessionPad view.
4. Compact Note shows four sections:
   - Pinned
   - Todo
   - Commands
   - Notes
5. Use simple sample in-memory placeholder content.
6. No file saving is needed.
7. No external app/window integration is needed.

Implementation guidelines:
- Keep code simple.
- Use nullable reference types.
- Use a small ViewModel.
- Use commands or simple event handlers where appropriate.
- Do not over-engineer.
- Do not add packages unless necessary.
- Make the UI functional rather than visually perfect.

After implementation:
1. Run `dotnet build`.
2. Report the exact command used.
3. Report whether the build passed.
4. Summarize files created or changed.
5. Mention anything intentionally not implemented because it belongs to later slices.