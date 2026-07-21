using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using STACK_HUB.Docking.Models;
using STACK_HUB.Docking.Views;

namespace STACK_HUB.Docking.Templates;

public class LayoutTemplateSelector : IDataTemplate
{
    public bool Match(object? data)
    {
        return data is LayoutNode || data is string;
    }

    public Control? Build(object? param)
    {
        return param switch
        {
            SplitNode split => new SplitNodeControl { DataContext = split },
            TabGroupNode tabGroup => new TabGroupControl { DataContext = tabGroup },
            PaneNode pane => new ContentControl
            {
                [!ContentControl.ContentProperty] = new Binding(nameof(PaneNode.ContentViewModel)),
                ContentTemplate = this
            },
            string text => new TextBlock { Text = text, Margin = new Avalonia.Thickness(8) },
            _ => null
        };
    }
}