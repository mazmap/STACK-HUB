using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using STACK_HUB.Models;

namespace STACK_HUB.ViewModels;

public enum PrtBottomPaneTab
{
    FeedbackVariables,
    Settings
}

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
    [NotifyPropertyChangedFor(nameof(NodeEditorColumnWidth))]
    [NotifyPropertyChangedFor(nameof(NodeEditorColumnMinWidth))]
    private double _nodeEditorWidth = 340.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FeedbackVariablesRowHeight))]
    [NotifyPropertyChangedFor(nameof(FeedbackVariablesRowMinHeight))]
    private double _feedbackVariablesHeight = 240.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FeedbackVariablesRowHeight))]
    [NotifyPropertyChangedFor(nameof(FeedbackVariablesRowMinHeight))]
    [NotifyPropertyChangedFor(nameof(IsFeedbackVariablesExpanded))]
    [NotifyPropertyChangedFor(nameof(IsSettingsExpanded))]
    [NotifyPropertyChangedFor(nameof(IsFeedbackVariablesTabActive))]
    [NotifyPropertyChangedFor(nameof(IsSettingsTabActive))]
    private bool _isBottomPaneExpanded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFeedbackVariablesExpanded))]
    [NotifyPropertyChangedFor(nameof(IsSettingsExpanded))]
    [NotifyPropertyChangedFor(nameof(IsFeedbackVariablesTabActive))]
    [NotifyPropertyChangedFor(nameof(IsSettingsTabActive))]
    private PrtBottomPaneTab _activeBottomPaneTab = PrtBottomPaneTab.FeedbackVariables;

    public bool IsFeedbackVariablesExpanded => IsBottomPaneExpanded && ActiveBottomPaneTab == PrtBottomPaneTab.FeedbackVariables;
    public bool IsSettingsExpanded => IsBottomPaneExpanded && ActiveBottomPaneTab == PrtBottomPaneTab.Settings;
    public bool IsFeedbackVariablesTabActive => ActiveBottomPaneTab == PrtBottomPaneTab.FeedbackVariables;
    public bool IsSettingsTabActive => ActiveBottomPaneTab == PrtBottomPaneTab.Settings;

    public GridLength NodeEditorColumnWidth
    {
        get => SelectedNode != null ? new GridLength(NodeEditorWidth, GridUnitType.Pixel) : new GridLength(0, GridUnitType.Pixel);
        set
        {
            if (value.IsAbsolute && value.Value > 0)
            {
                NodeEditorWidth = value.Value;
            }
        }
    }

    public double NodeEditorColumnMinWidth => SelectedNode != null ? 260.0 : 0.0;

    public GridLength FeedbackVariablesRowHeight
    {
        get => IsBottomPaneExpanded ? new GridLength(FeedbackVariablesHeight, GridUnitType.Pixel) : new GridLength(0, GridUnitType.Pixel);
        set
        {
            if (value.IsAbsolute && value.Value > 0)
            {
                FeedbackVariablesHeight = value.Value;
            }
        }
    }

    public double FeedbackVariablesRowMinHeight => IsBottomPaneExpanded ? 120.0 : 0.0;

    public bool HasInitiallyCentered { get; set; }

    public ObservableCollection<StackPrtNode> Nodes => Prt.Nodes;
    public ObservableCollection<PrtGraphNodeViewModel> GraphNodes { get; } = new();
    public ObservableCollection<PrtGraphWireViewModel> GraphWires { get; } = new();

    public static List<string> AvailableFeedbackStyles { get; } = new()
    {
        "Formativ", "Standard", "Kompakt", "Only Symbol"
    };

    [ObservableProperty]
    private string _editingPrtValue = "1.0";

    [ObservableProperty]
    private string? _prtValueValidationError;

    partial void OnEditingPrtValueChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            PrtValueValidationError = "Wert darf nicht leer sein.";
            return;
        }

        string normalized = value.Trim().Replace(',', '.');
        if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedVal))
        {
            PrtValueValidationError = "Ungültige Zahl.";
            return;
        }

        if (parsedVal <= 0)
        {
            PrtValueValidationError = "Wert muss eine positive Zahl sein (> 0).";
            return;
        }

        PrtValueValidationError = null;
        if (Math.Abs(Prt.Value - parsedVal) > 0.0001)
        {
            Prt.Value = parsedVal;
        }
    }

    public static List<string> AvailableAnswerTests { get; } = new()
    {
        "AlgEquiv", "SubstEquiv", "EqualComAss", "CasString", "NumAbsolute",
        "NumRelative", "NumSigSeqs", "SysEquiv", "Units", "LowestTerms",
        "Expanded", "FacForm", "SingleFrac"
    };

    public static List<string> AvailableScoreModes { get; } = new()
    {
        "Set to", "Add", "Subtract"
    };

    public static string FormatBranchScore(double score, string? scoreMode)
    {
        string numStr = Math.Abs(score).ToString(CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(scoreMode))
        {
            return score >= 0 ? $"+{numStr}" : $"-{numStr}";
        }

        if (scoreMode.Equals("Add", StringComparison.OrdinalIgnoreCase) || scoreMode.Contains("+"))
        {
            return score < 0 ? $"-{numStr}" : $"+{numStr}";
        }
        else if (scoreMode.Equals("Subtract", StringComparison.OrdinalIgnoreCase) || scoreMode.Contains("-"))
        {
            return $"-{numStr}";
        }
        else if (scoreMode.Equals("Set to", StringComparison.OrdinalIgnoreCase) || scoreMode.Contains("=") || scoreMode.StartsWith("Set", StringComparison.OrdinalIgnoreCase))
        {
            return score < 0 ? $"=-{numStr}" : $"={numStr}";
        }

        return score >= 0 ? $"+{numStr}" : $"-{numStr}";
    }

    private static readonly IBrush TrueBrush = SolidColorBrush.Parse("#4EC9B0");
    private static readonly IBrush FalseBrush = SolidColorBrush.Parse("#F92672");

    public MaximaEditorViewModel FeedbackVariablesEditor { get; }

    public PrtEditorViewModel(StackPrt prt)
    {
        Prt = prt;
        SelectedNode = null;

        EditingPrtValue = Prt.Value.ToString("0.##", CultureInfo.InvariantCulture);
        Prt.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(StackPrt.Value))
            {
                string formatted = Prt.Value.ToString("0.##", CultureInfo.InvariantCulture);
                if (EditingPrtValue != formatted && PrtValueValidationError == null)
                {
                    EditingPrtValue = formatted;
                }
            }
        };

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
            node.ParentPrt = Prt;
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

            if (SelectedGraphNodes.Count == 1)
            {
                SelectedNode = SelectedGraphNodes[0].Node;
            }
            else
            {
                SelectedNode = null;
            }
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

    [ObservableProperty]
    private string _editingNodeId = string.Empty;

    [ObservableProperty]
    private string? _nodeIdValidationError;

    partial void OnEditingNodeIdChanged(string value)
    {
        if (SelectedNode == null)
        {
            NodeIdValidationError = null;
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            NodeIdValidationError = "Node Id must be a whole number starting from 1 (e.g. 1, 2, 3...).";
            return;
        }

        string trimmed = value.Trim();
        if (!int.TryParse(trimmed, out int parsedNum) || parsedNum < 1)
        {
            NodeIdValidationError = "Node Id must be a whole number starting from 1 (e.g. 1, 2, 3...).";
            return;
        }

        string canonical = parsedNum.ToString();

        if (Prt.Nodes.Any(n => n.Id != SelectedNode.Id && n.NodeId == canonical))
        {
            NodeIdValidationError = $"Node Id '{canonical}' ist bereits vergeben.";
            return;
        }

        // Successfully validated: clear error and update underlying model
        NodeIdValidationError = null;
        if (SelectedNode.NodeId != canonical)
        {
            SelectedNode.NodeId = canonical;
        }
    }

    [ObservableProperty]
    private string _editingScoreTrue = "1.0";

    [ObservableProperty]
    private string? _scoreTrueValidationError;

    partial void OnEditingScoreTrueChanged(string value)
    {
        if (SelectedNode == null)
        {
            ScoreTrueValidationError = null;
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            ScoreTrueValidationError = "Score must be a number (e.g. 1.0, 0.5, 0).";
            return;
        }

        string normalized = value.Trim().Replace(',', '.');
        if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedScore) || parsedScore < 0)
        {
            ScoreTrueValidationError = "Score must be a non-negative number (e.g. 1.0, 0.5, 0).";
            return;
        }

        ScoreTrueValidationError = null;
        if (Math.Abs(SelectedNode.ScoreTrue - parsedScore) > 0.0001)
        {
            SelectedNode.ScoreTrue = parsedScore;
        }
    }

    [ObservableProperty]
    private string _editingPenaltyTrue = "0.0";

    [ObservableProperty]
    private string? _penaltyTrueValidationError;

    partial void OnEditingPenaltyTrueChanged(string value)
    {
        if (SelectedNode == null)
        {
            PenaltyTrueValidationError = null;
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            PenaltyTrueValidationError = "Penalty must be a number (e.g. 0.1, 0.0).";
            return;
        }

        string normalized = value.Trim().Replace(',', '.');
        if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedPenalty) || parsedPenalty < 0)
        {
            PenaltyTrueValidationError = "Penalty must be a non-negative number (e.g. 0.1, 0.0).";
            return;
        }

        PenaltyTrueValidationError = null;
        if (Math.Abs(SelectedNode.PenaltyTrue - parsedPenalty) > 0.0001)
        {
            SelectedNode.PenaltyTrue = parsedPenalty;
        }
    }

    [ObservableProperty]
    private string _editingScoreFalse = "0.0";

    [ObservableProperty]
    private string? _scoreFalseValidationError;

    partial void OnEditingScoreFalseChanged(string value)
    {
        if (SelectedNode == null)
        {
            ScoreFalseValidationError = null;
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            ScoreFalseValidationError = "Score must be a number (e.g. 1.0, 0.5, 0).";
            return;
        }

        string normalized = value.Trim().Replace(',', '.');
        if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedScore) || parsedScore < 0)
        {
            ScoreFalseValidationError = "Score must be a non-negative number (e.g. 1.0, 0.5, 0).";
            return;
        }

        ScoreFalseValidationError = null;
        if (Math.Abs(SelectedNode.ScoreFalse - parsedScore) > 0.0001)
        {
            SelectedNode.ScoreFalse = parsedScore;
        }
    }

    [ObservableProperty]
    private string _editingPenaltyFalse = "0.1";

    [ObservableProperty]
    private string? _penaltyFalseValidationError;

    partial void OnEditingPenaltyFalseChanged(string value)
    {
        if (SelectedNode == null)
        {
            PenaltyFalseValidationError = null;
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            PenaltyFalseValidationError = "Penalty must be a number (e.g. 0.1, 0.0).";
            return;
        }

        string normalized = value.Trim().Replace(',', '.');
        if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedPenalty) || parsedPenalty < 0)
        {
            PenaltyFalseValidationError = "Penalty must be a non-negative number (e.g. 0.1, 0.0).";
            return;
        }

        PenaltyFalseValidationError = null;
        if (Math.Abs(SelectedNode.PenaltyFalse - parsedPenalty) > 0.0001)
        {
            SelectedNode.PenaltyFalse = parsedPenalty;
        }
    }

    [ObservableProperty]
    private CasTextEditorViewModel? _trueFeedbackEditor;

    [ObservableProperty]
    private CasTextEditorViewModel? _falseFeedbackEditor;

    partial void OnSelectedNodeChanged(StackPrtNode? value)
    {
        NodeIdValidationError = null;
        EditingNodeId = value?.NodeId ?? string.Empty;
        ScoreTrueValidationError = null;
        PenaltyTrueValidationError = null;
        ScoreFalseValidationError = null;
        PenaltyFalseValidationError = null;
        OnPropertyChanged(nameof(NodeEditorColumnWidth));
        OnPropertyChanged(nameof(NodeEditorColumnMinWidth));

        EditingScoreTrue = value != null ? value.ScoreTrue.ToString("0.##", CultureInfo.InvariantCulture) : "1.0";
        EditingPenaltyTrue = value != null ? value.PenaltyTrue.ToString("0.##", CultureInfo.InvariantCulture) : "0.0";
        EditingScoreFalse = value != null ? value.ScoreFalse.ToString("0.##", CultureInfo.InvariantCulture) : "0.0";
        EditingPenaltyFalse = value != null ? value.PenaltyFalse.ToString("0.##", CultureInfo.InvariantCulture) : "0.1";

        if (value == null)
        {
            TrueFeedbackEditor = null;
            FalseFeedbackEditor = null;
            if (SelectedGraphNodes.Count <= 1)
            {
                foreach (var node in GraphNodes)
                {
                    node.IsSelected = false;
                }
                SelectedGraphNodes.Clear();
            }
        }
        else
        {
            if (SelectedGraphNodes.Count != 1 || SelectedGraphNodes[0].Node.Id != value.Id)
            {
                SelectedGraphNodes.Clear();
                foreach (var node in GraphNodes)
                {
                    bool isSel = (node.Node.Id == value.Id);
                    node.IsSelected = isSel;
                    if (isSel)
                    {
                        SelectedGraphNodes.Add(node);
                    }
                }
            }

            var trueEd = new CasTextEditorViewModel(value.TrueFeedback, wordWrap: true);
            trueEd.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CasTextEditorViewModel.Text))
                {
                    value.TrueFeedback = trueEd.Text;
                }
            };
            TrueFeedbackEditor = trueEd;

            var falseEd = new CasTextEditorViewModel(value.FalseFeedback, wordWrap: true);
            falseEd.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CasTextEditorViewModel.Text))
                {
                    value.FalseFeedback = falseEd.Text;
                }
            };
            FalseFeedbackEditor = falseEd;
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
        if (e.PropertyName == nameof(StackPrtNode.NodeId))
        {
            if (SelectedNode != null && sender == SelectedNode && EditingNodeId != SelectedNode.NodeId)
            {
                EditingNodeId = SelectedNode.NodeId;
                NodeIdValidationError = null;
            }
            UpdateAvailableNodeNames();
            UpdateWires();
        }
        else if (e.PropertyName == nameof(StackPrtNode.Name) ||
                 e.PropertyName == nameof(StackPrtNode.DisplayName) ||
                 e.PropertyName == nameof(StackPrtNode.Description))
        {
            UpdateAvailableNodeNames();
            UpdateWires();
        }
        else if (e.PropertyName == nameof(StackPrtNode.ScoreTrue))
        {
            if (SelectedNode != null && sender == SelectedNode)
            {
                string strVal = SelectedNode.ScoreTrue.ToString("0.##", CultureInfo.InvariantCulture);
                if (EditingScoreTrue != strVal && (double.TryParse(EditingScoreTrue.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) && Math.Abs(parsed - SelectedNode.ScoreTrue) > 0.0001))
                {
                    EditingScoreTrue = strVal;
                }
            }
            UpdateWires();
        }
        else if (e.PropertyName == nameof(StackPrtNode.PenaltyTrue))
        {
            if (SelectedNode != null && sender == SelectedNode)
            {
                string strVal = SelectedNode.PenaltyTrue.ToString("0.##", CultureInfo.InvariantCulture);
                if (EditingPenaltyTrue != strVal && (double.TryParse(EditingPenaltyTrue.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) && Math.Abs(parsed - SelectedNode.PenaltyTrue) > 0.0001))
                {
                    EditingPenaltyTrue = strVal;
                }
            }
            UpdateWires();
        }
        else if (e.PropertyName == nameof(StackPrtNode.ScoreFalse))
        {
            if (SelectedNode != null && sender == SelectedNode)
            {
                string strVal = SelectedNode.ScoreFalse.ToString("0.##", CultureInfo.InvariantCulture);
                if (EditingScoreFalse != strVal && (double.TryParse(EditingScoreFalse.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) && Math.Abs(parsed - SelectedNode.ScoreFalse) > 0.0001))
                {
                    EditingScoreFalse = strVal;
                }
            }
            UpdateWires();
        }
        else if (e.PropertyName == nameof(StackPrtNode.PenaltyFalse))
        {
            if (SelectedNode != null && sender == SelectedNode)
            {
                string strVal = SelectedNode.PenaltyFalse.ToString("0.##", CultureInfo.InvariantCulture);
                if (EditingPenaltyFalse != strVal && (double.TryParse(EditingPenaltyFalse.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) && Math.Abs(parsed - SelectedNode.PenaltyFalse) > 0.0001))
                {
                    EditingPenaltyFalse = strVal;
                }
            }
            UpdateWires();
        }
        else if (e.PropertyName == nameof(StackPrtNode.NextNodeTrue) ||
                 e.PropertyName == nameof(StackPrtNode.NextNodeFalse) ||
                 e.PropertyName == nameof(StackPrtNode.DisplayNextNodeTrue) ||
                 e.PropertyName == nameof(StackPrtNode.DisplayNextNodeFalse) ||
                 e.PropertyName == nameof(StackPrtNode.ScoreModeTrue) ||
                 e.PropertyName == nameof(StackPrtNode.ScoreModeFalse) ||
                 e.PropertyName == nameof(StackPrtNode.NodeScore) ||
                 e.PropertyName == nameof(StackPrtNode.Penalty) ||
                 e.PropertyName == nameof(StackPrtNode.Quiet))
        {
            UpdateWires();
        }
    }

    private readonly Dictionary<string, Point> _userNodePositions = new();

    public void SaveNodePosition(string nodeId, double x, double y)
    {
        _userNodePositions[nodeId] = new Point(x, y);
        var node = Prt.Nodes.FirstOrDefault(n => n.Id == nodeId || n.NodeId == nodeId);
        if (node != null)
        {
            _userNodePositions[node.Id] = new Point(x, y);
        }
    }

    public ObservableCollection<string> AvailableNodeNames { get; } = new();

    public void UpdateAvailableNodeNames()
    {
        var currentList = new List<string> { "Keine" };
        foreach (var node in Prt.Nodes)
        {
            string item = $"Node {node.NodeId}";
            if (!currentList.Contains(item))
            {
                currentList.Add(item);
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
            // Snapshot current positions of existing controls before clearing
            foreach (var gn in GraphNodes)
            {
                _userNodePositions[gn.Node.Id] = new Point(gn.X, gn.Y);
            }

            GraphNodes.Clear();
            GraphWires.Clear();
            UpdateAvailableNodeNames();
            if (!Prt.Nodes.Any()) return;

        double startX = 5000;
        double startY = 5000;
        double levelHeight = 220;
        double colWidth = 320;

        var positions = new Dictionary<string, (double X, double Y)>();

        // Layered DAG Layout:
        var levels = new Dictionary<string, int>();
        var desiredXMap = new Dictionary<string, double>();
        var rootNode = Prt.Nodes.FirstOrDefault();
        if (rootNode != null)
        {
            var queue = new Queue<StackPrtNode>();
            levels[rootNode.Id] = 0;
            desiredXMap[rootNode.Id] = startX;
            queue.Enqueue(rootNode);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                int currentLevel = levels[current.Id];
                double currentDesiredX = desiredXMap[current.Id];

                // WAHR (True) branch goes to the LEFT of parent (-colWidth / 2)
                var trueTarget = FindNodeModel(current.NextNodeTrue);
                if (trueTarget != null && (!levels.ContainsKey(trueTarget.Id) || levels[trueTarget.Id] < currentLevel + 1))
                {
                    levels[trueTarget.Id] = currentLevel + 1;
                    desiredXMap[trueTarget.Id] = currentDesiredX - colWidth / 2.0;
                    queue.Enqueue(trueTarget);
                }

                // FALSCH (False) branch goes to the RIGHT of parent (+colWidth / 2)
                var falseTarget = FindNodeModel(current.NextNodeFalse);
                if (falseTarget != null && (!levels.ContainsKey(falseTarget.Id) || levels[falseTarget.Id] < currentLevel + 1))
                {
                    levels[falseTarget.Id] = currentLevel + 1;
                    desiredXMap[falseTarget.Id] = currentDesiredX + colWidth / 2.0;
                    queue.Enqueue(falseTarget);
                }
            }
        }

        var nodesByLevel = new Dictionary<int, List<StackPrtNode>>();
        var unvisited = new List<StackPrtNode>();

        foreach (var node in Prt.Nodes)
        {
            if (levels.TryGetValue(node.Id, out int lvl))
            {
                if (!nodesByLevel.ContainsKey(lvl)) nodesByLevel[lvl] = new List<StackPrtNode>();
                nodesByLevel[lvl].Add(node);
            }
            else
            {
                unvisited.Add(node);
            }
        }

        // On each level, sort nodes from Left to Right based on desiredX!
        foreach (var kvp in nodesByLevel)
        {
            int lvl = kvp.Key;
            var list = kvp.Value.OrderBy(n => desiredXMap.TryGetValue(n.Id, out var dx) ? dx : 0).ToList();
            double totalWidth = (list.Count - 1) * colWidth;
            double leftX = startX - totalWidth / 2.0;

            for (int i = 0; i < list.Count; i++)
            {
                positions[list[i].Id] = (leftX + i * colWidth, startY + lvl * levelHeight);
            }
        }

        for (int i = 0; i < unvisited.Count; i++)
        {
            positions[unvisited[i].Id] = (startX + 600 + i * colWidth, startY);
        }

        SelectedGraphNodes.Clear();
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
                _userNodePositions[node.Id] = new Point(posX, posY);
            }
            else
            {
                _userNodePositions[node.Id] = new Point(posX, posY);
            }

            bool isSel = (SelectedNode != null && node.Id == SelectedNode.Id);
            var gNode = new PrtGraphNodeViewModel(node, posX, posY)
            {
                IsSelected = isSel
            };
            GraphNodes.Add(gNode);
            if (isSel)
            {
                SelectedGraphNodes.Add(gNode);
            }
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
            string trueScore = FormatBranchScore(gNode.Node.ScoreTrue, gNode.Node.ScoreModeTrue);
            string falseScore = FormatBranchScore(gNode.Node.ScoreFalse, gNode.Node.ScoreModeFalse);

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
        if (string.IsNullOrEmpty(target) || target == "-1" || target.Equals("Keine", StringComparison.OrdinalIgnoreCase)) return null;

        string normalized = target.StartsWith("Node ", StringComparison.OrdinalIgnoreCase) 
            ? target.Substring(5).Trim() 
            : target.Trim();

        var byNodeId = Prt.Nodes.FirstOrDefault(n => n.NodeId.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        if (byNodeId != null) return byNodeId;

        var byId = Prt.Nodes.FirstOrDefault(n => n.Id == target);
        if (byId != null) return byId;

        var byDesc = Prt.Nodes.FirstOrDefault(n => n.Description.Equals(target, StringComparison.OrdinalIgnoreCase));
        if (byDesc != null) return byDesc;

        if (int.TryParse(normalized, out int index) && index >= 1 && index <= Prt.Nodes.Count)
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
    public void ToggleFeedbackVariables()
    {
        if (IsBottomPaneExpanded && ActiveBottomPaneTab == PrtBottomPaneTab.FeedbackVariables)
        {
            IsBottomPaneExpanded = false;
        }
        else
        {
            ActiveBottomPaneTab = PrtBottomPaneTab.FeedbackVariables;
            IsBottomPaneExpanded = true;
        }
    }

    [RelayCommand]
    public void ToggleSettings()
    {
        if (IsBottomPaneExpanded && ActiveBottomPaneTab == PrtBottomPaneTab.Settings)
        {
            IsBottomPaneExpanded = false;
        }
        else
        {
            ActiveBottomPaneTab = PrtBottomPaneTab.Settings;
            IsBottomPaneExpanded = true;
        }
    }

    [RelayCommand]
    public void SelectFeedbackVariablesTab()
    {
        ActiveBottomPaneTab = PrtBottomPaneTab.FeedbackVariables;
        IsBottomPaneExpanded = true;
    }

    [RelayCommand]
    public void SelectSettingsTab()
    {
        ActiveBottomPaneTab = PrtBottomPaneTab.Settings;
        IsBottomPaneExpanded = true;
    }

    [RelayCommand]
    public void CloseBottomPane()
    {
        IsBottomPaneExpanded = false;
    }

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

        double viewportWidth = 800;
        double viewportHeight = 600;

        if (parameter is Size s && s.Width > 100 && s.Height > 100)
        {
            viewportWidth = s.Width;
            viewportHeight = s.Height;
        }
        else if (parameter is Rect r && r.Width > 100 && r.Height > 100)
        {
            viewportWidth = r.Width;
            viewportHeight = r.Height;
        }

        double minX = GraphNodes.Min(gn => gn.X);
        double maxX = GraphNodes.Max(gn => gn.X + gn.Width);
        double minY = GraphNodes.Min(gn => gn.Y);
        double maxY = GraphNodes.Max(gn => gn.Y + gn.Height);

        double treeCenterX = (minX + maxX) / 2.0;
        double treeCenterY = (minY + maxY) / 2.0;

        PanX = (viewportWidth / 2.0) - (treeCenterX * ZoomLevel);
        PanY = (viewportHeight / 2.0) - (treeCenterY * ZoomLevel);

        ClampPan(viewportWidth, viewportHeight);
    }

    public void ClampPan(double viewportWidth, double viewportHeight)
    {
        double scaledCanvasWidth = 10000 * ZoomLevel;
        double scaledCanvasHeight = 10000 * ZoomLevel;

        if (scaledCanvasWidth > viewportWidth)
        {
            double minPanX = viewportWidth - scaledCanvasWidth;
            double maxPanX = 0;
            PanX = Math.Clamp(PanX, minPanX, maxPanX);
        }
        else
        {
            PanX = (viewportWidth - scaledCanvasWidth) / 2.0;
        }

        if (scaledCanvasHeight > viewportHeight)
        {
            double minPanY = viewportHeight - scaledCanvasHeight;
            double maxPanY = 0;
            PanY = Math.Clamp(PanY, minPanY, maxPanY);
        }
        else
        {
            PanY = (viewportHeight - scaledCanvasHeight) / 2.0;
        }
    }

    public void ConnectBranch(StackPrtNode sourceNode, string branchType, StackPrtNode? targetNode)
    {
        string targetValue = targetNode != null ? targetNode.NodeId : "-1";
        if (branchType == "True") sourceNode.NextNodeTrue = targetValue;
        else if (branchType == "False") sourceNode.NextNodeFalse = targetValue;
        UpdateWires();
    }

    [RelayCommand]
    private void SelectNode(StackPrtNode? node) => SelectedNode = node;

    public void AddNodeAtPosition(double sceneX, double sceneY)
    {
        int nodeIndex = 1;
        while (Prt.Nodes.Any(n => n.NodeId == nodeIndex.ToString()))
        {
            nodeIndex++;
        }

        var newNode = new StackPrtNode
        {
            NodeId = nodeIndex.ToString(),
            Description = "Antwort korrekt?",
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

        string deletedNodeId = node.NodeId;
        string deletedName = node.DisplayName;
        _userNodePositions.Remove(node.Id);
        _userNodePositions.Remove(node.NodeId);
        Prt.Nodes.Remove(node);

        // Reset branch references pointing to the deleted node
        foreach (var remainingNode in Prt.Nodes)
        {
            if (remainingNode.NextNodeTrue == deletedNodeId || remainingNode.NextNodeTrue == deletedName)
            {
                remainingNode.NextNodeTrue = "-1";
            }
            if (remainingNode.NextNodeFalse == deletedNodeId || remainingNode.NextNodeFalse == deletedName)
            {
                remainingNode.NextNodeFalse = "-1";
            }
        }

        if (SelectedNode == node || SelectedNode?.Id == node.Id)
        {
            SelectedNode = null;
        }

        var selectedGNode = SelectedGraphNodes.FirstOrDefault(gn => gn.Node.Id == node.Id);
        if (selectedGNode != null)
        {
            SelectedGraphNodes.Remove(selectedGNode);
        }

        RebuildGraph();
    }
}
