using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace SessionPad.App.Services;

public sealed class SettingsService
{
    private const string AppDirectoryName = "SessionPad";
    private const string SettingsFileName = "settings.json";
    private const string DefaultTheme = "Dark";
    private const string DefaultHotkey = "Ctrl+Alt+N";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public string AppDataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppDirectoryName);

    public string SettingsPath => Path.Combine(AppDataDirectory, SettingsFileName);

    public string LoadTheme()
    {
        return NormalizeTheme(Load().Theme);
    }

    public void SaveTheme(string theme)
    {
        Save(Load() with { Theme = NormalizeTheme(theme) });
    }

    public bool LoadAutoTrackForeground()
    {
        return Load().AutoTrackForeground;
    }

    public void SaveAutoTrackForeground(bool value)
    {
        Save(Load() with { AutoTrackForeground = value });
    }

    public string LoadHotkey()
    {
        var hotkey = Load().Hotkey;
        return string.IsNullOrWhiteSpace(hotkey) ? DefaultHotkey : hotkey;
    }

    public void SaveHotkey(string token)
    {
        Save(Load() with { Hotkey = string.IsNullOrWhiteSpace(token) ? DefaultHotkey : token });
    }

    private AppSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return new AppSettings(DefaultTheme);
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            return settings ?? new AppSettings(DefaultTheme);
        }
        catch (Exception ex) when (ex is JsonException
            or NotSupportedException
            or IOException
            or UnauthorizedAccessException)
        {
            Debug.WriteLine($"SessionPad could not load settings '{SettingsPath}': {ex.Message}");
            return new AppSettings(DefaultTheme);
        }
    }

    private void Save(AppSettings settings)
    {
        try
        {
            SaveJsonAtomic(SettingsPath, settings with { Theme = NormalizeTheme(settings.Theme) });
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or InvalidOperationException
            or NotSupportedException)
        {
            Debug.WriteLine($"SessionPad could not save settings '{SettingsPath}': {ex.Message}");
        }
    }

    private static string NormalizeTheme(string? theme)
    {
        return string.Equals(theme, "Light", StringComparison.OrdinalIgnoreCase)
            ? "Light"
            : DefaultTheme;
    }

    private static void SaveJsonAtomic<T>(string path, T value)
    {
        var directory = Path.GetDirectoryName(path);
        if (directory is null)
        {
            throw new InvalidOperationException("The settings path does not have a parent directory.");
        }

        Directory.CreateDirectory(directory);

        var temporaryPath = Path.Combine(directory, $"{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private sealed record AppSettings(
        string Theme,
        bool AutoTrackForeground = false,
        string Hotkey = DefaultHotkey);
}
