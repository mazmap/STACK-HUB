using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace STACK_HUB.Models;

public class StackQuestion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "New STACK Question";
    public double DefaultGrade { get; set; } = 1.0;
    public double Penalty { get; set; } = 0.1;
    public string QuestionVariables { get; set; } = "/* Define question variables in Maxima here */\nvars: [x, y];\nexpr: 3*x^2 + 5*y;\nmodel_ans: 3*x^2 + 5*y;";
    public string QuestionText { get; set; } = "<!-- STYLING -->\n<style>\n.stack .option {\n    margin: .7em 0;\n    background: rgba(255, 255, 255, 0.05);\n    padding: 7px 10px;\n}\n</style>\n\n<!-- QUESTION TEXT -->\n<p>Consider the mathematical expression \\( A = {@expr@} \\).</p>\n<p>Please enter the expression in the input box below:</p>\n\n[[input:ans1]] [[validation:ans1]]\n\n[[feedback:prt1]]";
    public ObservableCollection<StackInput> Inputs { get; set; } = new();
    public string SpecificFeedback { get; set; } = "[[feedback:prt1]]";
    public string GeneralFeedback { get; set; } = "<p>Here is the worked solution... the correct answer was indeed \\( {@model_ans@} \\).</p>";
    public ObservableCollection<StackPrt> Prts { get; set; } = new();

    public StackQuestion()
    {
        // Default seeding
        Inputs.Add(new StackInput { Name = "ans1", ModelAnswer = "model_ans" });
        
        var defaultPrt = new StackPrt { Name = "prt1" };
        defaultPrt.Nodes.Add(new StackPrtNode
        {
            Name = "Node 1",
            AnswerTest = "AlgEquiv",
            StudentAnswer = "ans1",
            TeacherAnswer = "model_ans",
            NextNodeTrue = "-1",
            NextNodeFalse = "-1",
            TrueFeedback = "<p>Correct! Your answer is algebraically equivalent.</p>",
            FalseFeedback = "<p>Incorrect. Your answer is not equivalent.</p>"
        });
        Prts.Add(defaultPrt);
    }
}

public class StackInput
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "ans1";
    public string Type { get; set; } = "algebraic"; // algebraic, boolean, checkbox, dropdown, matrix, numerical
    public string ModelAnswer { get; set; } = "model_ans";
    public int BoxSize { get; set; } = 15;
    public string SyntaxHint { get; set; } = "";
    public bool SyntaxCheck { get; set; } = true;
    public bool ShowValidation { get; set; } = true;
    public string ExtraOptions { get; set; } = "";
}

public partial class StackPrt : ObservableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _name = "prt1";

    [ObservableProperty]
    private double _value = 1.0;

    [ObservableProperty]
    private string _feedbackStyle = "Standard"; // Standard, Compact, None

    public ObservableCollection<StackPrtNode> Nodes { get; set; } = new();
}

public partial class StackPrtNode : ObservableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MaximaCheck))]
    private string _name = "Node 1";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MaximaCheck))]
    private string _answerTest = "AlgEquiv";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MaximaCheck))]
    private string _studentAnswer = "ans1";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MaximaCheck))]
    private string _teacherAnswer = "model_ans";

    [ObservableProperty]
    private string _testOptions = "";

    [ObservableProperty]
    private bool _quiet;

    [ObservableProperty]
    private double _nodeScore = 1.0;

    [ObservableProperty]
    private double _penalty = 0.1;

    [ObservableProperty]
    private string _nextNodeTrue = "-1"; // "-1" for Stop, otherwise Node number

    [ObservableProperty]
    private string _nextNodeFalse = "-1";

    [ObservableProperty]
    private string _trueFeedback = "";

    [ObservableProperty]
    private string _falseFeedback = "";

    public string MaximaCheck => $"{AnswerTest}({StudentAnswer}, {TeacherAnswer})";
}
