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
        var hwnd = User32.GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return CreateErrorInfo(hwnd, "No foreground window was available.");
        }

        var processId = GetProcessId(hwnd, out var processError);
        var bounds = GetBounds(hwnd, out var boundsError);
        var error = JoinErrors(processError, boundsError);

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
            IsMinimized = User32.IsIconic(hwnd),
            IsVisible = User32.IsWindowVisible(hwnd),
            IsSessionPadWindow = hwnd == sessionPadHwnd,
            WindowClass = GetWindowClass(hwnd, out var classError),
            Error = JoinErrors(error, processNameError, titleError, classError)
        };
    }

    private static int GetProcessId(IntPtr hwnd, out string? error)
    {
        error = null;

        try
        {
            User32.GetWindowThreadProcessId(hwnd, out var processId);
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
            return copied <= 0 ? string.Empty : builder.ToString();
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
            return copied <= 0 ? null : builder.ToString();
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

    private static DetectedWindowInfo CreateErrorInfo(IntPtr hwnd, string error)
    {
        return new DetectedWindowInfo
        {
            HwndHex = FormatHwnd(hwnd),
            ProcessName = "(unknown)",
            Title = string.Empty,
            ProcessId = 0,
            Left = 0,
            Top = 0,
            Right = 0,
            Bottom = 0,
            IsMinimized = false,
            IsVisible = false,
            IsSessionPadWindow = false,
            Error = error
        };
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
