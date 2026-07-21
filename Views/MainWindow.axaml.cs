using Avalonia.Controls;
using STACK_HUB.ViewModels;

namespace STACK_HUB.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}