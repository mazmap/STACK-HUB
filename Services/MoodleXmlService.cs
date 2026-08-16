using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using STACK_HUB.Models;

namespace STACK_HUB.Services;

public static class MoodleXmlService
{
    public static StackQuestion ParseQuestion(string xmlContent)
    {
        var doc = XDocument.Parse(xmlContent);
        var qElem = doc.Descendants("question")
                       .FirstOrDefault(q => (string?)q.Attribute("type") == "stack")
                    ?? doc.Descendants("question").FirstOrDefault()
                    ?? throw new InvalidOperationException("No <question> element found in XML.");

        var question = new StackQuestion
        {
            Inputs = new(),
            Prts = new(),
            Hints = new()
        };

        // 1. General & Core settings
        question.Name = GetText(qElem, "name");
        question.QuestionText = GetText(qElem, "questiontext");
        question.GeneralFeedback = GetText(qElem, "generalfeedback");
        question.DefaultGrade = GetDouble(qElem, "defaultgrade", 3.0);
        question.Penalty = GetDouble(qElem, "penalty", 0.1);
        question.Hidden = GetBool(qElem, "hidden", false);
        question.IdNumber = GetDirectValue(qElem, "idnumber");
        question.StackVersion = GetText(qElem, "stackversion");
        question.QuestionVariables = GetText(qElem, "questionvariables");
        question.SpecificFeedback = GetText(qElem, "specificfeedback");
        question.QuestionNote = GetText(qElem, "questionnote");
        question.QuestionDescription = GetText(qElem, "questiondescription");
        question.VariantSelectionSeed = GetDirectValue(qElem, "variantsselectionseed");

        // 2. Maxima settings
        question.QuestionSimplify = GetBool(qElem, "questionsimplify", true);
        question.AssumePositive = GetBool(qElem, "assumepositive", false);
        question.AssumeReal = GetBool(qElem, "assumereal", false);
        
        string decStr = GetDirectValue(qElem, "decimals");
        question.Decimals = !string.IsNullOrEmpty(decStr) ? decStr[0] : '.';
        
        question.ScientificNotation = GetDirectValue(qElem, "scientificnotation", "10E");
        question.MultiplicationSign = GetDirectValue(qElem, "multiplicationsign", "dot");
        question.SqrtSign = GetBool(qElem, "sqrtsign", true);
        question.ComplexNo = GetDirectValue(qElem, "complexno", "i");
        question.InverseTrig = GetDirectValue(qElem, "inversetrig", "cos-1");
        question.LogicSymbol = GetDirectValue(qElem, "logicsymbol", "lang");
        question.MatrixParens = GetDirectValue(qElem, "matrixparens", "[");

        // 3. Feedback settings
        string correctFb = GetText(qElem, "prtcorrect");
        if (!string.IsNullOrEmpty(correctFb)) question.CorrectFeedback = correctFb;

        string partFb = GetText(qElem, "prtpartiallycorrect");
        if (!string.IsNullOrEmpty(partFb)) question.PartiallyCorrectFeedback = partFb;

        string incorrFb = GetText(qElem, "prtincorrect");
        if (!string.IsNullOrEmpty(incorrFb)) question.IncorrectFeedback = incorrFb;

        // Hints
        foreach (var hintElem in qElem.Elements("hint"))
        {
            string hintText = GetText(hintElem);
            if (!string.IsNullOrEmpty(hintText))
            {
                question.Hints.Add(hintText);
            }
        }

        // 4. Inputs (<input> elements)
        foreach (var inpElem in qElem.Elements("input"))
        {
            var input = new StackInput
            {
                Name = GetDirectValue(inpElem, "name", "ans1"),
                Type = GetDirectValue(inpElem, "type", "algebraic"),
                TeacherAnswer = GetDirectValue(inpElem, "tans", "model_ans"),
                BoxSize = GetInt(inpElem, "boxsize", 15),
                StrictSyntax = GetBool(inpElem, "strictsyntax", true),
                InsertStars = GetInt(inpElem, "insertstars", 0),
                SyntaxHint = GetDirectValue(inpElem, "syntaxhint", ""),
                SyntaxAttribute = GetInt(inpElem, "syntaxattribute", 0),
                ForbidWords = GetDirectValue(inpElem, "forbidwords", ""),
                AllowWords = GetDirectValue(inpElem, "allowwords", ""),
                ForbidFloat = GetBool(inpElem, "forbidfloat", false),
                RequireLowestTerm = GetBool(inpElem, "requirelowestterms", GetBool(inpElem, "requirelowestterm", true)),
                CheckAnswerType = GetBool(inpElem, "checkanswertype", false),
                MustVerify = GetBool(inpElem, "mustverify", true),
                ShowValidation = GetInt(inpElem, "showvalidation", 1),
                Options = GetDirectValue(inpElem, "options", "")
            };
            question.Inputs.Add(input);
        }

        // 5. PRTs (<prt> elements)
        foreach (var prtElem in qElem.Elements("prt"))
        {
            var prt = new StackPrt
            {
                Name = GetDirectValue(prtElem, "name", "prt1"),
                Value = GetDouble(prtElem, "value", 1.0),
                Autosimplify = GetBool(prtElem, "autosimplify", true),
                FeedbackStyle = GetDirectValue(prtElem, "feedbackstyle", "1"),
                FeedbackVariables = GetText(prtElem, "feedbackvariables")
            };

            foreach (var nodeElem in prtElem.Elements("node"))
            {
                string rawNodeName = GetDirectValue(nodeElem, "name", "1");
                // Moodle STACK often uses 0-based indexing (node 0, 1, 2...). We can store canonical string ID
                var node = new StackPrtNode
                {
                    ParentPrt = prt,
                    NodeId = rawNodeName,
                    Description = GetDirectValue(nodeElem, "description", ""),
                    AnswerTest = GetDirectValue(nodeElem, "answertest", "AlgEquiv"),
                    StudentAnswer = GetDirectValue(nodeElem, "sans", "sans1"),
                    TeacherAnswer = GetDirectValue(nodeElem, "tans", "tans1"),
                    TestOptions = GetDirectValue(nodeElem, "testoptions", ""),
                    Quiet = GetBool(nodeElem, "quiet", false),
                    ScoreModeTrue = GetDirectValue(nodeElem, "truescoremode", "="),
                    ScoreTrue = GetDouble(nodeElem, "truescore", 1.0),
                    PenaltyTrue = GetDouble(nodeElem, "truepenalty", 0.0),
                    NextNodeTrue = GetDirectValue(nodeElem, "truenextnode", "-1"),
                    AnswerNoteTrue = GetDirectValue(nodeElem, "trueanswernote", ""),
                    TrueFeedback = GetText(nodeElem, "truefeedback"),
                    ScoreModeFalse = GetDirectValue(nodeElem, "falsescoremode", "="),
                    ScoreFalse = GetDouble(nodeElem, "falsescore", 0.0),
                    PenaltyFalse = GetDouble(nodeElem, "falsepenalty", 0.1),
                    NextNodeFalse = GetDirectValue(nodeElem, "falsenextnode", "-1"),
                    AnswerNoteFalse = GetDirectValue(nodeElem, "falseanswernote", ""),
                    FalseFeedback = GetText(nodeElem, "falsefeedback")
                };
                prt.Nodes.Add(node);
            }

            question.Prts.Add(prt);
        }

        return question;
    }

    private static string GetText(XElement parent, string childName)
    {
        var child = parent.Element(childName);
        if (child == null) return string.Empty;
        return GetText(child);
    }

    private static string GetText(XElement element)
    {
        var textElem = element.Element("text");
        if (textElem != null)
        {
            return textElem.Value;
        }
        return element.Value;
    }

    private static string GetDirectValue(XElement parent, string childName, string fallback = "")
    {
        var child = parent.Element(childName);
        if (child == null) return fallback;
        string val = child.Value;
        return string.IsNullOrEmpty(val) ? fallback : val;
    }

    private static bool GetBool(XElement parent, string childName, bool fallback = false)
    {
        var child = parent.Element(childName);
        if (child == null) return fallback;
        string val = child.Value?.Trim() ?? "";
        if (val == "1" || val.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
        if (val == "0" || val.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
        return fallback;
    }

    private static int GetInt(XElement parent, string childName, int fallback = 0)
    {
        var child = parent.Element(childName);
        if (child == null) return fallback;
        string val = child.Value?.Trim() ?? "";
        if (int.TryParse(val, out int result)) return result;
        return fallback;
    }

    private static double GetDouble(XElement parent, string childName, double fallback = 0.0)
    {
        var child = parent.Element(childName);
        if (child == null) return fallback;
        string val = child.Value?.Trim().Replace(',', '.') ?? "";
        if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out double result)) return result;
        return fallback;
    }
}
