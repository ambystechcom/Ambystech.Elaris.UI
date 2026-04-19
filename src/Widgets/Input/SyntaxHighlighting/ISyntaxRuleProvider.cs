namespace Ambystech.Elaris.UI.Widgets.Input.SyntaxHighlighting;

/// <summary>
/// Interface for providers that supply syntax highlighting rules.
/// </summary>
public interface ISyntaxRuleProvider
{
    /// <summary>
    /// Gets the syntax rules for the specified language.
    /// </summary>
    /// <param name="language">The language identifier (e.g., "csharp", "javascript").</param>
    /// <returns>The syntax rules for the language, or null if not supported.</returns>
    SyntaxRules? GetRules(string language);

    /// <summary>
    /// Gets the list of languages supported by this provider.
    /// </summary>
    /// <returns>A collection of supported language identifiers.</returns>
    IEnumerable<string> GetSupportedLanguages();
}

