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
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 1. Pre-warm TextMate themes & grammars in background
            TextMateService.Prewarm();

            // 2. Pre-warm AvaloniaEdit Control Templates & JIT compilation
            // Runs on background UI priority while the MainWindow opens
            Dispatcher.UIThread.Post(() =>
            {
                _ = new CasTextEditor(); // Forces Avalonia to parse templates & JIT compile code
            }, DispatcherPriority.Background);

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}