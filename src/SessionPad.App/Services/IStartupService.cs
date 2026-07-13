namespace SessionPad.App.Services;

/// <summary>
/// The Windows "start on login" toggle. Abstracted so view models can be unit tested
/// without reading or writing the current-user Run registry key.
/// </summary>
public interface IStartupService
{
    bool IsEnabled();

    bool Enable(out string? error);

    bool Disable(out string? error);
}
