using System.Drawing;
using System.Text;

namespace Ambystech.Elaris.UI.Rendering;

/// <summary>
/// Renders text with 24-bit RGB colors using ANSI escape sequences.
/// </summary>
public class AnsiRenderer
{
    private const string ESC = "\x1b[";
    private const string RESET = "\x1b[0m";

    /// <summary>
    /// Renders text with foreground color.
    /// </summary>
    public string WithForeground(string text, Color color)
        => $"{ESC}38;2;{color.R};{color.G};{color.B}m{text}{RESET}";

    /// <summary>
    /// Renders text with background color.
    /// </summary>
    public string WithBackground(string text, Color color)
        => $"{ESC}48;2;{color.R};{color.G};{color.B}m{text}{RESET}";

    /// <summary>
    /// Renders text with foreground and background colors.
    /// </summary>
    public string WithColors(string text, Color foreground, Color background)
        => $"{ESC}38;2;{foreground.R};{foreground.G};{foreground.B};48;2;{background.R};{background.G};{background.B}m{text}{RESET}";

    /// <summary>
    /// Renders text with style (bold, italic, underline).
    /// </summary>
    public string WithStyle(string text, Color foreground, bool bold = false, bool italic = false, bool underline = false)
    {
        var sb = new StringBuilder(ESC);

        if (bold) sb.Append("1;");
        if (italic) sb.Append("3;");
        if (underline) sb.Append("4;");

        sb.Append($"38;2;{foreground.R};{foreground.G};{foreground.B}m");
        sb.Append(text);
        sb.Append(RESET);

        return sb.ToString();
    }

    /// <summary>
    /// Moves cursor to specified position (1-indexed).
    /// </summary>
    public string MoveCursor(int row, int col)
        => $"{ESC}{row};{col}H";

    /// <summary>
    /// Clears the entire screen.
    /// </summary>
    public string ClearScreen()
        => $"{ESC}2J";

    /// <summary>
    /// Clears from cursor to end of line.
    /// </summary>
    public string ClearLine()
        => $"{ESC}K";

    /// <summary>
    /// Hides the cursor.
    /// </summary>
    public string HideCursor()
        => $"{ESC}?25l";

    /// <summary>
    /// Shows the cursor.
    /// </summary>
    public string ShowCursor()
        => $"{ESC}?25h";

    /// <summary>
    /// Enables alternate screen buffer.
    /// </summary>
    public string EnableAlternateBuffer()
        => $"{ESC}?1049h";

    /// <summary>
    /// Disables alternate screen buffer.
    /// </summary>
    public string DisableAlternateBuffer()
        => $"{ESC}?1049l";

    /// <summary>
    /// Resets all text attributes.
    /// </summary>
    public string Reset()
        => RESET;
}
