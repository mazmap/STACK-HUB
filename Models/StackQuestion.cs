using System;
using System.Collections.ObjectModel;
using System.Linq;
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
        
        // Seed 7 Connected Nodes spanning 4 tiers to demonstrate DAG auto-layout & multi-branch evaluation
        // Node 1: Primary Algebraic Equivalence Check
        defaultPrt.Nodes.Add(new StackPrtNode
        {
            NodeId = "1",
            Description = "Antwort korrekt?",
            AnswerTest = "AlgEquiv",
            StudentAnswer = "sans1",
            TeacherAnswer = "tans1",
            ScoreModeTrue = "Set to",
            ScoreTrue = 0.5,
            NextNodeTrue = "3",
            ScoreModeFalse = "Set to",
            ScoreFalse = 0.0,
            NextNodeFalse = "2",
            TrueFeedback = "<p>Correct! Answer is algebraically equivalent. Checking simplification...</p>",
            FalseFeedback = "<p>Incorrect. Checking for common sign error...</p>"
        });

        // Node 2: Sign Error Check (with Quiet / dashed border as in mockup)
        defaultPrt.Nodes.Add(new StackPrtNode
        {
            NodeId = "2",
            Description = "Antwort korrekt?",
            AnswerTest = "AlgEquiv",
            StudentAnswer = "sans1",
            TeacherAnswer = "tans1",
            ScoreModeTrue = "Set to",
            ScoreTrue = 0.5,
            NextNodeTrue = "-1",
            ScoreModeFalse = "Set to",
            ScoreFalse = 0.0,
            NextNodeFalse = "4",
            Quiet = true,
            TrueFeedback = "<p>Sign error detected. Partial credit awarded.</p>",
            FalseFeedback = "<p>Checking for differentiation instead of integration...</p>"
        });

        // Node 3: Lowest Terms Check (Empty Description to showcase mockup no-description compact format!)
        defaultPrt.Nodes.Add(new StackPrtNode
        {
            NodeId = "3",
            Description = "",
            AnswerTest = "AlgEquiv",
            StudentAnswer = "sans1",
            TeacherAnswer = "tans1",
            ScoreModeTrue = "Set to",
            ScoreTrue = 1.0,
            NextNodeTrue = "5",
            ScoreModeFalse = "Set to",
            ScoreFalse = 0.8,
            NextNodeFalse = "-1",
            TrueFeedback = "<p>Answer is fully simplified.</p>",
            FalseFeedback = "<p>Your answer is equivalent but not in lowest terms.</p>"
        });

        // Node 4: Derivative Misconception Check
        defaultPrt.Nodes.Add(new StackPrtNode
        {
            NodeId = "4",
            Description = "Ableitung statt Integral",
            AnswerTest = "AlgEquiv",
            StudentAnswer = "ans1",
            TeacherAnswer = "diff(model_ans, x)",
            ScoreModeTrue = "Set to",
            ScoreTrue = 0.2,
            NextNodeTrue = "6",
            ScoreModeFalse = "Set to",
            ScoreFalse = 0.0,
            NextNodeFalse = "7",
            TrueFeedback = "<p>It appears you differentiated instead of integrating.</p>",
            FalseFeedback = "<p>Checking fallback structure...</p>"
        });

        // Node 5: Factored Form Check
        defaultPrt.Nodes.Add(new StackPrtNode
        {
            NodeId = "5",
            Description = "Faktorisierte Form",
            AnswerTest = "FacForm",
            StudentAnswer = "ans1",
            TeacherAnswer = "model_ans",
            ScoreModeTrue = "Set to",
            ScoreTrue = 1.0,
            NextNodeTrue = "-1",
            ScoreModeFalse = "Set to",
            ScoreFalse = 0.9,
            NextNodeFalse = "-1",
            TrueFeedback = "<p>Perfect! Answer is completely factored.</p>",
            FalseFeedback = "<p>Good, but answer could be factored further.</p>"
        });

        // Node 6: Missing Constant Check
        defaultPrt.Nodes.Add(new StackPrtNode
        {
            NodeId = "6",
            Description = "Integrationskonstante",
            AnswerTest = "AlgEquiv",
            StudentAnswer = "ans1",
            TeacherAnswer = "model_ans + c",
            ScoreModeTrue = "Set to",
            ScoreTrue = 0.3,
            NextNodeTrue = "-1",
            ScoreModeFalse = "Set to",
            ScoreFalse = 0.0,
            NextNodeFalse = "-1",
            TrueFeedback = "<p>Did you forget the constant of integration?</p>",
            FalseFeedback = "<p>Incorrect derivative result.</p>"
        });

        // Node 7: Expanded Form Fallback
        defaultPrt.Nodes.Add(new StackPrtNode
        {
            NodeId = "7",
            Description = "Ausmultiplizierte Form",
            AnswerTest = "Expanded",
            StudentAnswer = "ans1",
            TeacherAnswer = "model_ans",
            ScoreModeTrue = "Set to",
            ScoreTrue = 0.1,
            NextNodeTrue = "-1",
            ScoreModeFalse = "Set to",
            ScoreFalse = 0.0,
            NextNodeFalse = "-1",
            TrueFeedback = "<p>Expanded form recognized.</p>",
            FalseFeedback = "<p>No matching algebraic pattern recognized.</p>"
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
    private bool _autoCap;
    
    [ObservableProperty]
    private string _feedbackStyle = "Formative"; // Formative, Summative
    
    [ObservableProperty]
    private string _feedbackVariables = "/* Feedback-Variablen hier definieren */";
    
    public ObservableCollection<StackPrtNode> Nodes { get; } = new();

    public StackPrt()
    {
        Nodes.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (StackPrtNode node in e.NewItems)
                {
                    node.ParentPrt = this;
                }
            }
        };
    }
}

public partial class StackPrtNode : ObservableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public StackPrt? ParentPrt { get; set; }

    private string _nodeId = "1";

    public string NodeId
    {
        get => _nodeId;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                OnPropertyChanged(nameof(NodeId));
                return;
            }

            string trimmed = value.Trim();
            if (!int.TryParse(trimmed, out int parsedNum) || parsedNum < 1)
            {
                OnPropertyChanged(nameof(NodeId));
                return;
            }

            string canonical = parsedNum.ToString();
            if (_nodeId == canonical) return;

            // Enforce uniqueness: Prohibit duplicate NodeId among existing nodes
            if (ParentPrt != null && ParentPrt.Nodes.Any(n => n.Id != this.Id && n.NodeId == canonical))
            {
                // Duplicate detected! Revert UI binding to previous valid NodeId
                OnPropertyChanged(nameof(NodeId));
                return;
            }

            string oldNodeId = _nodeId;
            _nodeId = canonical;
            OnPropertyChanged(nameof(NodeId));
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(DisplayNextNodeTrue));
            OnPropertyChanged(nameof(DisplayNextNodeFalse));

            // Seamlessly migrate branch references across PRT from oldNodeId to new canonical NodeId
            if (ParentPrt != null)
            {
                foreach (var node in ParentPrt.Nodes)
                {
                    if (node.NextNodeTrue == oldNodeId)
                    {
                        node.NextNodeTrue = canonical;
                    }
                    if (node.NextNodeFalse == oldNodeId)
                    {
                        node.NextNodeFalse = canonical;
                    }
                }
            }
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDescription))]
    private string _description = "Antwort korrekt?";

    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    public string DisplayName => $"Node {NodeId}";

    public string Name
    {
        get => NodeId;
        set => NodeId = value;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MaximaCheck))]
    private string _answerTest = "AlgEquiv";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MaximaCheck))]
    private string _studentAnswer = "sans1";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MaximaCheck))]
    private string _teacherAnswer = "tans1";

    [ObservableProperty]
    private string _testOptions = "AlgEquiv";

    [ObservableProperty]
    private bool _quiet;

    [ObservableProperty]
    private string _scoreModeTrue = "Set to";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NodeScore))]
    private double _scoreTrue = 1.0;

    [ObservableProperty]
    private double _penaltyTrue = 0.0;

    [ObservableProperty]
    private string _scoreModeFalse = "Set to";

    [ObservableProperty]
    private double _scoreFalse = 0.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Penalty))]
    private double _penaltyFalse = 0.1;

    public double NodeScore
    {
        get => ScoreTrue;
        set => ScoreTrue = value;
    }

    public double Penalty
    {
        get => PenaltyFalse;
        set => PenaltyFalse = value;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayNextNodeTrue))]
    private string _nextNodeTrue = "-1"; // "-1" for Stop, otherwise Node ID/number

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayNextNodeFalse))]
    private string _nextNodeFalse = "-1";

    public string DisplayNextNodeTrue
    {
        get
        {
            if (string.IsNullOrEmpty(NextNodeTrue) || NextNodeTrue == "-1") return "Keine";
            if (NextNodeTrue.StartsWith("Node ", StringComparison.OrdinalIgnoreCase)) return NextNodeTrue;
            return $"Node {NextNodeTrue}";
        }
        set
        {
            if (value == null) return;
            string val;
            if (value == "Keine" || value == "-1")
            {
                val = "-1";
            }
            else if (value.StartsWith("Node ", StringComparison.OrdinalIgnoreCase))
            {
                val = value.Substring(5).Trim();
            }
            else
            {
                val = value;
            }

            if (NextNodeTrue != val)
            {
                NextNodeTrue = val;
            }
        }
    }

    public string DisplayNextNodeFalse
    {
        get
        {
            if (string.IsNullOrEmpty(NextNodeFalse) || NextNodeFalse == "-1") return "Keine";
            if (NextNodeFalse.StartsWith("Node ", StringComparison.OrdinalIgnoreCase)) return NextNodeFalse;
            return $"Node {NextNodeFalse}";
        }
        set
        {
            if (value == null) return;
            string val;
            if (value == "Keine" || value == "-1")
            {
                val = "-1";
            }
            else if (value.StartsWith("Node ", StringComparison.OrdinalIgnoreCase))
            {
                val = value.Substring(5).Trim();
            }
            else
            {
                val = value;
            }

            if (NextNodeFalse != val)
            {
                NextNodeFalse = val;
            }
        }
    }

    [ObservableProperty]
    private string _trueFeedback = "<p>Prima, das ist richtig!</p>";

    [ObservableProperty]
    private string _falseFeedback = "<p>Prima, das ist richtig!</p>";

    public string MaximaCheck => $"{AnswerTest}({StudentAnswer},{TeacherAnswer})";
}
