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
        // Tolerant converter first so an unknown NotePanelState falls back to the
        // default instead of throwing (and losing the whole note); the string enum
        // converter still handles any other enums.
        Converters = { new TolerantPanelStateConverter(), new JsonStringEnumConverter() }
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
            SessionNote? note;
            try
            {
                note = LoadSessionNote(session);
            }
            catch (InvalidOperationException ex)
            {
                // A corrupt or malicious index entry (e.g. a NoteFile that escapes the
                // app data directory) is rejected by GetAbsoluteStoragePath. Skip it so a
                // single bad entry never crashes the session list; the rejection stands.
                Debug.WriteLine(
                    $"SessionPad skipped an unloadable session '{session.SessionId}': {ex.Message}");
                continue;
            }

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

    /// <summary>
    /// Removes one session from the index and deletes its note file and backups.
    /// File deletion is best-effort: the index entry is removed even if a file
    /// cannot be deleted. The default note is never touched. Returns false when
    /// no session with the given id exists (no-op).
    /// </summary>
    public bool DeleteSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return false;
        }

        var index = LoadSessionIndex();
        var session = index.Sessions.FirstOrDefault(existing =>
            string.Equals(existing.SessionId, sessionId, StringComparison.Ordinal));
        if (session is null)
        {
            return false;
        }

        index.Sessions.Remove(session);
        SaveSessionIndex(index);
        TryDeleteSessionFiles(session);
        return true;
    }

    private void TryDeleteSessionFiles(SessionSummary session)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(session.NoteFile) && !Path.IsPathRooted(session.NoteFile))
            {
                var notePath = GetAbsoluteStoragePath(session.NoteFile);
                if (File.Exists(notePath))
                {
                    File.Delete(notePath);
                }
            }

            if (Directory.Exists(BackupsDirectory))
            {
                var backupStem = SanitizeFileStem(DeriveBackupKey(session));
                var prefix = backupStem + ".";
                foreach (var backup in Directory.GetFiles(BackupsDirectory, $"{backupStem}.*.json"))
                {
                    if (Path.GetFileName(backup).StartsWith(prefix, StringComparison.Ordinal))
                    {
                        File.Delete(backup);
                    }
                }
            }
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or InvalidOperationException)
        {
            Debug.WriteLine(
                $"SessionPad could not delete files for session '{session.SessionId}': {ex.Message}");
        }
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

        var combined = Path.Combine(AppDataDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var fullPath = Path.GetFullPath(combined);

        var root = Path.GetFullPath(AppDataDirectory);
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;

        if (!string.Equals(fullPath, root, StringComparison.OrdinalIgnoreCase)
            && !fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Session storage paths must stay inside the app data directory.");
        }

        return fullPath;
    }

    private static string DeriveBackupKey(SessionSummary session)
    {
        return string.IsNullOrWhiteSpace(session.SessionId) ? "session" : session.SessionId;
    }

    /// <summary>
    /// Reduces an arbitrary key (which may include characters illegal in a file name,
    /// such as a SessionId) to a filename-safe stem for backup files.
    /// </summary>
    private static string SanitizeFileStem(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return "session";
        }

        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(key
            .Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c)
            .ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "session" : sanitized;
    }

    private void TryWriteBackup(string sessionKey, SessionNote note)
    {
        // Best-effort: a backup failure must never break the primary save.
        var stem = SanitizeFileStem(sessionKey);
        try
        {
            Directory.CreateDirectory(BackupsDirectory);
            var timestamp = _clock.UtcNow.ToString("yyyyMMddHHmmssfff");
            var backupPath = NextAvailableBackupPath(stem, timestamp);
            File.WriteAllText(backupPath, JsonSerializer.Serialize(note, JsonOptions));
            PruneBackups(stem);
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or JsonException)
        {
            Debug.WriteLine($"SessionPad could not write a backup for '{sessionKey}': {ex.Message}");
        }
    }

    /// <summary>
    /// Returns a backup path that does not yet exist, so two saves that land on the
    /// same timestamp (e.g. within one clock tick) produce distinct files instead of
    /// one overwriting the other.
    /// </summary>
    private string NextAvailableBackupPath(string stem, string timestamp)
    {
        var path = Path.Combine(BackupsDirectory, $"{stem}.{timestamp}.json");
        var counter = 1;
        while (File.Exists(path))
        {
            path = Path.Combine(BackupsDirectory, $"{stem}.{timestamp}.{counter}.json");
            counter++;
        }

        return path;
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
