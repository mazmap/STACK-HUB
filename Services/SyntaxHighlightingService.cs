using System;
using System.IO;
using System.Xml;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;

namespace STACK_HUB.Services;

public static class SyntaxHighlightingService
{
    private static IHighlightingDefinition? _casTextDefinition;
    private static IHighlightingDefinition? _maximaDefinition;

    public static IHighlightingDefinition CasTextDefinition
    {
        get
        {
            if (_casTextDefinition == null)
            {
                try
                {
                    var assembly = typeof(SyntaxHighlightingService).Assembly;
                    var resourceName = Array.Find(
                        assembly.GetManifestResourceNames(),
                        r => r.EndsWith("castext.xshd", StringComparison.OrdinalIgnoreCase)
                    );

                    if (resourceName != null)
                    {
                        using var stream = assembly.GetManifestResourceStream(resourceName);
                        if (stream != null)
                        {
                            using var reader = new XmlTextReader(stream);
                            _casTextDefinition = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SyntaxHighlightingService] Error loading castext.xshd: {ex.Message}");
                }

                _casTextDefinition ??= HighlightingManager.Instance.GetDefinition("HTML");
            }

            return _casTextDefinition;
        }
    }

    public static IHighlightingDefinition MaximaDefinition
    {
        get
        {
            if (_maximaDefinition == null)
            {
                try
                {
                    var assembly = typeof(SyntaxHighlightingService).Assembly;
                    var resourceName = Array.Find(
                        assembly.GetManifestResourceNames(),
                        r => r.EndsWith("maxima.xshd", StringComparison.OrdinalIgnoreCase)
                    );

                    if (resourceName != null)
                    {
                        using var stream = assembly.GetManifestResourceStream(resourceName);
                        if (stream != null)
                        {
                            using var reader = new XmlTextReader(stream);
                            _maximaDefinition = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SyntaxHighlightingService] Error loading maxima.xshd: {ex.Message}");
                }

                _maximaDefinition ??= HighlightingManager.Instance.GetDefinition("C#");
            }

            return _maximaDefinition;
        }
    }
}
