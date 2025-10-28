using Ambystech.Elaris.UI.Enums;
using Ambystech.Elaris.UI.Rendering;

namespace Ambystech.Elaris.UI.Widgets.Display;

/// <summary>
/// A simple text label widget.
/// </summary>
public class Label(string text) : Widget
{
    private string _text = text ?? string.Empty;
    private bool _bold;
    private bool _italic;
    private bool _underline;
    private HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Left;
    private VerticalAlignment _verticalAlignment = VerticalAlignment.Top;

    /// <summary>
    /// Gets or sets the text to display.
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value ?? string.Empty;
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the text is bold.
    /// </summary>
    public bool Bold
    {
        get => _bold;
        set => _bold = value;
    }

    /// <summary>
    /// Gets or sets whether the text is italic.
    /// </summary>
    public bool Italic
    {
        get => _italic;
        set => _italic = value;
    }

    /// <summary>
    /// Gets or sets whether the text is underlined.
    /// </summary>
    public bool Underline
    {
        get => _underline;
        set => _underline = value;
    }

    /// <summary>
    /// Gets or sets the horizontal text alignment.
    /// </summary>
    public HorizontalAlignment HorizontalAlignment
    {
        get => _horizontalAlignment;
        set => _horizontalAlignment = value;
    }

    /// <summary>
    /// Gets or sets the vertical text alignment.
    /// </summary>
    public VerticalAlignment VerticalAlignment
    {
        get => _verticalAlignment;
        set => _verticalAlignment = value;
    }

    protected override void OnRender(Screen screen)
    {
        if (string.IsNullOrEmpty(_text) || Width <= 0 || Height <= 0)
            return;

        var lines = _text.Split(['\r', '\n'], StringSplitOptions.None)
                         .Where(line => line.Length > 0 || _text.Contains('\n'))
                         .ToList();

        if (lines.Count == 0)
            return;

        int startY = Y;
        switch (_verticalAlignment)
        {
            case VerticalAlignment.Middle:
                startY = Y + (Height - lines.Count) / 2;
                break;
            case VerticalAlignment.Bottom:
                startY = Y + Height - lines.Count;
                break;
        }

        for (int i = 0; i < lines.Count; i++)
        {
            int lineY = startY + i;
            if (lineY < Y || lineY >= Y + Height)
                continue;

            string line = lines[i];

            line = line.Replace("\t", "    ");

            int lineX = X;
            switch (_horizontalAlignment)
            {
                case HorizontalAlignment.Center:
                    lineX = X + Math.Max(0, (Width - line.Length) / 2);
                    break;
                case HorizontalAlignment.Right:
                    lineX = X + Math.Max(0, Width - line.Length);
                    break;
            }

            int availableWidth = Math.Max(0, X + Width - lineX);
            if (line.Length > availableWidth)
                line = line[..availableWidth];

            if (line.Length > 0)
            {
                screen.WriteText(lineX, lineY, line, ForegroundColor, BackgroundColor, _bold, _italic, _underline);
            }
        }
    }
}
