using SessionPad.App.Models;

namespace SessionPad.App.Services;

/// <summary>A screen rectangle, in pixels, independent of any Win32 type.</summary>
public readonly record struct ScreenBounds(int Left, int Top, int Right, int Bottom);

/// <summary>The monitor and work-area rectangles a window sits on.</summary>
public readonly record struct MonitorBounds(ScreenBounds Monitor, ScreenBounds WorkArea);

public enum WindowTargetRejection
{
    None,
    EmptyHandle,
    SessionPadOwned,
    Invisible,
    Minimized,
    ZeroSize,
    ShellSystemClass,
    AmbiguousExplorerWindow,
    TitlelessFullScreenBackground
}

public readonly record struct WindowTargetDecision(bool IsValid, WindowTargetRejection Rejection, string Reason)
{
    public static WindowTargetDecision Accept() =>
        new(true, WindowTargetRejection.None, string.Empty);

    public static WindowTargetDecision Reject(WindowTargetRejection rejection, string reason) =>
        new(false, rejection, reason);
}

/// <summary>
/// The single policy every attach path uses to decide whether an external window is a
/// valid target (global hotkey, auto-track, drag attach, and title-change switching).
/// Pure: it works only off the data in <see cref="DetectedWindowInfo"/> (plus optional
/// monitor bounds), so it can be unit tested without any Win32 calls.
/// </summary>
public static class WindowTargetPolicy
{
    private static readonly HashSet<string> RejectedShellClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Progman",
        "WorkerW",
        "Shell_TrayWnd",
        "Shell_SecondaryTrayWnd",
        "Button",
        "NotifyIconOverflowWindow",
        "DV2ControlHost",
        "SysShadow",
        "tooltips_class32",
        "#32768"
    };

    private static readonly HashSet<string> AllowedExplorerClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "CabinetWClass",
        "ExploreWClass"
    };

    /// <summary>Convenience wrapper returning only whether the window is a valid target.</summary>
    public static bool IsValidTarget(DetectedWindowInfo window, MonitorBounds? monitor = null) =>
        Evaluate(window, monitor).IsValid;

    /// <summary>
    /// Evaluates <paramref name="window"/> against the full target policy. The titleless
    /// full-screen background rule only applies when <paramref name="monitor"/> is
    /// supplied (i.e. when detectable from the provided data).
    /// </summary>
    public static WindowTargetDecision Evaluate(DetectedWindowInfo window, MonitorBounds? monitor = null)
    {
        if (window.Hwnd == IntPtr.Zero)
        {
            return WindowTargetDecision.Reject(WindowTargetRejection.EmptyHandle, "empty window handle");
        }

        if (window.IsCurrentProcessWindow)
        {
            return WindowTargetDecision.Reject(WindowTargetRejection.SessionPadOwned, "SessionPad-owned window");
        }

        if (!window.IsVisible)
        {
            return WindowTargetDecision.Reject(WindowTargetRejection.Invisible, "invisible window");
        }

        if (window.IsMinimized)
        {
            return WindowTargetDecision.Reject(WindowTargetRejection.Minimized, "minimized window");
        }

        if (window.Width <= 0 || window.Height <= 0)
        {
            return WindowTargetDecision.Reject(WindowTargetRejection.ZeroSize, "unusable window bounds");
        }

        if (IsRejectedShellClass(window.WindowClass))
        {
            return WindowTargetDecision.Reject(
                WindowTargetRejection.ShellSystemClass,
                $"shell/system window class {window.WindowClass}");
        }

        if (IsAmbiguousExplorerWindow(window.ProcessName, window.Title, window.WindowClass))
        {
            return WindowTargetDecision.Reject(
                WindowTargetRejection.AmbiguousExplorerWindow,
                string.IsNullOrWhiteSpace(window.WindowClass)
                    ? "ambiguous explorer shell window"
                    : $"explorer shell window {window.WindowClass}");
        }

        if (string.IsNullOrWhiteSpace(window.Title)
            && monitor is { } bounds
            && CoversMonitorOrWorkArea(window, bounds))
        {
            return WindowTargetDecision.Reject(
                WindowTargetRejection.TitlelessFullScreenBackground,
                "titleless full-screen background window");
        }

        return WindowTargetDecision.Accept();
    }

    private static bool IsRejectedShellClass(string? windowClass) =>
        !string.IsNullOrWhiteSpace(windowClass) && RejectedShellClasses.Contains(windowClass);

    private static bool IsAmbiguousExplorerWindow(string processName, string title, string? windowClass)
    {
        if (!IsProcessName(processName, "explorer"))
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(title)
            || string.IsNullOrWhiteSpace(windowClass)
            || !AllowedExplorerClasses.Contains(windowClass);
    }

    private static bool CoversMonitorOrWorkArea(DetectedWindowInfo window, MonitorBounds bounds)
    {
        var windowBounds = new ScreenBounds(window.Left, window.Top, window.Right, window.Bottom);
        return ContainsWithTolerance(windowBounds, bounds.Monitor)
            || ContainsWithTolerance(windowBounds, bounds.WorkArea);
    }

    private static bool ContainsWithTolerance(ScreenBounds outer, ScreenBounds inner)
    {
        const int tolerance = 8;

        return outer.Left <= inner.Left + tolerance
            && outer.Top <= inner.Top + tolerance
            && outer.Right >= inner.Right - tolerance
            && outer.Bottom >= inner.Bottom - tolerance;
    }

    private static bool IsProcessName(string processName, string expected) =>
        string.Equals(
            NormalizeProcessName(processName),
            NormalizeProcessName(expected),
            StringComparison.OrdinalIgnoreCase);

    private static string NormalizeProcessName(string processName)
    {
        var normalized = processName.Trim();
        return normalized.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? normalized[..^4]
            : normalized;
    }
}
