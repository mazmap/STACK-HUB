using System;
using Avalonia.Data;
using STACK_HUB.Docking.Models;
using Avalonia.Controls;
using Avalonia.Layout;

namespace STACK_HUB.Docking.Views;
public class SplitNodeControl : Grid
{
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

        // Host controls for the two children
        var firstHost = new ContentControl { [!ContentControl.ContentProperty] = new Binding(nameof(SplitNode.FirstChild)) };
        var secondHost = new ContentControl { [!ContentControl.ContentProperty] = new Binding(nameof(SplitNode.SecondChild)) };
        var splitter = new GridSplitter();

        if (split.Orientation == Orientation.Horizontal)
        {
            // Left | Splitter | Right
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
            // Top / Splitter / Bottom
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