using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using SessionPad.App.Models;
using SessionPad.App.Native;

namespace SessionPad.App.Services;

public sealed class WindowDetectionService
{
    public DetectedWindowInfo GetForegroundWindowInfo(IntPtr sessionPadHwnd)
    {
        try
        {
            var hwnd = User32.GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
            {
                return DetectedWindowInfo.FromError("No foreground window was available.", hwnd);
            }

            return GetWindowInfo(hwnd, sessionPadHwnd);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return DetectedWindowInfo.FromException(ex);
        }
    }

    public DetectedWindowInfo GetWindowInfo(IntPtr hwnd, IntPtr sessionPadHwnd)
    {
        try
        {
            if (hwnd == IntPtr.Zero)
            {
                return DetectedWindowInfo.FromError("No window handle was available.", hwnd);
            }

            var processId = GetProcessId(hwnd, out var processError);
            var processName = GetProcessName(processId, out var processNameError);
            var bounds = GetBounds(hwnd, out var boundsError);
            var isMinimized = IsMinimized(hwnd, out var minimizedError);
            var isVisible = IsVisible(hwnd, out var visibleError);
            var title = GetWindowTitle(hwnd, out var titleError);
            var windowClass = GetWindowClass(hwnd, out var classError);
            var isSessionPadWindow = hwnd == sessionPadHwnd
                || processId == Environment.ProcessId
                || IsProcessName(processName, "SessionPad.App");

            return new DetectedWindowInfo
            {
                Hwnd = hwnd,
                HwndHex = FormatHwnd(hwnd),
                ProcessName = processName,
                Title = title,
                ProcessId = processId,
                Left = bounds.Left,
                Top = bounds.Top,
                Right = bounds.Right,
                Bottom = bounds.Bottom,
                IsMinimized = isMinimized,
                IsVisible = isVisible,
                IsSessionPadWindow = isSessionPadWindow,
                WindowClass = windowClass,
                Error = JoinErrors(processError, processNameError, boundsError, minimizedError, visibleError, titleError, classError)
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return DetectedWindowInfo.FromException(ex, hwnd);
        }
    }

    public DetectedWindowInfo? FindNearestAttachTarget(
        IntPtr sessionPadHwnd,
        int thresholdPx,
        out string status)
    {
        status = "No nearby target window.";

        if (sessionPadHwnd == IntPtr.Zero)
        {
            status = "SessionPad window handle is not available.";
            return null;
        }

        if (!User32.GetWindowRect(sessionPadHwnd, out var sessionPadBounds))
        {
            status = $"Could not read SessionPad bounds. Win32 error: {Marshal.GetLastWin32Error()}";
            return null;
        }

        var effectiveThreshold = Math.Max(0, thresholdPx);
        DetectedWindowInfo? nearestTarget = null;
        var nearestDistance = double.MaxValue;
        var nearestHasTitle = false;
        string? lastRejectedReason = null;

        try
        {
            var enumerated = User32.EnumWindows((hwnd, _) =>
            {
                try
                {
                    if (!TryCreateAttachCandidate(
                            hwnd,
                            sessionPadHwnd,
                            out var candidate,
                            out var rejectionReason))
                    {
                        if (!string.IsNullOrWhiteSpace(rejectionReason))
                        {
                            lastRejectedReason = rejectionReason;
                            Debug.WriteLine(rejectionReason);
                        }

                        return true;
                    }

                    var distance = CalculateRectangleDistance(sessionPadBounds, candidate.Bounds);
                    var candidateHasTitle = !string.IsNullOrWhiteSpace(candidate.Title);
                    var isBetterCandidate = nearestTarget is null
                        || distance < nearestDistance
                        || (Math.Abs(distance - nearestDistance) < 0.5
                            && candidateHasTitle
                            && !nearestHasTitle);

                    if (distance > effectiveThreshold || !isBetterCandidate)
                    {
                        return true;
                    }

                    nearestTarget = candidate.ToDetectedWindowInfo(sessionPadHwnd);
                    nearestDistance = distance;
                    nearestHasTitle = candidateHasTitle;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"SessionPad skipped a drag attach candidate: {ex}");
                }

                return true;
            }, IntPtr.Zero);

            if (!enumerated)
            {
                status = $"Could not enumerate windows. Win32 error: {Marshal.GetLastWin32Error()}";
                return null;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            status = $"Nearest window search failed: {ex.GetType().Name}: {ex.Message}";
            return null;
        }

        if (nearestTarget is null)
        {
            status = lastRejectedReason is null
                ? $"No nearby target window within {effectiveThreshold}px."
                : $"No nearby target window within {effectiveThreshold}px. {lastRejectedReason}";
            return null;
        }

        status = $"Nearby target selected at {Math.Round(nearestDistance)}px.";
        return nearestTarget;
    }

    private static bool TryCreateAttachCandidate(
        IntPtr hwnd,
        IntPtr sessionPadHwnd,
        out AttachCandidate candidate,
        out string? rejectionReason)
    {
        candidate = default;
        rejectionReason = null;

        // Win32 precondition that the shared policy cannot express from data alone.
        if (hwnd == IntPtr.Zero || !User32.IsWindow(hwnd))
        {
            rejectionReason = "Drag attach skipped: invalid window.";
            return false;
        }

        var processId = GetProcessId(hwnd, out _);
        var processName = GetProcessName(processId, out _);
        var title = GetWindowTitle(hwnd, out _);
        var windowClass = GetWindowClass(hwnd, out _) ?? string.Empty;
        var bounds = GetBounds(hwnd, out _);

        var info = new DetectedWindowInfo
        {
            Hwnd = hwnd,
            HwndHex = FormatHwnd(hwnd),
            ProcessName = processName,
            Title = title,
            ProcessId = processId,
            Left = bounds.Left,
            Top = bounds.Top,
            Right = bounds.Right,
            Bottom = bounds.Bottom,
            IsMinimized = User32.IsIconic(hwnd),
            IsVisible = User32.IsWindowVisible(hwnd),
            IsSessionPadWindow = hwnd == sessionPadHwnd
                || processId == Environment.ProcessId
                || IsProcessName(processName, "SessionPad.App"),
            WindowClass = windowClass
        };

        // Only read monitor geometry when the titleless full-screen rule could apply.
        var monitor = string.IsNullOrWhiteSpace(title) ? TryGetMonitorBounds(hwnd) : null;
        var decision = WindowTargetPolicy.Evaluate(info, monitor);
        if (!decision.IsValid)
        {
            rejectionReason = $"Drag attach skipped: {decision.Reason}.";
            return false;
        }

        candidate = new AttachCandidate(
            hwnd,
            processId,
            processName,
            title,
            windowClass,
            bounds);

        return true;
    }

    private static MonitorBounds? TryGetMonitorBounds(IntPtr hwnd)
    {
        var monitor = User32.MonitorFromWindow(hwnd, User32.MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return null;
        }

        var monitorInfo = new MonitorInfo
        {
            Size = Marshal.SizeOf<MonitorInfo>()
        };

        if (!User32.GetMonitorInfoW(monitor, ref monitorInfo))
        {
            return null;
        }

        return new MonitorBounds(
            ToScreenBounds(monitorInfo.Monitor),
            ToScreenBounds(monitorInfo.WorkArea));
    }

    private static ScreenBounds ToScreenBounds(NativeRect rect) =>
        new(rect.Left, rect.Top, rect.Right, rect.Bottom);

    private static bool IsCurrentProcessWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        var processId = GetProcessId(hwnd, out _);
        return processId == Environment.ProcessId;
    }

    private static double CalculateRectangleDistance(NativeRect first, NativeRect second)
    {
        var horizontalGap = 0;
        if (first.Right < second.Left)
        {
            horizontalGap = second.Left - first.Right;
        }
        else if (second.Right < first.Left)
        {
            horizontalGap = first.Left - second.Right;
        }

        var verticalGap = 0;
        if (first.Bottom < second.Top)
        {
            verticalGap = second.Top - first.Bottom;
        }
        else if (second.Bottom < first.Top)
        {
            verticalGap = first.Top - second.Bottom;
        }

        return Math.Sqrt(((double)horizontalGap * horizontalGap) + ((double)verticalGap * verticalGap));
    }

    private static bool IsProcessName(string processName, string expected)
    {
        return string.Equals(
            NormalizeProcessName(processName),
            NormalizeProcessName(expected),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeProcessName(string processName)
    {
        var normalized = processName.Trim();
        return normalized.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? normalized[..^4]
            : normalized;
    }

    private static int GetProcessId(IntPtr hwnd, out string? error)
    {
        error = null;

        try
        {
            User32.GetWindowThreadProcessId(hwnd, out var processId);
            if (processId == 0)
            {
                error = $"Could not read process id. Win32 error: {Marshal.GetLastWin32Error()}";
                return 0;
            }

            return checked((int)processId);
        }
        catch (Exception ex) when (ex is OverflowException or InvalidOperationException)
        {
            error = $"Could not read process id: {ex.Message}";
            return 0;
        }
    }

    private static string GetProcessName(int processId, out string? error)
    {
        error = null;

        if (processId <= 0)
        {
            return "(unknown)";
        }

        try
        {
            using var process = Process.GetProcessById(processId);
            return process.ProcessName;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            error = $"Could not read process name: {ex.Message}";
            return "(unknown)";
        }
    }

    private static string GetWindowTitle(IntPtr hwnd, out string? error)
    {
        error = null;

        try
        {
            var length = User32.GetWindowTextLengthW(hwnd);
            if (length <= 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder(length + 1);
            var copied = User32.GetWindowTextW(hwnd, builder, builder.Capacity);
            if (copied <= 0)
            {
                error = $"Could not read window title. Win32 error: {Marshal.GetLastWin32Error()}";
                return string.Empty;
            }

            return builder.ToString();
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or OutOfMemoryException)
        {
            error = $"Could not read window title: {ex.Message}";
            return string.Empty;
        }
    }

    private static string? GetWindowClass(IntPtr hwnd, out string? error)
    {
        error = null;

        try
        {
            var builder = new StringBuilder(256);
            var copied = User32.GetClassNameW(hwnd, builder, builder.Capacity);
            if (copied <= 0)
            {
                error = $"Could not read window class. Win32 error: {Marshal.GetLastWin32Error()}";
                return null;
            }

            return builder.ToString();
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or OutOfMemoryException)
        {
            error = $"Could not read window class: {ex.Message}";
            return null;
        }
    }

    private static NativeRect GetBounds(IntPtr hwnd, out string? error)
    {
        error = null;

        if (User32.GetWindowRect(hwnd, out var rect))
        {
            return rect;
        }

        error = $"Could not read window bounds. Win32 error: {Marshal.GetLastWin32Error()}";
        return default;
    }

    private static bool IsMinimized(IntPtr hwnd, out string? error)
    {
        error = null;

        try
        {
            return User32.IsIconic(hwnd);
        }
        catch (Exception ex) when (ex is InvalidOperationException)
        {
            error = $"Could not read minimized state: {ex.Message}";
            return false;
        }
    }

    private static bool IsVisible(IntPtr hwnd, out string? error)
    {
        error = null;

        try
        {
            return User32.IsWindowVisible(hwnd);
        }
        catch (Exception ex) when (ex is InvalidOperationException)
        {
            error = $"Could not read visibility state: {ex.Message}";
            return false;
        }
    }

    private static string FormatHwnd(IntPtr hwnd)
    {
        return $"0x{hwnd.ToInt64():X16}";
    }

    private static string? JoinErrors(params string?[] errors)
    {
        var presentErrors = errors
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .ToArray();

        return presentErrors.Length == 0 ? null : string.Join("; ", presentErrors);
    }

    private readonly record struct AttachCandidate(
        IntPtr Hwnd,
        int ProcessId,
        string ProcessName,
        string Title,
        string? WindowClass,
        NativeRect Bounds)
    {
        public DetectedWindowInfo ToDetectedWindowInfo(IntPtr sessionPadHwnd)
        {
            var isSessionPadWindow = Hwnd == sessionPadHwnd
                || ProcessId == Environment.ProcessId
                || IsProcessName(ProcessName, "SessionPad.App");

            return new DetectedWindowInfo
            {
                Hwnd = Hwnd,
                HwndHex = FormatHwnd(Hwnd),
                ProcessName = ProcessName,
                Title = Title,
                ProcessId = ProcessId,
                Left = Bounds.Left,
                Top = Bounds.Top,
                Right = Bounds.Right,
                Bottom = Bounds.Bottom,
                IsMinimized = false,
                IsVisible = true,
                IsSessionPadWindow = isSessionPadWindow,
                WindowClass = WindowClass,
                Error = null
            };
        }
    }
}
