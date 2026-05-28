namespace SessionPad.App.Models;

public sealed record DetectedWindowInfo
{
    public IntPtr Hwnd { get; init; }

    public required string HwndHex { get; init; }

    public required string ProcessName { get; init; }

    public required string Title { get; init; }

    public required int ProcessId { get; init; }

    public required int Left { get; init; }

    public required int Top { get; init; }

    public required int Right { get; init; }

    public required int Bottom { get; init; }

    public int Width => Right - Left;

    public int Height => Bottom - Top;

    public bool IsMinimized { get; init; }

    public bool IsVisible { get; init; }

    public bool IsSessionPadWindow { get; init; }

    public bool IsCurrentProcessWindow =>
        IsSessionPadWindow
        || ProcessId == Environment.ProcessId
        || string.Equals(ProcessName, "SessionPad.App", StringComparison.OrdinalIgnoreCase);

    public string? WindowClass { get; init; }

    public string? Error { get; init; }

    public string BoundsText => $"{Left}, {Top}, {Right}, {Bottom} ({Width}x{Height})";

    public static DetectedWindowInfo FromError(string message, IntPtr hwnd = default)
    {
        return new DetectedWindowInfo
        {
            HwndHex = FormatHwnd(hwnd),
            Hwnd = hwnd,
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
            Error = message
        };
    }

    public static DetectedWindowInfo FromException(Exception exception, IntPtr hwnd = default)
    {
        return FromError($"Detection failed: {exception.GetType().Name}: {exception.Message}", hwnd);
    }

    private static string FormatHwnd(IntPtr hwnd)
    {
        return $"0x{hwnd.ToInt64():X16}";
    }
}
