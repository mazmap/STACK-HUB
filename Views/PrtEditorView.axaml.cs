using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using STACK_HUB.Models;
using STACK_HUB.ViewModels;

namespace STACK_HUB.Views;

public partial class PrtEditorView : UserControl
{
    private Point _dragStartPoint;
    private bool _isDraggingNode;
    private PrtGraphNodeViewModel? _draggedNode;

    private bool _isDraggingWire;
    private PrtGraphNodeViewModel? _wireSourceNode;
    private string _wireBranchType = "True";
    private PrtGraphWireViewModel? _tempWire;

    public PrtEditorView()
    {
        InitializeComponent();
    }

    private void OnNodeCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_isDraggingWire) return;

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
        else if (_isDraggingWire)
        {
            OnCanvasPointerMoved(sender, e);
        }
    }

    private void OnNodeCardPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDraggingWire)
        {
            OnCanvasPointerReleased(sender, e);
            return;
        }

        _isDraggingNode = false;
        _draggedNode = null;
        e.Pointer.Capture(null);
    }

    private void OnPortPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsLeftButtonPressed && sender is Control control && control.Tag is string branchType)
        {
            if (control.DataContext is PrtGraphNodeViewModel gNode && DataContext is PrtEditorViewModel vm)
            {
                _isDraggingWire = true;
                _wireSourceNode = gNode;
                _wireBranchType = branchType;

                Point startPort = branchType == "True" ? gNode.TruePortLocation : gNode.FalsePortLocation;
                Point currentPos = e.GetPosition(GraphCanvas);

                var brush = branchType == "True" ? SolidColorBrush.Parse("#4EC9B0") : SolidColorBrush.Parse("#F92672");
                _tempWire = new PrtGraphWireViewModel(startPort, currentPos, brush, branchType);

                vm.GraphWires.Add(_tempWire);
                e.Pointer.Capture(GraphCanvas);
                e.Handled = true;
            }
        }
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDraggingWire && _tempWire != null && _wireSourceNode != null)
        {
            Point startPort = _wireBranchType == "True" ? _wireSourceNode.TruePortLocation : _wireSourceNode.FalsePortLocation;
            Point currentPos = e.GetPosition(GraphCanvas);
            _tempWire.UpdatePath(startPort, currentPos);
        }
    }

    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDraggingWire && _wireSourceNode != null && DataContext is PrtEditorViewModel vm)
        {
            Point releasePos = e.GetPosition(GraphCanvas);

            PrtGraphNodeViewModel? targetGNode = vm.GraphNodes.FirstOrDefault(gn =>
                releasePos.X >= gn.X - 20 && releasePos.X <= gn.X + gn.Width + 20 &&
                releasePos.Y >= gn.Y - 20 && releasePos.Y <= gn.Y + gn.Height + 20 &&
                gn.Node.Id != _wireSourceNode.Node.Id);

            vm.ConnectBranch(_wireSourceNode.Node, _wireBranchType, targetGNode?.Node);

            _isDraggingWire = false;
            _wireSourceNode = null;
            _tempWire = null;
            e.Pointer.Capture(null);
        }
    }
}
