using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SessionPad.App.ViewModels;

public sealed class FloatingNoteViewModel : INotifyPropertyChanged
{
    private NotePanelState _panelState = NotePanelState.CompactNote;
    private string _newPinnedText = string.Empty;
    private string _newTodoText = string.Empty;
    private string _newCommandText = string.Empty;
    private string _newNoteText = string.Empty;

    public FloatingNoteViewModel()
    {
        ExpandCommand = new RelayCommand(() => PanelState = NotePanelState.CompactNote);
        CollapseCommand = new RelayCommand(() => PanelState = NotePanelState.DockedTab);

        AddPinnedCommand = new RelayCommand(AddPinned);
        DeletePinnedCommand = new RelayCommand(DeletePinned);
        AddTodoCommand = new RelayCommand(AddTodo);
        DeleteTodoCommand = new RelayCommand(DeleteTodo);
        AddCommandItemCommand = new RelayCommand(AddCommandItem);
        DeleteCommandItemCommand = new RelayCommand(DeleteCommandItem);
        AddNoteCommand = new RelayCommand(AddNote);
        DeleteNoteCommand = new RelayCommand(DeleteNote);

        TodoItems.CollectionChanged += OnTodoItemsChanged;

        PinnedItems.Add(new PinnedItemViewModel("Keep this note tied to the current work context."));
        TodoItems.Add(new TodoItemViewModel("Sketch Slice 2 UI behavior."));
        CommandItems.Add(new CommandItemViewModel("dotnet build"));
        NoteItems.Add(new NoteItemViewModel("Slice 2 edits are in memory only."));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand ExpandCommand { get; }

    public ICommand CollapseCommand { get; }

    public ICommand AddPinnedCommand { get; }

    public ICommand DeletePinnedCommand { get; }

    public ICommand AddTodoCommand { get; }

    public ICommand DeleteTodoCommand { get; }

    public ICommand AddCommandItemCommand { get; }

    public ICommand DeleteCommandItemCommand { get; }

    public ICommand AddNoteCommand { get; }

    public ICommand DeleteNoteCommand { get; }

    public ObservableCollection<PinnedItemViewModel> PinnedItems { get; } = new();

    public ObservableCollection<TodoItemViewModel> TodoItems { get; } = new();

    public ObservableCollection<CommandItemViewModel> CommandItems { get; } = new();

    public ObservableCollection<NoteItemViewModel> NoteItems { get; } = new();

    public NotePanelState PanelState
    {
        get => _panelState;
        private set
        {
            if (_panelState == value)
            {
                return;
            }

            _panelState = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDockedTab));
            OnPropertyChanged(nameof(IsCompactNote));
        }
    }

    public bool IsDockedTab => PanelState == NotePanelState.DockedTab;

    public bool IsCompactNote => PanelState == NotePanelState.CompactNote;

    public int OpenTodoCount => TodoItems.Count(item => !item.IsDone);

    public string NewPinnedText
    {
        get => _newPinnedText;
        set => SetField(ref _newPinnedText, value);
    }

    public string NewTodoText
    {
        get => _newTodoText;
        set => SetField(ref _newTodoText, value);
    }

    public string NewCommandText
    {
        get => _newCommandText;
        set => SetField(ref _newCommandText, value);
    }

    public string NewNoteText
    {
        get => _newNoteText;
        set => SetField(ref _newNoteText, value);
    }

    private void AddPinned()
    {
        AddTextItem(NewPinnedText, text => PinnedItems.Add(new PinnedItemViewModel(text)));
        NewPinnedText = string.Empty;
    }

    private void DeletePinned(object? item)
    {
        if (item is PinnedItemViewModel pinnedItem)
        {
            PinnedItems.Remove(pinnedItem);
        }
    }

    private void AddTodo()
    {
        AddTextItem(NewTodoText, text => TodoItems.Add(new TodoItemViewModel(text)));
        NewTodoText = string.Empty;
    }

    private void DeleteTodo(object? item)
    {
        if (item is TodoItemViewModel todoItem)
        {
            TodoItems.Remove(todoItem);
        }
    }

    private void AddCommandItem()
    {
        AddTextItem(NewCommandText, text => CommandItems.Add(new CommandItemViewModel(text)));
        NewCommandText = string.Empty;
    }

    private void DeleteCommandItem(object? item)
    {
        if (item is CommandItemViewModel commandItem)
        {
            CommandItems.Remove(commandItem);
        }
    }

    private void AddNote()
    {
        AddTextItem(NewNoteText, text => NoteItems.Add(new NoteItemViewModel(text)));
        NewNoteText = string.Empty;
    }

    private void DeleteNote(object? item)
    {
        if (item is NoteItemViewModel noteItem)
        {
            NoteItems.Remove(noteItem);
        }
    }

    private static void AddTextItem(string input, Action<string> add)
    {
        var text = input.Trim();
        if (text.Length == 0)
        {
            return;
        }

        add(text);
    }

    private void OnTodoItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (TodoItemViewModel item in e.OldItems)
            {
                item.PropertyChanged -= OnTodoItemPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (TodoItemViewModel item in e.NewItems)
            {
                item.PropertyChanged += OnTodoItemPropertyChanged;
            }
        }

        OnPropertyChanged(nameof(OpenTodoCount));
    }

    private void OnTodoItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TodoItemViewModel.IsDone))
        {
            OnPropertyChanged(nameof(OpenTodoCount));
        }
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum NotePanelState
{
    DockedTab,
    CompactNote
}
