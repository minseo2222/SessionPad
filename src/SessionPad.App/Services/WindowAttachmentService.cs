using System.Diagnostics;
using System.Runtime.InteropServices;
using SessionPad.App.Models;
using SessionPad.App.Native;

namespace SessionPad.App.Services;

public sealed class WindowAttachmentService
{
    private const int Gap = 8;

    public WindowAttachmentResult TryAttachToWindow(IntPtr sessionPadHwnd, DetectedWindowInfo target)
    {
        try
        {
            var validationError = ValidateTarget(sessionPadHwnd, target);
            if (validationError is not null)
            {
                return WindowAttachmentResult.NotAttached(validationError);
            }

            if (!User32.GetWindowRect(sessionPadHwnd, out var sessionRect))
            {
                return WindowAttachmentResult.NotAttached(
                    $"Could not read SessionPad bounds. Win32 error: {Marshal.GetLastWin32Error()}");
            }

            var sessionWidth = sessionRect.Right - sessionRect.Left;
            var sessionHeight = sessionRect.Bottom - sessionRect.Top;
            if (sessionWidth <= 0 || sessionHeight <= 0)
            {
                return WindowAttachmentResult.NotAttached("SessionPad bounds are not usable.");
            }

            var side = "Right";
            var x = target.Right + Gap;
            var y = target.Top;

            if (TryGetWorkArea(target.Hwnd, out var workArea))
            {
                var rightX = target.Right + Gap;
                var leftX = target.Left - Gap - sessionWidth;

                if (rightX + sessionWidth <= workArea.Right)
                {
                    x = rightX;
                    side = "Right";
                }
                else if (leftX >= workArea.Left)
                {
                    x = leftX;
                    side = "Left";
                }
                else
                {
                    x = Clamp(rightX, workArea.Left, workArea.Right - sessionWidth);
                    side = "Clamped";
                }

                y = Clamp(target.Top, workArea.Top, workArea.Bottom - sessionHeight);
            }

            var moved = User32.SetWindowPos(
                sessionPadHwnd,
                IntPtr.Zero,
                x,
                y,
                0,
                0,
                User32.SwpNoSize | User32.SwpNoZOrder | User32.SwpNoActivate);

            if (!moved)
            {
                return WindowAttachmentResult.NotAttached(
                    $"Could not position SessionPad. Win32 error: {Marshal.GetLastWin32Error()}");
            }

            return WindowAttachmentResult.Attached(side);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return WindowAttachmentResult.NotAttached($"Attach failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static string? ValidateTarget(IntPtr sessionPadHwnd, DetectedWindowInfo target)
    {
        if (sessionPadHwnd == IntPtr.Zero)
        {
            return "SessionPad window handle is not available.";
        }

        if (target.Hwnd == IntPtr.Zero)
        {
            return "No external target detected.";
        }

        if (target.IsSessionPadWindow || target.Hwnd == sessionPadHwnd)
        {
            return "No external target detected.";
        }

        if (!target.IsVisible)
        {
            return "Target window is not visible.";
        }

        if (target.IsMinimized)
        {
            return "Target window is minimized.";
        }

        if (target.Width <= 0 || target.Height <= 0)
        {
            return "Target window bounds are not usable.";
        }

        return null;
    }

    private static bool TryGetWorkArea(IntPtr hwnd, out NativeRect workArea)
    {
        workArea = default;

        var monitor = User32.MonitorFromWindow(hwnd, User32.MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return false;
        }

        var monitorInfo = new MonitorInfo
        {
            Size = Marshal.SizeOf<MonitorInfo>()
        };

        if (!User32.GetMonitorInfoW(monitor, ref monitorInfo))
        {
            return false;
        }

        workArea = monitorInfo.WorkArea;
        return true;
    }

    private static int Clamp(int value, int min, int max)
    {
        if (max < min)
        {
            return min;
        }

        return Math.Min(Math.Max(value, min), max);
    }
}
