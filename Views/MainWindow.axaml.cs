using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using STACK_HUB.Models;
using STACK_HUB.ViewModels;

namespace STACK_HUB.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var mainVm = new MainViewModel();
        DataContext = mainVm;

        mainVm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsAddDialogOpen) && mainVm.IsAddDialogOpen)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    var textBox = this.FindControl<TextBox>("AddDialogNameTextBox");
                    if (textBox != null)
                    {
                        textBox.Focus();
                        textBox.SelectAll();
                    }
                }, DispatcherPriority.Loaded);
            }
        };

        // Pre-warm non-empty text document layout & line tree at launch
        PrewarmCasTextEditor.DataContext = new CasTextEditorViewModel("<p>Prewarm text</p>");
        PrewarmMaximaEditor.DataContext = new MaximaEditorViewModel("ta1: ?;");

        // Pre-warm PRT graph layout, data templates, and styling pipeline at launch
        if (mainVm.ActiveQuestion.Prts.Any())
        {
            PrewarmPrtEditorView.DataContext = mainVm.GetPrtEditorViewModel(mainVm.ActiveQuestion.Prts.First());
        }
        else
        {
            PrewarmPrtEditorView.DataContext = new PrtEditorViewModel(new StackPrt());
        }
    }

    private void OnDialogBackdropPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.CancelAddDialog();
        }
    }

    private void OnDialogCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
    }
}