using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using STACK_HUB.Models;

namespace STACK_HUB.ViewModels;

public partial class PrtEditorViewModel : ViewModelBase, ICacheablePane
{
    public StackPrt Prt { get; }

    [ObservableProperty]
    private StackPrtNode? _selectedNode;

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    private double _panX = 150.0;

    [ObservableProperty]
    private double _panY = 100.0;

    [ObservableProperty]
    private double _nodeEditorWidth = 340.0;

    [ObservableProperty]
    private double _feedbackVariablesHeight = 240.0;

    [ObservableProperty]
    private bool _isFeedbackVariablesExpanded;

    public ObservableCollection<StackPrtNode> Nodes => Prt.Nodes;
    public ObservableCollection<PrtGraphNodeViewModel> GraphNodes { get; } = new();
    public ObservableCollection<PrtGraphWireViewModel> GraphWires { get; } = new();

    public static List<string> AvailableAnswerTests { get; } = new()
    {
        "AlgEquiv", "SubstEquiv", "EqualComAss", "CasString", "NumAbsolute",
        "NumRelative", "NumSigSeqs", "SysEquiv", "Units", "LowestTerms",
        "Expanded", "FacForm", "SingleFrac"
    };

    public static List<string> AvailableScoreModes { get; } = new()
    {
        "Set to / Add / Subtract", "Set to", "Add", "Subtract"
    };

    private static readonly IBrush TrueBrush = SolidColorBrush.Parse("#4EC9B0");
    private static readonly IBrush FalseBrush = SolidColorBrush.Parse("#F92672");

    public MaximaEditorViewModel FeedbackVariablesEditor { get; }

    public PrtEditorViewModel(StackPrt prt)
    {
        Prt = prt;
        SelectedNode = null;

        FeedbackVariablesEditor = new MaximaEditorViewModel(Prt.FeedbackVariables);
        FeedbackVariablesEditor.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MaximaEditorViewModel.Text))
            {
                Prt.FeedbackVariables = FeedbackVariablesEditor.Text;
            }
        };

        Prt.Nodes.CollectionChanged += OnNodesCollectionChanged;
        foreach (var node in Prt.Nodes)
        {
            node.PropertyChanged += OnNodePropertyChanged;
        }

        RebuildGraph();
    }

    public ObservableCollection<PrtGraphNodeViewModel> SelectedGraphNodes { get; } = new();

    public void SelectNodeViewModel(PrtGraphNodeViewModel gNode, bool isMultiSelect)
    {
        if (!isMultiSelect)
        {
            foreach (var node in GraphNodes)
            {
                node.IsSelected = false;
            }
            SelectedGraphNodes.Clear();

            gNode.IsSelected = true;
            SelectedGraphNodes.Add(gNode);
            SelectedNode = gNode.Node;
        }
        else
        {
            gNode.IsSelected = !gNode.IsSelected;
            if (gNode.IsSelected)
            {
                if (!SelectedGraphNodes.Contains(gNode)) SelectedGraphNodes.Add(gNode);
            }
            else
            {
                SelectedGraphNodes.Remove(gNode);
            }
            SelectedNode = null;
        }
    }

    public void ClearSelection()
    {
        foreach (var node in GraphNodes)
        {
            node.IsSelected = false;
        }
        SelectedGraphNodes.Clear();
        SelectedNode = null;
    }

    partial void OnSelectedNodeChanged(StackPrtNode? value)
    {
        if (value == null)
        {
            foreach (var node in GraphNodes)
            {
                node.IsSelected = false;
            }
            SelectedGraphNodes.Clear();
        }
    }

    private void OnNodesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (StackPrtNode oldNode in e.OldItems) oldNode.PropertyChanged -= OnNodePropertyChanged;
        }
        if (e.NewItems != null)
        {
            foreach (StackPrtNode newNode in e.NewItems) newNode.PropertyChanged += OnNodePropertyChanged;
        }
        RebuildGraph();
    }

    private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(StackPrtNode.NextNodeTrue) ||
            e.PropertyName == nameof(StackPrtNode.NextNodeFalse) ||
            e.PropertyName == nameof(StackPrtNode.DisplayNextNodeTrue) ||
            e.PropertyName == nameof(StackPrtNode.DisplayNextNodeFalse) ||
            e.PropertyName == nameof(StackPrtNode.Name) ||
            e.PropertyName == nameof(StackPrtNode.AnswerTest) ||
            e.PropertyName == nameof(StackPrtNode.StudentAnswer) ||
            e.PropertyName == nameof(StackPrtNode.TeacherAnswer) ||
            e.PropertyName == nameof(StackPrtNode.NodeScore) ||
            e.PropertyName == nameof(StackPrtNode.Penalty))
        {
            RebuildGraph();
        }
    }

    private readonly Dictionary<string, Point> _userNodePositions = new();

    public void SaveNodePosition(string nodeId, double x, double y)
    {
        _userNodePositions[nodeId] = new Point(x, y);
    }

    public ObservableCollection<string> AvailableNodeNames { get; } = new();

    public void UpdateAvailableNodeNames()
    {
        var currentList = new List<string> { "Keine" };
        foreach (var node in Prt.Nodes)
        {
            if (!currentList.Contains(node.Name))
            {
                currentList.Add(node.Name);
            }
        }

        for (int i = AvailableNodeNames.Count - 1; i >= 0; i--)
        {
            if (!currentList.Contains(AvailableNodeNames[i]))
            {
                AvailableNodeNames.RemoveAt(i);
            }
        }

        foreach (var item in currentList)
        {
            if (!AvailableNodeNames.Contains(item))
            {
                AvailableNodeNames.Add(item);
            }
        }
    }

    private bool _isRebuildingGraph;

    public void RebuildGraph()
    {
        if (_isRebuildingGraph) return;
        _isRebuildingGraph = true;
        try
        {
            GraphNodes.Clear();
            GraphWires.Clear();
            UpdateAvailableNodeNames();
            if (!Prt.Nodes.Any()) return;

        double startX = 5000;
        double startY = 5000;
        double levelHeight = 180;
        double colWidth = 280;

        var visited = new HashSet<string>();
        var positions = new Dictionary<string, (double X, double Y)>();

        void LayoutNode(StackPrtNode node, int level, double xOffset)
        {
            if (visited.Contains(node.Id)) return;
            visited.Add(node.Id);
            positions[node.Id] = (xOffset, startY + level * levelHeight);

            var trueTarget = FindNodeModel(node.NextNodeTrue);
            if (trueTarget != null && !visited.Contains(trueTarget.Id))
            {
                LayoutNode(trueTarget, level + 1, xOffset - colWidth / 2);
            }

            var falseTarget = FindNodeModel(node.NextNodeFalse);
            if (falseTarget != null && !visited.Contains(falseTarget.Id))
            {
                LayoutNode(falseTarget, level + 1, xOffset + colWidth / 2);
            }
        }

        var rootNode = Prt.Nodes.FirstOrDefault();
        if (rootNode != null) LayoutNode(rootNode, 0, startX);

        int unvisitedCol = 0;
        foreach (var unvisitedNode in Prt.Nodes)
        {
            if (!visited.Contains(unvisitedNode.Id))
            {
                positions[unvisitedNode.Id] = (startX + (unvisitedCol + 1) * colWidth, startY);
                unvisitedCol++;
            }
        }

        foreach (var node in Prt.Nodes)
        {
            double posX = startX;
            double posY = startY;

            if (_userNodePositions.TryGetValue(node.Id, out var savedPos))
            {
                posX = savedPos.X;
                posY = savedPos.Y;
            }
            else if (positions.TryGetValue(node.Id, out var layoutPos))
            {
                posX = layoutPos.X;
                posY = layoutPos.Y;
            }

            var gNode = new PrtGraphNodeViewModel(node, posX, posY)
            {
                IsSelected = (SelectedNode != null && node.Id == SelectedNode.Id)
            };
            GraphNodes.Add(gNode);
        }

            UpdateWires();
        }
        finally
        {
            _isRebuildingGraph = false;
        }
    }

    public void UpdateWires()
    {
        GraphWires.Clear();
        var nodeMap = GraphNodes.GroupBy(gn => gn.Node.Id).ToDictionary(g => g.Key, g => g.First());

        foreach (var gNode in GraphNodes)
        {
            string trueScore = gNode.Node.NodeScore >= 0 ? $"+{gNode.Node.NodeScore.ToString(CultureInfo.InvariantCulture)}" : gNode.Node.NodeScore.ToString(CultureInfo.InvariantCulture);
            string falseScore = gNode.Node.Penalty > 0 ? $"-{gNode.Node.Penalty.ToString(CultureInfo.InvariantCulture)}" : gNode.Node.Penalty.ToString(CultureInfo.InvariantCulture);

            var trueTarget = FindGraphNode(gNode.Node.NextNodeTrue, nodeMap);
            if (trueTarget != null)
            {
                GraphWires.Add(new PrtGraphWireViewModel(gNode.TruePortLocation, trueTarget.InputPortLocation, TrueBrush, "Wahr", trueScore)
                {
                    SourceNodeId = gNode.Node.Id,
                    BranchType = "True"
                });
            }
            else
            {
                Point stopEnd = new Point(gNode.TruePortLocation.X, gNode.TruePortLocation.Y + 35);
                GraphWires.Add(new PrtGraphWireViewModel(gNode.TruePortLocation, stopEnd, TrueBrush, "Wahr", trueScore)
                {
                    SourceNodeId = gNode.Node.Id,
                    BranchType = "True"
                });
            }

            var falseTarget = FindGraphNode(gNode.Node.NextNodeFalse, nodeMap);
            if (falseTarget != null)
            {
                GraphWires.Add(new PrtGraphWireViewModel(gNode.FalsePortLocation, falseTarget.InputPortLocation, FalseBrush, "Falsch", falseScore)
                {
                    SourceNodeId = gNode.Node.Id,
                    BranchType = "False"
                });
            }
            else
            {
                Point stopEnd = new Point(gNode.FalsePortLocation.X, gNode.FalsePortLocation.Y + 35);
                GraphWires.Add(new PrtGraphWireViewModel(gNode.FalsePortLocation, stopEnd, FalseBrush, "Falsch", falseScore)
                {
                    SourceNodeId = gNode.Node.Id,
                    BranchType = "False"
                });
            }
        }
    }

    private StackPrtNode? FindNodeModel(string target)
    {
        if (string.IsNullOrEmpty(target) || target == "-1") return null;
        var byId = Prt.Nodes.FirstOrDefault(n => n.Id == target);
        if (byId != null) return byId;

        var byName = Prt.Nodes.FirstOrDefault(n => n.Name.Equals(target, StringComparison.OrdinalIgnoreCase) || n.Name.Equals($"Node {target}", StringComparison.OrdinalIgnoreCase));
        if (byName != null) return byName;

        if (int.TryParse(target, out int index) && index >= 1 && index <= Prt.Nodes.Count)
        {
            return Prt.Nodes[index - 1];
        }

        return null;
    }

    private PrtGraphNodeViewModel? FindGraphNode(string target, Dictionary<string, PrtGraphNodeViewModel> map)
    {
        var model = FindNodeModel(target);
        if (model != null && map.TryGetValue(model.Id, out var gNode)) return gNode;
        return null;
    }

    [RelayCommand]
    private void ToggleFeedbackVariables() => IsFeedbackVariablesExpanded = !IsFeedbackVariablesExpanded;

    [RelayCommand]
    private void CloseNodeEditor() => SelectedNode = null;

    [RelayCommand]
    private void ResetZoom()
    {
        ZoomLevel = 1.0;
        PanX = 0;
        PanY = 0;
    }

    [RelayCommand]
    public void CenterView(object? parameter = null)
    {
        if (!GraphNodes.Any())
        {
            PanX = 150;
            PanY = 100;
            return;
        }

        double fullWidth = 800;
        double fullHeight = 600;

        if (parameter is Size s && s.Width > 100 && s.Height > 100)
        {
            fullWidth = s.Width;
            fullHeight = s.Height;
        }
        else if (parameter is Rect r && r.Width > 100 && r.Height > 100)
        {
            fullWidth = r.Width;
            fullHeight = r.Height;
        }

        double visibleWidth = SelectedNode != null ? Math.Max(200, fullWidth - NodeEditorWidth) : fullWidth;
        double visibleHeight = IsFeedbackVariablesExpanded ? Math.Max(200, fullHeight - FeedbackVariablesHeight) : fullHeight;

        double minX = GraphNodes.Min(gn => gn.X);
        double maxX = GraphNodes.Max(gn => gn.X + gn.Width);
        double minY = GraphNodes.Min(gn => gn.Y);
        double maxY = GraphNodes.Max(gn => gn.Y + gn.Height);

        double treeCenterX = (minX + maxX) / 2.0;
        double treeCenterY = (minY + maxY) / 2.0;

        PanX = (visibleWidth / 2.0) - (treeCenterX * ZoomLevel);
        PanY = (visibleHeight / 2.0) - (treeCenterY * ZoomLevel);

        ClampPan(fullWidth, fullHeight);
    }

    public void ClampPan(double viewportWidth, double viewportHeight)
    {
        double scaledCanvasWidth = 10000 * ZoomLevel;
        double scaledCanvasHeight = 10000 * ZoomLevel;

        double visibleWidth = SelectedNode != null ? Math.Max(200, viewportWidth - NodeEditorWidth) : viewportWidth;
        double visibleHeight = IsFeedbackVariablesExpanded ? Math.Max(200, viewportHeight - FeedbackVariablesHeight) : viewportHeight;

        if (scaledCanvasWidth > visibleWidth)
        {
            double minPanX = visibleWidth - scaledCanvasWidth;
            double maxPanX = 0;
            PanX = Math.Clamp(PanX, minPanX, maxPanX);
        }
        else
        {
            PanX = (visibleWidth - scaledCanvasWidth) / 2.0;
        }

        if (scaledCanvasHeight > visibleHeight)
        {
            double minPanY = visibleHeight - scaledCanvasHeight;
            double maxPanY = 0;
            PanY = Math.Clamp(PanY, minPanY, maxPanY);
        }
        else
        {
            PanY = (visibleHeight - scaledCanvasHeight) / 2.0;
        }
    }

    public void ConnectBranch(StackPrtNode sourceNode, string branchType, StackPrtNode? targetNode)
    {
        string targetValue = targetNode != null ? targetNode.Name : "-1";
        if (branchType == "True") sourceNode.NextNodeTrue = targetValue;
        else if (branchType == "False") sourceNode.NextNodeFalse = targetValue;
        RebuildGraph();
    }

    [RelayCommand]
    private void SelectNode(StackPrtNode? node) => SelectedNode = node;

    public void AddNodeAtPosition(double sceneX, double sceneY)
    {
        int nodeIndex = 1;
        string candidateName;
        do
        {
            candidateName = $"Node {nodeIndex}";
            nodeIndex++;
        } while (Prt.Nodes.Any(n => n.Name == candidateName));

        var newNode = new StackPrtNode
        {
            Name = candidateName,
            AnswerTest = "AlgEquiv",
            StudentAnswer = "sans1",
            TeacherAnswer = "tans1",
            NextNodeTrue = "-1",
            NextNodeFalse = "-1",
            TrueFeedback = "<p>Correct!</p>",
            FalseFeedback = "<p>Incorrect.</p>"
        };

        _userNodePositions[newNode.Id] = new Point(sceneX, sceneY);
        SelectedNode = newNode;
        Prt.Nodes.Add(newNode);
    }

    [RelayCommand]
    private void AddNode()
    {
        double centerSceneX = 5000;
        double centerSceneY = 5000;
        if (GraphNodes.Any())
        {
            centerSceneX = GraphNodes.Max(gn => gn.X) + 260;
            centerSceneY = GraphNodes.Min(gn => gn.Y);
        }
        AddNodeAtPosition(centerSceneX, centerSceneY);
    }

    [RelayCommand]
    private void RemoveNode(StackPrtNode? node)
    {
        if (node == null) return;

        string deletedName = node.Name;
        Prt.Nodes.Remove(node);

        // Reset branch references pointing to the deleted node
        foreach (var remainingNode in Prt.Nodes)
        {
            if (remainingNode.NextNodeTrue == deletedName)
            {
                remainingNode.NextNodeTrue = "-1";
            }
            if (remainingNode.NextNodeFalse == deletedName)
            {
                remainingNode.NextNodeFalse = "-1";
            }
        }

        if (SelectedNode == node) SelectedNode = Prt.Nodes.LastOrDefault();
        RebuildGraph();
    }
}
