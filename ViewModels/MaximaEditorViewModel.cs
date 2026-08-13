using CommunityToolkit.Mvvm.ComponentModel;
using STACK_HUB.Models;

namespace STACK_HUB.ViewModels;

public partial class MaximaEditorViewModel : ViewModelBase, ICacheablePane
{
    [ObservableProperty]
    public partial string Text { get; set; }

    public MaximaEditorViewModel(string initialText)
    {
        Text = initialText ?? string.Empty;
    }
}