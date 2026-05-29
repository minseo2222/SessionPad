using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SessionPad.App.ViewModels;

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
