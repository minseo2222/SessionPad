using System.ComponentModel;
using System.Runtime.CompilerServices;
using SessionPad.App.Models;

namespace SessionPad.App.ViewModels;

public sealed class SearchResultViewModel
{
    public SearchResultViewModel(
        SessionSummary? session,
        string sessionName,
        string sectionLabel,
        string snippet,
        int matchCount)
    {
        Session = session;
        SessionName = sessionName;
        SectionLabel = sectionLabel;
        Snippet = snippet;
        MatchCount = matchCount;
    }

    public SessionSummary? Session { get; }

    public string SessionName { get; }

    public string SectionLabel { get; }

    public string Snippet { get; }

    public int MatchCount { get; }

    public string MatchSummary => MatchCount == 1
        ? $"{SectionLabel} · 1 match"
        : $"{SectionLabel} · {MatchCount} matches";
}

public sealed class SessionRowViewModel : INotifyPropertyChanged
{
    private bool _isDeleteConfirmPending;

    public SessionRowViewModel(SessionSummary session)
    {
        Session = session;
        DisplayName = string.IsNullOrWhiteSpace(session.DisplayName)
            ? "Window session"
            : session.DisplayName;
        ProcessName = string.IsNullOrWhiteSpace(session.Identity.ProcessName)
            ? "(unknown)"
            : session.Identity.ProcessName;
        LastSeenText = session.LastSeenAt == default
            ? "never"
            : session.LastSeenAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        IsPinned = session.IsPinned;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public SessionSummary Session { get; }

    public string DisplayName { get; }

    public string ProcessName { get; }

    public string LastSeenText { get; }

    public bool IsPinned { get; }

    public string Detail => $"{ProcessName} · {LastSeenText}";

    public bool IsDeleteConfirmPending
    {
        get => _isDeleteConfirmPending;
        set
        {
            if (_isDeleteConfirmPending == value)
            {
                return;
            }

            _isDeleteConfirmPending = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDeleteConfirmPending)));
        }
    }
}

public sealed class PinnedItemViewModel
{
    public PinnedItemViewModel(string id, string text)
    {
        Id = id;
        Text = text;
    }

    public string Id { get; }

    public string Text { get; }
}

public sealed class TodoItemViewModel : INotifyPropertyChanged
{
    private bool _isDone;

    public TodoItemViewModel(string id, string text, bool isDone = false)
    {
        Id = id;
        Text = text;
        _isDone = isDone;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id { get; }

    public string Text { get; }

    public bool IsDone
    {
        get => _isDone;
        set
        {
            if (_isDone == value)
            {
                return;
            }

            _isDone = value;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class CommandItemViewModel
{
    public CommandItemViewModel(string id, string text)
    {
        Id = id;
        Text = text;
    }

    public string Id { get; }

    public string Text { get; }
}

public sealed class NoteItemViewModel : INotifyPropertyChanged
{
    private string _text;
    private bool _isExpanded;
    private string _editText = string.Empty;

    public NoteItemViewModel(string id, string text)
    {
        Id = id;
        _text = text;
        _editText = text;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id { get; }

    public string Text
    {
        get => _text;
        set
        {
            if (_text == value)
            {
                return;
            }

            _text = value;
            OnPropertyChanged();
        }
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
            {
                return;
            }

            _isExpanded = value;
            OnPropertyChanged();
        }
    }

    public string EditText
    {
        get => _editText;
        set
        {
            if (_editText == value)
            {
                return;
            }

            _editText = value;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
