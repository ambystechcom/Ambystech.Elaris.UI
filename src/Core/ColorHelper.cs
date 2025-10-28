using System.Drawing;

namespace Ambystech.Elaris.UI.Core;

/// <summary>
/// Helper methods and extension methods for System.Drawing.Color to provide additional functionality.
/// </summary>
public static class ColorHelper
{
    /// <summary>
    /// Creates a color from RGB values (0-255) with full opacity.
    /// </summary>
    public static Color FromRgb(byte r, byte g, byte b) => Color.FromArgb(255, r, g, b);


    /// <summary>
    /// Creates a color from RGBA values (0-255).
    /// </summary>
    public static Color FromRgba(byte r, byte g, byte b, byte a) => Color.FromArgb(a, r, g, b);


    /// <summary>
    /// Creates a color from a hex string (#RRGGBB or #RRGGBBAA).
    /// </summary>
    public static Color FromHex(string hex)
    {
        hex = hex.TrimStart('#');

        if (hex.Length is not 6 and not 8)
            throw new ArgumentException("Hex color must be 6 or 8 characters", nameof(hex));

        byte r = Convert.ToByte(hex[..2], 16);
        byte g = Convert.ToByte(hex[2..4], 16);
        byte b = Convert.ToByte(hex[4..6], 16);
        byte a = hex.Length == 8 ? Convert.ToByte(hex[6..8], 16) : (byte)255;

        return Color.FromArgb(a, r, g, b);
    }

    /// <summary>
    /// Tries to parse a hex color string, returning the fallback color if parsing fails.
    /// </summary>
    /// <param name="hex">Hex color string (#RRGGBB or #RRGGBBAA)</param>
    /// <param name="fallback">Fallback color to return if parsing fails</param>
    /// <returns>Parsed color or fallback if parsing fails</returns>
    public static Color TryFromHex(string hex, Color fallback)
    {
        try
        {
            if (string.IsNullOrEmpty(hex))
                return fallback;

            return FromHex(hex);
        }
        catch
        {
            return fallback;
        }

    }

    /// <summary>
    /// Converts the color to a hex string (#RRGGBB).
    /// </summary>
    public static string ToHex(this Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    /// <summary>
    /// Converts the color to a hex string with alpha (#RRGGBBAA).
    /// </summary>
    public static string ToHexWithAlpha(this Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";

    /// <summary>
    /// Linearly interpolates between two colors.
    /// </summary>
    /// <param name="start">Starting color</param>
    /// <param name="end">Ending color</param>
    /// <param name="t">Interpolation factor (0.0 to 1.0)</param>
    /// <returns>Interpolated color</returns>
    public static Color Lerp(Color start, Color end, float t)
    {
        t = Math.Clamp(t, 0f, 1f);

        byte r = (byte)(start.R + (end.R - start.R) * t);
        byte g = (byte)(start.G + (end.G - start.G) * t);
        byte b = (byte)(start.B + (end.B - start.B) * t);
        byte a = (byte)(start.A + (end.A - start.A) * t);

        return Color.FromArgb(a, r, g, b);
    }
}
