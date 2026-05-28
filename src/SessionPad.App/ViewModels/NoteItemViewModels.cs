using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SessionPad.App.ViewModels;

public sealed class PinnedItemViewModel
{
    public PinnedItemViewModel(string text)
    {
        Text = text;
    }

    public string Text { get; }
}

public sealed class TodoItemViewModel : INotifyPropertyChanged
{
    private bool _isDone;

    public TodoItemViewModel(string text)
    {
        Text = text;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

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
    public CommandItemViewModel(string text)
    {
        Text = text;
    }

    public string Text { get; }
}

public sealed class NoteItemViewModel
{
    public NoteItemViewModel(string text)
    {
        Text = text;
    }

    public string Text { get; }
}
