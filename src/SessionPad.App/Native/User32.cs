using System.Runtime.InteropServices;

namespace SessionPad.App.Native;

internal static partial class User32
{
    public const int WmHotkey = 0x0312;

    public const uint ModAlt = 0x0001;

    public const uint ModControl = 0x0002;

    public const uint VirtualKeyN = 0x4E;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
