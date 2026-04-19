using System.Drawing;

namespace Ambystech.Elaris.UI.Widgets.Input.SyntaxHighlighting;

/// <summary>
/// Defines a color theme for syntax highlighting.
/// </summary>
public class SyntaxTheme
{
    /// <summary>
    /// Gets or sets the theme name (identifier).
    /// </summary>
    public string Name { get; set; } = "default";

    /// <summary>
    /// Gets or sets the display name for the theme.
    /// </summary>
    public string DisplayName { get; set; } = "Default";

    /// <summary>
    /// Gets or sets the color mapping for each token type.
    /// </summary>
    public Dictionary<TokenType, Color> Colors { get; set; } = new();
}

