using System.Drawing;
using Ambystech.Elaris.UI.Core;

namespace Ambystech.Elaris.UI.Widgets.Input.SyntaxHighlighting;

/// <summary>
/// Built-in theme provider that supplies default color themes for syntax highlighting.
/// </summary>
internal class BuiltInThemeProvider : IThemeProvider
{
    private static readonly SyntaxTheme DefaultTheme = new()
    {
        Name = "default",
        DisplayName = "Default",
        Colors = new Dictionary<TokenType, Color>
        {
            [TokenType.Keyword] = ColorHelper.FromRgb(86, 156, 214),
            [TokenType.String] = ColorHelper.FromRgb(206, 145, 120),
            [TokenType.Comment] = ColorHelper.FromRgb(106, 153, 85),
            [TokenType.Number] = ColorHelper.FromRgb(181, 206, 168),
            [TokenType.Operator] = ColorHelper.FromRgb(212, 212, 212),
            [TokenType.Identifier] = Color.White
        }
    };

    public SyntaxTheme? GetTheme(string name)
    {
        if (name == "default")
        {
            return DefaultTheme;
        }
        return null;
    }

    public SyntaxTheme GetDefaultTheme()
    {
        return DefaultTheme;
    }

    public IEnumerable<string> GetSupportedThemes()
    {
        return new[] { "default" };
    }
}

