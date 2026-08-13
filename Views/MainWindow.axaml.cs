using Avalonia.Controls;
using STACK_HUB.ViewModels;

namespace STACK_HUB.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        // Pre-warm non-empty text document layout & line tree at launch
        PrewarmCasTextEditor.DataContext = new CasTextEditorViewModel("<p>Prewarm text</p>");
        PrewarmMaximaEditor.DataContext = new MaximaEditorViewModel("ta1: ?;");
    }
}