using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SessionPad.App.Views;

public partial class CompactNoteView : UserControl
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
}
