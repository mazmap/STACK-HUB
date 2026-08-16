using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using STACK_HUB.Models;

namespace STACK_HUB.ViewModels;

public partial class PrtTreeNodeViewModel : ObservableObject
{
    public StackPrtNode Node { get; }

    [ObservableProperty]
    private PrtTreeNodeViewModel? _trueChild;

    [ObservableProperty]
    private PrtTreeNodeViewModel? _falseChild;

    public bool IsTrueStop => string.IsNullOrEmpty(Node.NextNodeTrue) || Node.NextNodeTrue == "-1";
    public bool IsFalseStop => string.IsNullOrEmpty(Node.NextNodeFalse) || Node.NextNodeFalse == "-1";

    public string TrueTargetText => IsTrueStop ? "End / Stop (-1)" : $"Node {Node.NextNodeTrue}";
    public string FalseTargetText => IsFalseStop ? "End / Stop (-1)" : $"Node {Node.NextNodeFalse}";

    public PrtTreeNodeViewModel(StackPrtNode node)
    {
        Node = node;
    }

    public void NotifyBranchChanged()
    {
        OnPropertyChanged(nameof(IsTrueStop));
        OnPropertyChanged(nameof(IsFalseStop));
        OnPropertyChanged(nameof(TrueTargetText));
        OnPropertyChanged(nameof(FalseTargetText));
    }
}
