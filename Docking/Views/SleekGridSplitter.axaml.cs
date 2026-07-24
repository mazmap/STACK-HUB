using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace STACK_HUB.Docking.Views;

[PseudoClasses(":horizontal", ":vertical")]
public partial class SleekGridSplitter : GridSplitter
{
    public SleekGridSplitter()
    {
        InitializeComponent();
        UpdatePseudoClasses();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ResizeDirectionProperty) 
        {
            UpdatePseudoClasses();
        }
    }

    private void UpdatePseudoClasses()
    {
        // A vertical bar splitting left/right columns stretches vertically
        bool isHorizontalSplitter = ResizeDirection == GridResizeDirection.Columns;

        PseudoClasses.Set(":horizontal", isHorizontalSplitter);
        PseudoClasses.Set(":vertical", !isHorizontalSplitter);
    }
}