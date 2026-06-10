using System.Runtime.InteropServices;

namespace SessionPad.App.Native;

internal static class DwmApi
{
    public const int DwmwaUseImmersiveDarkMode = 20;

    public const int DwmwaWindowCornerPreference = 33;

    public const int DwmwaSystemBackdropType = 38;

    /// <summary>DWM_WINDOW_CORNER_PREFERENCE: round corners (Windows 11).</summary>
    public const int DwmwcpRound = 2;

    [DllImport("dwmapi.dll", SetLastError = false)]
    public static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attribute,
        ref int value,
        int size);
}
