using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using STACK_HUB.Docking.Models;
using STACK_HUB.Docking.Services;
using STACK_HUB.ViewModels;

namespace STACK_HUB.Docking.Views;

public partial class TabGroupControl : UserControl
{
    private Point _dragStartPoint;
    private bool _isDragging;
    private PaneNode? _draggedPane;

    public TabGroupControl()
    {
        InitializeComponent();
    }

    private void OnTabPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsLeftButtonPressed && sender is Control control && control.DataContext is PaneNode pane)
        {
            _dragStartPoint = e.GetPosition(this);
            _draggedPane = pane;
            _isDragging = false;
        }
    }

    private void OnTabPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggedPane == null) return;

        var currentPoint = e.GetPosition(this);
        var diff = _dragStartPoint - currentPoint;

        // Detect if pointer moved more than 6 pixels to begin drag
        if (!_isDragging && (Math.Abs(diff.X) > 6 || Math.Abs(diff.Y) > 6))
        {
            _isDragging = true;
            e.Pointer.Capture(sender as IInputElement);
        }

        if (_isDragging)
        {
            // Find target TabGroupControl under pointer
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var screenPoint = e.GetPosition(topLevel);
            
            // Highlight target tab group under pointer
            UpdateDockOverlay(topLevel, screenPoint);
        }
    }

    private void OnTabPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging && _draggedPane != null)
        {
            e.Pointer.Capture(null);

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                var dropPoint = e.GetPosition(topLevel);
                ExecuteDrop(topLevel, dropPoint, _draggedPane, e);

                // Clear visual preview upon drop
                var canvas = topLevel.FindControl<Canvas>("DragOverlayCanvas");
                canvas?.Children.Clear();
            }
        }

        _isDragging = false;
        _draggedPane = null;
    }
    
    private void UpdateDockOverlay(TopLevel topLevel, Point pointerPosition)
    {
        var hitControl = topLevel.InputHitTest(pointerPosition) as Visual;
        var targetTabGroupControl = hitControl?.FindAncestorOfType<TabGroupControl>();

        var canvas = topLevel.FindControl<Canvas>("DragOverlayCanvas");
        if (canvas == null) return;

        if (targetTabGroupControl != null && targetTabGroupControl.DataContext is TabGroupNode)
        {
            var relativePos = pointerPosition - targetTabGroupControl.TranslatePoint(new Point(0, 0), topLevel)!.Value;
            var dockPos = CalculateDockPosition(targetTabGroupControl.Bounds, relativePos);

            // Get matrix transformation from target control relative to MainWindow (TopLevel)
            var transform = targetTabGroupControl.TransformToVisual(canvas);
            if (transform.HasValue)
            {
                // 1. Transform top-left corner (0,0) to topLevel coordinates
                Point topLeftOnTopLevel = transform.Value.Transform(new Point(0, 0));

                // 2. Construct the bounds Rect using the transformed origin and control size
                var targetBoundsOnTopLevel = new Rect(
                    topLeftOnTopLevel, 
                    targetTabGroupControl.Bounds.Size
                );

                DrawDockPreview(canvas, targetBoundsOnTopLevel, dockPos);
                return;
            }
        }

        canvas.Children.Clear();
    }

private void DrawDockPreview(Canvas canvas, Rect targetBounds, DockPosition position)
{
    canvas.Children.Clear();

    // Use nullable Rect? to avoid needing Rect.Empty
    Rect? previewRect = position switch
    {
        DockPosition.Left => new Rect(targetBounds.X, targetBounds.Y, targetBounds.Width / 2, targetBounds.Height),
        DockPosition.Right => new Rect(targetBounds.X + targetBounds.Width / 2, targetBounds.Y, targetBounds.Width / 2, targetBounds.Height),
        DockPosition.Top => new Rect(targetBounds.X, targetBounds.Y, targetBounds.Width, targetBounds.Height / 2),
        DockPosition.Bottom => new Rect(targetBounds.X, targetBounds.Y + targetBounds.Height / 2, targetBounds.Width, targetBounds.Height / 2),
        DockPosition.Center => targetBounds,
        _ => null
    };

    if (!previewRect.HasValue) return;

    var rect = previewRect.Value;

    var highlightBox = new Avalonia.Controls.Shapes.Rectangle
    {
        Width = rect.Width,
        Height = rect.Height,
        Fill = new SolidColorBrush(Color.Parse("#007ACC"), opacity: 0.35),
        Stroke = new SolidColorBrush(Color.Parse("#007ACC")),
        StrokeThickness = 2,
        IsHitTestVisible = false
    };

    Canvas.SetLeft(highlightBox, rect.X);
    Canvas.SetTop(highlightBox, rect.Y);

    canvas.Children.Add(highlightBox);
}

    private DockPosition CalculateDockPosition(Rect bounds, Point relativePoint)
    {
        double xRatio = relativePoint.X / bounds.Width;
        double yRatio = relativePoint.Y / bounds.Height;

        // Outer 25% regions trigger splits
        if (xRatio < 0.25) return DockPosition.Left;
        if (xRatio > 0.75) return DockPosition.Right;
        if (yRatio < 0.25) return DockPosition.Top;
        if (yRatio > 0.75) return DockPosition.Bottom;

        // Middle 50% triggers tab insertion (Center)
        return DockPosition.Center;
    }

    private void ExecuteDrop(TopLevel topLevel, Point dropPoint, PaneNode sourcePane, PointerReleasedEventArgs e)
    {
        // 1. Release pointer capture FIRST before mutating the layout model
        e.Pointer.Capture(null);

        // Clear overlay preview
        var canvas = topLevel.FindControl<Canvas>("DragOverlayCanvas");
        canvas?.Children.Clear();

        var hitControl = topLevel.InputHitTest(dropPoint) as Visual;
        var targetControl = hitControl?.FindAncestorOfType<TabGroupControl>();

        if (targetControl?.DataContext is TabGroupNode targetNode)
        {
            var relativePos = dropPoint - targetControl.TranslatePoint(new Point(0, 0), topLevel)!.Value;
            var position = CalculateDockPosition(targetControl.Bounds, relativePos);

            if (DataContext is TabGroupNode currentGroup)
            {
                // Prevent dropping onto itself if it's the only pane in Center position
                if (currentGroup == targetNode && currentGroup.Panes.Count == 1 && position == DockPosition.Center)
                    return;

                if (topLevel.DataContext is MainViewModel mainVm)
                {
                    var currentRoot = mainVm.RootLayout;

                    // 2. Perform atomic relocate (Close source + Dock target + Normalize tree)
                    mainVm.LayoutManager.RelocatePane(sourcePane, targetNode, position, ref currentRoot);

                    // 3. Update main root property to refresh UI bindings
                    mainVm.RootLayout = currentRoot;
                }
            }
        }
    }
}