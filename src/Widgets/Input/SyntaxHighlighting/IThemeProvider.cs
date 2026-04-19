namespace Ambystech.Elaris.UI.Widgets.Input.SyntaxHighlighting;

/// <summary>
/// Interface for providers that supply color themes for syntax highlighting.
/// </summary>
public interface IThemeProvider
{
    /// <summary>
    /// Gets the theme with the specified name.
    /// </summary>
    /// <param name="name">The theme name (e.g., "default", "monokai").</param>
    /// <returns>The theme, or null if not found.</returns>
    SyntaxTheme? GetTheme(string name);

    /// <summary>
    /// Gets the default theme.
    /// </summary>
    /// <returns>The default theme.</returns>
    SyntaxTheme GetDefaultTheme();

    /// <summary>
    /// Gets the list of themes supported by this provider.
    /// </summary>
    /// <returns>A collection of supported theme names.</returns>
    IEnumerable<string> GetSupportedThemes();
}

