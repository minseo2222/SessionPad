# Data Model

This document describes the intended MVP data model.

Slice 1 does not need persistence yet, but UI models should not conflict with this direction.

## AppSettings

```csharp
public sealed record AppSettings
{
    public int SchemaVersion { get; init; } = 1;
    public HotkeySettings Hotkeys { get; init; } = new();
    public WindowingSettings Windowing { get; init; } = new();
    public PrivacySettings Privacy { get; init; } = new();
}
```

## SessionNote

```csharp
public sealed record SessionNote
{
    public int SchemaVersion { get; init; } = 1;
    public required string SessionId { get; init; }

    public NotePanelState PanelState { get; init; } = NotePanelState.DockedTab;
    public DockSide DockSide { get; init; } = DockSide.Right;

    public NoteSections Sections { get; init; } = new();

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
```

## Enums

```csharp
public enum DockSide
{
    Left,
    Right,
    Top,
    Bottom
}

public enum NotePanelState
{
    DockedTab,
    CompactNote,
    ExpandedNote
}
```

## NoteSections

```csharp
public sealed record NoteSections
{
    public List<PinnedItem> Pinned { get; init; } = new();
    public List<TodoItem> Todo { get; init; } = new();
    public List<CommandItem> Commands { get; init; } = new();
    public List<NoteTextItem> Notes { get; init; } = new();
}
```

## Item Types

```csharp
public sealed record PinnedItem
{
    public required string Id { get; init; }
    public required string Text { get; init; }
    public int SortOrder { get; init; }
}

public sealed record TodoItem
{
    public required string Id { get; init; }
    public required string Text { get; init; }
    public bool IsDone { get; init; }
    public int SortOrder { get; init; }
}

public sealed record CommandItem
{
    public required string Id { get; init; }
    public string? Label { get; init; }
    public required string CommandText { get; init; }
    public int SortOrder { get; init; }
}

public sealed record NoteTextItem
{
    public required string Id { get; init; }
    public required string Text { get; init; }
    public int SortOrder { get; init; }
}
```

## WindowIdentity

```csharp
public sealed record WindowIdentity
{
    public required string ProcessName { get; init; }
    public required string WindowTitle { get; init; }
    public required string NormalizedWindowTitle { get; init; }

    public string? ExecutablePathHash { get; init; }
    public string? WindowClass { get; init; }
    public string? UserDefinedSessionName { get; init; }

    public int MatchVersion { get; init; } = 1;
}
```

## Storage Shape

Future storage location:

```text
%APPDATA%/SessionPad/
  settings.json
  sessions.index.json
  notes/
    <sessionId>.json
  backups/
    <sessionId>.<timestamp>.json
```

## Example Note JSON

```json
{
  "schemaVersion": 1,
  "sessionId": "example-session-id",
  "panelState": "DockedTab",
  "dockSide": "Right",
  "sections": {
    "pinned": [
      {
        "id": "p1",
        "text": "Do not modify generated runtime files.",
        "sortOrder": 0
      }
    ],
    "todo": [
      {
        "id": "t1",
        "text": "Reproduce failing test.",
        "isDone": false,
        "sortOrder": 0
      }
    ],
    "commands": [
      {
        "id": "c1",
        "label": "Run tests",
        "commandText": "pnpm test",
        "sortOrder": 0
      }
    ],
    "notes": [
      {
        "id": "n1",
        "text": "Auth mock returns undefined in the failing case.",
        "sortOrder": 0
      }
    ]
  },
  "createdAt": "2026-05-28T00:00:00Z",
  "updatedAt": "2026-05-28T00:00:00Z"
}
```

## Important Rule

Do not persist HWND.

HWND is a runtime handle only.

## Schema Versioning & Compatibility

`SchemaVersion` is stored on `SessionNote` and `SessionIndex` but is informational:
there is no version-branching migration code. Loading instead relies on tolerant
deserialization, and this contract is locked down by tests in
`tests/SessionPad.Tests/CompatibilityTests.cs`.

Rules for changing the on-disk shape:

- **Add fields additively only.** New fields must have a sensible default so older
  files (which lack them) load unchanged. Missing fields fall back to their model
  default (e.g. `SchemaVersion` → 1, `PanelState` → `CompactNote`, `SortOrder` → 0).
- **Never remove or rename a field** without an explicit migration. Doing so silently
  drops user data.
- **Loading is forward/backward tolerant.** A future `SchemaVersion` still loads, and
  unknown JSON properties are ignored without data loss.
- **Unknown enum values do not destroy the note.** A `PanelState` value this build does
  not recognize (e.g. one added by a newer version) falls back to the default via
  `TolerantPanelStateConverter`, so the rest of the note is preserved rather than lost.
- **Corrupt or partial files degrade gracefully.** Unreadable note JSON loads as
  `null` (the app recreates a default note) and an unreadable session index loads as
  empty — never a crash.

Separately, window-identity matching has its own `MatchVersion` (1 → 2): legacy v1
IDE sessions are reused and migrated to v2 project-level matching by `SessionMatcher`
(see `tests/SessionPad.Tests/SessionMatcherTests.cs`). This is identity matching, not
the storage schema, but follows the same "reuse existing data, never lose it" intent.