using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SessionPad.App.Views;

public partial class DockedTabView : System.Windows.Controls.UserControl
{
    public DockedTabView()
    {
        InitializeComponent();
    }

    private void OnTabMoveMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // The chromeless window has no caption bar; dragging the tab's padding ring
        // moves the window. The inner Expand button swallows its own clicks.
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
}
