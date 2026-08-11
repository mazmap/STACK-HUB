using Avalonia.Threading;
using STACK_HUB.Views;

namespace STACK_HUB.Services;

public static class CasTextEditorPool
{
    private static CasTextEditor? _prewarmedInstance;

    /// <summary>
    /// Pre-creates 1 single editor instance in the background at app startup.
    /// </summary>
    public static void Prewarm()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _prewarmedInstance ??= new CasTextEditor();
        }, DispatcherPriority.Background);
    }

    /// <summary>
    /// Returns the pre-created instance in 0ms, and prepares 1 fresh instance for the next new file.
    /// </summary>
    public static CasTextEditor GetOrCreate()
    {
        var editor = _prewarmedInstance ?? new CasTextEditor();
        _prewarmedInstance = null; // Clear reference
        Prewarm(); // Prepare 1 fresh instance in background for the next new tab
        return editor;
    }
}