namespace SessionPad.App.Models;

public sealed record SessionIndex
{
    public int SchemaVersion { get; init; } = 1;

    public List<SessionSummary> Sessions { get; init; } = new();
}
