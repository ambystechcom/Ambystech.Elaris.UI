using System.Drawing;
using Ambystech.Elaris.UI.Core;
using Ambystech.Elaris.UI.Enums;
using Ambystech.Elaris.UI.Rendering;

namespace Ambystech.Elaris.UI.Widgets.Layout;

/// <summary>
/// A frame widget with borders and an optional title.
/// </summary>
public class Frame : Container
{
    private string _title = string.Empty;
    private BorderStyle _borderStyle = BorderStyle.Single;
    private Color _borderColor;
    private int _cornerRadius = 2;
    
    public Frame(string title)
    {
        _title = title ?? string.Empty;
    }

    public Frame()
    {
    }

    /// <summary>
    /// Gets or sets the frame title.
    /// </summary>
    public string Title
    {
        get => _title;
        set => _title = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the border style.
    /// </summary>
    public BorderStyle BorderStyle
    {
        get => _borderStyle;
        set => _borderStyle = value;
    }

    /// <summary>
    /// Gets or sets the border color. If not set, uses ForegroundColor.
    /// </summary>
    public Color BorderColor
    {
        get => _borderColor.A == 0 ? ForegroundColor : _borderColor;
        set => _borderColor = value;
    }

    /// <summary>
    /// Gets or sets the corner radius. When greater than 0, corners will be rounded.
    /// </summary>
    public int CornerRadius
    {
        get => _cornerRadius;
        set => _cornerRadius = Math.Max(0, value);
    }

    protected override void OnRender(Screen screen)
    {
        if (Width < 2 || Height < 2)
            return;

        var rect = new Rectangle(X, Y, Width, Height);
        var borderColor = BorderColor;

        switch (_borderStyle)
        {
            case BorderStyle.Single:
                DrawSingleBorder(screen, rect, borderColor);
                break;
            case BorderStyle.Double:
                DrawDoubleBorder(screen, rect, borderColor);
                break;
            case BorderStyle.Rounded:
                DrawRoundedBorder(screen, rect, borderColor);
                break;
            case BorderStyle.None:
                break;
        }

        if (!string.IsNullOrEmpty(_title) && Width > 4)
        {
            int titleX = X + 2;
            int titleLength = Math.Min(_title.Length, Width - 4);
            string displayTitle = titleLength < _title.Length ? _title.Substring(0, titleLength) : _title;
            screen.WriteText(titleX, Y, $" {displayTitle} ", ForegroundColor, BackgroundColor, bold: true);
        }
    }

    private void DrawSingleBorder(Screen screen, Rectangle rect, Color color)
    {
        for (int x = rect.Left + 1; x < rect.Right - 1; x++)
        {
            screen.SetCell(x, rect.Top, new Cell('─', color, BackgroundColor));
            screen.SetCell(x, rect.Bottom - 1, new Cell('─', color, BackgroundColor));
        }

        for (int y = rect.Top + 1; y < rect.Bottom - 1; y++)
        {
            screen.SetCell(rect.Left, y, new Cell('│', color, BackgroundColor));
            screen.SetCell(rect.Right - 1, y, new Cell('│', color, BackgroundColor));
        }

        if (_cornerRadius > 0)
        {
            screen.SetCell(rect.Left, rect.Top, new Cell('╭', color, BackgroundColor));
            screen.SetCell(rect.Right - 1, rect.Top, new Cell('╮', color, BackgroundColor));
            screen.SetCell(rect.Left, rect.Bottom - 1, new Cell('╰', color, BackgroundColor));
            screen.SetCell(rect.Right - 1, rect.Bottom - 1, new Cell('╯', color, BackgroundColor));
        }
        else
        {
            screen.SetCell(rect.Left, rect.Top, new Cell('┌', color, BackgroundColor));
            screen.SetCell(rect.Right - 1, rect.Top, new Cell('┐', color, BackgroundColor));
            screen.SetCell(rect.Left, rect.Bottom - 1, new Cell('└', color, BackgroundColor));
            screen.SetCell(rect.Right - 1, rect.Bottom - 1, new Cell('┘', color, BackgroundColor));
        }
    }

    private void DrawDoubleBorder(Screen screen, Rectangle rect, Color color)
    {
        for (int x = rect.Left + 1; x < rect.Right - 1; x++)
        {
            screen.SetCell(x, rect.Top, new Cell('═', color, BackgroundColor));
            screen.SetCell(x, rect.Bottom - 1, new Cell('═', color, BackgroundColor));
        }

        for (int y = rect.Top + 1; y < rect.Bottom - 1; y++)
        {
            screen.SetCell(rect.Left, y, new Cell('║', color, BackgroundColor));
            screen.SetCell(rect.Right - 1, y, new Cell('║', color, BackgroundColor));
        }

        if (_cornerRadius > 0)
        {
            screen.SetCell(rect.Left, rect.Top, new Cell('╭', color, BackgroundColor));
            screen.SetCell(rect.Right - 1, rect.Top, new Cell('╮', color, BackgroundColor));
            screen.SetCell(rect.Left, rect.Bottom - 1, new Cell('╰', color, BackgroundColor));
            screen.SetCell(rect.Right - 1, rect.Bottom - 1, new Cell('╯', color, BackgroundColor));
        }
        else
        {
            screen.SetCell(rect.Left, rect.Top, new Cell('╔', color, BackgroundColor));
            screen.SetCell(rect.Right - 1, rect.Top, new Cell('╗', color, BackgroundColor));
            screen.SetCell(rect.Left, rect.Bottom - 1, new Cell('╚', color, BackgroundColor));
            screen.SetCell(rect.Right - 1, rect.Bottom - 1, new Cell('╝', color, BackgroundColor));
        }
    }

    private void DrawRoundedBorder(Screen screen, Rectangle rect, Color color)
    {
        for (int x = rect.Left + 1; x < rect.Right - 1; x++)
        {
            screen.SetCell(x, rect.Top, new Cell('─', color, BackgroundColor));
            screen.SetCell(x, rect.Bottom - 1, new Cell('─', color, BackgroundColor));
        }

        for (int y = rect.Top + 1; y < rect.Bottom - 1; y++)
        {
            screen.SetCell(rect.Left, y, new Cell('│', color, BackgroundColor));
            screen.SetCell(rect.Right - 1, y, new Cell('│', color, BackgroundColor));
        }

        screen.SetCell(rect.Left, rect.Top, new Cell('╭', color, BackgroundColor));
        screen.SetCell(rect.Right - 1, rect.Top, new Cell('╮', color, BackgroundColor));
        screen.SetCell(rect.Left, rect.Bottom - 1, new Cell('╰', color, BackgroundColor));
        screen.SetCell(rect.Right - 1, rect.Bottom - 1, new Cell('╯', color, BackgroundColor));
    }
}
