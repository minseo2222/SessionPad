using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SessionPad.App.Models;
using SessionPad.App.Native;
using SessionPad.App.Services;

namespace SessionPad.App.ViewModels;

/// <summary>
/// Owns the Settings-panel state and behavior extracted from
/// <see cref="FloatingNoteViewModel"/>: the attach hotkey, theme, "start on login",
/// and auto-track toggles. The parent view model delegates to this so existing XAML
/// bindings keep working. The persistence and startup services are abstracted behind
/// interfaces so hotkey/theme/startup behavior can be unit tested without disk or
/// registry I/O.
/// </summary>
public sealed class SettingsPanelViewModel : INotifyPropertyChanged
{
    private static readonly HotkeyOption[] HotkeyPresets =
    [
        new("Ctrl+Alt+N", "Ctrl + Alt + N", User32.ModControl | User32.ModAlt, 0x4E),
        new("Ctrl+Shift+N", "Ctrl + Shift + N", User32.ModControl | User32.ModShift, 0x4E),
        new("Ctrl+Alt+S", "Ctrl + Alt + S", User32.ModControl | User32.ModAlt, 0x53),
        new("Ctrl+Alt+Space", "Ctrl + Alt + Space", User32.ModControl | User32.ModAlt, 0x20),
        new("Ctrl+Shift+Space", "Ctrl + Shift + Space", User32.ModControl | User32.ModShift, 0x20),
    ];

    private readonly ISettingsService _settingsService;
    private readonly IStartupService _startupService;
    private readonly ThemeService _themeService;

    private HotkeyOption _selectedHotkey;
    private HotkeyOption _appliedHotkey;
    private string _hotkeyStatus;
    // Whether an attach hotkey is currently registered. Starts true (the app registers
    // the applied hotkey on launch); a failed registration clears it so re-applying the
    // same option can recover instead of no-opping.
    private bool _isHotkeyActive = true;
    private bool _startOnLogin;
    private string _startupStatus;
    private bool _isDarkTheme;
    private bool _autoTrackForeground;
    private bool _isAttachHintDismissed;
    private bool _isHotkeyStatusWarning;

    public SettingsPanelViewModel(
        ISettingsService settingsService,
        IStartupService startupService,
        ThemeService themeService)
    {
        _settingsService = settingsService;
        _startupService = startupService;
        _themeService = themeService;

        _startOnLogin = _startupService.IsEnabled();
        _startupStatus = _startOnLogin ? "Enabled" : "Disabled";
        _isDarkTheme = string.Equals(
            _themeService.CurrentTheme,
            ThemeService.DarkThemeName,
            StringComparison.OrdinalIgnoreCase);
        _autoTrackForeground = _settingsService.LoadAutoTrackForeground();
        _isAttachHintDismissed = _settingsService.LoadAttachHintDismissed();

        var hotkeyToken = _settingsService.LoadHotkey();
        _appliedHotkey = Array.Find(HotkeyPresets, option => option.Token == hotkeyToken) ?? HotkeyPresets[0];
        _selectedHotkey = _appliedHotkey;
        _hotkeyStatus = $"Current: {_appliedHotkey.Display}";

        ApplyHotkeyCommand = new RelayCommand(ApplyHotkey);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<bool>? AutoTrackForegroundChanged;

    public event EventHandler<HotkeyOption>? HotkeyChangeRequested;

    /// <summary>Raised when a hotkey message should surface as a transient status toast (owned by the parent).</summary>
    public event EventHandler<string>? StatusToastRequested;

    public ICommand ApplyHotkeyCommand { get; }

    public IReadOnlyList<HotkeyOption> HotkeyOptions => HotkeyPresets;

    public HotkeyOption SelectedHotkey
    {
        get => _selectedHotkey;
        set => SetField(ref _selectedHotkey, value);
    }

    public HotkeyOption AppliedHotkey => _appliedHotkey;

    public string HotkeyStatus
    {
        get => _hotkeyStatus;
        private set => SetField(ref _hotkeyStatus, value);
    }

    /// <summary>True while <see cref="HotkeyStatus"/> reports a registration problem,
    /// so the view can color only real warnings — neutral statuses stay muted.</summary>
    public bool IsHotkeyStatusWarning
    {
        get => _isHotkeyStatusWarning;
        private set => SetField(ref _isHotkeyStatusWarning, value);
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

    public bool IsAttachHintDismissed
    {
        get => _isAttachHintDismissed;
        private set => SetField(ref _isAttachHintDismissed, value);
    }

    /// <summary>Hides the first-run attach hint for good (first attach or manual close).</summary>
    public void DismissAttachHint()
    {
        if (_isAttachHintDismissed)
        {
            return;
        }

        IsAttachHintDismissed = true;
        _settingsService.SaveAttachHintDismissed(true);
    }

    public void NotifyHotkeyApplied()
    {
        _appliedHotkey = _selectedHotkey;
        _settingsService.SaveHotkey(_appliedHotkey.Token);
        OnPropertyChanged(nameof(AppliedHotkey));
        HotkeyStatus = $"Hotkey set to {_appliedHotkey.Display}";
        IsHotkeyStatusWarning = false;
        _isHotkeyActive = true;
    }

    /// <summary>
    /// The requested hotkey could not be registered, but the previously applied hotkey
    /// was restored and is still active. <paramref name="reason"/> is a short, plain-language
    /// cause shown to the user (e.g. "it may be in use by another app").
    /// </summary>
    public void NotifyHotkeyRevertedToPrevious(string? reason)
    {
        var attempted = _selectedHotkey;
        SelectedHotkey = _appliedHotkey;
        HotkeyStatus = string.IsNullOrWhiteSpace(reason)
            ? $"Couldn't set {attempted.Display}. Still using {_appliedHotkey.Display}."
            : $"Couldn't set {attempted.Display} — {reason}. Still using {_appliedHotkey.Display}.";
        IsHotkeyStatusWarning = true;
        _isHotkeyActive = true; // the previous hotkey was restored and is active
    }

    /// <summary>
    /// The requested hotkey could not be registered and the previous hotkey could not be
    /// restored, so no attach hotkey is active right now.
    /// </summary>
    public void NotifyHotkeyUnavailable(string? reason)
    {
        var attempted = _selectedHotkey;
        SelectedHotkey = _appliedHotkey;
        HotkeyStatus = string.IsNullOrWhiteSpace(reason)
            ? $"Couldn't set {attempted.Display}, and no attach shortcut is active now. Try another combination."
            : $"Couldn't set {attempted.Display} — {reason}. No attach shortcut is active now; try another combination.";
        StatusToastRequested?.Invoke(this, "No attach shortcut is active — pick another in Settings.");
        IsHotkeyStatusWarning = true;
        _isHotkeyActive = false;
    }

    public void NotifyHotkeyRegistrationFailed(string display, int win32Error)
    {
        Debug.WriteLine($"SessionPad could not register {display}. Win32 error {win32Error}.");
        HotkeyStatus =
            $"Couldn't register {display} — it may be in use by another app. Pick another shortcut in Settings.";
        StatusToastRequested?.Invoke(this, $"Attach shortcut {display} is unavailable — it may be in use by another app.");
        IsHotkeyStatusWarning = true;
        _isHotkeyActive = false;
    }

    private void ApplyHotkey()
    {
        if (_selectedHotkey is null)
        {
            HotkeyStatus = $"Current: {_appliedHotkey.Display}";
            IsHotkeyStatusWarning = false;
            return;
        }

        // Re-request whenever the selection differs, or whenever no hotkey is currently
        // active — the latter lets the user recover by re-applying the same option after
        // a failed registration. Only no-op when the selected hotkey is already active.
        if (_isHotkeyActive
            && string.Equals(_selectedHotkey.Token, _appliedHotkey.Token, StringComparison.Ordinal))
        {
            HotkeyStatus = $"Current: {_appliedHotkey.Display}";
            IsHotkeyStatusWarning = false;
            return;
        }

        HotkeyChangeRequested?.Invoke(this, _selectedHotkey);
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
