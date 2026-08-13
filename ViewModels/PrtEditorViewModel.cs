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

    public ObservableCollection<StackPrtNode> Nodes => Prt.Nodes;

    public ObservableCollection<PrtGraphNodeViewModel> GraphNodes { get; } = new();
    public ObservableCollection<PrtGraphWireViewModel> GraphWires { get; } = new();

    public static List<string> AvailableAnswerTests { get; } = new()
    {
        "AlgEquiv",
        "SubstEquiv",
        "EqualComAss",
        "CasString",
        "NumAbsolute",
        "NumRelative",
        "NumSigSeqs",
        "SysEquiv",
        "Units",
        "LowestTerms",
        "Expanded",
        "FacForm",
        "SingleFrac"
    };

    private static readonly IBrush TrueBrush = SolidColorBrush.Parse("#4EC9B0");
    private static readonly IBrush FalseBrush = SolidColorBrush.Parse("#F92672");

    public PrtEditorViewModel(StackPrt prt)
    {
        Prt = prt;
        SelectedNode = Prt.Nodes.FirstOrDefault();

        Prt.Nodes.CollectionChanged += OnNodesCollectionChanged;
        foreach (var node in Prt.Nodes)
        {
            node.PropertyChanged += OnNodePropertyChanged;
        }

        RebuildGraph();
    }

    private void OnNodesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (StackPrtNode oldNode in e.OldItems)
            {
                oldNode.PropertyChanged -= OnNodePropertyChanged;
            }
        }
        if (e.NewItems != null)
        {
            foreach (StackPrtNode newNode in e.NewItems)
            {
                newNode.PropertyChanged += OnNodePropertyChanged;
            }
        }

        RebuildGraph();
    }

    private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(StackPrtNode.NextNodeTrue) ||
            e.PropertyName == nameof(StackPrtNode.NextNodeFalse) ||
            e.PropertyName == nameof(StackPrtNode.Name) ||
            e.PropertyName == nameof(StackPrtNode.AnswerTest) ||
            e.PropertyName == nameof(StackPrtNode.StudentAnswer) ||
            e.PropertyName == nameof(StackPrtNode.TeacherAnswer))
        {
            RebuildGraph();
        }
    }

    public void RebuildGraph()
    {
        GraphNodes.Clear();
        GraphWires.Clear();
        if (!Prt.Nodes.Any()) return;

        // 1. Assign 2D Graph Layout Positions
        double startX = 300;
        double startY = 50;
        double levelHeight = 180;
        double colWidth = 280;

        var visited = new HashSet<string>();
        var positions = new Dictionary<string, (double X, double Y)>();

        void LayoutNode(StackPrtNode node, int level, double xOffset)
        {
            if (visited.Contains(node.Id)) return;
            visited.Add(node.Id);

            positions[node.Id] = (xOffset, startY + level * levelHeight);

            // True child
            var trueTarget = FindNodeModel(node.NextNodeTrue);
            if (trueTarget != null && !visited.Contains(trueTarget.Id))
            {
                LayoutNode(trueTarget, level + 1, xOffset - colWidth / 2);
            }

            // False child
            var falseTarget = FindNodeModel(node.NextNodeFalse);
            if (falseTarget != null && !visited.Contains(falseTarget.Id))
            {
                LayoutNode(falseTarget, level + 1, xOffset + colWidth / 2);
            }
        }

        // Start from Node 1 or first node
        var rootNode = Prt.Nodes.FirstOrDefault();
        if (rootNode != null)
        {
            LayoutNode(rootNode, 0, startX);
        }

        // Layout any remaining unvisited nodes
        int unvisitedCol = 0;
        foreach (var unvisitedNode in Prt.Nodes)
        {
            if (!visited.Contains(unvisitedNode.Id))
            {
                positions[unvisitedNode.Id] = (startX + (unvisitedCol + 1) * colWidth, startY);
                unvisitedCol++;
            }
        }

        // Create GraphNodeViewModels
        foreach (var node in Prt.Nodes)
        {
            var pos = positions.TryGetValue(node.Id, out var p) ? p : (X: startX, Y: startY);
            var gNode = new PrtGraphNodeViewModel(node, pos.X, pos.Y);
            GraphNodes.Add(gNode);
        }

        // 2. Generate Bezier Connecting Wires
        UpdateWires();
    }

    public void UpdateWires()
    {
        GraphWires.Clear();
        var nodeMap = GraphNodes.ToDictionary(gn => gn.Node.Id, gn => gn);

        foreach (var gNode in GraphNodes)
        {
            // True Wire
            var trueTarget = FindGraphNode(gNode.Node.NextNodeTrue, nodeMap);
            if (trueTarget != null)
            {
                GraphWires.Add(new PrtGraphWireViewModel(gNode.TruePortLocation, trueTarget.InputPortLocation, TrueBrush, "True"));
            }
            else
            {
                // Terminal Stop Wire (short stub downward)
                Point stopEnd = new Point(gNode.TruePortLocation.X, gNode.TruePortLocation.Y + 35);
                GraphWires.Add(new PrtGraphWireViewModel(gNode.TruePortLocation, stopEnd, TrueBrush, "Stop"));
            }

            // False Wire
            var falseTarget = FindGraphNode(gNode.Node.NextNodeFalse, nodeMap);
            if (falseTarget != null)
            {
                GraphWires.Add(new PrtGraphWireViewModel(gNode.FalsePortLocation, falseTarget.InputPortLocation, FalseBrush, "False"));
            }
            else
            {
                // Terminal Stop Wire (short stub downward)
                Point stopEnd = new Point(gNode.FalsePortLocation.X, gNode.FalsePortLocation.Y + 35);
                GraphWires.Add(new PrtGraphWireViewModel(gNode.FalsePortLocation, stopEnd, FalseBrush, "Stop"));
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
        if (model != null && map.TryGetValue(model.Id, out var gNode))
        {
            return gNode;
        }
        return null;
    }

    [RelayCommand]
    private void SelectNode(StackPrtNode? node)
    {
        SelectedNode = node;
    }

    [RelayCommand]
    private void AddNode()
    {
        int nodeCount = Prt.Nodes.Count + 1;
        var newNode = new StackPrtNode
        {
            Name = $"Node {nodeCount}",
            AnswerTest = "AlgEquiv",
            StudentAnswer = "ans1",
            TeacherAnswer = "model_ans",
            NextNodeTrue = "-1",
            NextNodeFalse = "-1",
            TrueFeedback = "<p>Correct!</p>",
            FalseFeedback = "<p>Incorrect.</p>"
        };

        Prt.Nodes.Add(newNode);
        SelectedNode = newNode;
    }

    [RelayCommand]
    private void RemoveNode(StackPrtNode? node)
    {
        if (node == null) return;
        
        Prt.Nodes.Remove(node);
        if (SelectedNode == node)
        {
            SelectedNode = Prt.Nodes.LastOrDefault();
        }
    }
}
