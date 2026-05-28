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
                || string.Equals(processName, "SessionPad.App", StringComparison.OrdinalIgnoreCase);

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

        try
        {
            var enumerated = User32.EnumWindows((hwnd, _) =>
            {
                try
                {
                    if (!IsAttachCandidate(hwnd, sessionPadHwnd, out var candidateBounds))
                    {
                        return true;
                    }

                    var distance = CalculateRectangleDistance(sessionPadBounds, candidateBounds);
                    if (distance > effectiveThreshold || distance >= nearestDistance)
                    {
                        return true;
                    }

                    var windowInfo = GetWindowInfo(hwnd, sessionPadHwnd);
                    if (!IsUsableExternalTarget(windowInfo))
                    {
                        return true;
                    }

                    nearestTarget = windowInfo;
                    nearestDistance = distance;
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
            status = $"No nearby target window within {effectiveThreshold}px.";
            return null;
        }

        status = $"Nearby target selected at {Math.Round(nearestDistance)}px.";
        return nearestTarget;
    }

    private static bool IsAttachCandidate(IntPtr hwnd, IntPtr sessionPadHwnd, out NativeRect bounds)
    {
        bounds = default;

        if (hwnd == IntPtr.Zero || hwnd == sessionPadHwnd)
        {
            return false;
        }

        if (IsCurrentProcessWindow(hwnd))
        {
            return false;
        }

        if (!User32.IsWindow(hwnd) || !User32.IsWindowVisible(hwnd) || User32.IsIconic(hwnd))
        {
            return false;
        }

        if (!User32.GetWindowRect(hwnd, out bounds))
        {
            return false;
        }

        return bounds.Right > bounds.Left && bounds.Bottom > bounds.Top;
    }

    private static bool IsUsableExternalTarget(DetectedWindowInfo window)
    {
        return window.Hwnd != IntPtr.Zero
            && !window.IsCurrentProcessWindow
            && window.IsVisible
            && !window.IsMinimized
            && window.Width > 0
            && window.Height > 0;
    }

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
}
