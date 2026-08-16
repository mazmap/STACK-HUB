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

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System.Threading.Tasks;
using STACK_HUB.Services;

namespace STACK_HUB.ViewModels;

public enum AddDialogTarget
{
    Input,
    Prt
}

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private StackQuestion _activeQuestion = new();

    public DockingWorkspaceModel Workspace { get; } = new();

    public Func<Task>? RequestOpenFilePickerAsync { get; set; }

    [RelayCommand]
    public async Task OpenQuestionFile()
    {
        if (RequestOpenFilePickerAsync != null)
        {
            await RequestOpenFilePickerAsync.Invoke();
        }
    }

    [RelayCommand]
    public void CloseApplication()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    public void LoadQuestion(StackQuestion question)
    {
        ActiveQuestion = question;
        _prtEditorCache.Clear();
        _inputEditorCache.Clear();
        _generalSettingsViewModel = null;
        _maximaSettingsViewModel = null;
        _feedbackSettingsViewModel = null;

        foreach (var input in question.Inputs)
        {
            _inputEditorCache[input.Id] = new InputEditorViewModel(input);
        }

        foreach (var prt in question.Prts)
        {
            _prtEditorCache[prt.Id] = new PrtEditorViewModel(prt);
        }

        OpenGeneralSettings();
    }

    public void LoadQuestionXml(string xmlContent)
    {
        var question = MoodleXmlService.ParseQuestion(xmlContent);
        LoadQuestion(question);
    }

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

    private GeneralSettingsViewModel? _generalSettingsViewModel;

    public GeneralSettingsViewModel GetGeneralSettingsViewModel()
    {
        _generalSettingsViewModel ??= new GeneralSettingsViewModel(ActiveQuestion);
        return _generalSettingsViewModel;
    }

    // Fixed panel with explicit ID string key
    [RelayCommand]
    private void OpenGeneralSettings()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var vm = GetGeneralSettingsViewModel();
        Workspace.OpenOrFocusPane("settings:general", "General Settings", vm);
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            sw.Stop();
            System.Console.WriteLine($"[GeneralSettingsView]: {sw.ElapsedMilliseconds} ms");
        }, DispatcherPriority.Render);
    }
    
    private MaximaSettingsViewModel? _maximaSettingsViewModel;

    public MaximaSettingsViewModel GetMaximaSettingsViewModel()
    {
        _maximaSettingsViewModel ??= new MaximaSettingsViewModel(ActiveQuestion);
        return _maximaSettingsViewModel;
    }

    [RelayCommand]
    private void OpenMaximaSettings()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var vm = GetMaximaSettingsViewModel();
        Workspace.OpenOrFocusPane("settings:maxima", "Maxima Settings", vm);
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            sw.Stop();
            System.Console.WriteLine($"[MaximaSettingsView]: {sw.ElapsedMilliseconds} ms");
        }, DispatcherPriority.Render);
    }

    private FeedbackSettingsViewModel? _feedbackSettingsViewModel;

    public FeedbackSettingsViewModel GetFeedbackSettingsViewModel()
    {
        _feedbackSettingsViewModel ??= new FeedbackSettingsViewModel(ActiveQuestion);
        return _feedbackSettingsViewModel;
    }

    [RelayCommand]
    private void OpenFeedbackSettings()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var vm = GetFeedbackSettingsViewModel();
        Workspace.OpenOrFocusPane("settings:feedback", "Feedback Settings", vm);
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            sw.Stop();
            System.Console.WriteLine($"[FeedbackSettingsView]: {sw.ElapsedMilliseconds} ms");
        }, DispatcherPriority.Render);
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
    private readonly Dictionary<string, InputEditorViewModel> _inputEditorCache = new();
    private readonly Dictionary<string, PrtEditorViewModel> _prtEditorCache = new();

    public MainViewModel()
    {
        foreach (var input in ActiveQuestion.Inputs)
        {
            _inputEditorCache[input.Id] = new InputEditorViewModel(input);
        }

        foreach (var prt in ActiveQuestion.Prts)
        {
            _prtEditorCache[prt.Id] = new PrtEditorViewModel(prt);
        }
    }

    public InputEditorViewModel GetInputEditorViewModel(StackInput input)
    {
        if (!_inputEditorCache.TryGetValue(input.Id, out var vm))
        {
            vm = new InputEditorViewModel(input);
            _inputEditorCache[input.Id] = vm;
        }
        return vm;
    }

    // Dynamic ItemsControl items use their unique model ID
    [RelayCommand]
    private void SelectInput(StackInput input)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var vm = GetInputEditorViewModel(input);
        Workspace.OpenOrFocusPane($"input:{input.Id}", input.Name, vm);
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            sw.Stop();
            System.Console.WriteLine($"[InputEditorView]: {sw.ElapsedMilliseconds} ms");
        }, DispatcherPriority.Render);
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
