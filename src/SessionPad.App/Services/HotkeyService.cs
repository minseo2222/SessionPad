using System.Runtime.InteropServices;
using SessionPad.App.Native;

namespace SessionPad.App.Services;

public sealed class HotkeyService
{
    public const int ShowSessionPadHotkeyId = 1;

    public int LastRegistrationError { get; private set; }

    public bool Register(IntPtr hwnd)
    {
        LastRegistrationError = 0;

        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        var registered = User32.RegisterHotKey(
            hwnd,
            ShowSessionPadHotkeyId,
            User32.ModControl | User32.ModAlt,
            User32.VirtualKeyN);

        if (!registered)
        {
            LastRegistrationError = Marshal.GetLastWin32Error();
        }

        return registered;
    }

    public void Unregister(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        User32.UnregisterHotKey(hwnd, ShowSessionPadHotkeyId);
    }
}
