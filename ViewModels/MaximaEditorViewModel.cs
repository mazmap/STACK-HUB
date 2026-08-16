using CommunityToolkit.Mvvm.ComponentModel;
using STACK_HUB.Models;

namespace STACK_HUB.ViewModels;

public partial class MaximaEditorViewModel : ViewModelBase, ICacheablePane
{
    [ObservableProperty]
    public partial string Text { get; set; }

    [ObservableProperty]
    private bool _wordWrap = true;

    [ObservableProperty]
    private bool _showLineNumbers = true;

    [ObservableProperty]
    private double _fontSize = 13.0;

    public MaximaEditorViewModel(string initialText, bool wordWrap = true, bool showLineNumbers = true, double fontSize = 13.0)
    {
        Text = initialText ?? string.Empty;
        WordWrap = wordWrap;
        ShowLineNumbers = showLineNumbers;
        FontSize = fontSize;
    }
}