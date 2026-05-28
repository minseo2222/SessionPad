using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SessionPad.App.Models;
using SessionPad.App.Services;

namespace SessionPad.App.ViewModels;

public sealed class FloatingNoteViewModel : INotifyPropertyChanged
{
    private readonly NoteStorageService _storageService;
    private DateTimeOffset _createdAt = DateTimeOffset.UtcNow;
    private NotePanelState _panelState = NotePanelState.CompactNote;
    private DetectedWindowInfo? _lastDetectedWindow;
    private bool _isAttachedToWindow;
    private string _attachmentStatus = "Not attached";
    private string? _attachmentSide;
    private string? _attachmentError;
    private string _newPinnedText = string.Empty;
    private string _newTodoText = string.Empty;
    private string _newCommandText = string.Empty;
    private string _newNoteText = string.Empty;

    public FloatingNoteViewModel()
        : this(new NoteStorageService())
    {
    }

    public FloatingNoteViewModel(NoteStorageService storageService)
    {
        _storageService = storageService;

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
        LoadNote();
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

    public DetectedWindowInfo? LastDetectedWindow
    {
        get => _lastDetectedWindow;
        private set => SetField(ref _lastDetectedWindow, value);
    }

    public bool IsAttachedToWindow
    {
        get => _isAttachedToWindow;
        private set => SetField(ref _isAttachedToWindow, value);
    }

    public string AttachmentStatus
    {
        get => _attachmentStatus;
        private set => SetField(ref _attachmentStatus, value);
    }

    public string? AttachmentSide
    {
        get => _attachmentSide;
        private set => SetField(ref _attachmentSide, value);
    }

    public string? AttachmentError
    {
        get => _attachmentError;
        private set => SetField(ref _attachmentError, value);
    }

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

    public void SetLastDetectedWindow(DetectedWindowInfo detectedWindow)
    {
        LastDetectedWindow = detectedWindow;
    }

    public void SetAttachmentResult(WindowAttachmentResult result)
    {
        IsAttachedToWindow = result.IsAttached;
        AttachmentStatus = result.Status;
        AttachmentSide = result.Side;
        AttachmentError = result.Error;
    }

    private void LoadNote()
    {
        var savedNote = _storageService.LoadDefaultNote();
        if (savedNote is null)
        {
            LoadDefaultItems();
            return;
        }

        _createdAt = savedNote.CreatedAt == default ? DateTimeOffset.UtcNow : savedNote.CreatedAt;
        PanelState = savedNote.PanelState;
        ReplaceItems(savedNote);
    }

    private void LoadDefaultItems()
    {
        _createdAt = DateTimeOffset.UtcNow;
        PanelState = NotePanelState.CompactNote;

        PinnedItems.Add(new PinnedItemViewModel("p1", "Keep this note tied to the current work context."));
        TodoItems.Add(new TodoItemViewModel("t1", "Sketch Slice 3 persistence behavior."));
        CommandItems.Add(new CommandItemViewModel("c1", "dotnet build"));
        NoteItems.Add(new NoteItemViewModel("n1", "Slice 3 saves the default note locally as JSON."));
    }

    private void ReplaceItems(SessionNote note)
    {
        PinnedItems.Clear();
        CommandItems.Clear();
        NoteItems.Clear();
        ClearTodoItems();

        foreach (var item in note.Sections.Pinned.OrderBy(item => item.SortOrder))
        {
            PinnedItems.Add(new PinnedItemViewModel(RequiredId(item.Id, "p"), item.Text));
        }

        foreach (var item in note.Sections.Todo.OrderBy(item => item.SortOrder))
        {
            TodoItems.Add(new TodoItemViewModel(RequiredId(item.Id, "t"), item.Text, item.IsDone));
        }

        foreach (var item in note.Sections.Commands.OrderBy(item => item.SortOrder))
        {
            CommandItems.Add(new CommandItemViewModel(RequiredId(item.Id, "c"), item.Text));
        }

        foreach (var item in note.Sections.Notes.OrderBy(item => item.SortOrder))
        {
            NoteItems.Add(new NoteItemViewModel(RequiredId(item.Id, "n"), item.Text));
        }
    }

    private void ClearTodoItems()
    {
        foreach (var item in TodoItems)
        {
            item.PropertyChanged -= OnTodoItemPropertyChanged;
        }

        TodoItems.Clear();
    }

    private void AddPinned()
    {
        if (AddTextItem(NewPinnedText, text => PinnedItems.Add(new PinnedItemViewModel(CreateId("p"), text))))
        {
            NewPinnedText = string.Empty;
            SaveNote();
        }
    }

    private void DeletePinned(object? item)
    {
        if (item is PinnedItemViewModel pinnedItem && PinnedItems.Remove(pinnedItem))
        {
            SaveNote();
        }
    }

    private void AddTodo()
    {
        if (AddTextItem(NewTodoText, text => TodoItems.Add(new TodoItemViewModel(CreateId("t"), text))))
        {
            NewTodoText = string.Empty;
            SaveNote();
        }
    }

    private void DeleteTodo(object? item)
    {
        if (item is TodoItemViewModel todoItem && TodoItems.Remove(todoItem))
        {
            SaveNote();
        }
    }

    private void AddCommandItem()
    {
        if (AddTextItem(NewCommandText, text => CommandItems.Add(new CommandItemViewModel(CreateId("c"), text))))
        {
            NewCommandText = string.Empty;
            SaveNote();
        }
    }

    private void DeleteCommandItem(object? item)
    {
        if (item is CommandItemViewModel commandItem && CommandItems.Remove(commandItem))
        {
            SaveNote();
        }
    }

    private void AddNote()
    {
        if (AddTextItem(NewNoteText, text => NoteItems.Add(new NoteItemViewModel(CreateId("n"), text))))
        {
            NewNoteText = string.Empty;
            SaveNote();
        }
    }

    private void DeleteNote(object? item)
    {
        if (item is NoteItemViewModel noteItem && NoteItems.Remove(noteItem))
        {
            SaveNote();
        }
    }

    private static bool AddTextItem(string input, Action<string> add)
    {
        var text = input.Trim();
        if (text.Length == 0)
        {
            return false;
        }

        add(text);
        return true;
    }

    private void SaveNote()
    {
        try
        {
            _storageService.SaveDefaultNote(CreateSnapshot());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Debug.WriteLine($"SessionPad could not save the default note: {ex.Message}");
        }
    }

    private SessionNote CreateSnapshot()
    {
        return new SessionNote
        {
            SchemaVersion = 1,
            SessionId = "default",
            PanelState = PanelState,
            CreatedAt = _createdAt,
            UpdatedAt = DateTimeOffset.UtcNow,
            Sections = new NoteSections
            {
                Pinned = PinnedItems
                    .Select((item, index) => new PinnedItem
                    {
                        Id = item.Id,
                        Text = item.Text,
                        SortOrder = index
                    })
                    .ToList(),
                Todo = TodoItems
                    .Select((item, index) => new TodoItem
                    {
                        Id = item.Id,
                        Text = item.Text,
                        IsDone = item.IsDone,
                        SortOrder = index
                    })
                    .ToList(),
                Commands = CommandItems
                    .Select((item, index) => new CommandItem
                    {
                        Id = item.Id,
                        Text = item.Text,
                        SortOrder = index
                    })
                    .ToList(),
                Notes = NoteItems
                    .Select((item, index) => new NoteTextItem
                    {
                        Id = item.Id,
                        Text = item.Text,
                        SortOrder = index
                    })
                    .ToList()
            }
        };
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
            SaveNote();
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

    private static string CreateId(string prefix)
    {
        return $"{prefix}{Guid.NewGuid():N}";
    }

    private static string RequiredId(string id, string prefix)
    {
        return string.IsNullOrWhiteSpace(id) ? CreateId(prefix) : id;
    }
}
