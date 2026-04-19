using System.Drawing;

namespace Ambystech.Elaris.UI.CodeEditor.Plugins.Models;

/// <summary>
/// Data transfer object for deserializing theme JSON files.
/// </summary>
public class ThemeFile
{
    /// <summary>
    /// Gets or sets the theme name (identifier).
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets or sets the display name for the theme.
    /// </summary>
    public string DisplayName { get; set; } = "";

    /// <summary>
    /// Gets or sets the color mapping for each token type.
    /// </summary>
    public Dictionary<string, ColorDto> Colors { get; set; } = new();
}

/// <summary>
/// Data transfer object for color values in JSON.
/// </summary>
public class ColorDto
{
    /// <summary>
    /// Gets or sets the red component (0-255).
    /// </summary>
    public byte R { get; set; }

    /// <summary>
    /// Gets or sets the green component (0-255).
    /// </summary>
    public byte G { get; set; }

    /// <summary>
    /// Gets or sets the blue component (0-255).
    /// </summary>
    public byte B { get; set; }
}

