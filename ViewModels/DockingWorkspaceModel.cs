using System.Linq;
using Avalonia.Layout;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using STACK_HUB.Docking.Models;
using STACK_HUB.Docking.Services;

namespace STACK_HUB.ViewModels;

public partial class DockingWorkspaceModel : ObservableObject
{
    
    private readonly LayoutManager _layoutManager = new();

    public LayoutManager LayoutManager
    {
        get => _layoutManager;
    }

    [ObservableProperty]
    private LayoutNode? _rootLayout;

    public DockingWorkspaceModel()
    {
        InitializeDefaultLayout();
    }

    private void InitializeDefaultLayout()
    {
        // // 1. Create Left Pane Group (Solution Explorer)
        // var leftGroup = new TabGroupNode();
        // leftGroup.AddPane(CreatePane("Solution Explorer", "File Tree View"));
        // leftGroup.ActivePane = leftGroup.Panes[0];

        // // 2. Create Right Pane Group (Code Editor)
        // var rightGroup = new TabGroupNode();
        // rightGroup.AddPane(CreatePane("MainWindow.axaml", "<Window> Code View </Window>"));
        // rightGroup.AddPane(CreatePane("MainWindow.axaml.cs", "C# code view"));
        // rightGroup.ActivePane = rightGroup.Panes[0];

        // // 3. Create Root Split Container
        // var rootSplit = new SplitNode
        // {
        //     Orientation = Orientation.Horizontal,
        //     Ratio = 0.3,
        //     FirstChild = leftGroup,
        //     SecondChild = rightGroup
        // };

        // CRITICAL: Ensure children know their parent!
        // leftGroup.Parent = rootSplit;
        // rightGroup.Parent = rootSplit;

        // RootLayout = rootSplit;
    }
    
    private PaneNode CreatePane(string id, string title, object content)
    {
        var pane = new PaneNode
        {
            Id = id,
            Title = title,
            ContentViewModel = content,
            OnCloseRequested = ClosePane
        };

        return pane;
    }

    public void OpenOrFocusPane(string paneId, string title, object contentViewModel)
    {
        // Case 1: Workspace is completely empty -> initialize root with a new TabGroup
        if (RootLayout == null)
        {
            var pane = CreatePane(paneId, title, contentViewModel);
            var initialGroup = new TabGroupNode();
            initialGroup.AddPane(pane);
            initialGroup.ActivePane = pane;

            RootLayout = initialGroup;
            return;
        }

        // Case 2: Pane is already open -> bring its tab to focus
        var existingPane = FindPaneById(RootLayout, paneId);
        if (existingPane != null)
        {
            if (existingPane.Parent is TabGroupNode group)
            {
                group.ActivePane = existingPane;
            }
            return;
        }

        // Case 3: Pane is not open -> add it to an existing tab group
        var newPane = CreatePane(paneId, title, contentViewModel);
        var targetGroup = FindFirstTabGroup(RootLayout);

        if (targetGroup != null)
        {
            targetGroup.AddPane(newPane);
            targetGroup.ActivePane = newPane;
            // No RootLayout re-assignment needed here!
            // ObservableCollection notifies UI of the new tab automatically.
        }
        else
        {
            // Fallback if no TabGroup exists in the current tree
            var newGroup = new TabGroupNode();
            newGroup.AddPane(newPane);
            newGroup.ActivePane = newPane;

            RootLayout = newGroup;
        }
    }

    private PaneNode? FindPaneById(LayoutNode? node, string paneId)
    {
        if (node is TabGroupNode group)
        {
            return group.Panes.FirstOrDefault(p => p.Id == paneId);
        }
        if (node is SplitNode split)
        {
            return FindPaneById(split.FirstChild, paneId) ?? FindPaneById(split.SecondChild, paneId);
        }
        return null;
    }

    /// <summary>
    /// Command to close a specific pane.
    /// </summary>
    [RelayCommand]
    public void ClosePane(PaneNode pane)
    {
        if (RootLayout == null || pane == null) return;

        var root = RootLayout;
        _layoutManager.RemovePane(pane, ref root);
        
        // Notify UI of root tree mutation (triggers full dynamic UI re-render if root collapsed)
        RootLayout = root; 
    }

// Helper method to recursively find a TabGroupNode
    private TabGroupNode? FindFirstTabGroup(LayoutNode? node)
    {
        if (node is TabGroupNode tabGroup)
            return tabGroup;

        if (node is SplitNode split)
        {
            // Prefer searching the right/bottom branch first, or fall back to left/top
            return FindFirstTabGroup(split.SecondChild) ?? FindFirstTabGroup(split.FirstChild);
        }

        return null;
    }
    
    /// <summary>
    /// Handles relocating a pane within the docking tree safely.
    /// </summary>
    public void RelocatePane(PaneNode sourcePane, TabGroupNode targetGroup, DockPosition position)
    {
        if (RootLayout == null) return;

        // Find the source group containing the source pane
        var sourceGroup = LayoutManager.FindTabGroupContaining(RootLayout, sourcePane);

        // Rule: Prevent dropping onto itself if it's the sole pane in that group
        if (sourceGroup == targetGroup && sourceGroup.Panes.Count == 1)
            return;

        // Perform atomic relocate (Close source + Dock target + Normalize tree)
        var currentRoot = RootLayout;
        LayoutManager.RelocatePane(sourcePane, targetGroup, position, ref currentRoot);

        // Re-assign RootLayout to notify UI bindings of the structural change
        RootLayout = currentRoot;
    }
}