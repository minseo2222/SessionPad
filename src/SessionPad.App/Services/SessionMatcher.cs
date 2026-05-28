using System.Text.RegularExpressions;
using SessionPad.App.Models;

namespace SessionPad.App.Services;

public sealed class SessionMatcher
{
    private static readonly Regex WhitespacePattern = new(@"\s+", RegexOptions.Compiled);

    private static readonly HashSet<string> IdeProcessNames = new(StringComparer.Ordinal)
    {
        "code",
        "cursor",
        "windsurf"
    };

    private static readonly string[] AppSuffixes =
    [
        " - Visual Studio Code",
        " - Cursor",
        " - Windsurf",
        " - Windows Terminal"
    ];

    private static readonly string[] AdminPrefixes =
    [
        "Administrator: ",
        "\uAD00\uB9AC\uC790: "
    ];

    private readonly NoteStorageService _storageService;

    public SessionMatcher(NoteStorageService storageService)
    {
        _storageService = storageService;
    }

    public SessionSummary FindOrCreateSession(DetectedWindowInfo window)
    {
        var identity = CreateIdentity(window);
        var index = _storageService.LoadSessionIndex();
        var now = DateTimeOffset.UtcNow;
        var matchKey = CreateMatchKey(identity);

        var pinnedSessionIndex = -1;
        var pinnedSessionLastSeenAt = DateTimeOffset.MinValue;
        for (var i = 0; i < index.Sessions.Count; i++)
        {
            var session = index.Sessions[i];
            if (!session.IsPinned || session.Identity is null)
            {
                continue;
            }

            if (!string.Equals(
                NormalizeProcessName(session.Identity.ProcessName),
                identity.ProcessName,
                StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (pinnedSessionIndex < 0 || session.LastSeenAt > pinnedSessionLastSeenAt)
            {
                pinnedSessionIndex = i;
                pinnedSessionLastSeenAt = session.LastSeenAt;
            }
        }

        if (pinnedSessionIndex >= 0)
        {
            var session = index.Sessions[pinnedSessionIndex];
            var existingSessionId = string.IsNullOrWhiteSpace(session.SessionId)
                ? Guid.NewGuid().ToString("N")
                : session.SessionId;
            var updatedSession = session with
            {
                SessionId = existingSessionId,
                DisplayName = session.IsUserNamed ? session.DisplayName : CreateDisplayName(identity),
                IsUserNamed = session.IsUserNamed,
                IsPinned = true,
                Identity = identity,
                NoteFile = string.IsNullOrWhiteSpace(session.NoteFile)
                    ? $"notes/{existingSessionId}.json"
                    : session.NoteFile,
                LastSeenAt = now,
                RecentNormalizedTitles = UpdateRecentNormalizedTitles(
                    session.RecentNormalizedTitles,
                    identity.NormalizedWindowTitle)
            };

            index.Sessions[pinnedSessionIndex] = updatedSession;
            _storageService.SaveSessionIndex(index);
            return updatedSession;
        }

        for (var i = 0; i < index.Sessions.Count; i++)
        {
            var session = index.Sessions[i];
            if (session.Identity is null)
            {
                continue;
            }

            if (!string.Equals(CreateMatchKey(session.Identity), matchKey, StringComparison.Ordinal))
            {
                continue;
            }

            var existingSessionId = string.IsNullOrWhiteSpace(session.SessionId)
                ? Guid.NewGuid().ToString("N")
                : session.SessionId;
            var updatedSession = session with
            {
                SessionId = existingSessionId,
                DisplayName = session.IsUserNamed ? session.DisplayName : CreateDisplayName(identity),
                IsUserNamed = session.IsUserNamed,
                Identity = identity,
                NoteFile = string.IsNullOrWhiteSpace(session.NoteFile)
                    ? $"notes/{existingSessionId}.json"
                    : session.NoteFile,
                LastSeenAt = now,
                RecentNormalizedTitles = UpdateRecentNormalizedTitles(
                    session.RecentNormalizedTitles,
                    identity.NormalizedWindowTitle)
            };

            index.Sessions[i] = updatedSession;
            _storageService.SaveSessionIndex(index);
            return updatedSession;
        }

        if (IsIdeProcess(identity.ProcessName))
        {
            var legacySessionIndex = -1;
            var legacySessionLastSeenAt = DateTimeOffset.MinValue;
            for (var i = 0; i < index.Sessions.Count; i++)
            {
                var session = index.Sessions[i];
                if (session.Identity is null || session.Identity.MatchVersion >= 2)
                {
                    continue;
                }

                if (!string.Equals(
                    NormalizeProcessName(session.Identity.ProcessName),
                    identity.ProcessName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.Equals(
                    ExtractIdeProjectKey(session.Identity.WindowTitle),
                    identity.NormalizedWindowTitle,
                    StringComparison.Ordinal))
                {
                    continue;
                }

                if (legacySessionIndex < 0 || session.LastSeenAt > legacySessionLastSeenAt)
                {
                    legacySessionIndex = i;
                    legacySessionLastSeenAt = session.LastSeenAt;
                }
            }

            if (legacySessionIndex >= 0)
            {
                var session = index.Sessions[legacySessionIndex];
                var existingSessionId = string.IsNullOrWhiteSpace(session.SessionId)
                    ? Guid.NewGuid().ToString("N")
                    : session.SessionId;
                var updatedSession = session with
                {
                    SessionId = existingSessionId,
                    DisplayName = session.IsUserNamed ? session.DisplayName : CreateDisplayName(identity),
                    IsUserNamed = session.IsUserNamed,
                    IsPinned = session.IsPinned,
                    Identity = identity,
                    NoteFile = string.IsNullOrWhiteSpace(session.NoteFile)
                        ? $"notes/{existingSessionId}.json"
                        : session.NoteFile,
                    LastSeenAt = now,
                    RecentNormalizedTitles = UpdateRecentNormalizedTitles(
                        session.RecentNormalizedTitles,
                        identity.NormalizedWindowTitle)
                };

                index.Sessions[legacySessionIndex] = updatedSession;
                _storageService.SaveSessionIndex(index);
                return updatedSession;
            }
        }

        var sessionId = Guid.NewGuid().ToString("N");
        var newSession = new SessionSummary
        {
            SessionId = sessionId,
            DisplayName = CreateDisplayName(identity),
            Identity = identity,
            NoteFile = $"notes/{sessionId}.json",
            CreatedAt = now,
            LastSeenAt = now,
            RecentNormalizedTitles = string.IsNullOrWhiteSpace(identity.NormalizedWindowTitle)
                ? []
                : [identity.NormalizedWindowTitle]
        };

        index.Sessions.Add(newSession);
        _storageService.SaveSessionIndex(index);
        return newSession;
    }

    public string CreateMatchKey(WindowIdentity identity)
    {
        var processName = NormalizeProcessName(identity.ProcessName);
        var normalizedTitle = string.IsNullOrWhiteSpace(identity.NormalizedWindowTitle)
            ? "(untitled)"
            : identity.NormalizedWindowTitle;

        return $"{processName.ToLowerInvariant()}|{normalizedTitle.ToLowerInvariant()}";
    }

    private static WindowIdentity CreateIdentity(DetectedWindowInfo window)
    {
        var processName = NormalizeProcessName(window.ProcessName);
        var originalTitle = CollapseWhitespace(window.Title);
        var isIdeProcess = IsIdeProcess(processName);
        var normalizedTitle = isIdeProcess
            ? ExtractIdeProjectKey(originalTitle)
            : NormalizeWindowTitle(originalTitle);
        if (string.IsNullOrWhiteSpace(normalizedTitle))
        {
            normalizedTitle = string.IsNullOrWhiteSpace(originalTitle)
                ? "(untitled)"
                : originalTitle.ToLowerInvariant();
        }

        return new WindowIdentity
        {
            ProcessName = processName,
            WindowTitle = originalTitle,
            NormalizedWindowTitle = normalizedTitle,
            WindowClass = string.IsNullOrWhiteSpace(window.WindowClass) ? null : window.WindowClass.Trim(),
            MatchVersion = isIdeProcess ? 2 : 1
        };
    }

    private static string ExtractIdeProjectKey(string title)
    {
        var normalized = CollapseWhitespace(title);

        foreach (var suffix in AppSuffixes)
        {
            if (normalized.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized[..^suffix.Length].Trim();
                break;
            }
        }

        if (normalized.Length > 0 && normalized[0] is '\u25CF' or '\u2022' or '*')
        {
            normalized = normalized[1..].TrimStart();
        }

        var segments = normalized
            .Split(" - ", StringSplitOptions.None)
            .Select(CollapseWhitespace)
            .Where(segment => segment.Length > 0)
            .ToArray();

        var projectKey = segments.Length >= 2
            ? string.Join(" - ", segments.Skip(1))
            : normalized;

        projectKey = CollapseWhitespace(projectKey).ToLowerInvariant();
        return string.IsNullOrWhiteSpace(projectKey)
            ? NormalizeWindowTitle(title)
            : projectKey;
    }

    private static string NormalizeWindowTitle(string title)
    {
        var normalized = CollapseWhitespace(title);

        foreach (var suffix in AppSuffixes)
        {
            if (normalized.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized[..^suffix.Length].Trim();
                break;
            }
        }

        foreach (var prefix in AdminPrefixes)
        {
            if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized[prefix.Length..].Trim();
                break;
            }
        }

        return CollapseWhitespace(normalized).ToLowerInvariant();
    }

    private static string CreateDisplayName(WindowIdentity identity)
    {
        if (!string.IsNullOrWhiteSpace(identity.WindowTitle))
        {
            return identity.WindowTitle;
        }

        return string.IsNullOrWhiteSpace(identity.ProcessName)
            ? "Window session"
            : identity.ProcessName;
    }

    private static string NormalizeProcessName(string processName)
    {
        var normalized = CollapseWhitespace(processName);
        return string.IsNullOrWhiteSpace(normalized) || normalized == "(unknown)"
            ? "unknown"
            : normalized;
    }

    private static bool IsIdeProcess(string processName)
    {
        return IdeProcessNames.Contains(NormalizeProcessName(processName).ToLowerInvariant());
    }

    private static List<string> UpdateRecentNormalizedTitles(
        IEnumerable<string>? existingTitles,
        string normalizedTitle)
    {
        var updated = (existingTitles ?? Enumerable.Empty<string>())
            .Where(title => !string.IsNullOrWhiteSpace(title)
                && !string.Equals(title, normalizedTitle, StringComparison.Ordinal))
            .Take(4)
            .ToList();

        if (!string.IsNullOrWhiteSpace(normalizedTitle))
        {
            updated.Insert(0, normalizedTitle);
        }

        return updated;
    }

    private static string CollapseWhitespace(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : WhitespacePattern.Replace(value.Trim(), " ");
    }
}
