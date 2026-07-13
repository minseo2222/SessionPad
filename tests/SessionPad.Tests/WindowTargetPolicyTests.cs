using SessionPad.App.Models;
using SessionPad.App.Services;

namespace SessionPad.Tests;

public class WindowTargetPolicyTests
{
    private static DetectedWindowInfo Window(
        string processName = "Code",
        string title = "main.cs - project - Visual Studio Code",
        string? windowClass = "Chrome_WidgetWin_1",
        int left = 100,
        int top = 100,
        int right = 900,
        int bottom = 700,
        bool isVisible = true,
        bool isMinimized = false,
        bool isSessionPadWindow = false,
        IntPtr? hwnd = null)
    {
        return new DetectedWindowInfo
        {
            Hwnd = hwnd ?? new IntPtr(0x1234),
            HwndHex = "0x0000000000001234",
            ProcessName = processName,
            Title = title,
            ProcessId = 4321,
            Left = left,
            Top = top,
            Right = right,
            Bottom = bottom,
            IsVisible = isVisible,
            IsMinimized = isMinimized,
            IsSessionPadWindow = isSessionPadWindow,
            WindowClass = windowClass
        };
    }

    [Fact]
    public void Valid_vs_code_like_window_is_accepted()
    {
        Assert.True(WindowTargetPolicy.IsValidTarget(Window()));
    }

    [Fact]
    public void Valid_browser_like_window_is_accepted()
    {
        var browser = Window(
            processName: "msedge",
            title: "Anthropic - Microsoft Edge",
            windowClass: "Chrome_WidgetWin_1");

        Assert.True(WindowTargetPolicy.IsValidTarget(browser));
    }

    [Fact]
    public void Empty_handle_is_rejected()
    {
        var decision = WindowTargetPolicy.Evaluate(Window(hwnd: IntPtr.Zero));

        Assert.False(decision.IsValid);
        Assert.Equal(WindowTargetRejection.EmptyHandle, decision.Rejection);
    }

    [Fact]
    public void Current_process_window_is_rejected()
    {
        var ownWindow = Window(processName: "SessionPad.App", title: "SessionPad");

        var decision = WindowTargetPolicy.Evaluate(ownWindow);

        Assert.False(decision.IsValid);
        Assert.Equal(WindowTargetRejection.SessionPadOwned, decision.Rejection);
    }

    [Fact]
    public void Session_pad_flagged_window_is_rejected()
    {
        var decision = WindowTargetPolicy.Evaluate(Window(isSessionPadWindow: true));

        Assert.False(decision.IsValid);
        Assert.Equal(WindowTargetRejection.SessionPadOwned, decision.Rejection);
    }

    [Fact]
    public void Invisible_window_is_rejected()
    {
        var decision = WindowTargetPolicy.Evaluate(Window(isVisible: false));

        Assert.False(decision.IsValid);
        Assert.Equal(WindowTargetRejection.Invisible, decision.Rejection);
    }

    [Fact]
    public void Minimized_window_is_rejected()
    {
        var decision = WindowTargetPolicy.Evaluate(Window(isMinimized: true));

        Assert.False(decision.IsValid);
        Assert.Equal(WindowTargetRejection.Minimized, decision.Rejection);
    }

    [Fact]
    public void Zero_size_window_is_rejected()
    {
        var decision = WindowTargetPolicy.Evaluate(Window(left: 0, top: 0, right: 0, bottom: 0));

        Assert.False(decision.IsValid);
        Assert.Equal(WindowTargetRejection.ZeroSize, decision.Rejection);
    }

    [Theory]
    [InlineData("Progman")]            // the desktop
    [InlineData("WorkerW")]            // desktop wallpaper host
    [InlineData("Shell_TrayWnd")]      // the taskbar
    [InlineData("Shell_SecondaryTrayWnd")] // taskbar on a secondary monitor
    public void Shell_and_system_classes_are_rejected(string windowClass)
    {
        var decision = WindowTargetPolicy.Evaluate(Window(processName: "explorer", title: "", windowClass: windowClass));

        Assert.False(decision.IsValid);
        Assert.Equal(WindowTargetRejection.ShellSystemClass, decision.Rejection);
    }

    [Fact]
    public void Desktop_window_is_rejected()
    {
        // The desktop: explorer-owned, titleless, Progman class.
        var desktop = Window(processName: "explorer", title: "", windowClass: "Progman");

        Assert.False(WindowTargetPolicy.IsValidTarget(desktop));
    }

    [Fact]
    public void Taskbar_window_is_rejected()
    {
        var taskbar = Window(processName: "explorer", title: "", windowClass: "Shell_TrayWnd");

        Assert.False(WindowTargetPolicy.IsValidTarget(taskbar));
    }

    [Fact]
    public void Ambiguous_explorer_window_is_rejected()
    {
        // explorer-owned but not a real file window class.
        var ambiguous = Window(processName: "explorer", title: "", windowClass: "SomeOtherClass");

        var decision = WindowTargetPolicy.Evaluate(ambiguous);

        Assert.False(decision.IsValid);
        Assert.Equal(WindowTargetRejection.AmbiguousExplorerWindow, decision.Rejection);
    }

    [Fact]
    public void Real_explorer_file_window_is_accepted()
    {
        var fileWindow = Window(processName: "explorer", title: "Documents", windowClass: "CabinetWClass");

        Assert.True(WindowTargetPolicy.IsValidTarget(fileWindow));
    }

    [Fact]
    public void Titleless_full_screen_background_window_is_rejected_when_monitor_known()
    {
        var monitor = new MonitorBounds(
            new ScreenBounds(0, 0, 1920, 1080),
            new ScreenBounds(0, 0, 1920, 1040));
        var background = Window(
            processName: "weirdapp",
            title: "",
            windowClass: "RandomClass",
            left: 0,
            top: 0,
            right: 1920,
            bottom: 1080);

        var decision = WindowTargetPolicy.Evaluate(background, monitor);

        Assert.False(decision.IsValid);
        Assert.Equal(WindowTargetRejection.TitlelessFullScreenBackground, decision.Rejection);
    }

    [Fact]
    public void Titleless_full_screen_rule_is_skipped_without_monitor_data()
    {
        // Same window, but no monitor bounds provided: the rule cannot apply, so the
        // titleless window is accepted (other rules permitting).
        var background = Window(
            processName: "weirdapp",
            title: "",
            windowClass: "RandomClass",
            left: 0,
            top: 0,
            right: 1920,
            bottom: 1080);

        Assert.True(WindowTargetPolicy.IsValidTarget(background));
    }
}
