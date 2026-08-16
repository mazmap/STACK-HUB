using System;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit.Highlighting;
using STACK_HUB.Services;
using STACK_HUB.ViewModels;

namespace STACK_HUB.Views;

public partial class MaximaEditor : UserControl
{
    public MaximaEditor()
    {
        InitializeComponent();
        Editor.SyntaxHighlighting = SyntaxHighlightingService.MaximaDefinition;
        Editor.TextArea.SelectionBrush = new SolidColorBrush(Color.Parse("#264F78"));
        Editor.TextArea.SelectionForeground = null;
        Editor.TextArea.SelectionBorder = null;
        
        Editor.TextChanged += (s, e) =>
        {
            if (DataContext is MaximaEditorViewModel vm && vm.Text != Editor.Text)
            {
                vm.Text = Editor.Text;
            }
        };
    }
    
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is MaximaEditorViewModel vm)
        {
            if (Editor.Text != vm.Text)
            {
                Editor.Text = vm.Text ?? string.Empty;
            }
            Editor.WordWrap = vm.WordWrap;
            Editor.ShowLineNumbers = vm.ShowLineNumbers;
            Editor.FontSize = vm.FontSize;
            Editor.HorizontalScrollBarVisibility = vm.WordWrap ? Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled : Avalonia.Controls.Primitives.ScrollBarVisibility.Auto;
        }
    }
}