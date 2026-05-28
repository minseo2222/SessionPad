using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SessionPad.App.ViewModels;

public sealed class FloatingNoteViewModel : INotifyPropertyChanged
{
    private NotePanelState _panelState = NotePanelState.CompactNote;

    public FloatingNoteViewModel()
    {
        ExpandCommand = new RelayCommand(() => PanelState = NotePanelState.CompactNote);
        CollapseCommand = new RelayCommand(() => PanelState = NotePanelState.DockedTab);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand ExpandCommand { get; }

    public ICommand CollapseCommand { get; }

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

    public int TodoCount => 1;

    public string PinnedText => "Keep this note tied to the current work context.";

    public string TodoText => "Sketch Slice 1 UI behavior.";

    public string CommandText => "dotnet build";

    public string NotesText => "Placeholder notes are in memory only for Slice 1.";

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
