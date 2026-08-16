using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using STACK_HUB.Models;

namespace STACK_HUB.ViewModels;

public record SettingChoice<T>(T Value, string Label);

public partial class MaximaSettingsViewModel : ViewModelBase, ICacheablePane
{
    public StackQuestion Question { get; }

    public static readonly List<SettingChoice<char>> DecimalChoices = new()
    {
        new('.', "Punkt (.)"),
        new(',', "Komma (,)")
    };

    public static readonly List<SettingChoice<string>> MultiplicationSignChoices = new()
    {
        new("dot", "Punkt (z.B. 2 · x)"),
        new("none", "Kein Multiplikationszeichen (z.B. 2x)"),
        new("cross", "Kreuz (z.B. 2 × x)"),
        new("space", "Leerzeichen (z.B. 2 x)"),
        new("onum", "Nur bei Zahlen (z.B. 2 · 3)")
    };

    public static readonly List<SettingChoice<string>> ComplexNoChoices = new()
    {
        new("i", "i"),
        new("j", "j (Ingenieurwesen)"),
        new("symi", "symi"),
        new("symj", "symj")
    };

    public static readonly List<SettingChoice<string>> InverseTrigChoices = new()
    {
        new("cos-1", "cos⁻¹(x), sin⁻¹(x)"),
        new("acos", "acos(x), asin(x)"),
        new("arccos", "arccos(x), arcsin(x)")
    };

    public static readonly List<SettingChoice<string>> LogicSymbolChoices = new()
    {
        new("lang", "Textuell (and, or, not)"),
        new("symbol", "Symbolisch (∧, ∨, ¬)")
    };

    public static readonly List<SettingChoice<string>> MatrixParensChoices = new()
    {
        new("[", "Eckige Klammern [...]"),
        new("(", "Runde Klammern (...)"),
        new("{", "Geschweifte Klammern {...}"),
        new("|", "Betragsstriche |...|"),
        new("", "Keine Klammern")
    };

    public MaximaSettingsViewModel(StackQuestion question)
    {
        Question = question;
    }
}
