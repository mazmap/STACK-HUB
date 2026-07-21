using System;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using STACK_HUB.Docking.Models;
using STACK_HUB.Docking.Templates; // Make sure this is imported

namespace STACK_HUB.Docking.Views;

public class SplitNodeControl : Grid
{
    private static readonly LayoutTemplateSelector Selector = new();

    public SplitNodeControl()
    {
        this.DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not SplitNode split) return;

        Children.Clear();
        ColumnDefinitions.Clear();
        RowDefinitions.Clear();

        // Host controls for the two children with explicit ContentTemplate assigned
        var firstHost = new ContentControl 
        { 
            [!ContentControl.ContentProperty] = new Binding(nameof(SplitNode.FirstChild)),
            ContentTemplate = Selector 
        };
        
        var secondHost = new ContentControl 
        { 
            [!ContentControl.ContentProperty] = new Binding(nameof(SplitNode.SecondChild)),
            ContentTemplate = Selector 
        };

        var splitter = new GridSplitter();

        if (split.Orientation == Orientation.Horizontal)
        {
            ColumnDefinitions.Add(new ColumnDefinition(new GridLength(split.Ratio, GridUnitType.Star)));
            ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1.0 - split.Ratio, GridUnitType.Star)));

            Grid.SetColumn(firstHost, 0);
            Grid.SetColumn(splitter, 1);
            Grid.SetColumn(secondHost, 2);

            splitter.Width = 4;
            splitter.HorizontalAlignment = HorizontalAlignment.Center;
        }
        else
        {
            RowDefinitions.Add(new RowDefinition(new GridLength(split.Ratio, GridUnitType.Star)));
            RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            RowDefinitions.Add(new RowDefinition(new GridLength(1.0 - split.Ratio, GridUnitType.Star)));

            Grid.SetRow(firstHost, 0);
            Grid.SetRow(splitter, 1);
            Grid.SetRow(secondHost, 2);

            splitter.Height = 4;
            splitter.VerticalAlignment = VerticalAlignment.Center;
        }

        Children.Add(firstHost);
        Children.Add(splitter);
        Children.Add(secondHost);
    }
}