using System;
using System.Collections.Generic;
using System.IO;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars.Reader;
using TextMateSharp.Internal.Types;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace STACK_HUB.Services;

public class CustomRegistryOptions : IRegistryOptions
{
    private readonly RegistryOptions _defaultOptions;

    public CustomRegistryOptions(ThemeName themeName)
    {
        _defaultOptions = new RegistryOptions(themeName);
    }

    public string GetScopeByLanguageId(string languageId)
    {
        if (languageId.Equals("maxima", StringComparison.OrdinalIgnoreCase) ||
            languageId.Equals("mac", StringComparison.OrdinalIgnoreCase))
        {
            return "source.mac";
        }

        return _defaultOptions.GetScopeByLanguageId(languageId);
    }

    public IRawGrammar GetGrammar(string scopeName)
    {
        if (scopeName.Equals("source.mac", StringComparison.OrdinalIgnoreCase) ||
            scopeName.Equals("source.maxima", StringComparison.OrdinalIgnoreCase))
        {
            var assembly = typeof(CustomRegistryOptions).Assembly;

            // Finds Resources/mac.tmLanguage.json in assembly
            var resourceName = Array.Find(
                assembly.GetManifestResourceNames(), 
                r => r.EndsWith("mac.tmLanguage.json", StringComparison.OrdinalIgnoreCase)
            );

            if (resourceName != null)
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    return GrammarReader.ReadGrammarSync(reader);
                }
            }
        }

        return _defaultOptions.GetGrammar(scopeName);
    }

    public IRawTheme GetTheme(string scopeName) => _defaultOptions.GetTheme(scopeName);
    public IRawTheme GetDefaultTheme() => _defaultOptions.GetDefaultTheme();
    public ICollection<string> GetInjections(string scopeName) => _defaultOptions.GetInjections(scopeName);
}
