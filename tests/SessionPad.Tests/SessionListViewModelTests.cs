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

    [Fact]
    public void Collapse_and_expand_persist_panel_state()
    {
        using var dir = new TempDir();
        var store = new NoteStorageService(dir.Path, new FakeClock(DateTimeOffset.UnixEpoch, TimeSpan.FromSeconds(1)));
        var vm = new FloatingNoteViewModel(store, new LocalDataService(), new ClipboardService());

        vm.CollapseCommand.Execute(null);

        var afterCollapse = store.LoadDefaultNote();
        Assert.NotNull(afterCollapse);
        Assert.Equal(NotePanelState.DockedTab, afterCollapse!.PanelState);

        vm.ExpandCommand.Execute(null);

        Assert.Equal(NotePanelState.CompactNote, vm.PanelState);
        var afterExpand = store.LoadDefaultNote();
        Assert.NotNull(afterExpand);
        Assert.Equal(NotePanelState.CompactNote, afterExpand!.PanelState);
    }

    [Fact]
    public void Fresh_default_note_is_usable_and_has_no_dev_or_slice_text()
    {
        using var dir = new TempDir();
        // A fresh, empty data directory: no saved default note, so the view model
        // populates the built-in first-run default content.
        var store = new NoteStorageService(dir.Path, new FakeClock(DateTimeOffset.UnixEpoch, TimeSpan.FromSeconds(1)));
        var vm = new FloatingNoteViewModel(store, new LocalDataService(), new ClipboardService());

        // Fresh launch still creates a usable default note (every section seeded).
        Assert.NotEmpty(vm.PinnedItems);
        Assert.NotEmpty(vm.TodoItems);
        Assert.NotEmpty(vm.CommandItems);
        Assert.NotEmpty(vm.NoteItems);

        var defaultText = vm.PinnedItems.Select(i => i.Text)
            .Concat(vm.TodoItems.Select(i => i.Text))
            .Concat(vm.CommandItems.Select(i => i.Text))
            .Concat(vm.NoteItems.Select(i => i.Text));

        foreach (var text in defaultText)
        {
            var lower = text.ToLowerInvariant();
            Assert.False(lower.Contains("slice"), $"default content must not mention 'Slice': '{text}'");
            Assert.False(lower.Contains("mvp slice"), $"default content must not mention 'MVP slice': '{text}'");
            Assert.False(lower.Contains("dotnet build"), $"default content must not mention 'dotnet build': '{text}'");
        }
    }
}
