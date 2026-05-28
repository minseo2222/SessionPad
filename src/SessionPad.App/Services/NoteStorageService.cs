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

    public string AppDataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SessionPad");

    public string NotesDirectory => Path.Combine(AppDataDirectory, "notes");

    public string SessionIndexPath => Path.Combine(AppDataDirectory, "sessions.index.json");

    public string DefaultNotePath => Path.Combine(NotesDirectory, "default.json");

    public SessionNote? LoadDefaultNote()
    {
        return LoadNoteFromPath(DefaultNotePath);
    }

    public void SaveDefaultNote(SessionNote note)
    {
        SaveJsonAtomic(DefaultNotePath, note);
    }

    public SessionNote? LoadSessionNote(SessionSummary session)
    {
        return LoadNoteFromPath(GetAbsoluteStoragePath(session.NoteFile));
    }

    public void SaveSessionNote(SessionSummary session, SessionNote note)
    {
        SaveJsonAtomic(GetAbsoluteStoragePath(session.NoteFile), note);
    }

    public SessionIndex LoadSessionIndex()
    {
        if (!File.Exists(SessionIndexPath))
        {
            return new SessionIndex();
        }

        try
        {
            var json = File.ReadAllText(SessionIndexPath);
            var index = JsonSerializer.Deserialize<SessionIndex>(json, JsonOptions);
            return index?.Sessions is null ? new SessionIndex() : index;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"SessionPad could not load session index '{SessionIndexPath}': {ex.Message}");
            return new SessionIndex();
        }
    }

    public void SaveSessionIndex(SessionIndex index)
    {
        SaveJsonAtomic(SessionIndexPath, index);
    }

    private SessionNote? LoadNoteFromPath(string notePath)
    {
        if (!File.Exists(notePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(notePath);
            var note = JsonSerializer.Deserialize<SessionNote>(json, JsonOptions);
            return note?.Sections is null ? null : note;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"SessionPad could not load saved note '{notePath}': {ex.Message}");
            return null;
        }
    }

    private string GetAbsoluteStoragePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new InvalidOperationException("The storage path is empty.");
        }

        if (Path.IsPathRooted(relativePath))
        {
            throw new InvalidOperationException("Session storage paths must be relative.");
        }

        return Path.Combine(AppDataDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static void SaveJsonAtomic<T>(string path, T value)
    {
        var directory = Path.GetDirectoryName(path);
        if (directory is null)
        {
            throw new InvalidOperationException("The storage path does not have a parent directory.");
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
}
