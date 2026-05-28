using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using SessionPad.App.Native;
using SessionPad.App.Services;
using SessionPad.App.ViewModels;

namespace SessionPad.App;

public partial class MainWindow : Window
{
    private readonly HotkeyService _hotkeyService = new();
    private readonly WindowDetectionService _windowDetectionService = new();
    private readonly FloatingNoteViewModel _viewModel = new();
    private HwndSource? _source;
    private IntPtr _windowHandle;
    private bool _hotkeyRegistered;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
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
        try
        {
            var detectedWindow = _windowDetectionService.GetForegroundWindowInfo(_windowHandle);
            _viewModel.SetLastDetectedWindow(detectedWindow);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            _viewModel.SetLastDetectedWindow(Models.DetectedWindowInfo.FromException(ex));
        }

        ShowAndActivateSafely();
    }

    private void ShowAndActivateSafely()
    {
        try
        {
            ShowAndActivate();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SessionPad could not show or activate the window: {ex}");
        }
    }

    private void ShowAndActivate()
    {
        if (!IsVisible)
        {
            Show();
        }

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }
}
