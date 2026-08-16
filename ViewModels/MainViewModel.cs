using System.Collections.Generic;
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
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Workspace.OpenOrFocusPane(
            paneId: "content:variables", 
            title: "Question Variables", 
            contentViewModel: new MaximaEditorViewModel(ActiveQuestion.QuestionVariables));
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            sw.Stop();
            System.Console.WriteLine($"[MaximaEditor]: {sw.ElapsedMilliseconds} ms");
        }, DispatcherPriority.Render);
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
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            sw.Stop();
            System.Console.WriteLine($"[CasTextEditor]: {sw.ElapsedMilliseconds} ms");
        }, DispatcherPriority.Render);
    }

    [RelayCommand]
    private void OpenSpecificFeedback()
    {
        Workspace.OpenOrFocusPane("content:specific_feedback", "Specific Feedback", new CasTextEditorViewModel(ActiveQuestion.SpecificFeedback));
    }

    [RelayCommand]
    private void OpenGeneralFeedback()
    {
        Workspace.OpenOrFocusPane("content:general_feedback", "General Feedback", new CasTextEditorViewModel(ActiveQuestion.GeneralFeedback));
    }    
    // Dynamic ItemsControl items use their unique model ID
    [RelayCommand]
    private void SelectInput(StackInput input)
    {
        Workspace.OpenOrFocusPane($"input:{input.Id}", input.Name, input);
    }

    private readonly Dictionary<string, PrtEditorViewModel> _prtEditorCache = new();

    public MainViewModel()
    {
        foreach (var prt in ActiveQuestion.Prts)
        {
            _prtEditorCache[prt.Id] = new PrtEditorViewModel(prt);
        }
    }

    public PrtEditorViewModel GetPrtEditorViewModel(StackPrt prt)
    {
        if (!_prtEditorCache.TryGetValue(prt.Id, out var vm))
        {
            vm = new PrtEditorViewModel(prt);
            _prtEditorCache[prt.Id] = vm;
        }
        return vm;
    }

    [RelayCommand]
    private void SelectPrt(StackPrt prt)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var vm = GetPrtEditorViewModel(prt);
        Workspace.OpenOrFocusPane($"prt:{prt.Id}", prt.Name, vm);
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            sw.Stop();
            System.Console.WriteLine($"[PrtView]: {sw.ElapsedMilliseconds} ms");
        }, DispatcherPriority.Render);
    }
}
