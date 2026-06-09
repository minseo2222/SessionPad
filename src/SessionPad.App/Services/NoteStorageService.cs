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

    private const int MaxBackupsPerSession = 5;

    private readonly IClock _clock;

    public NoteStorageService()
        : this(DefaultAppDataDirectory(), new SystemClock())
    {
    }

    public NoteStorageService(IClock clock)
        : this(DefaultAppDataDirectory(), clock)
    {
    }

    public NoteStorageService(string baseDirectory, IClock clock)
    {
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            throw new ArgumentException("The base directory must not be empty.", nameof(baseDirectory));
        }

        AppDataDirectory = baseDirectory;
        _clock = clock;
    }

    public string AppDataDirectory { get; }

    private static string DefaultAppDataDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SessionPad");
    }

    public string NotesDirectory => Path.Combine(AppDataDirectory, "notes");

    public string BackupsDirectory => Path.Combine(AppDataDirectory, "backups");

    public string SessionIndexPath => Path.Combine(AppDataDirectory, "sessions.index.json");

    public string DefaultNotePath => Path.Combine(NotesDirectory, "default.json");

    public SessionNote? LoadDefaultNote()
    {
        return LoadNoteFromPath(DefaultNotePath);
    }

    public void SaveDefaultNote(SessionNote note)
    {
        SaveJsonAtomic(DefaultNotePath, note);
        TryWriteBackup("default", note);
    }

    public SessionNote? LoadSessionNote(SessionSummary session)
    {
        return LoadNoteFromPath(GetAbsoluteStoragePath(session.NoteFile));
    }

    public void SaveSessionNote(SessionSummary session, SessionNote note)
    {
        SaveJsonAtomic(GetAbsoluteStoragePath(session.NoteFile), note);
        TryWriteBackup(DeriveBackupKey(session), note);
    }

    public IReadOnlyList<StoredNote> LoadAllNotes()
    {
        var results = new List<StoredNote>();

        var defaultNote = LoadDefaultNote();
        if (defaultNote is not null)
        {
            results.Add(new StoredNote(null, "Default local note", defaultNote));
        }

        foreach (var session in LoadSessionIndex().Sessions)
        {
            var note = LoadSessionNote(session);
            if (note is not null)
            {
                var name = string.IsNullOrWhiteSpace(session.DisplayName)
                    ? "Window session"
                    : session.DisplayName;
                results.Add(new StoredNote(session, name, note));
            }
        }

        return results;
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

    private static string DeriveBackupKey(SessionSummary session)
    {
        return string.IsNullOrWhiteSpace(session.SessionId) ? "session" : session.SessionId;
    }

    private void TryWriteBackup(string sessionKey, SessionNote note)
    {
        // Best-effort: a backup failure must never break the primary save.
        try
        {
            Directory.CreateDirectory(BackupsDirectory);
            var timestamp = _clock.UtcNow.ToString("yyyyMMddHHmmssfff");
            var backupPath = Path.Combine(BackupsDirectory, $"{sessionKey}.{timestamp}.json");
            File.WriteAllText(backupPath, JsonSerializer.Serialize(note, JsonOptions));
            PruneBackups(sessionKey);
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or JsonException)
        {
            Debug.WriteLine($"SessionPad could not write a backup for '{sessionKey}': {ex.Message}");
        }
    }

    private void PruneBackups(string sessionKey)
    {
        try
        {
            var prefix = sessionKey + ".";
            var stale = Directory.GetFiles(BackupsDirectory, $"{sessionKey}.*.json")
                .Where(path => Path.GetFileName(path).StartsWith(prefix, StringComparison.Ordinal))
                .OrderByDescending(path => path, StringComparer.Ordinal)
                .Skip(MaxBackupsPerSession)
                .ToList();

            foreach (var file in stale)
            {
                File.Delete(file);
            }
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or NotSupportedException)
        {
            Debug.WriteLine($"SessionPad could not prune backups for '{sessionKey}': {ex.Message}");
        }
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

    public sealed record StoredNote(SessionSummary? Session, string DisplayName, SessionNote Note);
}
