using Avalonia.Layout;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using STACK_HUB.Docking.Models;
using STACK_HUB.Docking.Services;
using STACK_HUB.Models;

namespace STACK_HUB.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly LayoutManager _layoutManager = new();

    [ObservableProperty]
    private LayoutNode? _rootLayout;

    public MainViewModel()
    {
        InitializeDefaultLayout();
    }

    private void InitializeDefaultLayout()
    {
        // 1. Create Left Pane Group (Solution Explorer)
        var leftGroup = new TabGroupNode();
        leftGroup.AddPane(CreatePane("Solution Explorer", "File Tree View"));
        leftGroup.ActivePane = leftGroup.Panes[0];

        // 2. Create Right Pane Group (Code Editor)
        var rightGroup = new TabGroupNode();
        rightGroup.AddPane(CreatePane("MainWindow.axaml", "<Window> Code View </Window>"));
        rightGroup.AddPane(CreatePane("MainWindow.axaml.cs", "C# code view"));
        rightGroup.ActivePane = rightGroup.Panes[0];

        // 3. Create Root Split Container
        var rootSplit = new SplitNode
        {
            Orientation = Orientation.Horizontal,
            Ratio = 0.3,
            FirstChild = leftGroup,
            SecondChild = rightGroup
        };

// CRITICAL: Ensure children know their parent!
        leftGroup.Parent = rootSplit;
        rightGroup.Parent = rootSplit;

        RootLayout = rootSplit;
    }
    
    private PaneNode CreatePane(string title, object content)
    {
        var pane = new PaneNode
        {
            Title = title,
            ContentViewModel = content
        };
    
        // Inject close delegate
        pane.OnCloseRequested = p => ClosePane(p);
    
        return pane;
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

    /// <summary>
    /// Command to add a new pane dynamically.
    /// </summary>
    [RelayCommand]
    public void OpenNewTerminal()
    {
        if (RootLayout == null) return;

        var newPane = CreatePane("Terminal", "Terminal Output Window");

        var root = RootLayout;

        // 1. Find the first available TabGroup in the tree to dock against
        var targetGroup = FindFirstTabGroup(root);

        if (targetGroup != null)
        {
            // Dock to the bottom of that tab group
            _layoutManager.Dock(newPane, targetGroup, DockPosition.Bottom, ref root);
        }
        else
        {
            // If the layout is completely empty, start a new TabGroup at root
            var initialGroup = new TabGroupNode();
            initialGroup.Panes.Add(newPane);
            root = initialGroup;
        }

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
}