namespace SessionPad.App.Models;

public sealed record SessionNote
{
    public int SchemaVersion { get; init; } = 1;

    public string SessionId { get; init; } = "default";

    public NotePanelState PanelState { get; init; } = NotePanelState.CompactNote;

    public NoteSections Sections { get; init; } = new();

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}

public enum NotePanelState
{
    DockedTab,
    CompactNote
}
