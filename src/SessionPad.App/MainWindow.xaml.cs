using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using SessionPad.App.Models;
using SessionPad.App.Native;
using SessionPad.App.Services;
using SessionPad.App.ViewModels;

namespace SessionPad.App;

public partial class MainWindow : Window
{
    private static readonly TimeSpan AttachmentPollInterval = TimeSpan.FromMilliseconds(150);
    private const int DragAttachThresholdPx = 48;

    private readonly HotkeyService _hotkeyService = new();
    private readonly WindowDetectionService _windowDetectionService = new();
    private readonly WindowAttachmentService _windowAttachmentService = new();
    private readonly LocalDataService _localDataService;
    private readonly NoteStorageService _noteStorageService;
    private readonly SessionMatcher _sessionMatcher;
    private readonly FloatingNoteViewModel _viewModel;
    private readonly DispatcherTimer _attachmentTimer = new();
    private HwndSource? _source;
    private IntPtr _windowHandle;
    private bool _hotkeyRegistered;

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
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _windowHandle = new WindowInteropHelper(this).Handle;
        _source = HwndSource.FromHwnd(_windowHandle);
        _source?.AddHook(OnWindowMessage);

        _hotkeyRegistered = _hotkeyService.Register(_windowHandle);
        if (!_hotkeyRegistered)
        {
            Debug.WriteLine(
                $"SessionPad could not register Ctrl+Alt+N. Win32 error: {_hotkeyService.LastRegistrationError}.");
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.DeleteLocalDataRequested -= OnDeleteLocalDataRequested;
        _attachmentTimer.Stop();
        _attachmentTimer.Tick -= OnAttachmentTimerTick;
        _windowAttachmentService.Detach("SessionPad closed.");

        if (_hotkeyRegistered)
        {
            _hotkeyService.Unregister(_windowHandle);
            _hotkeyRegistered = false;
        }

        _source?.RemoveHook(OnWindowMessage);
        _source = null;

        base.OnClosed(e);
    }

    public void BeginDragAttach()
    {
        _viewModel.SetDragAttachStatus("Drag attach started.");

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
            Debug.WriteLine($"SessionPad drag attach could not start: {ex.Message}");
            _viewModel.SetDragAttachStatus($"Drag attach could not start: {ex.Message}");
            ResumeAttachmentTimerIfAttached();
            return;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SessionPad drag attach failed: {ex}");
            _viewModel.SetDragAttachStatus($"Drag attach failed: {ex.GetType().Name}: {ex.Message}");
            ResumeAttachmentTimerIfAttached();
            return;
        }

        TryAttachToNearestWindowAfterDrag();
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

    private void OnHotkeyPressed()
    {
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
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SessionPad could not match or load the window session: {ex}");
            _viewModel.SetSessionStatus($"Session matching failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static bool CanUseWindowSession(DetectedWindowInfo detectedWindow)
    {
        return detectedWindow.Hwnd != IntPtr.Zero
            && !detectedWindow.IsCurrentProcessWindow
            && detectedWindow.IsVisible
            && !detectedWindow.IsMinimized
            && detectedWindow.Width > 0
            && detectedWindow.Height > 0;
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

        if (!IsVisible)
        {
            ShowAndRestoreSafely();
        }
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
        var result = MessageBox.Show(
            this,
            "Delete all local SessionPad data? This removes saved notes and sessions from this device. This cannot be undone.",
            "Delete Local Data",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result != MessageBoxResult.Yes)
        {
            _viewModel.SetLocalDataStatus("Delete canceled. Local data was not changed.");
            return;
        }

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
