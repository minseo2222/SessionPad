using System.IO;
using SessionPad.App.Models;
using SessionPad.App.Services;

namespace SessionPad.Tests;

public class SessionDeleteTests
{
    private static NoteStorageService Store(string dir)
    {
        return new NoteStorageService(dir, new FakeClock(DateTimeOffset.UnixEpoch, TimeSpan.FromSeconds(1)));
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

    private static SessionNote Note(string id)
    {
        return new SessionNote
        {
            SessionId = id,
            Sections = new NoteSections
            {
                Notes = [new NoteTextItem { Id = "n1", Text = $"note for {id}" }]
            }
        };
    }

    private static (NoteStorageService Store, SessionSummary Doomed, SessionSummary Kept) Seed(string dir)
    {
        var store = Store(dir);
        var doomed = Session("doomed");
        var kept = Session("kept");
        store.SaveSessionNote(doomed, Note("doomed"));
        store.SaveSessionNote(kept, Note("kept"));
        var index = new SessionIndex();
        index.Sessions.Add(doomed);
        index.Sessions.Add(kept);
        store.SaveSessionIndex(index);
        return (store, doomed, kept);
    }

    [Fact]
    public void Removes_only_the_target_index_entry()
    {
        using var dir = new TempDir();
        var (store, doomed, kept) = Seed(dir.Path);

        var deleted = store.DeleteSession(doomed.SessionId);

        Assert.True(deleted);
        var remaining = store.LoadSessionIndex().Sessions;
        Assert.Single(remaining);
        Assert.Equal(kept.SessionId, remaining[0].SessionId);
    }

    [Fact]
    public void Deletes_the_note_file()
    {
        using var dir = new TempDir();
        var (store, doomed, kept) = Seed(dir.Path);

        store.DeleteSession(doomed.SessionId);

        Assert.False(File.Exists(Path.Combine(dir.Path, "notes", "doomed.json")));
        Assert.True(File.Exists(Path.Combine(dir.Path, "notes", "kept.json")));
    }

    [Fact]
    public void Deletes_only_that_sessions_backups()
    {
        using var dir = new TempDir();
        var (store, doomed, _) = Seed(dir.Path);
        Assert.NotEmpty(Directory.GetFiles(store.BackupsDirectory, "doomed.*.json"));

        store.DeleteSession(doomed.SessionId);

        Assert.Empty(Directory.GetFiles(store.BackupsDirectory, "doomed.*.json"));
        Assert.NotEmpty(Directory.GetFiles(store.BackupsDirectory, "kept.*.json"));
    }

    [Fact]
    public void Unknown_id_is_a_noop()
    {
        using var dir = new TempDir();
        var (store, _, _) = Seed(dir.Path);

        var deleted = store.DeleteSession("does-not-exist");

        Assert.False(deleted);
        Assert.Equal(2, store.LoadSessionIndex().Sessions.Count);
    }

    [Fact]
    public void Missing_note_file_still_removes_the_index_entry()
    {
        using var dir = new TempDir();
        var (store, doomed, _) = Seed(dir.Path);
        File.Delete(Path.Combine(dir.Path, "notes", "doomed.json"));

        var deleted = store.DeleteSession(doomed.SessionId);

        Assert.True(deleted);
        Assert.Single(store.LoadSessionIndex().Sessions);
    }
}
