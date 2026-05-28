namespace SessionPad.App.Models;

public sealed record WindowIdentity
{
    public string ProcessName { get; init; } = string.Empty;

    public string WindowTitle { get; init; } = string.Empty;

    public string NormalizedWindowTitle { get; init; } = string.Empty;

    public string? WindowClass { get; init; }

    public int MatchVersion { get; init; } = 1;
}
