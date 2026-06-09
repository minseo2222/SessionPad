namespace SessionPad.App.Services;

/// <summary>
/// Abstracts the current time so time-dependent behavior (backup timestamps)
/// can be made deterministic in tests.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
