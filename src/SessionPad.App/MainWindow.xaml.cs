using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using SessionPad.App.Models;
using SessionPad.App.Native;
using SessionPad.App.Services;
using SessionPad.App.ViewModels;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace SessionPad.App;

public partial class MainWindow : Window
{
    private static readonly TimeSpan AttachmentPollInterval = TimeSpan.FromMilliseconds(60);
    private static readonly TimeSpan ForegroundWatchInterval = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan TitleSwitchDebounceInterval = TimeSpan.FromMilliseconds(400);
    private const int DragAttachThresholdPx = 48;

    private readonly HotkeyService _hotkeyService = new();
    private readonly WindowDetectionService _windowDetectionService = new();
    private readonly WindowAttachmentService _windowAttachmentService = new();
    private readonly LocalDataService _localDataService;
    private readonly NoteStorageService _noteStorageService;
    private readonly SessionMatcher _sessionMatcher;
    private readonly FloatingNoteViewModel _viewModel;
    private readonly DispatcherTimer _attachmentTimer = new(DispatcherPriority.Render);
    private readonly DispatcherTimer _foregroundWatchTimer = new(DispatcherPriority.Background);
    private readonly DispatcherTimer _titleSwitchDebounceTimer = new(DispatcherPriority.Background);
    private readonly WinEventHookService _winEventService = new();
    private Forms.NotifyIcon? _notifyIcon;
    private Forms.ContextMenuStrip? _trayMenu;
    private Drawing.Icon? _trayIcon;
    private HwndSource? _source;
    private IntPtr _windowHandle;
    private bool _hotkeyRegistered;
    private bool _exitRequested;
    private bool _userHiddenToTray;
    private bool _isDragAttachInProgress;
    private string? _lastAttachedTitle;
    private IntPtr _lastForegroundHwnd;
    private bool _isHandlingWinEvent;

    public MainWindow()
    {
        _localDataService = new LocalDataService();
        _noteStorageService = new NoteStorageService();
        _sessionMatcher = new SessionMatcher(_noteStorageService);
        _viewModel = new FloatingNoteViewModel(_noteStorageService, _localDataService);
        _viewModel.DeleteLocalDataRequested += OnDeleteLocalDataRequested;

        InitializeComponent();
        DataContext = _viewModel;

        _attachmentTimer.Interval = AttachmentPollInterval;
        _attachmentTimer.Tick += OnAttachmentTimerTick;

        _foregroundWatchTimer.Interval = ForegroundWatchInterval;
        _foregroundWatchTimer.Tick += OnForegroundWatchTick;
        _titleSwitchDebounceTimer.Interval = TitleSwitchDebounceInterval;
        _titleSwitchDebounceTimer.Tick += OnTitleSwitchDebounceTick;
        _viewModel.AutoTrackForegroundChanged += OnAutoTrackForegroundChanged;
        _viewModel.HotkeyChangeRequested += OnHotkeyChangeRequested;
        _winEventService.WinEventReceived += OnWinEventReceived;

        InitializeTrayIcon();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _windowHandle = new WindowInteropHelper(this).Handle;
        _source = HwndSource.FromHwnd(_windowHandle);
        _source?.AddHook(OnWindowMessage);

        ApplyDwmWindowDecorations();
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        var hotkey = _viewModel.AppliedHotkey;
        _hotkeyRegistered = _hotkeyService.Register(_windowHandle, hotkey.Modifiers, hotkey.VirtualKey);
        if (!_hotkeyRegistered)
        {
            Debug.WriteLine(
                $"SessionPad could not register {hotkey.Display}. Win32 error: {_hotkeyService.LastRegistrationError}.");
            _viewModel.NotifyHotkeyRegistrationFailed(hotkey.Display, _hotkeyService.LastRegistrationError);
        }

        var hooksActive = _winEventService.Start();
        ConfigureTimerIntervals(hooksActive);
        UpdateForegroundWatchTimer();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_exitRequested)
        {
            e.Cancel = true;
            _userHiddenToTray = true;
            HideSafely();
            return;
        }

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.DeleteLocalDataRequested -= OnDeleteLocalDataRequested;
        _viewModel.AutoTrackForegroundChanged -= OnAutoTrackForegroundChanged;
        _viewModel.HotkeyChangeRequested -= OnHotkeyChangeRequested;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _attachmentTimer.Stop();
        _attachmentTimer.Tick -= OnAttachmentTimerTick;
        _foregroundWatchTimer.Stop();
        _foregroundWatchTimer.Tick -= OnForegroundWatchTick;
        _titleSwitchDebounceTimer.Stop();
        _titleSwitchDebounceTimer.Tick -= OnTitleSwitchDebounceTick;
        _winEventService.WinEventReceived -= OnWinEventReceived;
        _winEventService.Stop();
        _windowAttachmentService.Detach("SessionPad closed.");

        if (_hotkeyRegistered)
        {
            _hotkeyService.Unregister(_windowHandle);
            _hotkeyRegistered = false;
        }

        _source?.RemoveHook(OnWindowMessage);
        _source = null;

        DisposeTrayIcon();

        base.OnClosed(e);
    }

    public void ShowAndActivateFromExternalRequest()
    {
        _userHiddenToTray = false;
        ShowAndRestoreSafely();
        ActivateSafely();
    }

    public void BeginDragAttach()
    {
        _viewModel.SetDragAttachStatus("Drag attach started.");
        _isDragAttachInProgress = true;

        if (_attachmentTimer.IsEnabled)
        {
            _attachmentTimer.Stop();
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException ex)
        {
            _isDragAttachInProgress = false;
            Debug.WriteLine($"SessionPad drag attach could not start: {ex.Message}");
            _viewModel.SetDragAttachStatus($"Drag attach could not start: {ex.Message}");
            ResumeAttachmentTimerIfAttached();
            return;
        }
        catch (Exception ex)
        {
            _isDragAttachInProgress = false;
            Debug.WriteLine($"SessionPad drag attach failed: {ex}");
            _viewModel.SetDragAttachStatus($"Drag attach failed: {ex.GetType().Name}: {ex.Message}");
            ResumeAttachmentTimerIfAttached();
            return;
        }

        try
        {
            TryAttachToNearestWindowAfterDrag();
        }
        finally
        {
            _isDragAttachInProgress = false;
        }
    }

    private void ApplyDwmWindowDecorations()
    {
        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        try
        {
            // Native rounded corners on Windows 11; older Windows ignores this and
            // keeps square corners (purely cosmetic, behavior identical).
            var cornerPreference = DwmApi.DwmwcpRound;
            DwmApi.DwmSetWindowAttribute(
                _windowHandle,
                DwmApi.DwmwaWindowCornerPreference,
                ref cornerPreference,
                sizeof(int));

            var useDark = _viewModel.IsDarkTheme ? 1 : 0;
            DwmApi.DwmSetWindowAttribute(
                _windowHandle,
                DwmApi.DwmwaUseImmersiveDarkMode,
                ref useDark,
                sizeof(int));
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            Debug.WriteLine($"SessionPad could not apply DWM window decorations: {ex.Message}");
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FloatingNoteViewModel.IsDarkTheme))
        {
            ApplyDwmWindowDecorations();
        }
    }

    private IntPtr OnWindowMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == User32.WmHotkey && wParam.ToInt64() == HotkeyService.ShowSessionPadHotkeyId)
        {
            OnHotkeyPressed();
            handled = true;
        }

        return IntPtr.Zero;
    }

    private void InitializeTrayIcon()
    {
        try
        {
            _trayIcon = LoadTrayIcon();
            _trayMenu = new Forms.ContextMenuStrip();
            _trayMenu.Items.Add("Open", null, (_, _) => BeginInvokeOnDispatcher(ShowAndActivateFromTray));
            _trayMenu.Items.Add("Open data folder", null, (_, _) => BeginInvokeOnDispatcher(OpenLocalDataFolderFromTray));
            _trayMenu.Items.Add(new Forms.ToolStripSeparator());
            _trayMenu.Items.Add("Exit", null, (_, _) => BeginInvokeOnDispatcher(ExitFromTray));

            _notifyIcon = new Forms.NotifyIcon
            {
                ContextMenuStrip = _trayMenu,
                Icon = _trayIcon,
                Text = "SessionPad",
                Visible = true
            };
            _notifyIcon.DoubleClick += OnTrayIconDoubleClick;
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or ArgumentException
            or InvalidOperationException)
        {
            Debug.WriteLine($"SessionPad could not initialize tray icon: {ex}");
        }
    }

    private void OnTrayIconDoubleClick(object? sender, EventArgs e)
    {
        BeginInvokeOnDispatcher(ShowAndActivateFromTray);
    }

    private void ShowAndActivateFromTray()
    {
        ShowAndActivateFromExternalRequest();
    }

    private void OpenLocalDataFolderFromTray()
    {
        try
        {
            _localDataService.OpenAppDataDirectory();
            _viewModel.SetLocalDataStatus($"Opened {_localDataService.GetAppDataDirectory()}");
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or InvalidOperationException
            or System.ComponentModel.Win32Exception)
        {
            Debug.WriteLine($"SessionPad could not open local data folder from tray: {ex}");
            _viewModel.SetLocalDataStatus($"Open folder failed: {ex.Message}");
        }
    }

    private void ExitFromTray()
    {
        _exitRequested = true;
        Close();
    }

    private void BeginInvokeOnDispatcher(Action action)
    {
        try
        {
            Dispatcher.BeginInvoke(action);
        }
        catch (Exception ex) when (ex is InvalidOperationException)
        {
            Debug.WriteLine($"SessionPad could not dispatch tray action: {ex.Message}");
        }
    }

    private static Drawing.Icon LoadTrayIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "SessionPad.ico");
        if (File.Exists(iconPath))
        {
            return new Drawing.Icon(iconPath);
        }

        return (Drawing.Icon)Drawing.SystemIcons.Application.Clone();
    }

    private void DisposeTrayIcon()
    {
        if (_notifyIcon is not null)
        {
            _notifyIcon.DoubleClick -= OnTrayIconDoubleClick;
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        _trayMenu?.Dispose();
        _trayMenu = null;

        _trayIcon?.Dispose();
        _trayIcon = null;
    }

    private void OnHotkeyPressed()
    {
        _userHiddenToTray = false;
        DetectedWindowInfo detectedWindow;

        try
        {
            detectedWindow = _windowDetectionService.GetForegroundWindowInfo(_windowHandle);
            _viewModel.SetLastDetectedWindow(detectedWindow);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            detectedWindow = DetectedWindowInfo.FromException(ex);
            _viewModel.SetLastDetectedWindow(detectedWindow);
        }

        AttachToDetectedWindow(detectedWindow, activateAfterAttach: true, dragStatusOnSuccess: null);
    }

    private void OnHotkeyChangeRequested(object? sender, Models.HotkeyOption option)
    {
        if (_windowHandle == IntPtr.Zero)
        {
            // Nothing was registered. Only claim the previous hotkey is "still active"
            // when one actually is; otherwise no attach shortcut is active.
            if (_hotkeyRegistered)
            {
                _viewModel.NotifyHotkeyRevertedToPrevious("the window isn't ready yet");
            }
            else
            {
                _viewModel.NotifyHotkeyUnavailable("the window isn't ready yet");
            }

            return;
        }

        var coordinator = new HotkeyRegistrationCoordinator(
            new HotkeyServiceRegistrar(_hotkeyService, _windowHandle));
        var result = coordinator.Apply(option, _viewModel.AppliedHotkey, _hotkeyRegistered);
        _hotkeyRegistered = result.HotkeyRegistered;

        switch (result.Outcome)
        {
            case HotkeyApplyOutcome.Applied:
                _viewModel.NotifyHotkeyApplied();
                break;
            case HotkeyApplyOutcome.RevertedToPrevious:
                Debug.WriteLine($"SessionPad hotkey change failed (Win32 error {result.Error}); kept previous.");
                _viewModel.NotifyHotkeyRevertedToPrevious("it may be in use by another app");
                break;
            case HotkeyApplyOutcome.NoHotkeyActive:
                Debug.WriteLine($"SessionPad hotkey change failed (Win32 error {result.Error}); none active.");
                _viewModel.NotifyHotkeyUnavailable("it may be in use by another app");
                break;
        }
    }

    /// <summary>Adapts <see cref="HotkeyService"/> to <see cref="IHotkeyRegistrar"/> for a fixed window handle.</summary>
    private sealed class HotkeyServiceRegistrar : IHotkeyRegistrar
    {
        private readonly HotkeyService _service;
        private readonly IntPtr _hwnd;

        public HotkeyServiceRegistrar(HotkeyService service, IntPtr hwnd)
        {
            _service = service;
            _hwnd = hwnd;
        }

        public int LastError => _service.LastRegistrationError;

        public bool Register(uint modifiers, uint virtualKey) =>
            _service.Register(_hwnd, modifiers, virtualKey);

        public void Unregister() => _service.Unregister(_hwnd);
    }

    private void TryAttachToNearestWindowAfterDrag()
    {
        DetectedWindowInfo? detectedWindow;
        string status;

        try
        {
            detectedWindow = _windowDetectionService.FindNearestAttachTarget(
                _windowHandle,
                DragAttachThresholdPx,
                out status);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            status = $"Drag attach search failed: {ex.GetType().Name}: {ex.Message}";
            detectedWindow = null;
        }

        if (detectedWindow is null)
        {
            var detachResult = _windowAttachmentService.Detach(status);
            _viewModel.SetAttachmentResult(detachResult);
            _viewModel.SetDragAttachStatus(status);
            UpdateAttachmentTimer(detachResult);
            return;
        }

        _viewModel.SetLastDetectedWindow(detectedWindow);
        AttachToDetectedWindow(
            detectedWindow,
            activateAfterAttach: true,
            dragStatusOnSuccess: $"Attached by drag to {CreateTargetLabel(detectedWindow)}.");
    }

    private void AttachToDetectedWindow(
        DetectedWindowInfo detectedWindow,
        bool activateAfterAttach,
        string? dragStatusOnSuccess)
    {
        if (detectedWindow.IsCurrentProcessWindow)
        {
            var selfAttachmentResult = _windowAttachmentService.IgnoreSessionPadWindow();
            _viewModel.SetAttachmentResult(selfAttachmentResult);
            if (dragStatusOnSuccess is not null)
            {
                _viewModel.SetDragAttachStatus("Ignored SessionPad window during drag attach.");
            }

            UpdateAttachmentTimer(selfAttachmentResult);
            ShowAndRestoreSafely();
            if (activateAfterAttach)
            {
                ActivateSafely();
            }

            return;
        }

        if (CanUseWindowSession(detectedWindow))
        {
            LoadWindowSessionSafely(detectedWindow);
        }
        else
        {
            _viewModel.SetSessionStatus("No valid external session target detected.");
        }

        ShowAndRestoreSafely();
        var attachmentResult = _windowAttachmentService.TryAttachToWindow(_windowHandle, detectedWindow);
        _viewModel.SetAttachmentResult(attachmentResult);
        if (dragStatusOnSuccess is not null)
        {
            _viewModel.SetDragAttachStatus(attachmentResult.IsAttached
                ? dragStatusOnSuccess
                : attachmentResult.Error ?? attachmentResult.Status);
        }

        UpdateAttachmentTimer(attachmentResult);
        ApplyAttachmentVisibility(attachmentResult);
        if (activateAfterAttach && !attachmentResult.IsHiddenBecauseTargetMinimized)
        {
            ActivateSafely();
        }
    }

    private void LoadWindowSessionSafely(DetectedWindowInfo detectedWindow)
    {
        try
        {
            var session = _sessionMatcher.FindOrCreateSession(detectedWindow);
            var matchKey = _sessionMatcher.CreateMatchKey(session.Identity);
            _viewModel.LoadWindowSession(session, matchKey);
            _lastAttachedTitle = string.IsNullOrWhiteSpace(detectedWindow.Title)
                ? null
                : detectedWindow.Title;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SessionPad could not match or load the window session: {ex}");
            _viewModel.SetSessionStatus($"Session matching failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static bool CanUseWindowSession(DetectedWindowInfo detectedWindow)
    {
        // The shared policy used by every attach path. Monitor geometry is not
        // available here, so the titleless full-screen rule is skipped (it only
        // applies when detectable from the provided data); the drag path supplies it.
        return WindowTargetPolicy.IsValidTarget(detectedWindow);
    }

    private void OnAttachmentTimerTick(object? sender, EventArgs e)
    {
        WindowAttachmentResult attachmentResult;

        try
        {
            attachmentResult = _windowAttachmentService.UpdateAttachedWindowPosition(_windowHandle);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            attachmentResult = _windowAttachmentService.Detach(
                $"Follow update failed: {ex.GetType().Name}: {ex.Message}");
        }

        _viewModel.SetAttachmentResult(attachmentResult);
        UpdateAttachmentTimer(attachmentResult);
        ApplyAttachmentVisibility(attachmentResult);
        SwitchSessionIfAttachedTitleChanged(attachmentResult);
    }

    private void OnAutoTrackForegroundChanged(object? sender, bool enabled)
    {
        _viewModel.SetDragAttachStatus(enabled
            ? "Auto-tracking on. SessionPad follows the focused window."
            : "Auto-tracking off.");
        UpdateForegroundWatchTimer();
    }

    private void UpdateForegroundWatchTimer()
    {
        if (_viewModel.AutoTrackForeground)
        {
            if (!_foregroundWatchTimer.IsEnabled)
            {
                _foregroundWatchTimer.Start();
            }

            return;
        }

        if (_foregroundWatchTimer.IsEnabled)
        {
            _foregroundWatchTimer.Stop();
        }

        _lastForegroundHwnd = IntPtr.Zero;
    }

    private void OnForegroundWatchTick(object? sender, EventArgs e)
    {
        if (!_viewModel.AutoTrackForeground)
        {
            _foregroundWatchTimer.Stop();
            return;
        }

        // Suppress automatic switching while the user hid SessionPad to the tray
        // or is dragging it to attach manually.
        if (_userHiddenToTray || _isDragAttachInProgress)
        {
            return;
        }

        DetectedWindowInfo detectedWindow;
        try
        {
            detectedWindow = _windowDetectionService.GetForegroundWindowInfo(_windowHandle);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SessionPad auto-track could not read the foreground window: {ex}");
            return;
        }

        if (detectedWindow.Hwnd == IntPtr.Zero || detectedWindow.IsCurrentProcessWindow)
        {
            return;
        }

        // Already attached to and following this window; the attachment timer handles it.
        if (_windowAttachmentService.HasAttachedTarget
            && detectedWindow.Hwnd == _windowAttachmentService.AttachedTargetHwnd)
        {
            _lastForegroundHwnd = detectedWindow.Hwnd;
            return;
        }

        // Only attempt once per distinct foreground window to avoid repeated retries.
        if (detectedWindow.Hwnd == _lastForegroundHwnd)
        {
            return;
        }

        if (!CanUseWindowSession(detectedWindow))
        {
            return;
        }

        _lastForegroundHwnd = detectedWindow.Hwnd;
        _viewModel.SetLastDetectedWindow(detectedWindow);

        // activateAfterAttach must stay false so we never steal focus from the
        // window the user just switched to.
        AttachToDetectedWindow(detectedWindow, activateAfterAttach: false, dragStatusOnSuccess: null);
    }

    private void OnWinEventReceived(uint eventType, IntPtr hwnd)
    {
        if (!Dispatcher.CheckAccess())
        {
            try
            {
                Dispatcher.BeginInvoke(() => HandleWinEvent(eventType, hwnd));
            }
            catch (Exception ex) when (ex is InvalidOperationException)
            {
                Debug.WriteLine($"SessionPad could not dispatch a WinEvent: {ex.Message}");
            }

            return;
        }

        HandleWinEvent(eventType, hwnd);
    }

    private void HandleWinEvent(uint eventType, IntPtr hwnd)
    {
        // Guard against re-entrancy if our own repositioning triggers more events.
        if (_isHandlingWinEvent)
        {
            return;
        }

        _isHandlingWinEvent = true;
        try
        {
            switch (eventType)
            {
                case User32.EventSystemForeground:
                    OnForegroundWatchTick(this, EventArgs.Empty);
                    break;
                case User32.EventSystemMinimizeStart:
                case User32.EventSystemMinimizeEnd:
                case User32.EventObjectDestroy:
                case User32.EventObjectLocationChange:
                case User32.EventObjectNameChange:
                    if (_windowAttachmentService.HasAttachedTarget
                        && hwnd == _windowAttachmentService.AttachedTargetHwnd)
                    {
                        OnAttachmentTimerTick(this, EventArgs.Empty);
                    }

                    break;
            }
        }
        finally
        {
            _isHandlingWinEvent = false;
        }
    }

    private void ConfigureTimerIntervals(bool hooksActive)
    {
        // With hooks active, polling stays only as a low-frequency safety net.
        _attachmentTimer.Interval = hooksActive
            ? TimeSpan.FromMilliseconds(1000)
            : AttachmentPollInterval;
        _foregroundWatchTimer.Interval = hooksActive
            ? TimeSpan.FromMilliseconds(1000)
            : ForegroundWatchInterval;
    }

    private void UpdateAttachmentTimer(WindowAttachmentResult result)
    {
        if (result.ShouldContinueTracking)
        {
            if (!_attachmentTimer.IsEnabled)
            {
                _attachmentTimer.Start();
            }

            return;
        }

        if (_attachmentTimer.IsEnabled)
        {
            _attachmentTimer.Stop();
        }

        _lastAttachedTitle = null;
    }

    private void ResumeAttachmentTimerIfAttached()
    {
        if (_windowAttachmentService.HasAttachedTarget && !_attachmentTimer.IsEnabled)
        {
            _attachmentTimer.Start();
        }
    }

    private void ApplyAttachmentVisibility(WindowAttachmentResult result)
    {
        if (result.IsHiddenBecauseTargetMinimized)
        {
            HideSafely();
            return;
        }

        if (!IsVisible && !_userHiddenToTray)
        {
            ShowAndRestoreSafely();
        }
    }

    private void SwitchSessionIfAttachedTitleChanged(WindowAttachmentResult result)
    {
        if (!result.ShouldContinueTracking
            || result.IsHiddenBecauseTargetMinimized
            || _isDragAttachInProgress
            || _userHiddenToTray
            || !IsVisible)
        {
            return;
        }

        var attachedHwnd = _windowAttachmentService.AttachedTargetHwnd;
        if (attachedHwnd == IntPtr.Zero)
        {
            return;
        }

        DetectedWindowInfo windowInfo;
        try
        {
            windowInfo = _windowDetectionService.GetWindowInfo(attachedHwnd, _windowHandle);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SessionPad could not read attached window title: {ex}");
            return;
        }

        if (string.IsNullOrWhiteSpace(windowInfo.Title))
        {
            return;
        }

        if (string.Equals(windowInfo.Title, _lastAttachedTitle, StringComparison.Ordinal))
        {
            return;
        }

        _lastAttachedTitle = windowInfo.Title;
        _viewModel.SetLastDetectedWindow(windowInfo);

        if (!CanUseWindowSession(windowInfo))
        {
            return;
        }

        // The raw title changed but still resolves to the current pad (e.g. the same
        // VS Code project, or only whitespace/case differs) — do not reload.
        var candidateKey = _sessionMatcher.CreateMatchKey(windowInfo);
        if (string.Equals(candidateKey, _viewModel.CurrentSessionMatchKey, StringComparison.Ordinal))
        {
            return;
        }

        // Coalesce rapid title churn (e.g. a shell that puts the running command or path
        // in its title): perform the actual switch only after the title settles.
        _titleSwitchDebounceTimer.Stop();
        _titleSwitchDebounceTimer.Start();
    }

    private void OnTitleSwitchDebounceTick(object? sender, EventArgs e)
    {
        _titleSwitchDebounceTimer.Stop();

        if (_isDragAttachInProgress || _userHiddenToTray || !IsVisible)
        {
            return;
        }

        var attachedHwnd = _windowAttachmentService.AttachedTargetHwnd;
        if (attachedHwnd == IntPtr.Zero)
        {
            return;
        }

        DetectedWindowInfo windowInfo;
        try
        {
            windowInfo = _windowDetectionService.GetWindowInfo(attachedHwnd, _windowHandle);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SessionPad could not read the settled window title: {ex}");
            return;
        }

        if (string.IsNullOrWhiteSpace(windowInfo.Title) || !CanUseWindowSession(windowInfo))
        {
            return;
        }

        // Re-check against the settled title: it may have returned to the current pad.
        var candidateKey = _sessionMatcher.CreateMatchKey(windowInfo);
        if (string.Equals(candidateKey, _viewModel.CurrentSessionMatchKey, StringComparison.Ordinal))
        {
            return;
        }

        _viewModel.SetLastDetectedWindow(windowInfo);
        LoadWindowSessionSafely(windowInfo);
    }

    private void HideSafely()
    {
        try
        {
            if (IsVisible)
            {
                Hide();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SessionPad could not hide while target is minimized: {ex}");
        }
    }

    private void ShowAndRestoreSafely()
    {
        try
        {
            ShowAndRestore();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SessionPad could not show or restore the window: {ex}");
        }
    }

    private void ShowAndRestore()
    {
        if (!IsVisible)
        {
            Show();
        }

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
    }

    private void ActivateSafely()
    {
        try
        {
            Activate();
            Topmost = true;
            Topmost = false;
            Focus();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SessionPad could not activate the window: {ex}");
        }
    }

    private void OnDeleteLocalDataRequested(object? sender, EventArgs e)
    {
        // Confirmation already happened in-app (inline Delete/Cancel in Settings).
        _attachmentTimer.Stop();
        var attachmentResult = _windowAttachmentService.Detach("Local data deleted.");
        _viewModel.SetAttachmentResult(attachmentResult);

        if (_localDataService.DeleteAllLocalData(out var error))
        {
            _viewModel.ResetAfterLocalDataDeleted();
            ShowAndRestoreSafely();
            return;
        }

        _viewModel.SetLocalDataStatus($"Delete failed: {error ?? "Unknown error"}");
    }

    private static string CreateTargetLabel(DetectedWindowInfo detectedWindow)
    {
        if (!string.IsNullOrWhiteSpace(detectedWindow.ProcessName)
            && !string.IsNullOrWhiteSpace(detectedWindow.Title))
        {
            return $"{detectedWindow.ProcessName} / {detectedWindow.Title}";
        }

        if (!string.IsNullOrWhiteSpace(detectedWindow.Title))
        {
            return detectedWindow.Title;
        }

        return string.IsNullOrWhiteSpace(detectedWindow.ProcessName)
            ? "target window"
            : detectedWindow.ProcessName;
    }

}
