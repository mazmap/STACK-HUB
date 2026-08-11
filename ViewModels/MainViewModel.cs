using Avalonia.Layout;
using Avalonia.Threading;
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
        Workspace.OpenOrFocusPane("content:variables", "Question Variables", new CasTextEditorViewModel(ActiveQuestion.QuestionVariables));
    }
    
    [RelayCommand]
    private void OpenQuestionText()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Workspace.OpenOrFocusPane(
            paneId: "content:question_text",
            title: "Question Text",
            contentViewModel: new CasTextEditorViewModel(ActiveQuestion.QuestionText)
        );
        // 🚀 Measure until Avalonia completes layout & drawing on screen
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            sw.Stop();
            System.Console.WriteLine($"⏱️ [FULL ON-SCREEN RENDER]: {sw.ElapsedMilliseconds} ms");
        }, DispatcherPriority.Render);
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
