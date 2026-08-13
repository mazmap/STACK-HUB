using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace STACK_HUB.ViewModels;

public partial class PrtGraphWireViewModel : ObservableObject
{
    [ObservableProperty]
    private string _pathData = "";

    [ObservableProperty]
    private IBrush _strokeBrush = Brushes.Green;

    [ObservableProperty]
    private string _label = "";

    [ObservableProperty]
    private Point _labelPosition;

    public PrtGraphWireViewModel(Point start, Point end, IBrush strokeBrush, string label = "")
    {
        _strokeBrush = strokeBrush;
        _label = label;
        UpdatePath(start, end);
    }

    public void UpdatePath(Point start, Point end)
    {
        // Smooth vertical Bezier Curve formula
        double controlDist = System.Math.Max(50, System.Math.Abs(end.Y - start.Y) / 2);
        Point control1 = new Point(start.X, start.Y + controlDist);
        Point control2 = new Point(end.X, end.Y - controlDist);

        PathData = string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "M {0},{1} C {2},{3} {4},{5} {6},{7}",
            start.X, start.Y,
            control1.X, control1.Y,
            control2.X, control2.Y,
            end.X, end.Y);

        LabelPosition = new Point((start.X + end.X) / 2, (start.Y + end.Y) / 2);
    }
}
