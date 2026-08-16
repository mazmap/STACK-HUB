using System;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using STACK_HUB.Models;

namespace STACK_HUB.ViewModels;

public partial class GeneralSettingsViewModel : ViewModelBase, ICacheablePane
{
    public StackQuestion Question { get; }

    public GeneralSettingsViewModel(StackQuestion question)
    {
        Question = question;
        _editingDefaultGrade = question.DefaultGrade.ToString("0.##", CultureInfo.InvariantCulture);
        _descriptionEditor = new CasTextEditorViewModel(question.QuestionDescription, wordWrap: true);
        _descriptionEditor.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(CasTextEditorViewModel.Text))
            {
                question.QuestionDescription = _descriptionEditor.Text;
            }
        };

        _questionNoteEditor = new CasTextEditorViewModel(question.QuestionNote, wordWrap: true);
        _questionNoteEditor.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(CasTextEditorViewModel.Text))
            {
                question.QuestionNote = _questionNoteEditor.Text;
            }
        };
    }

    [ObservableProperty]
    private CasTextEditorViewModel _descriptionEditor;

    [ObservableProperty]
    private CasTextEditorViewModel _questionNoteEditor;

    [ObservableProperty]
    private string _editingDefaultGrade = "3";

    partial void OnEditingDefaultGradeChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        string norm = value.Trim().Replace(',', '.');
        if (double.TryParse(norm, NumberStyles.Float, CultureInfo.InvariantCulture, out double grade) && grade >= 0)
        {
            Question.DefaultGrade = grade;
        }
    }

    [ObservableProperty]
    private string _editingPenalty = "0.1";

    partial void OnEditingPenaltyChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        string norm = value.Trim().Replace(',', '.');
        if (double.TryParse(norm, NumberStyles.Float, CultureInfo.InvariantCulture, out double pen) && pen >= 0)
        {
            Question.Penalty = pen;
        }
    }
}
