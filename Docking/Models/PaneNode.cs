using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace STACK_HUB.Docking.Models;

public partial class PaneNode : LayoutNode
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();
    
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private object? _contentViewModel;

    // Action delegate injected from outside or set directly
    public Action<PaneNode>? OnCloseRequested { get; set; }

    [RelayCommand]
    private void Close()
    {
        OnCloseRequested?.Invoke(this);
    }
}