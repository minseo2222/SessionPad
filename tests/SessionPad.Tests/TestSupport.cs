using System.IO;
using SessionPad.App.Models;
using SessionPad.App.Services;

namespace SessionPad.Tests;

/// <summary>Deterministic clock; each read advances by an optional fixed step.</summary>
public sealed class FakeClock : IClock
{
    private DateTimeOffset _now;
    private readonly TimeSpan _step;

    public FakeClock(DateTimeOffset start, TimeSpan? step = null)
    {
        _now = start;
        _step = step ?? TimeSpan.Zero;
    }

    public DateTimeOffset UtcNow
    {
        get
        {
            var value = _now;
            _now += _step;
            return value;
        }
    }
}

/// <summary>Unique temp directory, deleted on dispose.</summary>
public sealed class TempDir : IDisposable
{
    public string Path { get; }

    public TempDir()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "SessionPadTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

internal static class Win
{
    public static DetectedWindowInfo Make(string processName, string title, string? windowClass = null)
    {
        return new DetectedWindowInfo
        {
            Hwnd = new IntPtr(1234),
            HwndHex = "0x00000000000004D2",
            ProcessName = processName,
            Title = title,
            ProcessId = 4321,
            Left = 0,
            Top = 0,
            Right = 100,
            Bottom = 100,
            IsMinimized = false,
            IsVisible = true,
            IsSessionPadWindow = false,
            WindowClass = windowClass
        };
    }
}
