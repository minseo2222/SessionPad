using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using SessionPad.App.Services;

namespace SessionPad.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private const string SingleInstanceMutexName = "SessionPad.SingleInstance";
    private const string ActivationEventName = "SessionPad.Activate";

    private Mutex? _singleInstanceMutex;
    private EventWaitHandle? _activationEvent;
    private Thread? _activationThread;
    private volatile bool _isShuttingDown;

    protected override void OnStartup(StartupEventArgs e)
    {
        var shouldStartActivationListener = false;

        try
        {
            _singleInstanceMutex = new Mutex(
                initiallyOwned: true,
                name: SingleInstanceMutexName,
                createdNew: out var isFirstInstance);

            if (!isFirstInstance)
            {
                SignalExistingInstance();
                _singleInstanceMutex.Dispose();
                _singleInstanceMutex = null;
                Shutdown();
                return;
            }

            _activationEvent = new EventWaitHandle(
                initialState: false,
                mode: EventResetMode.AutoReset,
                name: ActivationEventName);
            shouldStartActivationListener = true;
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or WaitHandleCannotBeOpenedException
            or System.Security.SecurityException)
        {
            Debug.WriteLine($"SessionPad single-instance setup failed: {ex}");
        }

        base.OnStartup(e);

        var settingsService = new SettingsService();
        var themeService = new ThemeService();
        themeService.ApplyTheme(settingsService.LoadTheme());

        var startSilent = e.Args.Any(argument =>
            string.Equals(argument, "--silent", StringComparison.OrdinalIgnoreCase));
        var mainWindow = new MainWindow();
        MainWindow = mainWindow;

        if (startSilent)
        {
            new WindowInteropHelper(mainWindow).EnsureHandle();
        }
        else
        {
            mainWindow.Show();
        }

        if (shouldStartActivationListener)
        {
            StartActivationListener();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _isShuttingDown = true;

        try
        {
            _activationEvent?.Set();
        }
        catch (Exception ex) when (ex is ObjectDisposedException or IOException)
        {
            Debug.WriteLine($"SessionPad could not signal activation listener shutdown: {ex.Message}");
        }

        try
        {
            _activationEvent?.Dispose();
        }
        catch (Exception ex) when (ex is ObjectDisposedException)
        {
            Debug.WriteLine($"SessionPad could not dispose activation event: {ex.Message}");
        }

        try
        {
            _singleInstanceMutex?.ReleaseMutex();
        }
        catch (Exception ex) when (ex is ApplicationException or ObjectDisposedException)
        {
            Debug.WriteLine($"SessionPad could not release single-instance mutex: {ex.Message}");
        }

        try
        {
            _singleInstanceMutex?.Dispose();
        }
        catch (Exception ex) when (ex is ObjectDisposedException)
        {
            Debug.WriteLine($"SessionPad could not dispose single-instance mutex: {ex.Message}");
        }

        _activationEvent = null;
        _singleInstanceMutex = null;

        base.OnExit(e);
    }

    private static void SignalExistingInstance()
    {
        try
        {
            using var activationEvent = new EventWaitHandle(
                initialState: false,
                mode: EventResetMode.AutoReset,
                name: ActivationEventName);
            activationEvent.Set();
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or WaitHandleCannotBeOpenedException
            or System.Security.SecurityException)
        {
            Debug.WriteLine($"SessionPad could not signal the existing instance: {ex.Message}");
        }
    }

    private void StartActivationListener()
    {
        if (_activationEvent is null)
        {
            return;
        }

        _activationThread = new Thread(WaitForActivationRequests)
        {
            IsBackground = true,
            Name = "SessionPad activation listener"
        };
        _activationThread.Start();
    }

    private void WaitForActivationRequests()
    {
        while (!_isShuttingDown)
        {
            try
            {
                _activationEvent?.WaitOne();
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException)
            {
                Debug.WriteLine($"SessionPad activation listener failed: {ex.Message}");
                return;
            }

            if (_isShuttingDown)
            {
                return;
            }

            try
            {
                Dispatcher.BeginInvoke(ShowAndActivateMainWindow);
            }
            catch (Exception ex) when (ex is InvalidOperationException)
            {
                Debug.WriteLine($"SessionPad could not dispatch activation request: {ex.Message}");
                return;
            }
        }
    }

    private void ShowAndActivateMainWindow()
    {
        if (MainWindow is MainWindow sessionPadWindow)
        {
            sessionPadWindow.ShowAndActivateFromExternalRequest();
            return;
        }

        if (MainWindow is null)
        {
            return;
        }

        if (!MainWindow.IsVisible)
        {
            MainWindow.Show();
        }

        if (MainWindow.WindowState == System.Windows.WindowState.Minimized)
        {
            MainWindow.WindowState = System.Windows.WindowState.Normal;
        }

        MainWindow.Activate();
    }
}
