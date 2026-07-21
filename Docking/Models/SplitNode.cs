using Avalonia.Layout;
using CommunityToolkit.Mvvm.ComponentModel;

namespace STACK_HUB.Docking.Models;

public partial class SplitNode : LayoutNode
{
    [ObservableProperty]
    private Orientation _orientation = Orientation.Horizontal;

    [ObservableProperty]
    private LayoutNode? _firstChild;

    [ObservableProperty]
    private LayoutNode? _secondChild;

    // Split ratio between 0.0 and 1.0 (default 50/50 split)
    [ObservableProperty]
    private double _ratio = 0.5;
}