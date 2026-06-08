using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using System.Windows.Threading;
using SessionPad.App.Models;
using SessionPad.App.Services;

namespace SessionPad.App.ViewModels;

public sealed class FloatingNoteViewModel : INotifyPropertyChanged
{
    private readonly NoteStorageService _storageService;
    private readonly SessionMatcher _sessionMatcher;
    private readonly LocalDataService _localDataService;
    private readonly ClipboardService _clipboardService;
    private readonly StartupService _startupService = new();
    private readonly SettingsService _settingsService = new();
    private readonly ThemeService _themeService = new();
    private readonly DispatcherTimer _copyToastTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(1500)
    };
    private SessionSummary? _currentSession;
    private DateTimeOffset _createdAt = DateTimeOffset.UtcNow;
    private NotePanelState _panelState = NotePanelState.CompactNote;
    private DetectedWindowInfo? _lastDetectedWindow;
    private bool _isAttachedToWindow;
    private bool _isHiddenBecauseTargetMinimized;
    private string _currentSessionId = "default";
    private string _currentSessionDisplayName = "Default local note";
    private string _currentSessionKind = "Default";
    private string _currentSessionStatus = "Default local note";
    private string? _currentSessionNoteFile = "notes/default.json";
    private string? _currentNormalizedWindowTitle;
    private string? _currentSessionMatchKey = "default";
    private string _attachmentStatus = "Not attached";
    private string? _attachmentSide;
    private string? _attachmentError;
    private string? _lastFollowUpdateText;
    private string _localDataStatus = "Local data ready";
    private string _startupStatus = "Startup ready";
    private string _lastCommandCopyStatus = "Ready";
    private string _copyToastText = string.Empty;
    private string _lastDragAttachStatus = "Drag near a window to attach.";
    private string _activeNoteTab = "Key";
    private bool _startOnLogin;
    private bool _isDarkTheme;
    private bool _autoTrackForeground;
    private bool _showCopyToast;
    private bool _isCurrentSessionPinned;
    private bool _isSettingsOpen;
    private string _newSessionName = string.Empty;
    private string _newPinnedText = string.Empty;
    private string _newTodoText = string.Empty;
    private string _newCommandText = string.Empty;
    private string _newNoteText = string.Empty;
    private string _searchQuery = string.Empty;
    private string _searchStatus = "Type a word, then press Enter to search your notes.";

    public FloatingNoteViewModel()
        : this(new NoteStorageService(), new LocalDataService(), new ClipboardService())
    {
    }

    public FloatingNoteViewModel(NoteStorageService storageService, LocalDataService localDataService)
        : this(storageService, localDataService, new ClipboardService())
    {
    }

    public FloatingNoteViewModel(
        NoteStorageService storageService,
        LocalDataService localDataService,
        ClipboardService clipboardService)
    {
        _storageService = storageService;
        _sessionMatcher = new SessionMatcher(storageService);
        _localDataService = localDataService;
        _clipboardService = clipboardService;
        _startOnLogin = _startupService.IsEnabled();
        _startupStatus = _startOnLogin ? "Enabled" : "Disabled";
        _isDarkTheme = string.Equals(
            _themeService.CurrentTheme,
            ThemeService.DarkThemeName,
            StringComparison.OrdinalIgnoreCase);
        _autoTrackForeground = _settingsService.LoadAutoTrackForeground();

        ExpandCommand = new RelayCommand(() => PanelState = NotePanelState.CompactNote);
        CollapseCommand = new RelayCommand(() => PanelState = NotePanelState.DockedTab);
        SelectNoteTabCommand = new RelayCommand(SelectNoteTab);
        ToggleSettingsCommand = new RelayCommand(() => IsSettingsOpen = !IsSettingsOpen);
        RenameSessionCommand = new RelayCommand(RenameSession);
        OpenLocalDataFolderCommand = new RelayCommand(OpenLocalDataFolder);
        DeleteLocalDataCommand = new RelayCommand(() => DeleteLocalDataRequested?.Invoke(this, EventArgs.Empty));

        AddPinnedCommand = new RelayCommand(AddPinned);
        DeletePinnedCommand = new RelayCommand(DeletePinned);
        AddTodoCommand = new RelayCommand(AddTodo);
        DeleteTodoCommand = new RelayCommand(DeleteTodo);
        AddCommandItemCommand = new RelayCommand(AddCommandItem);
        DeleteCommandItemCommand = new RelayCommand(DeleteCommandItem);
        CopyCommandItemCommand = new RelayCommand(CopyCommandItem);
        AddNoteCommand = new RelayCommand(AddNote);
        DeleteNoteCommand = new RelayCommand(DeleteNote);
        ToggleNoteExpandCommand = new RelayCommand(ToggleNoteExpand);
        SaveNoteEditCommand = new RelayCommand(SaveNoteEdit);
        CopyNoteCommand = new RelayCommand(CopyNote);
        MoveItemUpCommand = new RelayCommand(MoveItemUp);
        MoveItemDownCommand = new RelayCommand(MoveItemDown);
        SearchCommand = new RelayCommand(RunSearch);
        ClearSearchCommand = new RelayCommand(ClearSearch);
        JumpToSearchResultCommand = new RelayCommand(JumpToSearchResult);

        _copyToastTimer.Tick += OnCopyToastTimerTick;
        TodoItems.CollectionChanged += OnTodoItemsChanged;
        LoadDefaultNote();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? DeleteLocalDataRequested;

    public event EventHandler<bool>? AutoTrackForegroundChanged;

    public ICommand ExpandCommand { get; }

    public ICommand CollapseCommand { get; }

    public ICommand SelectNoteTabCommand { get; }

    public ICommand ToggleSettingsCommand { get; }

    public ICommand RenameSessionCommand { get; }

    public ICommand OpenLocalDataFolderCommand { get; }

    public ICommand DeleteLocalDataCommand { get; }

    public ICommand AddPinnedCommand { get; }

    public ICommand DeletePinnedCommand { get; }

    public ICommand AddTodoCommand { get; }

    public ICommand DeleteTodoCommand { get; }

    public ICommand AddCommandItemCommand { get; }

    public ICommand DeleteCommandItemCommand { get; }

    public ICommand CopyCommandItemCommand { get; }

    public ICommand AddNoteCommand { get; }

    public ICommand DeleteNoteCommand { get; }

    public ICommand ToggleNoteExpandCommand { get; }

    public ICommand SaveNoteEditCommand { get; }

    public ICommand CopyNoteCommand { get; }

    public ICommand MoveItemUpCommand { get; }

    public ICommand MoveItemDownCommand { get; }

    public ICommand SearchCommand { get; }

    public ICommand ClearSearchCommand { get; }

    public ICommand JumpToSearchResultCommand { get; }

    public ObservableCollection<SearchResultViewModel> SearchResults { get; } = new();

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

    public string TodoSummary => $"{TodoItems.Count - OpenTodoCount} of {TodoItems.Count} done";

    public string ActiveNoteTab
    {
        get => _activeNoteTab;
        set
        {
            var normalizedTab = NormalizeNoteTab(value);
            if (_activeNoteTab == normalizedTab)
            {
                return;
            }

            _activeNoteTab = normalizedTab;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsKeyTabActive));
            OnPropertyChanged(nameof(IsTodoTabActive));
            OnPropertyChanged(nameof(IsCommandsTabActive));
            OnPropertyChanged(nameof(IsNotesTabActive));
        }
    }

    public bool IsKeyTabActive => ActiveNoteTab == "Key";

    public bool IsTodoTabActive => ActiveNoteTab == "Todo";

    public bool IsCommandsTabActive => ActiveNoteTab == "Commands";

    public bool IsNotesTabActive => ActiveNoteTab == "Notes";

    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        set => SetField(ref _isSettingsOpen, value);
    }

    public string CurrentSessionId
    {
        get => _currentSessionId;
        private set => SetField(ref _currentSessionId, value);
    }

    public string CurrentSessionDisplayName
    {
        get => _currentSessionDisplayName;
        private set => SetField(ref _currentSessionDisplayName, value);
    }

    public string CurrentSessionKind
    {
        get => _currentSessionKind;
        private set => SetField(ref _currentSessionKind, value);
    }

    public string CurrentSessionStatus
    {
        get => _currentSessionStatus;
        private set => SetField(ref _currentSessionStatus, value);
    }

    public string? CurrentSessionNoteFile
    {
        get => _currentSessionNoteFile;
        private set => SetField(ref _currentSessionNoteFile, value);
    }

    public string? CurrentNormalizedWindowTitle
    {
        get => _currentNormalizedWindowTitle;
        private set => SetField(ref _currentNormalizedWindowTitle, value);
    }

    public string? CurrentSessionMatchKey
    {
        get => _currentSessionMatchKey;
        private set => SetField(ref _currentSessionMatchKey, value);
    }

    public string NewSessionName
    {
        get => _newSessionName;
        set => SetField(ref _newSessionName, value);
    }

    public bool IsCurrentSessionPinned
    {
        get => _isCurrentSessionPinned;
        set
        {
            if (_isCurrentSessionPinned == value)
            {
                return;
            }

            PinCurrentSession(value);
        }
    }

    public bool IsCurrentSessionNamed => _currentSession?.IsUserNamed == true;

    public string LocalDataPath => _localDataService.GetAppDataDirectory();

    public string LocalDataStatus
    {
        get => _localDataStatus;
        private set => SetField(ref _localDataStatus, value);
    }

    public bool StartOnLogin
    {
        get => _startOnLogin;
        set
        {
            if (_startOnLogin == value)
            {
                return;
            }

            string? error;
            var succeeded = value
                ? _startupService.Enable(out error)
                : _startupService.Disable(out error);

            if (succeeded)
            {
                _startOnLogin = value;
                OnPropertyChanged();
                StartupStatus = value ? "Enabled" : "Disabled";
                return;
            }

            _startOnLogin = _startupService.IsEnabled();
            OnPropertyChanged();
            StartupStatus = $"Failed to {(value ? "enable" : "disable")}: {error ?? "Unknown error"}";
        }
    }

    public string StartupStatus
    {
        get => _startupStatus;
        private set => SetField(ref _startupStatus, value);
    }

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (_isDarkTheme == value)
            {
                return;
            }

            var theme = value ? ThemeService.DarkThemeName : ThemeService.LightThemeName;
            _themeService.ApplyTheme(theme);
            _settingsService.SaveTheme(theme);
            _isDarkTheme = value;
            OnPropertyChanged();
        }
    }

    public bool AutoTrackForeground
    {
        get => _autoTrackForeground;
        set
        {
            if (_autoTrackForeground == value)
            {
                return;
            }

            _autoTrackForeground = value;
            _settingsService.SaveAutoTrackForeground(value);
            OnPropertyChanged();
            AutoTrackForegroundChanged?.Invoke(this, value);
        }
    }

    public string LastCommandCopyStatus
    {
        get => _lastCommandCopyStatus;
        private set => SetField(ref _lastCommandCopyStatus, value);
    }

    public bool ShowCopyToast
    {
        get => _showCopyToast;
        private set => SetField(ref _showCopyToast, value);
    }

    public string CopyToastText
    {
        get => _copyToastText;
        private set => SetField(ref _copyToastText, value);
    }

    public string LastDragAttachStatus
    {
        get => _lastDragAttachStatus;
        private set => SetField(ref _lastDragAttachStatus, value);
    }

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

    public bool IsHiddenBecauseTargetMinimized
    {
        get => _isHiddenBecauseTargetMinimized;
        private set => SetField(ref _isHiddenBecauseTargetMinimized, value);
    }

    public string? LastFollowUpdateText
    {
        get => _lastFollowUpdateText;
        private set => SetField(ref _lastFollowUpdateText, value);
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

    public string SearchQuery
    {
        get => _searchQuery;
        set => SetField(ref _searchQuery, value);
    }

    public string SearchStatus
    {
        get => _searchStatus;
        private set => SetField(ref _searchStatus, value);
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
        IsHiddenBecauseTargetMinimized = result.IsHiddenBecauseTargetMinimized;
        LastFollowUpdateText = result.FollowUpdateText;
    }

    public void LoadWindowSession(SessionSummary session, string matchKey)
    {
        if (session.SessionId == CurrentSessionId)
        {
            ApplyWindowSessionContext(session, matchKey, "Window session already loaded");
            return;
        }

        SaveNote();

        _currentSession = session;
        ApplyWindowSessionContext(session, matchKey, "Window session loaded");
        ClearNewItemInputs();
        LastCommandCopyStatus = "Ready";

        var savedNote = _storageService.LoadSessionNote(session);
        if (savedNote is null)
        {
            _createdAt = DateTimeOffset.UtcNow;
            PanelState = NotePanelState.CompactNote;
            ReplaceItems(CreateEmptyNote(session.SessionId));
            SaveNote();
            return;
        }

        _createdAt = savedNote.CreatedAt == default ? DateTimeOffset.UtcNow : savedNote.CreatedAt;
        PanelState = savedNote.PanelState;
        ReplaceItems(savedNote);
    }

    public void SetSessionStatus(string status)
    {
        CurrentSessionStatus = status;
    }

    public void SetLocalDataStatus(string status)
    {
        LocalDataStatus = status;
    }

    public void SetDragAttachStatus(string status)
    {
        LastDragAttachStatus = status;
    }

    public void ResetAfterLocalDataDeleted()
    {
        _currentSession = null;
        ApplyDefaultSessionContext("Local data deleted. Default note reset.");
        ClearNewItemInputs();
        LoadDefaultItems();
        LocalDataStatus = "Local data deleted. Future edits will recreate local JSON files.";
        LastCommandCopyStatus = "Ready";
        LastDragAttachStatus = "Drag near a window to attach.";
    }

    private void LoadDefaultNote()
    {
        _currentSession = null;
        ApplyDefaultSessionContext("Default local note");

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

        PinnedItems.Clear();
        CommandItems.Clear();
        NoteItems.Clear();
        ClearTodoItems();

        PinnedItems.Add(new PinnedItemViewModel("p1", "Keep this note tied to the current work context."));
        TodoItems.Add(new TodoItemViewModel("t1", "Sketch Slice 3 persistence behavior."));
        CommandItems.Add(new CommandItemViewModel("c1", "dotnet build"));
        NoteItems.Add(new NoteItemViewModel("n1", "Slice 3 saves the default note locally as JSON."));
    }

    private void OpenLocalDataFolder()
    {
        try
        {
            _localDataService.OpenAppDataDirectory();
            LocalDataStatus = $"Opened {LocalDataPath}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            Debug.WriteLine($"SessionPad could not open local data folder: {ex.Message}");
            LocalDataStatus = $"Open folder failed: {ex.Message}";
        }
    }

    private void RenameSession()
    {
        if (_currentSession is null)
        {
            CurrentSessionStatus = "Default note cannot be renamed.";
            return;
        }

        var name = (NewSessionName ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            CurrentSessionStatus = "Enter a session name.";
            return;
        }

        try
        {
            var index = _storageService.LoadSessionIndex();
            var sessionIndex = index.Sessions.FindIndex(session =>
                string.Equals(session.SessionId, _currentSession.SessionId, StringComparison.Ordinal));
            if (sessionIndex < 0)
            {
                CurrentSessionStatus = "Rename failed: session was not found.";
                return;
            }

            var updatedSession = index.Sessions[sessionIndex] with
            {
                DisplayName = name,
                IsUserNamed = true
            };
            index.Sessions[sessionIndex] = updatedSession;
            _storageService.SaveSessionIndex(index);

            _currentSession = _currentSession with
            {
                DisplayName = name,
                IsUserNamed = true
            };
            CurrentSessionDisplayName = name;
            CurrentSessionStatus = "Renamed";
            NewSessionName = string.Empty;
            OnPropertyChanged(nameof(IsCurrentSessionNamed));
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or JsonException
            or NotSupportedException)
        {
            CurrentSessionStatus = $"Rename failed: {ex.Message}";
        }
    }

    private void PinCurrentSession(bool pin)
    {
        if (_currentSession is null)
        {
            CurrentSessionStatus = "Default note cannot be pinned.";
            SetCurrentSessionPinnedState(false, forceNotify: true);
            return;
        }

        try
        {
            var index = _storageService.LoadSessionIndex();
            var sessionIndex = index.Sessions.FindIndex(session =>
                string.Equals(session.SessionId, _currentSession.SessionId, StringComparison.Ordinal));
            if (sessionIndex < 0)
            {
                CurrentSessionStatus = "Pin failed: session was not found.";
                SetCurrentSessionPinnedState(_currentSession.IsPinned, forceNotify: true);
                return;
            }

            if (pin)
            {
                var currentProcessName = index.Sessions[sessionIndex].Identity?.ProcessName
                    ?? _currentSession.Identity.ProcessName;
                for (var i = 0; i < index.Sessions.Count; i++)
                {
                    var session = index.Sessions[i];
                    if (i == sessionIndex || session.Identity is null)
                    {
                        continue;
                    }

                    if (string.Equals(
                        session.Identity.ProcessName,
                        currentProcessName,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        index.Sessions[i] = session with { IsPinned = false };
                    }
                }
            }

            var updatedSession = index.Sessions[sessionIndex] with
            {
                IsPinned = pin
            };
            index.Sessions[sessionIndex] = updatedSession;
            _storageService.SaveSessionIndex(index);

            _currentSession = _currentSession with { IsPinned = pin };
            SetCurrentSessionPinnedState(pin, forceNotify: true);
            CurrentSessionStatus = pin ? "Pinned to this app" : "Unpinned";
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or JsonException
            or NotSupportedException)
        {
            CurrentSessionStatus = $"Pin failed: {ex.Message}";
            SetCurrentSessionPinnedState(GetCurrentSessionPinnedState(), forceNotify: true);
        }
    }

    private void SelectNoteTab(object? tab)
    {
        ActiveNoteTab = tab as string ?? "Key";
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

    private void CopyCommandItem(object? item)
    {
        if (item is not CommandItemViewModel commandItem)
        {
            ShowCopyFeedback("No command selected.");
            return;
        }

        var result = _clipboardService.CopyText(commandItem.Text);
        ShowCopyFeedback(result.Message);
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

    private void ToggleNoteExpand(object? item)
    {
        if (item is not NoteItemViewModel noteItem)
        {
            return;
        }

        var shouldExpand = !noteItem.IsExpanded;
        foreach (var existingItem in NoteItems)
        {
            if (!ReferenceEquals(existingItem, noteItem))
            {
                existingItem.IsExpanded = false;
            }
        }

        if (shouldExpand)
        {
            noteItem.EditText = noteItem.Text;
        }

        noteItem.IsExpanded = shouldExpand;
    }

    private void SaveNoteEdit(object? item)
    {
        if (item is not NoteItemViewModel noteItem)
        {
            return;
        }

        var text = (noteItem.EditText ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            noteItem.EditText = noteItem.Text;
            return;
        }

        noteItem.Text = text;
        noteItem.EditText = text;
        noteItem.IsExpanded = false;
        SaveNote();
    }

    private void CopyNote(object? item)
    {
        if (item is not NoteItemViewModel noteItem)
        {
            ShowCopyFeedback("No note selected.");
            return;
        }

        var result = _clipboardService.CopyText(noteItem.Text);
        ShowCopyFeedback(result.Message);
    }

    private void MoveItemUp(object? item)
    {
        MoveItem(item, -1);
    }

    private void MoveItemDown(object? item)
    {
        MoveItem(item, 1);
    }

    private void MoveItem(object? item, int delta)
    {
        switch (item)
        {
            case PinnedItemViewModel pinned:
                MoveWithin(PinnedItems, pinned, delta);
                break;
            case TodoItemViewModel todo:
                MoveWithin(TodoItems, todo, delta);
                break;
            case CommandItemViewModel command:
                MoveWithin(CommandItems, command, delta);
                break;
            case NoteItemViewModel note:
                MoveWithin(NoteItems, note, delta);
                break;
        }
    }

    private void MoveWithin<T>(ObservableCollection<T> collection, T item, int delta)
    {
        var index = collection.IndexOf(item);
        if (index < 0)
        {
            return;
        }

        var target = index + delta;
        if (target < 0 || target >= collection.Count)
        {
            return;
        }

        collection.Move(index, target);
        SaveNote();
    }

    private void RunSearch()
    {
        SearchResults.Clear();
        var query = (SearchQuery ?? string.Empty).Trim();
        if (query.Length == 0)
        {
            SearchStatus = "Type a word, then press Enter to search your notes.";
            return;
        }

        try
        {
            foreach (var entry in _storageService.LoadAllNotes())
            {
                var match = FindFirstMatch(entry.Note, query);
                if (match is null)
                {
                    continue;
                }

                SearchResults.Add(new SearchResultViewModel(
                    entry.Session,
                    entry.DisplayName,
                    match.Section,
                    match.Text,
                    match.Total));
            }

            SearchStatus = SearchResults.Count == 0
                ? $"No matches for \"{query}\"."
                : $"{SearchResults.Count} session(s) matched \"{query}\".";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            Debug.WriteLine($"SessionPad search failed: {ex.Message}");
            SearchStatus = $"Search failed: {ex.Message}";
        }
    }

    private void ClearSearch()
    {
        SearchQuery = string.Empty;
        SearchResults.Clear();
        SearchStatus = "Type a word, then press Enter to search your notes.";
    }

    private void JumpToSearchResult(object? item)
    {
        if (item is not SearchResultViewModel result)
        {
            return;
        }

        if (result.Session is null)
        {
            SaveNote();
            LoadDefaultNote();
        }
        else
        {
            var matchKey = _sessionMatcher.CreateMatchKey(result.Session.Identity);
            LoadWindowSession(result.Session, matchKey);
        }

        IsSettingsOpen = false;
        ClearSearch();
    }

    private static SearchMatch? FindFirstMatch(SessionNote note, string query)
    {
        string? section = null;
        string? text = null;
        var total = 0;

        void Scan(string label, IEnumerable<string> values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrEmpty(value)
                    && value.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    total++;
                    if (section is null)
                    {
                        section = label;
                        text = value;
                    }
                }
            }
        }

        Scan("Key", note.Sections.Pinned.Select(item => item.Text));
        Scan("Todo", note.Sections.Todo.Select(item => item.Text));
        Scan("Commands", note.Sections.Commands.Select(item => item.Text));
        Scan("Notes", note.Sections.Notes.Select(item => item.Text));

        return section is null ? null : new SearchMatch(section, text!, total);
    }

    private sealed record SearchMatch(string Section, string Text, int Total);

    private void ShowCopyFeedback(string message)
    {
        LastCommandCopyStatus = message;
        CopyToastText = message;
        ShowCopyToast = true;

        _copyToastTimer.Stop();
        _copyToastTimer.Start();
    }

    private void OnCopyToastTimerTick(object? sender, EventArgs e)
    {
        _copyToastTimer.Stop();
        ShowCopyToast = false;
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
            var snapshot = CreateSnapshot();
            if (_currentSession is null)
            {
                _storageService.SaveDefaultNote(snapshot);
                return;
            }

            _storageService.SaveSessionNote(_currentSession, snapshot);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Debug.WriteLine($"SessionPad could not save the current note: {ex.Message}");
            CurrentSessionStatus = $"Save failed: {ex.Message}";
        }
    }

    private SessionNote CreateSnapshot()
    {
        return new SessionNote
        {
            SchemaVersion = 1,
            SessionId = CurrentSessionId,
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

    private static SessionNote CreateEmptyNote(string sessionId)
    {
        var now = DateTimeOffset.UtcNow;
        return new SessionNote
        {
            SchemaVersion = 1,
            SessionId = sessionId,
            PanelState = NotePanelState.CompactNote,
            CreatedAt = now,
            UpdatedAt = now,
            Sections = new NoteSections()
        };
    }

    private void ApplyDefaultSessionContext(string status)
    {
        CurrentSessionId = "default";
        CurrentSessionDisplayName = "Default local note";
        CurrentSessionKind = "Default";
        CurrentSessionStatus = status;
        CurrentSessionNoteFile = "notes/default.json";
        CurrentNormalizedWindowTitle = null;
        CurrentSessionMatchKey = "default";
        SetCurrentSessionPinnedState(false);
        OnPropertyChanged(nameof(IsCurrentSessionNamed));
    }

    private void ApplyWindowSessionContext(SessionSummary session, string matchKey, string status)
    {
        CurrentSessionId = session.SessionId;
        CurrentSessionDisplayName = string.IsNullOrWhiteSpace(session.DisplayName)
            ? "Window session"
            : session.DisplayName;
        CurrentSessionKind = "Window Session";
        CurrentSessionStatus = status;
        CurrentSessionNoteFile = session.NoteFile;
        CurrentNormalizedWindowTitle = session.Identity.NormalizedWindowTitle;
        CurrentSessionMatchKey = matchKey;
        SetCurrentSessionPinnedState(session.IsPinned);
        OnPropertyChanged(nameof(IsCurrentSessionNamed));
    }

    private void ClearNewItemInputs()
    {
        NewPinnedText = string.Empty;
        NewTodoText = string.Empty;
        NewCommandText = string.Empty;
        NewNoteText = string.Empty;
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
        OnPropertyChanged(nameof(TodoSummary));
    }

    private void OnTodoItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TodoItemViewModel.IsDone))
        {
            OnPropertyChanged(nameof(OpenTodoCount));
            OnPropertyChanged(nameof(TodoSummary));
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

    private static string NormalizeNoteTab(string? tab)
    {
        return tab switch
        {
            "Todo" => "Todo",
            "Commands" => "Commands",
            "Notes" => "Notes",
            _ => "Key"
        };
    }

    private void SetCurrentSessionPinnedState(bool isPinned, bool forceNotify = false)
    {
        if (_isCurrentSessionPinned == isPinned && !forceNotify)
        {
            return;
        }

        _isCurrentSessionPinned = isPinned;
        OnPropertyChanged(nameof(IsCurrentSessionPinned));
    }

    private bool GetCurrentSessionPinnedState()
    {
        if (_currentSession is null)
        {
            return false;
        }

        try
        {
            var index = _storageService.LoadSessionIndex();
            var session = index.Sessions.FirstOrDefault(session =>
                string.Equals(session.SessionId, _currentSession.SessionId, StringComparison.Ordinal));
            return session?.IsPinned ?? _currentSession.IsPinned;
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or JsonException
            or NotSupportedException)
        {
            Debug.WriteLine($"SessionPad could not refresh pin state: {ex.Message}");
            return _currentSession.IsPinned;
        }
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
