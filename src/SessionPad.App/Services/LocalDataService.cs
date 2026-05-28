using System.Diagnostics;
using System.IO;

namespace SessionPad.App.Services;

public sealed class LocalDataService
{
    private const string AppDirectoryName = "SessionPad";

    public string AppDataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppDirectoryName);

    public string GetAppDataDirectory()
    {
        return AppDataDirectory;
    }

    public void EnsureAppDataDirectory()
    {
        Directory.CreateDirectory(AppDataDirectory);
    }

    public void OpenAppDataDirectory()
    {
        EnsureAppDataDirectory();

        using var _ = Process.Start(new ProcessStartInfo
        {
            FileName = AppDataDirectory,
            UseShellExecute = true
        });
    }

    public bool DeleteAllLocalData(out string? error)
    {
        error = null;

        try
        {
            var targetDirectory = Path.GetFullPath(AppDataDirectory);
            var appDataRoot = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            var expectedDirectory = Path.GetFullPath(Path.Combine(appDataRoot, AppDirectoryName));

            if (!string.Equals(targetDirectory, expectedDirectory, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(new DirectoryInfo(targetDirectory).Name, AppDirectoryName, StringComparison.OrdinalIgnoreCase))
            {
                error = "The local data directory did not match the expected SessionPad app data path.";
                return false;
            }

            if (!Directory.Exists(targetDirectory))
            {
                return true;
            }

            Directory.Delete(targetDirectory, recursive: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            error = ex.Message;
            Debug.WriteLine($"SessionPad could not delete local data: {ex}");
            return false;
        }
    }
}
