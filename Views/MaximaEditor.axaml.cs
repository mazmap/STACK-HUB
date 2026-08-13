using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaEdit.TextMate;
using STACK_HUB.Services;
using STACK_HUB.ViewModels;
using TextMateSharp.Grammars;

namespace STACK_HUB.Views;

public partial class MaximaEditor : UserControl
{
    public MaximaEditor()
    {
        InitializeComponent();
        var textMateInstallation = Editor.InstallTextMate(TextMateService.Instance);
        textMateInstallation.SetGrammar(TextMateService.Instance.GetScopeByLanguageId("maxima"));
    }
    
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is MaximaEditorViewModel vm && Editor.Text != vm.Text)
        {
            Editor.Text = vm.Text ?? string.Empty;
        }
    }
}