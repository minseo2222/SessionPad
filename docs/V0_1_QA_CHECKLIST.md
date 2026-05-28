# SessionPad v0.1 QA Checklist

Use this checklist for the v0.1 release candidate.

## Launch And Basic UI

- [ ] Run SessionPad.
- [ ] Confirm the app launches with the window title `SessionPad`.
- [ ] Confirm Compact Note is visible.
- [ ] Collapse to Docked Tab.
- [ ] Confirm Docked Tab is clear, shows SessionPad identity, and displays the open TODO count.
- [ ] Expand back to Compact Note.

## Note Editing

- [ ] Add a Pinned item.
- [ ] Delete the Pinned item.
- [ ] Add a TODO item.
- [ ] Check the TODO item.
- [ ] Uncheck the TODO item.
- [ ] Delete the TODO item.
- [ ] Add a Command item.
- [ ] Delete a Command item.
- [ ] Add a Note item.
- [ ] Delete a Note item.

## Command Copy

- [ ] Add a Command item, for example `dotnet build`.
- [ ] Click Copy on that command.
- [ ] Paste manually into Notepad or another text field.
- [ ] Confirm the pasted text is the command text.
- [ ] Confirm SessionPad shows a copy status.
- [ ] Confirm SessionPad does not execute the command.
- [ ] Confirm SessionPad does not paste automatically or send keystrokes.

## Local Persistence

- [ ] Add one item in each note section.
- [ ] Check one TODO item.
- [ ] Close SessionPad.
- [ ] Relaunch SessionPad.
- [ ] Confirm all saved items return.
- [ ] Confirm TODO checked state returns.
- [ ] Delete an item.
- [ ] Close and relaunch.
- [ ] Confirm the deleted item stays deleted.

## Per-window Session Restore

- [ ] Focus VS Code.
- [ ] Press `Ctrl+Alt+N`.
- [ ] Confirm SessionPad attaches beside VS Code.
- [ ] Add a unique VS Code note item.
- [ ] Focus Notepad.
- [ ] Press `Ctrl+Alt+N`.
- [ ] Confirm SessionPad attaches beside Notepad.
- [ ] Confirm the VS Code-specific item is not shown unless identity intentionally matches.
- [ ] Add a unique Notepad note item.
- [ ] Close and relaunch SessionPad.
- [ ] Focus VS Code and press `Ctrl+Alt+N`.
- [ ] Confirm the VS Code note returns.
- [ ] Focus Notepad and press `Ctrl+Alt+N`.
- [ ] Confirm the Notepad note returns.

## Hotkey Attach And Follow

- [ ] Focus VS Code and press `Ctrl+Alt+N`.
- [ ] Confirm SessionPad appears beside VS Code.
- [ ] Move VS Code.
- [ ] Confirm SessionPad follows.
- [ ] Resize VS Code.
- [ ] Confirm SessionPad remains beside it.
- [ ] Minimize VS Code.
- [ ] Confirm SessionPad hides.
- [ ] Restore VS Code.
- [ ] Confirm SessionPad shows again.
- [ ] Repeat the hotkey attach smoke check with Notepad, Windows Terminal, a browser, and PowerShell or pwsh if available.

## Drag Attach

- [ ] Drag SessionPad near Notepad and release.
- [ ] Confirm SessionPad attaches to Notepad.
- [ ] Move, resize, minimize, and restore Notepad.
- [ ] Confirm follow and hide/restore still work.
- [ ] Drag SessionPad near VS Code and release.
- [ ] Confirm SessionPad attaches to VS Code.
- [ ] Drag SessionPad near Windows Terminal and release.
- [ ] Confirm SessionPad attaches to Windows Terminal.
- [ ] Drag SessionPad near PowerShell, pwsh, or a console window and release.
- [ ] Confirm SessionPad attaches to that console window, not explorer or shell.
- [ ] Drag SessionPad near a browser and release.
- [ ] Confirm SessionPad attaches to the browser.
- [ ] Drag SessionPad over or near itself.
- [ ] Confirm it does not attach to itself.
- [ ] Drag SessionPad near the desktop, taskbar, or empty shell background.
- [ ] Confirm it does not attach to explorer shell/background windows.
- [ ] Confirm a safe status such as `No nearby target window` is shown when no valid target exists.

## Local Data Controls

- [ ] Confirm the Local Data section shows `%APPDATA%\SessionPad`.
- [ ] Click Open Folder.
- [ ] Confirm the SessionPad app data folder opens.
- [ ] Click Delete All Local Data.
- [ ] Cancel the confirmation.
- [ ] Confirm saved data is not deleted.
- [ ] Click Delete All Local Data again.
- [ ] Confirm deletion.
- [ ] Confirm the app does not crash.
- [ ] Confirm the UI resets to a safe default note.
- [ ] Close and relaunch.
- [ ] Confirm old sessions are gone.
- [ ] Confirm new sessions can be created and saved again.

## Self-attach And Invalid Target Safety

- [ ] Focus SessionPad itself and press `Ctrl+Alt+N`.
- [ ] Confirm SessionPad does not attach to itself.
- [ ] Confirm no crash.
- [ ] Close an attached target window.
- [ ] Confirm SessionPad does not crash and shows a safe attach/follow status.

## Privacy Regression

- [ ] Confirm no login/account flow exists.
- [ ] Confirm no cloud sync exists.
- [ ] Confirm no telemetry exists.
- [ ] Confirm no AI feature exists.
- [ ] Confirm no screen scraping exists.
- [ ] Confirm no terminal output scraping exists.
- [ ] Confirm no clipboard read behavior exists.
- [ ] Confirm clipboard write happens only after the user clicks Copy on a command.
- [ ] Confirm commands are not executed.
- [ ] Confirm commands are not pasted automatically.

## Release Build

- [ ] Run `dotnet build`.
- [ ] Run `dotnet build -c Release`.
- [ ] Run `powershell -ExecutionPolicy Bypass -File scripts/publish-release.ps1`.
- [ ] Confirm publish output is under `artifacts/SessionPad-v0.1`.
- [ ] Confirm generated artifacts are ignored by Git and not staged.

## Manual Environment Notes

- [ ] Test multi-monitor behavior if multiple monitors are available.
- [ ] Test common DPI settings such as 100%, 125%, and 150% where available.
- [ ] Confirm SessionPad stays visible and reasonably positioned around monitor work areas.
