// Services/EditorPool.cs
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Threading;

namespace STACK_HUB.Services;

public static class EditorPool<TControl> where TControl : Control, new()
{
    private static TControl? _prewarmedInstance;

    /// <summary>
    /// Eagerly creates the initial instance at app launch before MainWindow is shown.
    /// </summary>
    public static void EagerPrewarm()
    {
        _prewarmedInstance ??= new TControl();
    }

    /// <summary>
    /// Asynchronously prepares 1 fresh instance in background for future new tabs.
    /// </summary>
    public static void PrewarmBackground()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _prewarmedInstance ??= new TControl();
        }, DispatcherPriority.Background);
    }

    /// <summary>
    /// Fetches the pre-created instance in 0ms, and prepares the next instance in background.
    /// </summary>
    public static TControl GetOrCreate()
    {
        var editor = _prewarmedInstance ?? new TControl();
        _prewarmedInstance = null; // Clear reference

        // Detach from previous parent if attached
        if (editor.Parent is ContentPresenter cp)
        {
            cp.Content = null;
        }
        else if (editor.Parent is Panel panel)
        {
            panel.Children.Remove(editor);
        }

        PrewarmBackground(); // Prepare next instance in background
        return editor;
    }
}