using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace STACK_HUB.Docking.Models;

public partial class TabGroupNode : LayoutNode
{
    public ObservableCollection<PaneNode> Panes { get; } = new();

    [ObservableProperty]
    private PaneNode? _activePane;
    
    // TODO: Add _selectedPane

    public void AddPane(PaneNode pane)
    {
        Panes.Add(pane);
        pane.Parent = this;
    }
}