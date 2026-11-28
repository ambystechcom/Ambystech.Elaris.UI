namespace Ambystech.Elaris.UI.Rendering;

/// <summary>
/// How many colors the terminal can show. <see cref="Screen"/> converts colors to fit.
/// </summary>
public enum ColorMode
{
    /// <summary>24-bit RGB (<c>ESC[38;2;r;g;bm</c>).</summary>
    TrueColor,

    /// <summary>The xterm 256-color palette (<c>ESC[38;5;nm</c>).</summary>
    Palette256,

    /// <summary>The 16 basic ANSI colors (<c>ESC[30–37m</c>, <c>ESC[90–97m</c>).</summary>
    Palette16,

    /// <summary>
    /// No colors, only bold, italic, underline and reverse video. Used when <c>NO_COLOR</c> is set.
    /// Cells whose background clearly differs from the screen background (selections, focused
    /// buttons, status bars) are shown in reverse video so they stay visible.
    /// </summary>
    NoColor,
}
