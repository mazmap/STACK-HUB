using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;

namespace STACK_HUB.Editor;

public static class IclsThemeLoader
{
    /// <summary>
    /// Loads an IntelliJ .icls theme file from a Stream and applies it to an AvaloniaEdit TextEditor instance.
    /// </summary>
    public static void ApplyTheme(TextEditor editor, Stream stream)
    {
        var doc = XDocument.Load(stream);
        ApplyThemeFromDocument(editor, doc);
    }

    /// <summary>
    /// Loads an IntelliJ .icls theme file from a file path and applies it to an AvaloniaEdit TextEditor instance.
    /// </summary>
    public static void ApplyTheme(TextEditor editor, string iclsFilePath)
    {
        var doc = XDocument.Load(iclsFilePath);
        ApplyThemeFromDocument(editor, doc);
    }

    private static void ApplyThemeFromDocument(TextEditor editor, XDocument doc)
    {
        var root = doc.Root;
        if (root == null) return;

        // 1. Parse global editor colors (<colors> tag)
        var colorsElement = root.Element("colors");
        if (colorsElement != null)
        {
            foreach (var option in colorsElement.Elements("option"))
            {
                var name = option.Attribute("name")?.Value;
                var val = option.Attribute("value")?.Value;
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(val)) continue;

                var color = ParseHexColor(val);
                if (!color.HasValue) continue;

                var brush = new SolidColorBrush(color.Value);

                if (name == "CONSOLE_BACKGROUND_KEY" || name == "GUTTER_BACKGROUND")
                {
                    editor.Background = brush;
                }
                else if (name == "LINE_NUMBERS_COLOR")
                {
                    editor.LineNumbersForeground = brush;
                }
            }
        }

        // 2. Parse token syntax colors (<attributes> tag)
        var attributesElement = root.Element("attributes");
        if (attributesElement != null)
        {
            foreach (var option in attributesElement.Elements("option"))
            {
                var attrName = option.Attribute("name")?.Value;
                var valueElem = option.Element("value");
                if (string.IsNullOrEmpty(attrName) || valueElem == null) continue;

                var fgOption = valueElem.Elements("option").FirstOrDefault(e => e.Attribute("name")?.Value == "FOREGROUND");
                var bgOption = valueElem.Elements("option").FirstOrDefault(e => e.Attribute("name")?.Value == "BACKGROUND");

                Color? fg = fgOption != null ? ParseHexColor(fgOption.Attribute("value")?.Value) : null;
                Color? bg = bgOption != null ? ParseHexColor(bgOption.Attribute("value")?.Value) : null;

                if (attrName == "TEXT")
                {
                    if (fg.HasValue) editor.Foreground = new SolidColorBrush(fg.Value);
                    if (bg.HasValue) editor.Background = new SolidColorBrush(bg.Value);
                }

                // Map IntelliJ ICLS token attributes to AvaloniaEdit HighlightingColors
                if (editor.SyntaxHighlighting != null)
                {
                    ApplyToHighlighting(editor.SyntaxHighlighting, attrName, fg, bg);
                }
            }
        }
    }

    private static void ApplyToHighlighting(IHighlightingDefinition definition, string iclsAttrName, Color? fg, Color? bg)
    {
        foreach (var color in definition.NamedHighlightingColors)
        {
            if (IsMatchingToken(iclsAttrName, color.Name))
            {
                if (fg.HasValue) color.Foreground = new SimpleHighlightingBrush(fg.Value);
                if (bg.HasValue) color.Background = new SimpleHighlightingBrush(bg.Value);
            }
        }
    }

    private static bool IsMatchingToken(string iclsName, string avaloniaColorName)
    {
        // Maps ICLS syntax names to AvaloniaEdit HTML/XML color names
        if (iclsName is "HTML_TAG_NAME" or "DEFAULT_KEYWORD" or "TAG_NAME")
            return avaloniaColorName.Contains("Tag", StringComparison.OrdinalIgnoreCase) || avaloniaColorName.Contains("Keyword", StringComparison.OrdinalIgnoreCase);

        if (iclsName is "HTML_ATTRIBUTE_NAME" or "DEFAULT_ATTRIBUTE" or "ATTRIBUTE_NAME")
            return avaloniaColorName.Contains("Attribute", StringComparison.OrdinalIgnoreCase);

        if (iclsName is "HTML_ATTRIBUTE_VALUE" or "DEFAULT_STRING" or "ATTRIBUTE_VALUE")
            return avaloniaColorName.Contains("String", StringComparison.OrdinalIgnoreCase) || avaloniaColorName.Contains("Value", StringComparison.OrdinalIgnoreCase);

        if (iclsName is "HTML_COMMENT" or "DEFAULT_COMMENT" or "COMMENT")
            return avaloniaColorName.Contains("Comment", StringComparison.OrdinalIgnoreCase);

        return false;
    }

    private static Color? ParseHexColor(string? hexStr)
    {
        if (string.IsNullOrWhiteSpace(hexStr)) return null;
        hexStr = hexStr.TrimStart('#');
        if (hexStr.Length == 6) hexStr = "FF" + hexStr;

        if (uint.TryParse(hexStr, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb))
        {
            return Color.FromUInt32(argb);
        }
        return null;
    }
}
