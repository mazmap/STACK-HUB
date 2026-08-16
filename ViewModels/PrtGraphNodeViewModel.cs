using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using STACK_HUB.Models;

namespace STACK_HUB.ViewModels;

public partial class PrtGraphNodeViewModel : ObservableObject
{
    public StackPrtNode Node { get; }

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private bool _isSelected;

    public double Width { get; set; } = 220;
    public double Height { get; set; } = 58;

    public Point InputPortLocation => new Point(X + Width / 2, Y);
    public Point TruePortLocation => new Point(X + Width / 4, Y + Height);
    public Point FalsePortLocation => new Point(X + (Width * 3) / 4, Y + Height);

    public PrtGraphNodeViewModel(StackPrtNode node, double x = 0, double y = 0)
    {
        Node = node;
        _x = x;
        _y = y;
    }

    public void NotifyPositionChanged()
    {
        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));
        OnPropertyChanged(nameof(InputPortLocation));
        OnPropertyChanged(nameof(TruePortLocation));
        OnPropertyChanged(nameof(FalsePortLocation));
    }
}
