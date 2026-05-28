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

    private readonly HotkeyService _hotkeyService = new();
    private readonly WindowDetectionService _windowDetectionService = new();
    private readonly WindowAttachmentService _windowAttachmentService = new();
    private readonly FloatingNoteViewModel _viewModel = new();
    private readonly DispatcherTimer _attachmentTimer = new();
    private HwndSource? _source;
    private IntPtr _windowHandle;
    private bool _hotkeyRegistered;

    public MainWindow()
    {
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

        ShowAndRestoreSafely();
        var attachmentResult = _windowAttachmentService.TryAttachToWindow(_windowHandle, detectedWindow);
        _viewModel.SetAttachmentResult(attachmentResult);
        UpdateAttachmentTimer(attachmentResult);
        ApplyAttachmentVisibility(attachmentResult);
        if (!attachmentResult.IsHiddenBecauseTargetMinimized)
        {
            ActivateSafely();
        }
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

}
