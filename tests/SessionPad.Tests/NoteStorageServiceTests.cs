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
}
