using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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

    private bool _isPanningCanvas;
    private Point _panStartPoint;
    private double _initialPanX;
    private double _initialPanY;

    private bool _isResizingRightPane;
    private bool _isResizingBottomPane;
    private Point _resizeStartPoint;
    private double _initialPaneSize;

    private bool _hasInitiallyCentered;

    public PrtEditorView()
    {
        InitializeComponent();
        Loaded += OnViewLoaded;
    }

    private void OnViewLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PrtEditorViewModel vm && ViewportBorder.Bounds.Width > 0 && ViewportBorder.Bounds.Height > 0)
        {
            vm.CenterView(ViewportBorder.Bounds.Size);
        }
    }

    private void OnViewportSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (!_hasInitiallyCentered && e.NewSize.Width > 0 && e.NewSize.Height > 0)
        {
            _hasInitiallyCentered = true;
            if (DataContext is PrtEditorViewModel vm)
            {
                vm.CenterView(e.NewSize);
            }
        }
    }

    private void OnCenterViewButtonClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PrtEditorViewModel vm)
        {
            vm.CenterView(ViewportBorder.Bounds.Size);
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
    }

    private void OnNodeCardSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (sender is Control control && control.DataContext is PrtGraphNodeViewModel gNode && DataContext is PrtEditorViewModel vm)
        {
            if (e.NewSize.Width > 0 && e.NewSize.Height > 0)
            {
                if (Math.Abs(gNode.Width - e.NewSize.Width) > 0.5 || Math.Abs(gNode.Height - e.NewSize.Height) > 0.5)
                {
                    gNode.Width = e.NewSize.Width;
                    gNode.Height = e.NewSize.Height;
                    gNode.NotifyPositionChanged();
                    vm.UpdateWires();
                }
            }
        }
    }

    private bool _isBoxSelecting;
    private Point _boxSelectStartPos;

    private void OnNodeCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_isDraggingWire) return;

        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsLeftButtonPressed && sender is Control control && control.DataContext is PrtGraphNodeViewModel gNode)
        {
            if (DataContext is PrtEditorViewModel vm)
            {
                bool isMultiSelect = e.KeyModifiers.HasFlag(KeyModifiers.Shift) || e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);

                if (isMultiSelect)
                {
                    vm.SelectNodeViewModel(gNode, isMultiSelect: true);
                }
                else if (!gNode.IsSelected || !vm.SelectedGraphNodes.Contains(gNode))
                {
                    vm.SelectNodeViewModel(gNode, isMultiSelect: false);
                }

                _draggedNode = gNode;
                _dragStartPoint = e.GetPosition(ViewportBorder);
                _isDraggingNode = true;
                e.Pointer.Capture(control);
                e.Handled = true;
            }
        }
    }

    private void OnNodeCardPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDraggingNode && _draggedNode != null && DataContext is PrtEditorViewModel vm)
        {
            var currentPoint = e.GetPosition(ViewportBorder);
            var delta = (currentPoint - _dragStartPoint) / vm.ZoomLevel;
            _dragStartPoint = currentPoint;

            var nodesToMove = (vm.SelectedGraphNodes.Contains(_draggedNode) && vm.SelectedGraphNodes.Count > 1)
                ? vm.SelectedGraphNodes.ToList()
                : new List<PrtGraphNodeViewModel> { _draggedNode };
            foreach (var node in nodesToMove)
            {
                node.X = Math.Clamp(node.X + delta.X, 10, 9770);
                node.Y = Math.Clamp(node.Y + delta.Y, 10, 9880);
                node.NotifyPositionChanged();
                vm.SaveNodePosition(node.Node.Id, node.X, node.Y);
            }

            vm.UpdateWires();
            e.Handled = true;
        }
        else if (_isDraggingWire)
        {
            OnViewportPointerMoved(sender, e);
        }
    }

    private void OnNodeCardPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDraggingWire)
        {
            OnViewportPointerReleased(sender, e);
            return;
        }

        _isDraggingNode = false;
        _draggedNode = null;
        e.Pointer.Capture(null);
    }

    private void OnTruePortPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        StartPortDrag(sender, e, "True");
    }

    private void OnFalsePortPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        StartPortDrag(sender, e, "False");
    }

    private void StartPortDrag(object? sender, PointerPressedEventArgs e, string branchType)
    {
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsLeftButtonPressed && sender is Control control && control.DataContext is PrtGraphNodeViewModel gNode && DataContext is PrtEditorViewModel vm)
        {
            _isDraggingWire = true;
            _wireSourceNode = gNode;
            _wireBranchType = branchType;

            // Remove any existing static wire from this branch while dragging
            var existingWires = vm.GraphWires.Where(w => w.SourceNodeId == gNode.Node.Id && w.BranchType == branchType).ToList();
            foreach (var existing in existingWires)
            {
                vm.GraphWires.Remove(existing);
            }

            Point startPort = branchType == "True" ? gNode.TruePortLocation : gNode.FalsePortLocation;
            Point currentPos = e.GetPosition(CanvasContainer);

            var brush = branchType == "True" ? SolidColorBrush.Parse("#4EC9B0") : SolidColorBrush.Parse("#F92672");
            string scoreText = branchType == "True"
                ? PrtEditorViewModel.FormatBranchScore(gNode.Node.ScoreTrue, gNode.Node.ScoreModeTrue)
                : PrtEditorViewModel.FormatBranchScore(gNode.Node.ScoreFalse, gNode.Node.ScoreModeFalse);

            _tempWire = new PrtGraphWireViewModel(startPort, currentPos, brush, branchType, scoreText)
            {
                SourceNodeId = gNode.Node.Id,
                BranchType = branchType
            };

            vm.GraphWires.Add(_tempWire);
            e.Pointer.Capture(ViewportBorder);
            e.Handled = true;
        }
    }

    private Point _lastRightClickScenePos = new Point(5000, 5000);

    private void OnViewportPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_isDraggingWire || _isDraggingNode || _isResizingRightPane || _isResizingBottomPane) return;

        var point = e.GetCurrentPoint(ViewportBorder);
        if (point.Properties.IsRightButtonPressed && DataContext is PrtEditorViewModel vmRight)
        {
            Point mousePos = e.GetPosition(ViewportBorder);
            double sceneX = (mousePos.X - vmRight.PanX) / vmRight.ZoomLevel;
            double sceneY = (mousePos.Y - vmRight.PanY) / vmRight.ZoomLevel;
            _lastRightClickScenePos = new Point(sceneX, sceneY);
        }
        else if (point.Properties.IsMiddleButtonPressed || (point.Properties.IsLeftButtonPressed && e.KeyModifiers.HasFlag(KeyModifiers.Alt)))
        {
            // Alt + Left Drag OR Middle Click Drag = Canvas Panning
            if (DataContext is PrtEditorViewModel vmPan)
            {
                _isPanningCanvas = true;
                _panStartPoint = e.GetPosition(ViewportBorder);
                _initialPanX = vmPan.PanX;
                _initialPanY = vmPan.PanY;
                e.Pointer.Capture(ViewportBorder);
                e.Handled = true;
            }
        }
        else if (point.Properties.IsLeftButtonPressed)
        {
            // Default Left Drag on background = Marquee Box Selection
            if (DataContext is PrtEditorViewModel vmBox)
            {
                bool isShiftPressed = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
                if (!isShiftPressed)
                {
                    vmBox.ClearSelection();
                }

                _isBoxSelecting = true;
                _boxSelectStartPos = e.GetPosition(CanvasContainer);
                SelectionMarquee.IsVisible = true;
                Canvas.SetLeft(SelectionMarquee, _boxSelectStartPos.X);
                Canvas.SetTop(SelectionMarquee, _boxSelectStartPos.Y);
                SelectionMarquee.Width = 0;
                SelectionMarquee.Height = 0;
                e.Pointer.Capture(ViewportBorder);
                e.Handled = true;
            }
        }
    }

    private void OnAddNodeAtCursorClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PrtEditorViewModel vm)
        {
            vm.AddNodeAtPosition(_lastRightClickScenePos.X, _lastRightClickScenePos.Y);
        }
    }

    private static double GetDistance(Point p1, Point p2)
    {
        double dx = p1.X - p2.X;
        double dy = p1.Y - p2.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private void OnViewportPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDraggingWire && _tempWire != null && _wireSourceNode != null && DataContext is PrtEditorViewModel vmWire)
        {
            Point startPort = _wireBranchType == "True" ? _wireSourceNode.TruePortLocation : _wireSourceNode.FalsePortLocation;
            Point currentPos = e.GetPosition(CanvasContainer);

            // Magnetic port snap check (within 55px radius of any target input port)
            PrtGraphNodeViewModel? snapTarget = vmWire.GraphNodes.FirstOrDefault(gn =>
                gn.Node.Id != _wireSourceNode.Node.Id &&
                GetDistance(currentPos, gn.InputPortLocation) <= 55.0);

            Point targetPos = snapTarget != null ? snapTarget.InputPortLocation : currentPos;
            _tempWire.UpdatePath(startPort, targetPos);
        }
        else if (_isBoxSelecting && DataContext is PrtEditorViewModel vmBox)
        {
            Point currentPos = e.GetPosition(CanvasContainer);
            double minX = Math.Min(_boxSelectStartPos.X, currentPos.X);
            double minY = Math.Min(_boxSelectStartPos.Y, currentPos.Y);
            double width = Math.Abs(currentPos.X - _boxSelectStartPos.X);
            double height = Math.Abs(currentPos.Y - _boxSelectStartPos.Y);

            Canvas.SetLeft(SelectionMarquee, minX);
            Canvas.SetTop(SelectionMarquee, minY);
            SelectionMarquee.Width = width;
            SelectionMarquee.Height = height;

            Rect boxRect = new Rect(minX, minY, width, height);

            foreach (var gNode in vmBox.GraphNodes)
            {
                Rect nodeRect = new Rect(gNode.X, gNode.Y, gNode.Width, gNode.Height);
                bool intersects = boxRect.Intersects(nodeRect);
                if (intersects && !gNode.IsSelected)
                {
                    gNode.IsSelected = true;
                    if (!vmBox.SelectedGraphNodes.Contains(gNode)) vmBox.SelectedGraphNodes.Add(gNode);
                }
                else if (!intersects && gNode.IsSelected)
                {
                    gNode.IsSelected = false;
                    vmBox.SelectedGraphNodes.Remove(gNode);
                }
            }
            vmBox.SelectedNode = null;
        }
        else if (_isPanningCanvas && DataContext is PrtEditorViewModel vm)
        {
            Point currentPos = e.GetPosition(ViewportBorder);
            Vector delta = currentPos - _panStartPoint;
            vm.PanX = _initialPanX + delta.X;
            vm.PanY = _initialPanY + delta.Y;
            vm.ClampPan(ViewportBorder.Bounds.Width, ViewportBorder.Bounds.Height);
        }
        else if (_isResizingRightPane && DataContext is PrtEditorViewModel vmRight)
        {
            Point currentPos = e.GetPosition(RootOverlayGrid);
            double deltaX = _resizeStartPoint.X - currentPos.X;
            double newWidth = Math.Clamp(_initialPaneSize + deltaX, 280, 600);
            vmRight.NodeEditorWidth = newWidth;
        }
        else if (_isResizingBottomPane && DataContext is PrtEditorViewModel vmBottom)
        {
            Point currentPos = e.GetPosition(RootOverlayGrid);
            double deltaY = _resizeStartPoint.Y - currentPos.Y;
            double newHeight = Math.Clamp(_initialPaneSize + deltaY, 140, 500);
            vmBottom.FeedbackVariablesHeight = newHeight;
        }
    }

    private void OnViewportPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDraggingWire && _wireSourceNode != null && DataContext is PrtEditorViewModel vm)
        {
            Point releasePos = e.GetPosition(CanvasContainer);

            PrtGraphNodeViewModel? targetGNode = vm.GraphNodes.FirstOrDefault(gn =>
                gn.Node.Id != _wireSourceNode.Node.Id &&
                GetDistance(releasePos, gn.InputPortLocation) <= 55.0)
                ?? vm.GraphNodes.FirstOrDefault(gn =>
                releasePos.X >= gn.X - 20 && releasePos.X <= gn.X + gn.Width + 20 &&
                releasePos.Y >= gn.Y - 20 && releasePos.Y <= gn.Y + gn.Height + 20 &&
                gn.Node.Id != _wireSourceNode.Node.Id);

            vm.ConnectBranch(_wireSourceNode.Node, _wireBranchType, targetGNode?.Node);

            _isDraggingWire = false;
            _wireSourceNode = null;
            _tempWire = null;
            e.Pointer.Capture(null);
        }
        else if (_isBoxSelecting && DataContext is PrtEditorViewModel vmRel)
        {
            Point endPos = e.GetPosition(CanvasContainer);
            double width = Math.Abs(endPos.X - _boxSelectStartPos.X);
            double height = Math.Abs(endPos.Y - _boxSelectStartPos.Y);

            // Single click tap on empty background (< 4px drag) clears selection if Shift is not held
            if (width < 4.0 && height < 4.0 && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                vmRel.ClearSelection();
            }

            _isBoxSelecting = false;
            SelectionMarquee.IsVisible = false;
            e.Pointer.Capture(null);
        }
        else if (_isPanningCanvas)
        {
            _isPanningCanvas = false;
            e.Pointer.Capture(null);
        }
        else if (_isResizingRightPane || _isResizingBottomPane)
        {
            _isResizingRightPane = false;
            _isResizingBottomPane = false;
            e.Pointer.Capture(null);
        }
    }

    private void OnViewportPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (DataContext is PrtEditorViewModel vm)
        {
            Point mousePos = e.GetPosition(ViewportBorder);
            double zoomFactor = e.Delta.Y > 0 ? 1.15 : 0.85;
            double oldZoom = vm.ZoomLevel;
            double newZoom = Math.Clamp(oldZoom * zoomFactor, 0.3, 3.0);

            if (Math.Abs(newZoom - oldZoom) > 0.001)
            {
                double scaleRatio = newZoom / oldZoom;
                vm.PanX = mousePos.X - (mousePos.X - vm.PanX) * scaleRatio;
                vm.PanY = mousePos.Y - (mousePos.Y - vm.PanY) * scaleRatio;
                vm.ZoomLevel = Math.Round(newZoom, 3);
                vm.ClampPan(ViewportBorder.Bounds.Width, ViewportBorder.Bounds.Height);
            }
            e.Handled = true;
        }
    }

    private void OnRightResizeHandlePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is PrtEditorViewModel vm)
        {
            _isResizingRightPane = true;
            _resizeStartPoint = e.GetPosition(RootOverlayGrid);
            _initialPaneSize = vm.NodeEditorWidth;
            e.Pointer.Capture(sender as Control);
            e.Handled = true;
        }
    }

    private void OnRightResizeHandlePointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isResizingRightPane) OnViewportPointerMoved(sender, e);
    }

    private void OnRightResizeHandlePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isResizingRightPane) OnViewportPointerReleased(sender, e);
    }

    private void OnBottomResizeHandlePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is PrtEditorViewModel vm)
        {
            _isResizingBottomPane = true;
            _resizeStartPoint = e.GetPosition(RootOverlayGrid);
            _initialPaneSize = vm.FeedbackVariablesHeight;
            e.Pointer.Capture(sender as Control);
            e.Handled = true;
        }
    }

    private void OnBottomResizeHandlePointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isResizingBottomPane) OnViewportPointerMoved(sender, e);
    }

    private void OnBottomResizeHandlePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isResizingBottomPane) OnViewportPointerReleased(sender, e);
    }
}
