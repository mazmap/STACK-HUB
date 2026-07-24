using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
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

        var splitter = new SleekGridSplitter
        {
            Background = Brushes.Black, 
        };

        if (split.Orientation == Orientation.Horizontal)
        {
            ColumnDefinitions.Add(new ColumnDefinition(new GridLength(split.Ratio, GridUnitType.Star)));
            ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1.0 - split.Ratio, GridUnitType.Star)));

            Grid.SetColumn(firstHost, 0);
            Grid.SetColumn(splitter, 1);
            Grid.SetColumn(secondHost, 2);

            splitter.HorizontalAlignment = HorizontalAlignment.Center;
            splitter.VerticalAlignment = VerticalAlignment.Stretch;
            splitter.ResizeDirection = GridResizeDirection.Columns;
        }
        else
        {
            RowDefinitions.Add(new RowDefinition(new GridLength(split.Ratio, GridUnitType.Star)));
            RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            RowDefinitions.Add(new RowDefinition(new GridLength(1.0 - split.Ratio, GridUnitType.Star)));

            Grid.SetRow(firstHost, 0);
            Grid.SetRow(splitter, 1);
            Grid.SetRow(secondHost, 2);

            splitter.VerticalAlignment = VerticalAlignment.Center;
            splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            splitter.ResizeDirection = GridResizeDirection.Rows;
        }

        Children.Add(firstHost);
        Children.Add(splitter);
        Children.Add(secondHost);
    }
}