using CommunityToolkit.Mvvm.ComponentModel;

namespace STACK_HUB.Docking.Models;

public partial class PaneNode : LayoutNode
{
    [ObservableProperty]
    private string _title = string.Empty;

    // This holds the actual ViewModel for your tab's content (e.g., TextEditorViewModel)
    [ObservableProperty]
    private object? _contentViewModel;
}