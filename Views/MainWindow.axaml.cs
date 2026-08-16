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

        // Pre-warm Input Editor layout & styling pipeline at launch
        if (mainVm.ActiveQuestion.Inputs.Any())
        {
            PrewarmInputEditorView.DataContext = mainVm.GetInputEditorViewModel(mainVm.ActiveQuestion.Inputs.First());
        }
        else
        {
            PrewarmInputEditorView.DataContext = new InputEditorViewModel(new StackInput());
        }

        // Pre-warm General Settings layout & styling pipeline at launch
        PrewarmGeneralSettingsView.DataContext = mainVm.GetGeneralSettingsViewModel();

        // Pre-warm Maxima Settings layout & styling pipeline at launch
        PrewarmMaximaSettingsView.DataContext = mainVm.GetMaximaSettingsViewModel();

        // Pre-warm Feedback Settings layout & styling pipeline at launch
        PrewarmFeedbackSettingsView.DataContext = mainVm.GetFeedbackSettingsViewModel();

        mainVm.RequestOpenFilePickerAsync = OpenQuestionFileDialogAsync;

        // On macOS, the system top menu bar is used; hide the duplicate in-window menu bar
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
        {
            var inWindowMenuBar = this.FindControl<Border>("InWindowMenuBar");
            if (inWindowMenuBar != null)
            {
                inWindowMenuBar.IsVisible = false;
            }
        }
    }

    private async void OnOpenQuestionMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await OpenQuestionFileDialogAsync();
    }

    private void OnExitMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private async void OnNativeOpenQuestionMenuClicked(object? sender, System.EventArgs e)
    {
        await OpenQuestionFileDialogAsync();
    }

    private void OnNativeExitMenuClicked(object? sender, System.EventArgs e)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    public async System.Threading.Tasks.Task OpenQuestionFileDialogAsync()
    {
        try
        {
            var storageProvider = this.StorageProvider;
            if (storageProvider == null)
            {
                System.Console.WriteLine("[OpenFilePicker] StorageProvider is null!");
                return;
            }

            var files = await storageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Open Moodle STACK Question XML",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("XML Files (*.xml)")
                    {
                        Patterns = new[] { "*.xml" }
                    },
                    new Avalonia.Platform.Storage.FilePickerFileType("All Files (*.*)")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            if (files != null && files.Count > 0)
            {
                await using var stream = await files[0].OpenReadAsync();
                using var reader = new System.IO.StreamReader(stream);
                string xmlContent = await reader.ReadToEndAsync();

                if (DataContext is MainViewModel mainVm)
                {
                    mainVm.LoadQuestionXml(xmlContent);
                }
            }
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"[OpenQuestionFileDialogAsync ERROR]: {ex}");
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