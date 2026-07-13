using System.IO;
using SessionPad.App.Models;
using SessionPad.App.Services;

namespace SessionPad.Tests;

public class NoteStorageServiceTests
{
    private static NoteStorageService Store(string dir, IClock? clock = null)
    {
        return new NoteStorageService(dir, clock ?? new FakeClock(DateTimeOffset.UnixEpoch, TimeSpan.FromSeconds(1)));
    }

    private static SessionSummary Session(string id)
    {
        return new SessionSummary
        {
            SessionId = id,
            DisplayName = id,
            NoteFile = $"notes/{id}.json",
            Identity = new WindowIdentity { ProcessName = "code", NormalizedWindowTitle = id }
        };
    }

    private static SessionNote Note(string id, params string[] notes)
    {
        return new SessionNote
        {
            SchemaVersion = 1,
            SessionId = id,
            PanelState = NotePanelState.CompactNote,
            Sections = new NoteSections
            {
                Notes = notes
                    .Select((text, i) => new NoteTextItem { Id = $"n{i}", Text = text, SortOrder = i })
                    .ToList()
            }
        };
    }

    [Fact]
    public void Session_note_round_trips()
    {
        using var dir = new TempDir();
        var store = Store(dir.Path);
        var session = Session("abc");

        store.SaveSessionNote(session, Note("abc", "hello", "world"));
        var loaded = store.LoadSessionNote(session);

        Assert.NotNull(loaded);
        Assert.Equal(new[] { "hello", "world" }, loaded!.Sections.Notes.Select(n => n.Text));
    }

    [Fact]
    public void Loads_note_with_future_schema_and_unknown_fields()
    {
        using var dir = new TempDir();
        var store = Store(dir.Path);
        var notesDir = Path.Combine(dir.Path, "notes");
        Directory.CreateDirectory(notesDir);
        var json = """
        {
          "schemaVersion": 999,
          "sessionId": "abc",
          "panelState": "CompactNote",
          "unknownField": 42,
          "sections": {
            "pinned": [], "todo": [], "commands": [],
            "notes": [ { "id": "n1", "text": "kept", "sortOrder": 0, "extra": "x" } ]
          }
        }
        """;
        File.WriteAllText(Path.Combine(notesDir, "abc.json"), json);

        var loaded = store.LoadSessionNote(Session("abc"));

        Assert.NotNull(loaded);
        Assert.Equal("kept", loaded!.Sections.Notes.Single().Text);
    }

    [Fact]
    public void Atomic_save_leaves_no_temp_files()
    {
        using var dir = new TempDir();
        var store = Store(dir.Path);

        store.SaveSessionIndex(new SessionIndex());

        Assert.True(File.Exists(store.SessionIndexPath));
        Assert.Empty(Directory.GetFiles(dir.Path, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public void Backups_keep_only_the_latest_five()
    {
        using var dir = new TempDir();
        var store = Store(dir.Path, new FakeClock(DateTimeOffset.UnixEpoch, TimeSpan.FromSeconds(1)));
        var session = Session("abc");

        for (var i = 0; i < 7; i++)
        {
            store.SaveSessionNote(session, Note("abc", $"v{i}"));
        }

        Assert.Equal(5, Directory.GetFiles(store.BackupsDirectory, "abc.*.json").Length);
    }

    [Fact]
    public void LoadAllNotes_returns_default_plus_indexed_sessions()
    {
        using var dir = new TempDir();
        var store = Store(dir.Path);

        store.SaveDefaultNote(Note("default", "d"));
        var s1 = Session("s1");
        var s2 = Session("s2");
        store.SaveSessionNote(s1, Note("s1", "a"));
        store.SaveSessionNote(s2, Note("s2", "b"));
        var index = new SessionIndex();
        index.Sessions.Add(s1);
        index.Sessions.Add(s2);
        store.SaveSessionIndex(index);

        var all = store.LoadAllNotes();

        Assert.Equal(3, all.Count);
        Assert.Contains(all, n => n.Session is null);
        Assert.Contains(all, n => n.Session?.SessionId == "s1");
        Assert.Contains(all, n => n.Session?.SessionId == "s2");
    }

    [Fact]
    public void LoadSessionNote_rejects_parent_traversal_path()
    {
        using var dir = new TempDir();
        var store = Store(dir.Path);
        var malicious = Session("x") with { NoteFile = "../outside.json" };

        Assert.Throws<InvalidOperationException>(() => store.LoadSessionNote(malicious));
    }

    [Fact]
    public void LoadSessionNote_rejects_nested_traversal_path()
    {
        using var dir = new TempDir();
        var store = Store(dir.Path);
        var malicious = Session("x") with { NoteFile = "notes/../../outside.json" };

        Assert.Throws<InvalidOperationException>(() => store.LoadSessionNote(malicious));
    }

    [Fact]
    public void DeleteSession_with_malicious_note_file_removes_index_but_spares_outside_file()
    {
        using var dir = new TempDir();
        using var outside = new TempDir();
        var store = Store(dir.Path);

        // A file that lives outside AppDataDirectory and must never be deleted.
        var outsidePath = Path.Combine(outside.Path, "outside.json");
        File.WriteAllText(outsidePath, "{}");

        // A relative NoteFile that escapes AppDataDirectory up to the outside file.
        var escape = Path.GetRelativePath(store.AppDataDirectory, outsidePath).Replace('\\', '/');
        var malicious = new SessionSummary { SessionId = "evil", DisplayName = "evil", NoteFile = escape };
        var index = new SessionIndex();
        index.Sessions.Add(malicious);
        store.SaveSessionIndex(index);

        var deleted = store.DeleteSession("evil");

        Assert.True(deleted);
        Assert.Empty(store.LoadSessionIndex().Sessions);   // index entry removed
        Assert.True(File.Exists(outsidePath));             // outside file untouched
    }

    [Fact]
    public void SaveSessionNote_writes_backup_with_filename_safe_key_for_invalid_session_id()
    {
        using var dir = new TempDir();
        var store = Store(dir.Path);
        var session = new SessionSummary
        {
            SessionId = "a:b/c\\d*e",     // characters illegal in a Windows filename
            DisplayName = "x",
            NoteFile = "notes/safe.json"  // the note file itself stays valid + contained
        };

        store.SaveSessionNote(session, Note("x", "hi"));

        var backup = Assert.Single(Directory.GetFiles(store.BackupsDirectory));
        var name = Path.GetFileName(backup);
        Assert.True(
            name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0,
            $"backup filename still contains invalid characters: '{name}'");
        Assert.EndsWith(".json", name);
    }

    [Fact]
    public void Two_saves_in_the_same_clock_tick_create_distinct_backups()
    {
        using var dir = new TempDir();
        // step = zero: the clock returns the same timestamp on every read.
        var store = Store(dir.Path, new FakeClock(DateTimeOffset.UnixEpoch));
        var session = Session("abc");

        store.SaveSessionNote(session, Note("abc", "first"));
        store.SaveSessionNote(session, Note("abc", "second"));

        Assert.Equal(2, Directory.GetFiles(store.BackupsDirectory, "abc.*.json").Length);
    }
}
