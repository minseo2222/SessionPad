using System.IO;
using Microsoft.Win32;

namespace SessionPad.App.Services;

public sealed class StartupService
{
    public const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public const string ValueName = "SessionPad";

    public bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(ValueName) is not null;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException
            or System.Security.SecurityException
            or IOException
            or ObjectDisposedException)
        {
            return false;
        }
    }

    public bool Enable(out string? error)
    {
        error = null;

        try
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exePath))
            {
                error = "Could not determine the SessionPad executable path.";
                return false;
            }

            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            if (key is null)
            {
                error = "Could not open the current-user Windows startup registry key.";
                return false;
            }

            key.SetValue(ValueName, $"\"{exePath}\" --silent", RegistryValueKind.String);
            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException
            or System.Security.SecurityException
            or IOException
            or ObjectDisposedException)
        {
            error = ex.Message;
            return false;
        }
    }

    public bool Disable(out string? error)
    {
        error = null;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            key?.DeleteValue(ValueName, throwOnMissingValue: false);
            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException
            or System.Security.SecurityException
            or IOException
            or ObjectDisposedException)
        {
            error = ex.Message;
            return false;
        }
    }
}
