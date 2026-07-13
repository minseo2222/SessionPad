using System.IO;
using SessionPad.App.Models;
using SessionPad.App.Services;

namespace SessionPad.Tests;

/// <summary>
/// Pins the forward/backward compatibility contract for on-disk JSON: missing fields
/// fall back to defaults, future SchemaVersion and unknown fields load without loss,
/// and corrupt input degrades gracefully instead of crashing.
/// </summary>
public class CompatibilityTests
{
    private static NoteStorageService Store(string dir)
    {
        return new NoteStorageService(dir, new FakeClock(DateTimeOffset.UnixEpoch));
    }

    private static SessionSummary Session(string id)
    {
        return new SessionSummary { SessionId = id, NoteFile = $"notes/{id}.json" };
    }

    private static void WriteNoteFile(string dir, string id, string json)
    {
        var notesDir = Path.Combine(dir, "notes");
        Directory.CreateDirectory(notesDir);
        File.WriteAllText(Path.Combine(notesDir, $"{id}.json"), json);
    }

    [Fact]
    public void Missing_fields_fall_back_to_defaults()
    {
        using var dir = new TempDir();
        WriteNoteFile(dir.Path, "abc", """
        { "sessionId": "abc",
          "sections": { "pinned": [], "todo": [], "commands": [],
            "notes": [ { "id": "n1", "text": "keep" } ] } }
        """);

        var loaded = Store(dir.Path).LoadSessionNote(Session("abc"));

        Assert.NotNull(loaded);
        Assert.Equal("keep", loaded!.Sections.Notes.Single().Text);
        Assert.Equal(1, loaded.SchemaVersion);                 // default
        Assert.Equal(NotePanelState.CompactNote, loaded.PanelState); // default
        Assert.Equal(0, loaded.Sections.Notes.Single().SortOrder);  // default
    }

    [Fact]
    public void Future_schema_version_still_loads()
    {
        using var dir = new TempDir();
        WriteNoteFile(dir.Path, "abc", """
        { "schemaVersion": 12345, "sessionId": "abc", "panelState": "DockedTab",
          "sections": { "pinned": [], "todo": [], "commands": [],
            "notes": [ { "id": "n1", "text": "future" } ] } }
        """);

        var loaded = Store(dir.Path).LoadSessionNote(Session("abc"));

        Assert.NotNull(loaded);
        Assert.Equal("future", loaded!.Sections.Notes.Single().Text);
    }

    [Fact]
    public void Unknown_fields_in_index_are_ignored()
    {
        using var dir = new TempDir();
        var store = Store(dir.Path);
        File.WriteAllText(store.SessionIndexPath, """
        { "schemaVersion": 1, "futureToggle": true,
          "sessions": [ { "sessionId": "s1", "displayName": "one", "experimental": 7,
            "identity": { "processName": "code", "normalizedWindowTitle": "p1" } } ] }
        """);

        var index = store.LoadSessionIndex();

        Assert.Single(index.Sessions);
        Assert.Equal("s1", index.Sessions[0].SessionId);
        Assert.Equal("one", index.Sessions[0].DisplayName);
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("")]
    [InlineData("{ \"sessionId\": \"x\"")]
    public void Corrupt_note_returns_null_without_crashing(string garbage)
    {
        using var dir = new TempDir();
        WriteNoteFile(dir.Path, "abc", garbage);

        var loaded = Store(dir.Path).LoadSessionNote(Session("abc"));

        Assert.Null(loaded);
    }

    [Fact]
    public void Corrupt_index_returns_empty_without_crashing()
    {
        using var dir = new TempDir();
        var store = Store(dir.Path);
        File.WriteAllText(store.SessionIndexPath, "{ broken");

        var index = store.LoadSessionIndex();

        Assert.NotNull(index);
        Assert.Empty(index.Sessions);
    }

    [Fact]
    public void PanelState_round_trips_as_string()
    {
        using var dir = new TempDir();
        var store = Store(dir.Path);
        var session = Session("abc");
        store.SaveSessionNote(session, new SessionNote { SessionId = "abc", PanelState = NotePanelState.DockedTab });

        var loaded = store.LoadSessionNote(session);

        Assert.NotNull(loaded);
        Assert.Equal(NotePanelState.DockedTab, loaded!.PanelState);
    }

    [Fact]
    public void Unknown_panel_state_value_preserves_the_note()
    {
        // A panelState written by a newer version must not destroy the whole note;
        // the tolerant converter falls back to the default panel state.
        using var dir = new TempDir();
        WriteNoteFile(dir.Path, "abc", """
        { "sessionId": "abc", "panelState": "ExpandedNote",
          "sections": { "pinned": [], "todo": [], "commands": [],
            "notes": [ { "id": "n1", "text": "must survive" } ] } }
        """);

        var loaded = Store(dir.Path).LoadSessionNote(Session("abc"));

        Assert.NotNull(loaded);
        Assert.Equal("must survive", loaded!.Sections.Notes.Single().Text);
        Assert.Equal(NotePanelState.CompactNote, loaded.PanelState);
    }

    [Fact]
    public void LoadAllNotes_skips_a_session_whose_note_file_escapes_app_data()
    {
        using var dir = new TempDir();
        var store = Store(dir.Path);
        WriteNoteFile(dir.Path, "good", """
        { "sessionId": "good",
          "sections": { "pinned": [], "todo": [], "commands": [],
            "notes": [ { "id": "n1", "text": "keep" } ] } }
        """);
        var index = new SessionIndex();
        index.Sessions.Add(Session("good"));
        index.Sessions.Add(new SessionSummary { SessionId = "evil", NoteFile = "../outside.json" });
        store.SaveSessionIndex(index);

        var all = store.LoadAllNotes();

        Assert.Contains(all, n => n.Session?.SessionId == "good");
        Assert.DoesNotContain(all, n => n.Session?.SessionId == "evil");
    }

    [Fact]
    public void LoadAllNotes_does_not_crash_on_a_nested_traversal_entry()
    {
        using var dir = new TempDir();
        var store = Store(dir.Path);
        var index = new SessionIndex();
        index.Sessions.Add(new SessionSummary { SessionId = "evil", NoteFile = "notes/../../outside.json" });
        store.SaveSessionIndex(index);

        var all = store.LoadAllNotes(); // must not throw

        Assert.Empty(all);
    }

    [Fact]
    public void A_valid_session_still_loads_when_another_index_entry_is_malicious()
    {
        using var dir = new TempDir();
        var store = Store(dir.Path);
        store.SaveSessionNote(Session("good"), new SessionNote
        {
            SessionId = "good",
            Sections = new NoteSections { Notes = [new NoteTextItem { Id = "n1", Text = "hi" }] }
        });
        var index = new SessionIndex();
        index.Sessions.Add(new SessionSummary { SessionId = "evil", NoteFile = "../../evil.json" });
        index.Sessions.Add(Session("good"));
        store.SaveSessionIndex(index);

        var loaded = store.LoadAllNotes().Single(n => n.Session?.SessionId == "good");

        Assert.Equal("hi", loaded.Note.Sections.Notes.Single().Text);
    }
}
