using System.Drawing;

namespace Ambystech.Elaris.UI.Rendering;

/// <summary>
/// Converts RGB colors to the 256- and 16-color palettes, and detects the color mode
/// from environment variables.
/// </summary>
public static class ColorConversion
{
    /// <summary>
    /// The 16 basic ANSI colors, using xterm's default RGB values. Index 0–7 are the normal
    /// colors (black, red, green, yellow, blue, magenta, cyan, white), 8–15 the bright ones.
    /// </summary>
    public static IReadOnlyList<Color> Ansi16 { get; } =
    [
        Color.FromArgb(0, 0, 0), Color.FromArgb(205, 0, 0), Color.FromArgb(0, 205, 0), Color.FromArgb(205, 205, 0),
        Color.FromArgb(0, 0, 238), Color.FromArgb(205, 0, 205), Color.FromArgb(0, 205, 205), Color.FromArgb(229, 229, 229),
        Color.FromArgb(127, 127, 127), Color.FromArgb(255, 0, 0), Color.FromArgb(0, 255, 0), Color.FromArgb(255, 255, 0),
        Color.FromArgb(92, 92, 255), Color.FromArgb(255, 0, 255), Color.FromArgb(0, 255, 255), Color.FromArgb(255, 255, 255),
    ];

    private static readonly int[] CubeLevels = [0, 95, 135, 175, 215, 255];

    /// <summary>
    /// Returns the nearest xterm 256-color palette index (16–255: the 6×6×6 color cube or the
    /// 24-step gray ramp; the first 16 vary between terminals and are not used).
    /// </summary>
    public static int ToPalette256(Color color)
    {
        int r = NearestCubeIndex(color.R), g = NearestCubeIndex(color.G), b = NearestCubeIndex(color.B);
        int cubeIndex = 16 + 36 * r + 6 * g + b;
        var cubeColor = Color.FromArgb(CubeLevels[r], CubeLevels[g], CubeLevels[b]);

        // Gray ramp: 232–255 are 8, 18, …, 238.
        int average = (color.R + color.G + color.B) / 3;
        int grayStep = Math.Clamp((average - 8 + 5) / 10, 0, 23);
        int grayLevel = 8 + grayStep * 10;
        var grayColor = Color.FromArgb(grayLevel, grayLevel, grayLevel);

        return Distance(color, grayColor) < Distance(color, cubeColor) ? 232 + grayStep : cubeIndex;
    }

    /// <summary>
    /// Returns the RGB value of an xterm 256-color palette index (16–255).
    /// Indexes 0–15 use <see cref="Ansi16"/>.
    /// </summary>
    public static Color FromPalette256(int index)
    {
        if (index < 16)
            return Ansi16[Math.Clamp(index, 0, 15)];

        if (index >= 232)
        {
            int level = 8 + (index - 232) * 10;
            return Color.FromArgb(level, level, level);
        }

        int i = index - 16;
        return Color.FromArgb(CubeLevels[i / 36], CubeLevels[i / 6 % 6], CubeLevels[i % 6]);
    }

    /// <summary>
    /// Returns the nearest of the 16 basic ANSI colors (index into <see cref="Ansi16"/>).
    /// </summary>
    public static int ToAnsi16(Color color)
    {
        int best = 0;
        double bestDistance = double.MaxValue;
        for (int i = 0; i < Ansi16.Count; i++)
        {
            double d = Distance(color, Ansi16[i]);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = i;
            }
        }
        return best;
    }

    /// <summary>
    /// Relative luminance (0 = black, 1 = white), per WCAG.
    /// </summary>
    public static double Luminance(Color color)
    {
        static double Channel(byte c)
        {
            double v = c / 255.0;
            return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);
    }

    /// <summary>
    /// WCAG contrast ratio between two colors, from 1 (identical) to 21 (black on white).
    /// </summary>
    public static double ContrastRatio(Color a, Color b)
    {
        double la = Luminance(a), lb = Luminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    /// <summary>
    /// Picks the color mode from environment variables, in this order:
    /// <c>NO_COLOR</c> (any value) → <see cref="ColorMode.NoColor"/>;
    /// <c>COLORTERM=truecolor|24bit</c>, Windows Terminal (<c>WT_SESSION</c>) or a terminal known
    /// to support 24-bit color (<c>TERM_PROGRAM</c>) → <see cref="ColorMode.TrueColor"/>;
    /// <c>TERM=dumb</c> → <see cref="ColorMode.NoColor"/>;
    /// <c>TERM</c> containing <c>256color</c> → <see cref="ColorMode.Palette256"/>;
    /// any other <c>TERM</c> → <see cref="ColorMode.Palette16"/>;
    /// no <c>TERM</c>: Windows (whose console supports 24-bit color) → <see cref="ColorMode.TrueColor"/>,
    /// otherwise <see cref="ColorMode.Palette16"/>.
    /// </summary>
    public static ColorMode DetectColorMode(Func<string, string?> getEnvironmentVariable, bool isWindows)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);

        string? Get(string name) => getEnvironmentVariable(name) is { Length: > 0 } value ? value : null;

        if (Get("NO_COLOR") != null)
            return ColorMode.NoColor;

        var colorTerm = Get("COLORTERM")?.ToLowerInvariant();
        if (colorTerm is "truecolor" or "24bit")
            return ColorMode.TrueColor;

        if (Get("WT_SESSION") != null)
            return ColorMode.TrueColor;

        var termProgram = Get("TERM_PROGRAM");
        if (termProgram is "iTerm.app" or "WezTerm" or "ghostty" or "vscode" or "Hyper" or "Tabby")
            return ColorMode.TrueColor;

        var term = Get("TERM")?.ToLowerInvariant();
        if (term == "dumb")
            return ColorMode.NoColor;

        if (term != null && (term.Contains("truecolor") || term.Contains("direct")))
            return ColorMode.TrueColor;

        if (term != null && term.Contains("256color"))
            return ColorMode.Palette256;

        if (term != null)
            return ColorMode.Palette16;

        return isWindows ? ColorMode.TrueColor : ColorMode.Palette16;
    }

    /// <summary>
    /// Detects the color mode from the current process's environment.
    /// </summary>
    public static ColorMode DetectColorMode()
        => DetectColorMode(Environment.GetEnvironmentVariable, OperatingSystem.IsWindows());

    private static int NearestCubeIndex(byte value)
    {
        // Midpoints between cube levels 0, 95, 135, 175, 215, 255.
        if (value < 48) return 0;
        if (value < 115) return 1;
        return Math.Min(5, (value - 35) / 40);
    }

    private static double Distance(Color a, Color b)
    {
        // Weighted for perceived brightness ("redmean").
        double rMean = (a.R + b.R) / 2.0;
        int dr = a.R - b.R, dg = a.G - b.G, db = a.B - b.B;
        return (2 + rMean / 256) * dr * dr + 4 * dg * dg + (2 + (255 - rMean) / 256) * db * db;
    }
}
