using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Highlighting;
using STACK_HUB.Editor;
using STACK_HUB.ViewModels;

namespace STACK_HUB.Views;

public partial class CASTextEditor : UserControl
{
    private CompletionWindow? _completionWindow;

    public CASTextEditor()
    {
        InitializeComponent();
        Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("HTML");
        // Load theme from Assets/your_theme.icls
        try
        {
            var uri = new Uri("avares://STACK-HUB/Assets/Rider_Islands_Dark.icls");
            using (var stream = AssetLoader.Open(uri))
            {
                IclsThemeLoader.ApplyTheme(Editor, stream);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not load theme asset: {ex.Message}");
        }
        
        Editor.TextArea.TextEntered += TextArea_TextEntered;
        Editor.TextArea.TextEntering += TextArea_TextEntering;
        // Set editor content on load & DataContext change
        DataContextChanged += (s, e) => LoadViewModelText();
        Loaded += (s, e) => LoadViewModelText();
        Editor.TextChanged += (s, e) =>
        {
            if (DataContext is CASTextEditorViewModel vm && vm.Text != Editor.Text)
            {
                vm.Text = Editor.Text;
            }
        };
    }
    private void LoadViewModelText()
    {
        if (DataContext is CASTextEditorViewModel vm && Editor.Text != vm.Text)
        {
            Editor.Text = vm.Text;
        }
    }
    private void TextArea_TextEntered(object? sender, TextInputEventArgs e)
    {
        if (e.Text == "<" || e.Text == "[")
        {
            _completionWindow = new CompletionWindow(Editor.TextArea);
            var data = _completionWindow.CompletionList.CompletionData;

            if (e.Text == "<")
            {
                data.Add(new StackCompletionData("p>", "Paragraph block"));
                data.Add(new StackCompletionData("div class=\"option\">", "Option div block"));
                data.Add(new StackCompletionData("b>", "Bold text"));
            }
            else if (e.Text == "[")
            {
                data.Add(new StackCompletionData("[input:ans1]]", "STACK Input Box"));
                data.Add(new StackCompletionData("[validation:ans1]]", "STACK Validation Feedback"));
                data.Add(new StackCompletionData("[feedback:prt1]]", "STACK PRT Feedback"));
            }

            _completionWindow.Show();
            _completionWindow.Closed += (s, args) => _completionWindow = null;
        }
    }

    private void TextArea_TextEntering(object? sender, TextInputEventArgs e)
    {
        if (e.Text?.Length > 0 && _completionWindow != null)
        {
            if (!char.IsLetterOrDigit(e.Text[0]) && e.Text[0] != ':')
            {
                _completionWindow.CompletionList.CompletionData.Clear();
            }
        }
    }
}
