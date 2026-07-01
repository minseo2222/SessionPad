namespace SessionPad.App.Services;

/// <summary>
/// Persistence for user settings (theme, auto-track, attach hotkey). Abstracted so
/// view models can be unit tested without touching the on-disk settings file.
/// </summary>
public interface ISettingsService
{
    string LoadTheme();

    void SaveTheme(string theme);

    bool LoadAutoTrackForeground();

    void SaveAutoTrackForeground(bool value);

    string LoadHotkey();

    void SaveHotkey(string token);
}
