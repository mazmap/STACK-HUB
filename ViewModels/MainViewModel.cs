using Avalonia.Layout;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using STACK_HUB.Docking.Models;
using STACK_HUB.Docking.Services;
using STACK_HUB.Models;

namespace STACK_HUB.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public StackQuestion ActiveQuestion { get; set; } = new();
    public DockingWorkspaceModel Workspace { get; } = new();

    // Fixed panel with explicit ID string key
    [RelayCommand]
    private void OpenGeneralSettings()
    {
        Workspace.OpenOrFocusPane("settings:general", "General Settings", "General Settings Content");
    }
    
    [RelayCommand]
    private void OpenMaximaSettings()
    {
        Workspace.OpenOrFocusPane("settings:maxima", "Maxima Settings", "Maxima Settings Content");
    }

    [RelayCommand]
    private void OpenQuestionVariables()
    {
        Workspace.OpenOrFocusPane("content:variables", "Question Variables", "Question Variables Editor Content");
    }
    
    [RelayCommand]
    private void OpenQuestionText()
    {
        Workspace.OpenOrFocusPane(
            paneId: "content:question_text",
            title: "Question Text",
            contentViewModel: new CASTextEditorViewModel(
                ActiveQuestion.QuestionText,
                updatedText => ActiveQuestion.QuestionText = updatedText
            )
        );
    }
    // Dynamic ItemsControl items use their unique model ID
    [RelayCommand]
    private void SelectInput(StackInput input)
    {
        Workspace.OpenOrFocusPane($"input:{input.Id}", input.Name, input);
    }

    [RelayCommand]
    private void SelectPrt(StackPrt prt)
    {
        Workspace.OpenOrFocusPane($"prt:{prt.Id}", prt.Name, prt);
    }
}
