namespace SessionPad.App.Models;

public sealed record DetectedWindowInfo
{
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

    public string? WindowClass { get; init; }

    public string? Error { get; init; }

    public string BoundsText => $"{Left}, {Top}, {Right}, {Bottom} ({Width}x{Height})";
}
