using System.Windows;
using SessionPad.App.ViewModels;

namespace SessionPad.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new FloatingNoteViewModel();
    }
}
