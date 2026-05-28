using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SessionPad.App.Models;

namespace SessionPad.App.Services;

public sealed class NoteStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public string DefaultNotePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SessionPad",
        "notes",
        "default.json");

    public SessionNote? LoadDefaultNote()
    {
        if (!File.Exists(DefaultNotePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(DefaultNotePath);
            return JsonSerializer.Deserialize<SessionNote>(json, JsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or IOException)
        {
            Debug.WriteLine($"SessionPad could not load saved note '{DefaultNotePath}': {ex.Message}");
            return null;
        }
    }

    public void SaveDefaultNote(SessionNote note)
    {
        var directory = Path.GetDirectoryName(DefaultNotePath);
        if (directory is null)
        {
            throw new InvalidOperationException("The default note path does not have a parent directory.");
        }

        Directory.CreateDirectory(directory);

        var temporaryPath = Path.Combine(directory, $"{Path.GetFileName(DefaultNotePath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            var json = JsonSerializer.Serialize(note, JsonOptions);
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, DefaultNotePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
