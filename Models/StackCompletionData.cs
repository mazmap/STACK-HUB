using System;
using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;

namespace STACK_HUB.Editor;

public class StackCompletionData : ICompletionData
{
    public StackCompletionData(string text, string description = "")
    {
        Text = text;
        Description = description;
    }

    public IImage? Image => null;
    public string Text { get; }
    public object Content => Text;          // Text displayed in dropdown list
    public object Description
    {
        get; // Tooltip description
    }

    public double Priority => 0;

    // Called when the user presses Enter or double-clicks an item
    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, Text);
    }
}