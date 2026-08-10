using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using STACK_HUB.ViewModels;

namespace STACK_HUB.Views;

public partial class DockingWorkspace : UserControl
{
    public DockingWorkspace()
    {
        InitializeComponent();
    }
    
    public void ClearOverlay()
    {
        DragOverlayCanvas.Children.Clear();
    }

    public void RemoveFromCanvasOverlay(Control control)
    {
        DragOverlayCanvas.Children.Remove(control);
    }
}