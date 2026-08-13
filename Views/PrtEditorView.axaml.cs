using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using STACK_HUB.Models;
using STACK_HUB.ViewModels;

namespace STACK_HUB.Views;

public partial class PrtEditorView : UserControl
{
    private Point _dragStartPoint;
    private bool _isDraggingNode;
    private PrtGraphNodeViewModel? _draggedNode;

    public PrtEditorView()
    {
        InitializeComponent();
    }

    private void OnNodeCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsLeftButtonPressed && sender is Control control && control.DataContext is PrtGraphNodeViewModel gNode)
        {
            if (DataContext is PrtEditorViewModel vm)
            {
                vm.SelectedNode = gNode.Node;
                _draggedNode = gNode;
                _dragStartPoint = e.GetPosition(this);
                _isDraggingNode = true;
                e.Pointer.Capture(control);
            }
        }
    }

    private void OnNodeCardPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDraggingNode && _draggedNode != null && sender is Control control)
        {
            var currentPoint = e.GetPosition(this);
            var delta = currentPoint - _dragStartPoint;
            _dragStartPoint = currentPoint;

            _draggedNode.X += delta.X;
            _draggedNode.Y += delta.Y;
            _draggedNode.NotifyPositionChanged();

            if (DataContext is PrtEditorViewModel vm)
            {
                vm.UpdateWires();
            }
        }
    }

    private void OnNodeCardPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDraggingNode = false;
        _draggedNode = null;
        e.Pointer.Capture(null);
    }
}
