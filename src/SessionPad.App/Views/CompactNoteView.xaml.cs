using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SessionPad.App.Views;

public partial class CompactNoteView : System.Windows.Controls.UserControl
{
    public CompactNoteView()
    {
        InitializeComponent();
    }

    private void OnDragAttachMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            e.Handled = true;
            mainWindow.BeginDragAttach();
        }
    }

    private void OnHeaderMoveMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Plain window move (the chromeless window has no caption bar). The Drag handle
        // marks its event handled first, so attach-drag is unaffected.
        if (e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        try
        {
            Window.GetWindow(this)?.DragMove();
        }
        catch (InvalidOperationException)
        {
            // Mouse button already released; nothing to do.
        }
    }

    private void OnCloseButtonClick(object sender, RoutedEventArgs e)
    {
        // Same behavior as the old caption close button: MainWindow.OnClosing
        // intercepts Close() and hides to the tray.
        Window.GetWindow(this)?.Close();
    }
}
