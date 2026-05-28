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

            var processId = GetProcessId(hwnd, out var processError);
            var bounds = GetBounds(hwnd, out var boundsError);
            var isMinimized = IsMinimized(hwnd, out var minimizedError);
            var isVisible = IsVisible(hwnd, out var visibleError);
            var error = JoinErrors(processError, boundsError, minimizedError, visibleError);

            return new DetectedWindowInfo
            {
                HwndHex = FormatHwnd(hwnd),
                ProcessName = GetProcessName(processId, out var processNameError),
                Title = GetWindowTitle(hwnd, out var titleError),
                ProcessId = processId,
                Left = bounds.Left,
                Top = bounds.Top,
                Right = bounds.Right,
                Bottom = bounds.Bottom,
                IsMinimized = isMinimized,
                IsVisible = isVisible,
                IsSessionPadWindow = hwnd == sessionPadHwnd,
                WindowClass = GetWindowClass(hwnd, out var classError),
                Error = JoinErrors(error, processNameError, titleError, classError)
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return DetectedWindowInfo.FromException(ex);
        }
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
