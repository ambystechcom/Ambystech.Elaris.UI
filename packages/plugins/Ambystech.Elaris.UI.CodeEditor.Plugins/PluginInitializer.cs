using Ambystech.Elaris.UI.Widgets.Input.SyntaxHighlighting;
using Ambystech.Elaris.UI.CodeEditor.Plugins.Providers;

namespace Ambystech.Elaris.UI.CodeEditor.Plugins;

/// <summary>
/// Helper class for initializing and registering JSON-based plugins.
/// </summary>
public static class PluginInitializer
{
    /// <summary>
    /// Initializes JSON providers from the specified directories and registers them with PluginManager.
    /// </summary>
    /// <param name="syntaxRulesDirectory">Directory containing syntax rule JSON files. If null, syntax rules are not loaded.</param>
    /// <param name="themesDirectory">Directory containing theme JSON files. If null, themes are not loaded.</param>
    public static void Initialize(string? syntaxRulesDirectory = null, string? themesDirectory = null)
    {
        if (!string.IsNullOrEmpty(syntaxRulesDirectory))
        {
            var ruleProvider = new JsonSyntaxRuleProvider(syntaxRulesDirectory);
            PluginManager.Instance.RegisterRuleProvider(ruleProvider);
        }

        if (!string.IsNullOrEmpty(themesDirectory))
        {
            var themeProvider = new JsonThemeProvider(themesDirectory);
            PluginManager.Instance.RegisterThemeProvider(themeProvider);
        }
    }
}

