using System;
using Avalonia.Controls;
using AvaloniaEdit.TextMate;
using STACK_HUB.Services;
using STACK_HUB.ViewModels;

namespace STACK_HUB.Views;

public partial class CasTextEditor : UserControl
{
    public CasTextEditor()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        InitializeComponent();
        var textMateInstallation = Editor.InstallTextMate(TextMateService.Instance);
        var htmlLanguage = TextMateService.Instance.GetLanguageByExtension(".html");
        textMateInstallation.SetGrammar(TextMateService.Instance.GetScopeByLanguageId(htmlLanguage.Id));
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is CasTextEditorViewModel vm && Editor.Text != vm.Text)
        {
            Editor.Text = vm.Text ?? string.Empty;
        }
    }
}