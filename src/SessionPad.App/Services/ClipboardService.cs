using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace SessionPad.App.Services;

public sealed record ClipboardCopyResult(bool Succeeded, string Message)
{
    public static ClipboardCopyResult Success()
    {
        return new ClipboardCopyResult(true, "Copied command.");
    }

    public static ClipboardCopyResult Failure(string message)
    {
        return new ClipboardCopyResult(false, message);
    }
}

public sealed class ClipboardService
{
    public ClipboardCopyResult CopyText(string text)
    {
        var trimmedText = text.Trim();
        if (trimmedText.Length == 0)
        {
            return ClipboardCopyResult.Failure("Nothing to copy.");
        }

        try
        {
            Clipboard.SetText(trimmedText);
            return ClipboardCopyResult.Success();
        }
        catch (Exception ex) when (ex is ExternalException or InvalidOperationException or ThreadStateException)
        {
            Debug.WriteLine($"SessionPad could not copy command text: {ex}");
            return ClipboardCopyResult.Failure($"Copy failed: {ex.Message}");
        }
    }
}
