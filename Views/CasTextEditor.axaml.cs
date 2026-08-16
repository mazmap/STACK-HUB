using System;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit.Highlighting;
using STACK_HUB.Services;
using STACK_HUB.ViewModels;

namespace STACK_HUB.Views;

public partial class CasTextEditor : UserControl
{
    public CasTextEditor()
    {
        InitializeComponent();
        Editor.SyntaxHighlighting = SyntaxHighlightingService.CasTextDefinition;
        Editor.TextArea.SelectionBrush = new SolidColorBrush(Color.Parse("#264F78"));
        Editor.TextArea.SelectionForeground = null;
        Editor.TextArea.SelectionBorder = null;
        
        Editor.TextChanged += (s, e) =>
        {
            if (DataContext is CasTextEditorViewModel vm && vm.Text != Editor.Text)
            {
                vm.Text = Editor.Text;
            }
        };
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is CasTextEditorViewModel vm)
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

    protected override Avalonia.Size MeasureOverride(Avalonia.Size availableSize)
    {
        if (double.IsInfinity(availableSize.Width))
        {
            if (Parent is Avalonia.Visual p && p.Bounds.Width > 0)
            {
                availableSize = new Avalonia.Size(p.Bounds.Width, availableSize.Height);
            }
        }
        return base.MeasureOverride(availableSize);
    }
}