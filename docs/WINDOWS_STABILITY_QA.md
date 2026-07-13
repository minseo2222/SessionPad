# Windows Attachment Stability QA

Repeatable manual QA for SessionPad's core promise: attach beside an external
Windows window and follow it reliably. This document is a blank execution record,
not evidence that the scenarios have been run.

## Verification boundary

Automated tests currently verify the placement calculation without Win32:

- 8 px gap and exact-fit right/left placement.
- Right-first priority, including the clamped-right tie breaker.
- Horizontal and vertical work-area clamping.
- Oversized SessionPad fallback to work-area left/top.
- Negative X/Y monitor coordinates and missing-monitor fallback.
- Rejection of non-positive SessionPad dimensions.
- Attached, Following, TargetMinimized, and NotAttached result invariants.

Automation does **not** verify real HWND discovery, `SetWindowPos`, WinEvent delivery,
mixed-DPI coordinate virtualization, taskbar work areas, application-specific title
behavior, sleep/resume, Explorer restart, tray behavior, or startup behavior. Every
scenario in the manual matrix below is currently **not run** and needs a human on
the recorded Windows hardware.

## Existing placement contract

The extraction preserves these pre-existing rules:

- The target-to-SessionPad gap is exactly 8 device pixels.
- Right placement has priority when right space is greater than or equal to the
  SessionPad width. Its side string is exactly `Right`.
- Left placement is used only when right space is insufficient and the full pad fits
  to the left. Its side string is exactly `Left`.
- If neither side fits, the side with more space is chosen. Equal space selects the
  right because the comparison is `rightSpace >= leftSpace`. The side strings are
  exactly `Clamped Right` and `Clamped Left`.
- Horizontal placement clamps to `workArea.Left .. workArea.Right - padWidth`.
  If the pad is wider than the work area, the fallback is `workArea.Left`.
- Vertical placement clamps the target top to
  `workArea.Top .. workArea.Bottom - padHeight`. If the pad is taller than the work
  area, the fallback is `workArea.Top`.
- Negative monitor coordinates use the same arithmetic without normalization to the
  primary monitor.
- If monitor/work-area lookup fails, placement remains unclamped at
  `(target.Right + 8, target.Top)` with side `Right`.
- The existing caller rejects non-positive SessionPad dimensions before calculation;
  the pure calculator makes that contract explicit with `ArgumentOutOfRangeException`.

## Run record

| Field | Value |
|---|---|
| QA date | |
| Tester | |
| Commit SHA | |
| Artifact version | |
| Artifact filename / SHA256 | |
| Windows edition and version | |
| Windows build (`winver`) | |
| CPU architecture | |
| Monitor count | |
| SessionPad fresh-data or existing-data run | |
| Notes | |

### Monitor and taskbar record

Record Windows display arrangement coordinates as well as physical placement.

| Environment ID | Monitor | Resolution | Scale | Primary | Desktop coordinates | Taskbar position | Auto-hide | Notes |
|---|---|---:|---:|---|---|---|---|---|
| E1 | 1 | | | Yes / No | | | Yes / No | |
| E1 | 2 | | | Yes / No | | | Yes / No | |
| E2 | 1 | | | Yes / No | | | Yes / No | |
| E2 | 2 | | | Yes / No | | | Yes / No | |

Add rows or environment IDs for every tested arrangement. At minimum, record one
single-monitor environment and each multi-monitor/mixed-DPI arrangement referenced
by the scenario matrix.

### Target application record

Record the exact application version before using it in the scenarios.

| Application | Version/build | Window/profile used | Environment IDs | Result notes |
|---|---|---|---|---|
| Notepad | | | | Not run |
| VS Code | | | | Not run |
| Cursor | | | | Not run |
| Windows Terminal | | | | Not run |
| PowerShell | | | | Not run |
| Chrome | | | | Not run |
| Edge | | | | Not run |
| Explorer | | | | Not run |

## Preflight

1. Verify the commit and artifact SHA256 against the run record.
2. Record Windows, monitor, scale, taskbar, and application versions.
3. Use non-sensitive test window titles and test-only SessionPad notes.
4. Confirm the configured attach hotkey and whether auto-track is initially off.
5. Create the evidence directory using the naming rules below.
6. Leave **Actual result**, **Pass / Fail / Blocked**, **Evidence file**, and
   **Discovered issue** unchanged until each scenario is actually run.

## Manual scenario matrix

All 26 rows below start as **not run**. Replace that text only with observed results.
Run core attach/follow scenarios against all applications to which they apply, and
record separate evidence or a separate copied row when outcomes differ by app.

| ID | Environment | Preconditions | Procedure | Expected result | Actual result | Pass / Fail / Blocked | Evidence file | Discovered issue |
|---|---|---|---|---|---|---|---|---|
| WSA-001 | E1; each target app | SessionPad running; auto-track off; target focused | Press the configured attach hotkey once. | Correct target is selected; SessionPad appears attached; no unrelated window is activated. | Not run | — (not run) | | |
| WSA-002 | E1; target with ample right space | Target restored and away from right work-area edge | Attach, then compare the target right edge and SessionPad left edge. | SessionPad is on the right with an approximately 8 px device-pixel gap and remains inside the work area. | Not run | — (not run) | | |
| WSA-003 | E1; target near right edge | Right side cannot fit SessionPad; left side can | Attach to the target. | SessionPad is placed on the left with the gap preserved and does not cross the work-area edge. | Not run | — (not run) | | |
| WSA-004 | E1; attached target | Attach successfully | Slowly drag the target around its monitor and stop at several positions. | SessionPad follows the target without detaching, oscillating sides, or accumulating visible drift. | Not run | — (not run) | | |
| WSA-005 | E1; attached target | Attach successfully | Resize from each target edge and corner, pausing between changes. | SessionPad recomputes position beside the current bounds and remains usable. | Not run | — (not run) | | |
| WSA-006 | E1; attached restorable target | Attach successfully | Maximize the target, restore it, then maximize again. | SessionPad remains associated, stays within the work area, and follows both transitions. | Not run | — (not run) | | |
| WSA-007 | E1; attached target | SessionPad visible beside target | Minimize the target from its title bar and taskbar. | SessionPad hides while the target is minimized and does not steal focus. | Not run | — (not run) | | |
| WSA-008 | E1; target minimized after WSA-007 | SessionPad hidden because target is minimized | Restore the target. | SessionPad reappears beside the restored target and tracking continues. | Not run | — (not run) | | |
| WSA-009 | E1; attached disposable target | Attach successfully; no unsaved work | Close the target normally, then wait through at least two follow intervals. | SessionPad detaches safely; no crash, repeated dialog, or attachment to a different window. | Not run | — (not run) | | |
| WSA-010 | E1; app whose title can change | Attach successfully | Change the document/tab so the native window title changes once. | Tracking remains stable and the intended session-switch behavior occurs without position loss. | Not run | — (not run) | | |
| WSA-011 | E1; Windows Terminal | Attach to a shell whose command updates the title | Run a test command that changes the title rapidly, then let it settle. | No rapid session thrash; stable title is handled after debounce; following remains responsive. | Not run | — (not run) | | |
| WSA-012 | E1; VS Code and Cursor | Open one project with at least two test files | Attach, then switch repeatedly between files in the same project. | The same project session remains selected and SessionPad stays attached. | Not run | — (not run) | | |
| WSA-013 | E1; VS Code and Cursor | Two non-sensitive test projects open | Attach in project A, focus project B, invoke attach, then return to A. | Project B uses its own session and returning to A restores A's session. | Not run | — (not run) | | |
| WSA-014 | E1; two target apps | Auto-track off | Attach to app A, then focus and move app B without invoking attach. | SessionPad remains attached to app A; focus alone does not retarget it. | Not run | — (not run) | | |
| WSA-015 | E1; two target apps | Auto-track on | Focus app A, then app B, pausing after each focus change. | SessionPad retargets only to eligible focused windows and follows the active target. | Not run | — (not run) | | |
| WSA-016 | E2; at least two monitors | Attach on the source monitor | Drag the target completely onto each other monitor and pause. | SessionPad follows to the target monitor and uses that monitor's work area. | Not run | — (not run) | | |
| WSA-017 | E2; 100% and 125% monitors | Attach on the 100% monitor | Move the target to the 125% monitor and back twice. | Gap and edge alignment remain visually stable; no cumulative DPI drift or off-screen placement. | Not run | — (not run) | | |
| WSA-018 | E2; 125% and 150% monitors | Attach on the 125% monitor | Move the target to the 150% monitor and back twice. | Gap and edge alignment remain visually stable; SessionPad stays within the destination work area. | Not run | — (not run) | | |
| WSA-019 | E2; secondary monitor left and/or above primary | Windows display layout produces negative X or Y coordinates | Attach and move/resize the target near every negative-coordinate work-area edge. | Right/left placement and vertical clamp work without jumping to the primary monitor. | Not run | — (not run) | | |
| WSA-020 | E1 and E2; each taskbar arrangement | Taskbar position and auto-hide state recorded | Attach and move/resize the target near the taskbar edge; reveal an auto-hidden taskbar if used. | SessionPad does not occupy the taskbar work area or become unreachable. | Not run | — (not run) | | |
| WSA-021 | E1; attached target | Start in Compact Note | Collapse to Docked Tab, move/resize target, expand, and repeat. | Each size change is reflected on the next follow update; attachment and content remain intact. | Not run | — (not run) | | |
| WSA-022 | E1; saved non-sensitive session | Attach, edit a test note, and exit normally | Restart SessionPad and reattach to the same target identity. | The saved session and note return; attachment positioning remains correct. | Not run | — (not run) | | |
| WSA-023 | E1; attached target; AC power recommended | Save work first | Put Windows to sleep, resume, unlock, and interact with the target. | SessionPad does not crash; tracking resumes or safely detaches with a recoverable reattach path. | Not run | — (not run) | | |
| WSA-024 | E1; Explorer target and another target | Attach to Explorer, then save all work | Restart Windows Explorer from Task Manager; afterward attach to Explorer and the other target again. | Tray/shell recovery does not crash SessionPad; stale HWND is discarded and new attaches work. | Not run | — (not run) | | |
| WSA-025 | E1; attached target | SessionPad visible | Hide to tray, move/resize target, restore SessionPad from tray. | Tray restore returns a usable SessionPad; attachment state is coherent and can be reattached if intentionally cleared. | Not run | — (not run) | | |
| WSA-026 | E1; start-on-login enabled | SessionPad closed; test account can sign out/in | Sign out and sign in, wait for startup, then invoke attach. | SessionPad starts silently as designed, remains accessible from tray/hotkey, and attaches normally. | Not run | — (not run) | | |

## Result rules

### Pass

Mark **Pass** only when every expected result in the row is observed on the recorded
environment and application set, with no crash, data loss, unexpected focus steal,
off-work-area placement, or unrecorded workaround. A partial application set is not
a pass for rows that specify multiple apps; copy the row per app if necessary.

### Fail versus Blocked

- **Fail:** The procedure was executed and an expected result was violated. Record
  exact reproduction steps, frequency, evidence, and an issue ID.
- **Blocked:** The procedure could not reach the behavior under test because of an
  environmental or prerequisite problem, such as missing hardware, policy, or an
  unrelated app failure. Record the blocker and do not treat it as product success.
- **Not run:** No execution attempt has been made. Keep the status as
  `— (not run)`; do not relabel it Blocked merely because hardware is unavailable.

### Reproduction rate

For any intermittent result, run at least 10 attempts and record `failures/attempts`
(for example, `3/10`) plus whether SessionPad and the target were restarted between
attempts. Record the first-failure attempt number and environment ID. A row with any
unexplained failure is not Pass.

### Evidence naming and privacy

Use filenames in this form:

```text
<date>_<commit7>_<environment>_<scenario>_<app>_<attempt>.<ext>
2026-07-13_abcdef0_E2_WSA-017_vscode_01.mp4
```

Prefer a screenshot for a stable placement result and a short video for follow,
minimize/restore, DPI transitions, or intermittent behavior. Before sharing evidence:

- Redact personal names, account identifiers, file paths, URLs, browser tabs, window
  titles, terminal history, and SessionPad note text.
- Use test-only projects, pages, commands, and notes whenever possible.
- Crop unrelated windows and notifications.
- Do not place unredacted evidence in the repository.
- Record the sanitized evidence filename or approved external evidence reference in
  the scenario row.

## Exit summary

| Metric | Value |
|---|---:|
| Manual scenarios total | 26 |
| Pass | 0 |
| Fail | 0 |
| Blocked | 0 |
| Not run | 26 |
| Open product issues | 0 |
| Open environment blockers | 0 |

Update the summary only from completed scenario rows. A release reviewer should not
interpret automated placement tests as completion of this manual Windows matrix.
