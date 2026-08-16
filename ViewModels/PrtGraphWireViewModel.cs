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
    private string _branchName = "";

    [ObservableProperty]
    private string _scoreText = "";

    [ObservableProperty]
    private Point _branchNamePosition;

    [ObservableProperty]
    private Point _scorePosition;

    [ObservableProperty]
    private string _sourceNodeId = "";

    [ObservableProperty]
    private string _branchType = "";

    public PrtGraphWireViewModel(Point start, Point end, IBrush strokeBrush, string branchName = "", string scoreText = "")
    {
        _strokeBrush = strokeBrush;
        _branchName = branchName;
        _scoreText = scoreText;
        UpdatePath(start, end);
    }

    public void UpdatePath(Point start, Point end)
    {
        double controlDist = System.Math.Max(50, System.Math.Abs(end.Y - start.Y) / 2);
        Point control1 = new Point(start.X, start.Y + controlDist);
        Point control2 = new Point(end.X, end.Y - controlDist);

        PathData = string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "M {0},{1} C {2},{3} {4},{5} {6},{7}",
            start.X, start.Y,
            control1.X, control1.Y,
            control2.X, control2.Y,
            end.X, end.Y);

        // BranchName label near port start (20% along curve)
        BranchNamePosition = new Point(start.X + (end.X - start.X) * 0.15, start.Y + 15);

        // ScoreText label along middle of curve (50% along curve)
        ScorePosition = new Point((start.X + end.X) / 2 - 14, (start.Y + end.Y) / 2 - 10);
    }
}
