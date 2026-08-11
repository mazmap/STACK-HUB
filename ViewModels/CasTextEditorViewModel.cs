using System;
using CommunityToolkit.Mvvm.ComponentModel;
using STACK_HUB.Models;

namespace STACK_HUB.ViewModels;

public partial class CasTextEditorViewModel : ViewModelBase, ICacheablePane
{
    [ObservableProperty]
    public partial string Text { get; set; }

    public CasTextEditorViewModel(string initialText)
    {
        Text = initialText ?? string.Empty;
    }
}