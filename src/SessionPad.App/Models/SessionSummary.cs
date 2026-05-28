namespace SessionPad.App.Models;

public sealed record SessionSummary
{
    public string SessionId { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public bool IsUserNamed { get; init; }

    public WindowIdentity Identity { get; init; } = new();

    public string NoteFile { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset LastSeenAt { get; init; }

    public List<string> RecentNormalizedTitles { get; init; } = new();
}
