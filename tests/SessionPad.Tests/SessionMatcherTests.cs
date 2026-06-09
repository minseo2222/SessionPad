using SessionPad.App.Models;
using SessionPad.App.Services;

namespace SessionPad.Tests;

public class SessionMatcherTests
{
    private static (SessionMatcher Matcher, NoteStorageService Storage) Create(string dir)
    {
        var storage = new NoteStorageService(dir, new FakeClock(DateTimeOffset.UnixEpoch, TimeSpan.FromSeconds(1)));
        return (new SessionMatcher(storage), storage);
    }

    [Theory]
    [InlineData("WindowsTerminal", "pwsh - Windows Terminal", "pwsh")]
    [InlineData("WindowsTerminal", "Administrator: Windows PowerShell", "windows powershell")]
    [InlineData("WindowsTerminal", "관리자: 명령 프롬프트", "명령 프롬프트")]
    [InlineData("notepad", "Untitled - Notepad", "untitled - notepad")]
    public void Normalizes_non_ide_titles(string process, string title, string expected)
    {
        using var dir = new TempDir();
        var (matcher, _) = Create(dir.Path);

        var session = matcher.FindOrCreateSession(Win.Make(process, title));

        Assert.Equal(expected, session.Identity.NormalizedWindowTitle);
    }

    [Fact]
    public void MatchKey_uses_lower_process_pipe_title()
    {
        using var dir = new TempDir();
        var (matcher, _) = Create(dir.Path);

        var withTitle = new WindowIdentity { ProcessName = "Code", NormalizedWindowTitle = "myproj" };
        var withoutTitle = new WindowIdentity { ProcessName = "Code", NormalizedWindowTitle = "" };

        Assert.Equal("code|myproj", matcher.CreateMatchKey(withTitle));
        Assert.Equal("code|(untitled)", matcher.CreateMatchKey(withoutTitle));
    }

    [Fact]
    public void MatchKey_from_window_is_consistent_and_side_effect_free()
    {
        using var dir = new TempDir();
        var (matcher, storage) = Create(dir.Path);

        var fileA = matcher.CreateMatchKey(Win.Make("code", "fileA.cs - myproj - Visual Studio Code"));
        var fileB = matcher.CreateMatchKey(Win.Make("code", "fileB.ts - myproj - Visual Studio Code"));
        var other = matcher.CreateMatchKey(Win.Make("code", "x.cs - otherproj - Visual Studio Code"));

        Assert.Equal(fileA, fileB);
        Assert.NotEqual(fileA, other);
        Assert.Empty(storage.LoadSessionIndex().Sessions);
    }

    [Fact]
    public void MatchKey_from_window_collapses_volatile_whitespace_and_case()
    {
        using var dir = new TempDir();
        var (matcher, _) = Create(dir.Path);

        var plain = matcher.CreateMatchKey(Win.Make("pwsh", "MyRepo"));
        var padded = matcher.CreateMatchKey(Win.Make("pwsh", "  MyRepo  "));

        Assert.Equal(plain, padded);
    }

    [Fact]
    public void Ide_matches_by_project_not_by_file()
    {
        using var dir = new TempDir();
        var (matcher, _) = Create(dir.Path);

        var fileA = matcher.FindOrCreateSession(Win.Make("code", "fileA.cs - myproj - Visual Studio Code"));
        var fileB = matcher.FindOrCreateSession(Win.Make("code", "fileB.ts - myproj - Visual Studio Code"));
        var other = matcher.FindOrCreateSession(Win.Make("code", "x.cs - otherproj - Visual Studio Code"));

        Assert.Equal(fileA.SessionId, fileB.SessionId);
        Assert.NotEqual(fileA.SessionId, other.SessionId);
        Assert.Equal(2, fileA.Identity.MatchVersion);
    }

    [Fact]
    public void Pinned_session_matches_same_process_ignoring_title()
    {
        using var dir = new TempDir();
        var (matcher, storage) = Create(dir.Path);

        var first = matcher.FindOrCreateSession(Win.Make("code", "a.cs - proj1 - Visual Studio Code"));

        var index = storage.LoadSessionIndex();
        var i = index.Sessions.FindIndex(s => s.SessionId == first.SessionId);
        index.Sessions[i] = index.Sessions[i] with { IsPinned = true };
        storage.SaveSessionIndex(index);

        var withDifferentTitle = matcher.FindOrCreateSession(
            Win.Make("code", "totally.ts - different - Visual Studio Code"));

        Assert.Equal(first.SessionId, withDifferentTitle.SessionId);
    }

    [Fact]
    public void Legacy_v1_ide_session_is_reused_and_migrated()
    {
        using var dir = new TempDir();
        var (matcher, storage) = Create(dir.Path);

        var legacy = new SessionSummary
        {
            SessionId = "legacy123",
            DisplayName = "old",
            NoteFile = "notes/legacy123.json",
            Identity = new WindowIdentity
            {
                ProcessName = "code",
                WindowTitle = "old.cs - proj1 - Visual Studio Code",
                NormalizedWindowTitle = "old.cs - proj1 - visual studio code",
                MatchVersion = 1
            }
        };
        var index = new SessionIndex();
        index.Sessions.Add(legacy);
        storage.SaveSessionIndex(index);

        var matched = matcher.FindOrCreateSession(Win.Make("code", "new.ts - proj1 - Visual Studio Code"));

        Assert.Equal("legacy123", matched.SessionId);
        Assert.Equal(2, matched.Identity.MatchVersion);
    }
}
