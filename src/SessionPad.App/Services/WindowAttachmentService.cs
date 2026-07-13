using System.Diagnostics;
using System.Runtime.InteropServices;
using SessionPad.App.Models;
using SessionPad.App.Native;

namespace SessionPad.App.Services;

public sealed class WindowAttachmentService
{
    private const int Gap = 8;

    private IntPtr _attachedTargetHwnd;
    private WindowBounds _lastTargetBounds;
    private int _lastSessionWidth;
    private int _lastSessionHeight;
    private string _attachSide = "Right";

    public bool HasAttachedTarget => _attachedTargetHwnd != IntPtr.Zero;

    public IntPtr AttachedTargetHwnd => _attachedTargetHwnd;

    public WindowAttachmentResult TryAttachToWindow(IntPtr sessionPadHwnd, DetectedWindowInfo target)
    {
        try
        {
            if (IsSessionPadTarget(sessionPadHwnd, target))
            {
                return IgnoreSessionPadWindow();
            }

            var validationError = ValidateTarget(sessionPadHwnd, target);
            if (validationError is not null)
            {
                ClearAttachedTarget();
                return WindowAttachmentResult.NotAttached(validationError);
            }

            if (!TryReadSessionSize(sessionPadHwnd, out var sessionWidth, out var sessionHeight, out var error))
            {
                ClearAttachedTarget();
                return WindowAttachmentResult.NotAttached(error ?? "SessionPad bounds are not usable.");
            }

            if (!TryReadTargetBounds(target.Hwnd, out var targetBounds, out error))
            {
                ClearAttachedTarget();
                return WindowAttachmentResult.NotAttached(error ?? "Target window bounds are not usable.");
            }

            if (!TryPositionSessionPad(sessionPadHwnd, target.Hwnd, targetBounds, sessionWidth, sessionHeight, out var side, out error))
            {
                ClearAttachedTarget();
                return WindowAttachmentResult.NotAttached(error ?? "Could not position SessionPad.");
            }

            _attachedTargetHwnd = target.Hwnd;
            _lastTargetBounds = targetBounds;
            _lastSessionWidth = sessionWidth;
            _lastSessionHeight = sessionHeight;
            _attachSide = side;

            return WindowAttachmentResult.Attached(side);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            ClearAttachedTarget();
            return WindowAttachmentResult.NotAttached($"Attach failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public WindowAttachmentResult IgnoreSessionPadWindow()
    {
        return WindowAttachmentResult.IgnoredSessionPadWindow(HasAttachedTarget, HasAttachedTarget ? _attachSide : null);
    }

    public WindowAttachmentResult UpdateAttachedWindowPosition(IntPtr sessionPadHwnd)
    {
        try
        {
            if (_attachedTargetHwnd == IntPtr.Zero)
            {
                return WindowAttachmentResult.NotAttached("No attached target.");
            }

            if (sessionPadHwnd == IntPtr.Zero)
            {
                return Detach("SessionPad window handle is not available.");
            }

            if (_attachedTargetHwnd == sessionPadHwnd)
            {
                return Detach("No external target detected.");
            }

            if (IsCurrentProcessWindow(_attachedTargetHwnd))
            {
                return Detach("Attached target is a SessionPad-owned window.");
            }

            if (!User32.IsWindow(_attachedTargetHwnd))
            {
                return Detach("Attached target is no longer valid.");
            }

            if (User32.IsIconic(_attachedTargetHwnd))
            {
                return WindowAttachmentResult.TargetMinimized(_attachSide);
            }

            if (!User32.IsWindowVisible(_attachedTargetHwnd))
            {
                return Detach("Attached target is not visible.");
            }

            if (!TryReadTargetBounds(_attachedTargetHwnd, out var targetBounds, out var error))
            {
                return WindowAttachmentResult.FollowWarning(
                    _attachSide,
                    error ?? "Could not read attached target bounds.");
            }

            if (!TryReadSessionSize(sessionPadHwnd, out var sessionWidth, out var sessionHeight, out error))
            {
                return WindowAttachmentResult.FollowWarning(
                    _attachSide,
                    error ?? "Could not read SessionPad bounds.");
            }

            if (targetBounds == _lastTargetBounds
                && sessionWidth == _lastSessionWidth
                && sessionHeight == _lastSessionHeight)
            {
                return WindowAttachmentResult.Following(_attachSide, "Target unchanged");
            }

            if (!TryPositionSessionPad(
                    sessionPadHwnd,
                    _attachedTargetHwnd,
                    targetBounds,
                    sessionWidth,
                    sessionHeight,
                    out var side,
                    out error))
            {
                return WindowAttachmentResult.FollowWarning(
                    _attachSide,
                    error ?? "Could not update SessionPad position.");
            }

            _lastTargetBounds = targetBounds;
            _lastSessionWidth = sessionWidth;
            _lastSessionHeight = sessionHeight;
            _attachSide = side;

            return WindowAttachmentResult.Following(
                side,
                $"Updated position beside target ({targetBounds.Width}x{targetBounds.Height})");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return WindowAttachmentResult.FollowWarning(
                _attachSide,
                $"Follow update failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public WindowAttachmentResult Detach(string reason)
    {
        ClearAttachedTarget();
        return WindowAttachmentResult.NotAttached(reason);
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

        if (IsSessionPadTarget(sessionPadHwnd, target))
        {
            return "Ignored SessionPad window.";
        }

        if (!User32.IsWindow(target.Hwnd))
        {
            return "Target window is no longer valid.";
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

    private static bool IsSessionPadTarget(IntPtr sessionPadHwnd, DetectedWindowInfo target)
    {
        return target.Hwnd != IntPtr.Zero
            && (target.Hwnd == sessionPadHwnd
                || target.IsCurrentProcessWindow
                || IsSessionPadProcess(target.ProcessName)
                || IsCurrentProcessWindow(target.Hwnd));
    }

    private static bool IsCurrentProcessWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            User32.GetWindowThreadProcessId(hwnd, out var processId);
            return processId != 0 && processId == (uint)Environment.ProcessId;
        }
        catch (Exception ex) when (ex is InvalidOperationException or OverflowException)
        {
            Debug.WriteLine($"SessionPad could not verify target process id: {ex.Message}");
            return false;
        }
    }

    private static bool IsSessionPadProcess(string processName)
    {
        var normalized = processName.Trim();
        if (normalized.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^4];
        }

        return string.Equals(normalized, "SessionPad.App", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryReadSessionSize(
        IntPtr sessionPadHwnd,
        out int sessionWidth,
        out int sessionHeight,
        out string? error)
    {
        sessionWidth = 0;
        sessionHeight = 0;
        error = null;

        if (!User32.GetWindowRect(sessionPadHwnd, out var sessionRect))
        {
            error = $"Could not read SessionPad bounds. Win32 error: {Marshal.GetLastWin32Error()}";
            return false;
        }

        sessionWidth = sessionRect.Right - sessionRect.Left;
        sessionHeight = sessionRect.Bottom - sessionRect.Top;
        if (sessionWidth <= 0 || sessionHeight <= 0)
        {
            error = "SessionPad bounds are not usable.";
            return false;
        }

        return true;
    }

    private static bool TryReadTargetBounds(IntPtr targetHwnd, out WindowBounds bounds, out string? error)
    {
        bounds = default;
        error = null;

        if (!User32.GetWindowRect(targetHwnd, out var targetRect))
        {
            error = $"Could not read target bounds. Win32 error: {Marshal.GetLastWin32Error()}";
            return false;
        }

        bounds = new WindowBounds(targetRect.Left, targetRect.Top, targetRect.Right, targetRect.Bottom);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            error = "Target window bounds are not usable.";
            return false;
        }

        return true;
    }

    private static bool TryPositionSessionPad(
        IntPtr sessionPadHwnd,
        IntPtr targetHwnd,
        WindowBounds targetBounds,
        int sessionWidth,
        int sessionHeight,
        out string side,
        out string? error)
    {
        error = null;

        WindowBounds? workAreaBounds = null;
        if (TryGetWorkArea(targetHwnd, out var workArea))
        {
            workAreaBounds = new WindowBounds(
                workArea.Left,
                workArea.Top,
                workArea.Right,
                workArea.Bottom);
        }

        var placement = WindowPlacementCalculator.Calculate(
            targetBounds,
            sessionWidth,
            sessionHeight,
            workAreaBounds,
            Gap);
        side = placement.Side;

        if (User32.SetWindowPos(
                sessionPadHwnd,
                IntPtr.Zero,
                placement.X,
                placement.Y,
                0,
                0,
                User32.SwpNoSize | User32.SwpNoZOrder | User32.SwpNoActivate))
        {
            return true;
        }

        error = $"Could not position SessionPad. Win32 error: {Marshal.GetLastWin32Error()}";
        return false;
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

    private void ClearAttachedTarget()
    {
        _attachedTargetHwnd = IntPtr.Zero;
        _lastTargetBounds = default;
        _lastSessionWidth = 0;
        _lastSessionHeight = 0;
        _attachSide = "Right";
    }
}
