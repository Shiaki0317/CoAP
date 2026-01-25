using Avalonia.Controls;
using CoapDesktopSender.Core;

namespace CoapDesktopSender.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
