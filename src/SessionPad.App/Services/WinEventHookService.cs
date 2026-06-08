using System.Diagnostics;
using SessionPad.App.Native;

namespace SessionPad.App.Services;

/// <summary>
/// Installs out-of-context WinEvent hooks so SessionPad can react to window
/// movement, minimize/restore, title changes, destruction, and foreground
/// changes without 60ms polling. Raises <see cref="WinEventReceived"/> on the
/// thread that called <see cref="Start"/> (the UI thread).
/// </summary>
public sealed class WinEventHookService
{
    private readonly User32.WinEventProc _callback;
    private readonly List<IntPtr> _hooks = new();
    private bool _isRunning;

    public WinEventHookService()
    {
        // Stored as a field so the delegate is not collected while hooks are live.
        _callback = OnWinEvent;
    }

    public event Action<uint, IntPtr>? WinEventReceived;

    public bool IsRunning => _isRunning;

    public bool Start()
    {
        if (_isRunning)
        {
            return true;
        }

        TryHook(User32.EventSystemForeground, User32.EventSystemForeground);
        TryHook(User32.EventSystemMinimizeStart, User32.EventSystemMinimizeEnd);
        TryHook(User32.EventObjectDestroy, User32.EventObjectDestroy);
        TryHook(User32.EventObjectLocationChange, User32.EventObjectNameChange);

        _isRunning = _hooks.Count > 0;
        return _isRunning;
    }

    public void Stop()
    {
        foreach (var hook in _hooks)
        {
            if (hook != IntPtr.Zero)
            {
                User32.UnhookWinEvent(hook);
            }
        }

        _hooks.Clear();
        _isRunning = false;
    }

    private void TryHook(uint eventMin, uint eventMax)
    {
        var hook = User32.SetWinEventHook(
            eventMin,
            eventMax,
            IntPtr.Zero,
            _callback,
            0,
            0,
            User32.WineventOutofcontext | User32.WineventSkipownprocess);

        if (hook != IntPtr.Zero)
        {
            _hooks.Add(hook);
            return;
        }

        Debug.WriteLine(
            $"SessionPad could not install WinEvent hook 0x{eventMin:X}-0x{eventMax:X}.");
    }

    private void OnWinEvent(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime)
    {
        // Only top-level window events; ignore caret, cursor, and child objects.
        if (hwnd == IntPtr.Zero || idObject != User32.ObjidWindow || idChild != 0)
        {
            return;
        }

        try
        {
            WinEventReceived?.Invoke(eventType, hwnd);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SessionPad WinEvent handler failed: {ex}");
        }
    }
}
