using System.Threading.Tasks;
using TextMateSharp.Grammars;

namespace STACK_HUB.Services;

public static class TextMateService
{
    public static CustomRegistryOptions Instance { get; } = new(ThemeName.DarkPlus);
    public static void Prewarm()
    {
        Task.Run(() =>
        {
            _ = Instance.GetGrammar(Instance.GetScopeByLanguageId("html"));
            _ = Instance.GetGrammar(Instance.GetScopeByLanguageId("maxima"));
        });
    }
}