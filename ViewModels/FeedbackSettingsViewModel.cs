using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using STACK_HUB.Models;

namespace STACK_HUB.ViewModels;

public class HintItemViewModel : ViewModelBase
{
    public CasTextEditorViewModel Editor { get; }
    public int Index { get; set; }

    public HintItemViewModel(string initialText, int index, System.Action<string> onTextChanged)
    {
        Index = index;
        Editor = new CasTextEditorViewModel(initialText, wordWrap: true);
        Editor.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(CasTextEditorViewModel.Text))
            {
                onTextChanged(Editor.Text);
            }
        };
    }
}

public partial class FeedbackSettingsViewModel : ViewModelBase, ICacheablePane
{
    public StackQuestion Question { get; }

    [ObservableProperty]
    private CasTextEditorViewModel _correctFeedbackEditor;

    [ObservableProperty]
    private CasTextEditorViewModel _partiallyCorrectFeedbackEditor;

    [ObservableProperty]
    private CasTextEditorViewModel _incorrectFeedbackEditor;

    public ObservableCollection<HintItemViewModel> HintEditors { get; } = new();

    public FeedbackSettingsViewModel(StackQuestion question)
    {
        Question = question;

        _correctFeedbackEditor = new CasTextEditorViewModel(question.CorrectFeedback, wordWrap: true);
        _correctFeedbackEditor.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(CasTextEditorViewModel.Text))
            {
                question.CorrectFeedback = _correctFeedbackEditor.Text;
            }
        };

        _partiallyCorrectFeedbackEditor = new CasTextEditorViewModel(question.PartiallyCorrectFeedback, wordWrap: true);
        _partiallyCorrectFeedbackEditor.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(CasTextEditorViewModel.Text))
            {
                question.PartiallyCorrectFeedback = _partiallyCorrectFeedbackEditor.Text;
            }
        };

        _incorrectFeedbackEditor = new CasTextEditorViewModel(question.IncorrectFeedback, wordWrap: true);
        _incorrectFeedbackEditor.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(CasTextEditorViewModel.Text))
            {
                question.IncorrectFeedback = _incorrectFeedbackEditor.Text;
            }
        };

        RebuildHintEditors();
    }

    private void RebuildHintEditors()
    {
        HintEditors.Clear();
        for (int i = 0; i < Question.Hints.Count; i++)
        {
            int capturedIdx = i;
            var hintVm = new HintItemViewModel(Question.Hints[i], i + 1, (newText) =>
            {
                if (capturedIdx < Question.Hints.Count)
                {
                    Question.Hints[capturedIdx] = newText;
                }
            });
            HintEditors.Add(hintVm);
        }
    }

    [RelayCommand]
    private void AddHint()
    {
        string newHint = "Hinweis...";
        Question.Hints.Add(newHint);
        RebuildHintEditors();
    }

    [RelayCommand]
    private void RemoveHint(HintItemViewModel? item)
    {
        if (item == null) return;
        int idx = HintEditors.IndexOf(item);
        if (idx >= 0 && idx < Question.Hints.Count)
        {
            Question.Hints.RemoveAt(idx);
            RebuildHintEditors();
        }
    }
}
