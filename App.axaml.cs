// App.axaml.cs
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using STACK_HUB.Services;
using STACK_HUB.ViewModels;
using STACK_HUB.Views;

namespace STACK_HUB;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // 1. Pre-warm TextMate themes & grammars
        TextMateService.Prewarm();

        // 2. Pre-warm Editor Pools
        EditorPool<CasTextEditor>.EagerPrewarm();
        EditorPool<MaximaEditor>.EagerPrewarm();
        EditorPool<PrtEditorView>.EagerPrewarm();

        // 🚀 3. Force AvaloniaEdit assembly DLL & static constructors to load at app launch!
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _ = AvaloniaEdit.TextEditor.FontFamilyProperty;
            _ = new AvaloniaEdit.Document.TextDocument();
        }, DispatcherPriority.Background);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}