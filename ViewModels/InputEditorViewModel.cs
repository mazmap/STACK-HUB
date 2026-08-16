using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using STACK_HUB.Models;

namespace STACK_HUB.ViewModels;

public record InputTypeOption(string Key, string DisplayName, string Description);
public record OptionChoice<T>(T Value, string Label);

public partial class InputEditorViewModel : ViewModelBase, ICacheablePane
{
    public StackInput Input { get; }

    public static readonly List<InputTypeOption> AllInputTypes = new()
    {
        new("algebraic", "Algebraische Eingabe", "Standard algebraische Eingabe mit Maxima-Parsing"),
        new("note", "Anmerkung", "Nur zur Anzeige/Notiz für Studenten"),
        new("checkbox", "Checkbox", "Mehrfachauswahl / Kontrollkästchen"),
        new("dropdown", "Dropdown-Liste", "Einzelauswahl aus Dropdown-Menü"),
        new("units", "Einheiten", "Eingabe physikalischer Größen mit Maßeinheiten"),
        new("singlechar", "Einzelnes Zeichen", "Eingabe genau eines Zeichens / Buchstabens"),
        new("equiv", "Equivalence reasoning", "Schrittweise Äquivalenzumformungen"),
        new("geogebra", "GeoGebra", "Interaktives GeoGebra-Applet"),
        new("matrix", "Matrix", "Matrix-Eingabe mit fester Zeilen-/Spaltenanzahl"),
        new("varmatrix", "Matrix mit variabler Größe", "Matrix-Eingabe mit dynamischer Zeilen-/Spaltenanzahl"),
        new("numeric", "Numerisch", "Reine Fließkomma- oder Festkomma-Zahleneingabe"),
        new("parsons", "Parsons", "Parsons-Puzzles (Code- / Beweis-Bausteine ordnen)"),
        new("radio", "Radiobuttons", "Einzelauswahl über Radiobutton-Liste"),
        new("textarea", "Textfeld", "Mehrzeiliges Freitext-Eingabefeld"),
        new("boolean", "Wahr/Falsch", "Wahr / Falsch (Boolean) Auswahl"),
        new("string", "Zeichenkette", "Einfache Zeichenketten-Eingabe (ohne CAS-Auswertung)")
    };

    public static readonly List<OptionChoice<int>> SyntaxAttributeChoices = new()
    {
        new(0, "0 : Wert (Value)"),
        new(1, "1 : Platzhalter (Placeholder)")
    };

    public static readonly List<OptionChoice<int>> InsertStarsChoices = new()
    {
        new(0, "0 : Keine Multiplikationssterne automatisch einfügen"),
        new(1, "1 : Multiplikationssterne implizit einfügen (z.B. 3x -> 3*x)")
    };

    public static readonly List<OptionChoice<bool>> BooleanFlagChoices = new()
    {
        new(false, "Nein (0)"),
        new(true, "Ja (1)")
    };

    public static readonly List<OptionChoice<int>> ShowValidationChoices = new()
    {
        new(0, "0 : Keine Validierung anzeigen"),
        new(1, "1 : Validierung mit Variable anzeigen"),
        new(2, "2 : Validierung ohne Variable anzeigen")
    };

    public InputEditorViewModel(StackInput input)
    {
        Input = input;
        Input.PropertyChanged += OnInputPropertyChanged;
    }

    private void OnInputPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(StackInput.Type))
        {
            NotifyVisibilityChanged();
        }
    }

    private void NotifyVisibilityChanged()
    {
        OnPropertyChanged(nameof(ShowBoxSize));
        OnPropertyChanged(nameof(ShowSyntaxHintAndAttribute));
        OnPropertyChanged(nameof(ShowStrictSyntaxAndStars));
        OnPropertyChanged(nameof(ShowForbidAllowWords));
        OnPropertyChanged(nameof(ShowForbidFloatAndLowestTerms));
        OnPropertyChanged(nameof(ShowCheckAnswerType));
        OnPropertyChanged(nameof(TypeDisplayName));
        OnPropertyChanged(nameof(TypeDescription));
    }

    private string NormalizedType => Input.Type?.Trim().ToLowerInvariant() ?? "algebraic";

    public string TypeDisplayName
    {
        get
        {
            var match = AllInputTypes.Find(t => t.Key == NormalizedType);
            return match?.DisplayName ?? Input.Type;
        }
    }

    public string TypeDescription
    {
        get
        {
            var match = AllInputTypes.Find(t => t.Key == NormalizedType);
            return match?.Description ?? string.Empty;
        }
    }

    // 1. Box Size (Algebraic, Note, Parsons, String, Equiv, Textarea, Units, Singlechar, Geogebra, Matrix, Varmatrix, Numeric)
    public bool ShowBoxSize => NormalizedType switch
    {
        "checkbox" or "dropdown" or "radio" or "boolean" => false,
        _ => true
    };

    // 2. Syntax Hint & Attribute (Algebraic, Note, Parsons, String, Equiv, Textarea, Units, Singlechar, Geogebra, Matrix, Varmatrix, Numeric)
    public bool ShowSyntaxHintAndAttribute => NormalizedType switch
    {
        "checkbox" or "dropdown" or "radio" or "boolean" => false,
        _ => true
    };

    // 3. Strict Syntax & Insert Stars (Algebraic, Units, Singlechar, Equiv, Geogebra, Matrix, Varmatrix, Numeric, Textarea)
    public bool ShowStrictSyntaxAndStars => NormalizedType switch
    {
        "algebraic" or "units" or "singlechar" or "equiv" or "geogebra" or "matrix" or "varmatrix" or "numeric" or "textarea" => true,
        _ => false
    };

    // 4. Forbidden & Allowed Words (Algebraic, Units, Singlechar, Equiv, Geogebra, Matrix, Varmatrix, Numeric, Textarea)
    public bool ShowForbidAllowWords => NormalizedType switch
    {
        "algebraic" or "units" or "singlechar" or "equiv" or "geogebra" or "matrix" or "varmatrix" or "numeric" or "textarea" => true,
        _ => false
    };

    // 5. Forbid Float & Require Lowest Terms (Algebraic, Units, Singlechar, Equiv, Geogebra, Matrix, Varmatrix, Numeric, Textarea)
    public bool ShowForbidFloatAndLowestTerms => NormalizedType switch
    {
        "algebraic" or "units" or "singlechar" or "equiv" or "geogebra" or "matrix" or "varmatrix" or "numeric" or "textarea" => true,
        _ => false
    };

    // 6. Check Answer Type (Algebraic, Units, Singlechar, Geogebra, Matrix, Varmatrix, Numeric) - NOT in Equiv or Textarea!
    public bool ShowCheckAnswerType => NormalizedType switch
    {
        "algebraic" or "units" or "singlechar" or "geogebra" or "matrix" or "varmatrix" or "numeric" => true,
        _ => false
    };
}
