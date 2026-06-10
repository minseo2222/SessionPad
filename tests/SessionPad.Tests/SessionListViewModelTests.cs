using SessionPad.App.Models;
using SessionPad.App.Services;
using SessionPad.App.ViewModels;

namespace SessionPad.Tests;

public class SessionListViewModelTests
{
    private static (FloatingNoteViewModel Vm, NoteStorageService Store, SessionSummary Old, SessionSummary Recent) Create(string dir)
    {
        var store = new NoteStorageService(dir, new FakeClock(DateTimeOffset.UnixEpoch, TimeSpan.FromSeconds(1)));
        var old = new SessionSummary
        {
            SessionId = "old",
            DisplayName = "Old session",
            NoteFile = "notes/old.json",
            LastSeenAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            Identity = new WindowIdentity { ProcessName = "code", NormalizedWindowTitle = "old" }
        };
        var recent = new SessionSummary
        {
            SessionId = "recent",
            DisplayName = "Recent session",
            NoteFile = "notes/recent.json",
            LastSeenAt = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            Identity = new WindowIdentity { ProcessName = "code", NormalizedWindowTitle = "recent" }
        };
        var index = new SessionIndex();
        index.Sessions.Add(old);
        index.Sessions.Add(recent);
        store.SaveSessionIndex(index);

        var vm = new FloatingNoteViewModel(store, new LocalDataService(), new ClipboardService());
        return (vm, store, old, recent);
    }

    [Fact]
    public void Opening_settings_lists_sessions_most_recent_first()
    {
        using var dir = new TempDir();
        var (vm, _, _, _) = Create(dir.Path);

        vm.IsSettingsOpen = true;

        Assert.Equal(2, vm.Sessions.Count);
        Assert.Equal("recent", vm.Sessions[0].Session.SessionId);
        Assert.Equal("old", vm.Sessions[1].Session.SessionId);
    }

    [Fact]
    public void Confirmed_delete_removes_row_and_index_entry()
    {
        using var dir = new TempDir();
        var (vm, store, old, _) = Create(dir.Path);
        vm.IsSettingsOpen = true;
        var row = vm.Sessions.Single(r => r.Session.SessionId == old.SessionId);

        vm.RequestDeleteSessionCommand.Execute(row);
        Assert.True(row.IsDeleteConfirmPending);
        vm.ConfirmDeleteSessionCommand.Execute(row);

        Assert.Single(vm.Sessions);
        Assert.Single(store.LoadSessionIndex().Sessions);
        Assert.Equal("recent", vm.Sessions[0].Session.SessionId);
    }

    [Fact]
    public void Cancel_keeps_the_session()
    {
        using var dir = new TempDir();
        var (vm, store, old, _) = Create(dir.Path);
        vm.IsSettingsOpen = true;
        var row = vm.Sessions.Single(r => r.Session.SessionId == old.SessionId);

        vm.RequestDeleteSessionCommand.Execute(row);
        vm.CancelDeleteSessionCommand.Execute(row);

        Assert.False(row.IsDeleteConfirmPending);
        Assert.Equal(2, vm.Sessions.Count);
        Assert.Equal(2, store.LoadSessionIndex().Sessions.Count);
    }

    [Fact]
    public void Deleting_the_current_session_returns_to_the_default_note()
    {
        using var dir = new TempDir();
        var (vm, _, _, recent) = Create(dir.Path);
        vm.LoadWindowSession(recent, "code|recent");
        Assert.Equal("recent", vm.CurrentSessionId);

        vm.IsSettingsOpen = true;
        var row = vm.Sessions.Single(r => r.Session.SessionId == recent.SessionId);
        vm.RequestDeleteSessionCommand.Execute(row);
        vm.ConfirmDeleteSessionCommand.Execute(row);

        Assert.Equal("default", vm.CurrentSessionId);
    }
}
