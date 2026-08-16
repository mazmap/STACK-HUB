using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Layout;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using STACK_HUB.Docking.Models;
using STACK_HUB.Docking.Services;
using STACK_HUB.Models;

namespace STACK_HUB.ViewModels;

public enum AddDialogTarget
{
    Input,
    Prt
}

public partial class MainViewModel : ObservableObject
{
    public StackQuestion ActiveQuestion { get; set; } = new();
    public DockingWorkspaceModel Workspace { get; } = new();

    [ObservableProperty]
    private bool _isAddDialogOpen;

    [ObservableProperty]
    private AddDialogTarget _addDialogType;

    [ObservableProperty]
    private string _addDialogTitle = string.Empty;

    [ObservableProperty]
    private string _addDialogPrompt = string.Empty;

    [ObservableProperty]
    private string _addDialogName = string.Empty;

    [ObservableProperty]
    private string? _addDialogErrorMessage;

    [RelayCommand]
    public void OpenAddInputDialog()
    {
        int index = 1;
        while (ActiveQuestion.Inputs.Any(i => string.Equals(i.Name, $"ans{index}", StringComparison.OrdinalIgnoreCase)))
        {
            index++;
        }

        AddDialogType = AddDialogTarget.Input;
        AddDialogTitle = "Add New Input";
        AddDialogPrompt = "Enter input name:";
        AddDialogName = $"ans{index}";
        AddDialogErrorMessage = null;
        IsAddDialogOpen = true;
    }

    [RelayCommand]
    public void OpenAddPrtDialog()
    {
        int index = 1;
        while (ActiveQuestion.Prts.Any(p => string.Equals(p.Name, $"prt{index}", StringComparison.OrdinalIgnoreCase)))
        {
            index++;
        }

        AddDialogType = AddDialogTarget.Prt;
        AddDialogTitle = "Add New PRT";
        AddDialogPrompt = "Enter PRT name:";
        AddDialogName = $"prt{index}";
        AddDialogErrorMessage = null;
        IsAddDialogOpen = true;
    }

    [RelayCommand]
    public void ConfirmAddDialog()
    {
        string name = AddDialogName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            AddDialogErrorMessage = "Name cannot be empty.";
            return;
        }

        if (AddDialogType == AddDialogTarget.Input)
        {
            if (ActiveQuestion.Inputs.Any(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                AddDialogErrorMessage = $"An input named '{name}' already exists.";
                return;
            }

            var newInput = new StackInput
            {
                Name = name,
                ModelAnswer = "model_ans"
            };
            ActiveQuestion.Inputs.Add(newInput);
            IsAddDialogOpen = false;
            AddDialogErrorMessage = null;
            SelectInput(newInput);
        }
        else if (AddDialogType == AddDialogTarget.Prt)
        {
            if (ActiveQuestion.Prts.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                AddDialogErrorMessage = $"A PRT named '{name}' already exists.";
                return;
            }

            var newPrt = new StackPrt
            {
                Name = name
            };
            newPrt.Nodes.Add(new StackPrtNode
            {
                NodeId = "1",
                Description = "Antwort korrekt?",
                AnswerTest = "AlgEquiv",
                StudentAnswer = "sans1",
                TeacherAnswer = "tans1",
                NextNodeTrue = "-1",
                NextNodeFalse = "-1",
                TrueFeedback = "<p>Correct!</p>",
                FalseFeedback = "<p>Incorrect.</p>"
            });
            ActiveQuestion.Prts.Add(newPrt);
            _prtEditorCache[newPrt.Id] = new PrtEditorViewModel(newPrt);
            IsAddDialogOpen = false;
            AddDialogErrorMessage = null;
            SelectPrt(newPrt);
        }
    }

    [RelayCommand]
    public void CancelAddDialog()
    {
        IsAddDialogOpen = false;
        AddDialogErrorMessage = null;
    }

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
