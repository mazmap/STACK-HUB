using CommunityToolkit.Mvvm.ComponentModel;

namespace STACK_HUB.Docking.Models;

public abstract partial class LayoutNode : ObservableObject
{
    [ObservableProperty]
    private LayoutNode? _parent;
}