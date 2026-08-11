using System.Threading.Tasks;
using TextMateSharp.Grammars;

namespace STACK_HUB.Services;

public static class TextMateService
{
    public static RegistryOptions Instance { get; } = new(ThemeName.DarkPlus);
    public static string HtmlScopeId { get; private set; } = string.Empty;

    public static void Prewarm()
    {
        Task.Run(() =>
        {
            // 1. Load Theme
            var options = Instance;

            // 2. Pre-load and compile HTML Grammar & Regexes in the background
            var htmlLanguage = options.GetLanguageByExtension(".html");
            if (htmlLanguage != null)
            {
                HtmlScopeId = options.GetScopeByLanguageId(htmlLanguage.Id);
                
                // 🚀 This forces TextMateSharp to parse html.tmLanguage.json 
                // and compile its regexes on the background thread NOW
                _ = options.GetGrammar(HtmlScopeId);
            }
        });
    }
}