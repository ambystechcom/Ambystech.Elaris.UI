using Ambystech.Elaris.UI.Rendering;

namespace Ambystech.Elaris.UI.Widgets.Input;

/// <summary>
/// A multi-line scrollable text view widget.
/// </summary>
public class TextView : Widget
{
    private readonly List<string> _lines = new();
    private int _scrollOffset = 0;
    private bool _autoScroll = true;
    private bool _wordWrap = false;

    /// <summary>
    /// Gets or sets whether to automatically scroll to the bottom when new lines are added.
    /// </summary>
    public bool AutoScroll
    {
        get => _autoScroll;
        set => _autoScroll = value;
    }

    /// <summary>
    /// Gets or sets whether to wrap long lines.
    /// </summary>
    public bool WordWrap
    {
        get => _wordWrap;
        set => _wordWrap = value;
    }

    /// <summary>
    /// Gets the current scroll offset.
    /// </summary>
    public int ScrollOffset => _scrollOffset;

    /// <summary>
    /// Gets the total number of lines.
    /// </summary>
    public int LineCount => _lines.Count;

    /// <summary>
    /// Gets whether this widget can receive keyboard focus.
    /// </summary>
    public override bool IsFocusable => true;

    public TextView() { }

    /// <summary>
    /// Adds a line of text.
    /// </summary>
    public void AppendLine(string text)
    {
        _lines.Add(text ?? string.Empty);

        if (_autoScroll)
        {
            ScrollToBottom();
        }
    }

    /// <summary>
    /// Adds multiple lines of text.
    /// </summary>
    public void AppendLines(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            _lines.Add(line ?? string.Empty);
        }

        if (_autoScroll)
        {
            ScrollToBottom();
        }
    }

    /// <summary>
    /// Sets the text content (replaces all lines).
    /// </summary>
    public void SetText(string text)
    {
        _lines.Clear();
        if (!string.IsNullOrEmpty(text))
        {
            _lines.AddRange(text.Split('\n'));
        }
        _scrollOffset = 0;
    }

    /// <summary>
    /// Clears all text.
    /// </summary>
    public new void Clear()
    {
        _lines.Clear();
        _scrollOffset = 0;
    }

    /// <summary>
    /// Scrolls up by the specified number of lines.
    /// </summary>
    public void ScrollUp(int lines = 1)
    {
        _scrollOffset = Math.Max(0, _scrollOffset - lines);
        _autoScroll = false;
    }

    /// <summary>
    /// Scrolls down by the specified number of lines.
    /// </summary>
    public void ScrollDown(int lines = 1)
    {
        int maxOffset = Math.Max(0, _lines.Count - Height);
        _scrollOffset = Math.Min(maxOffset, _scrollOffset + lines);

        // Re-enable auto-scroll if we're at the bottom
        if (_scrollOffset >= maxOffset)
        {
            _autoScroll = true;
        }
    }

    /// <summary>
    /// Scrolls to the top.
    /// </summary>
    public void ScrollToTop()
    {
        _scrollOffset = 0;
        _autoScroll = false;
    }

    /// <summary>
    /// Scrolls to the bottom.
    /// </summary>
    public void ScrollToBottom()
    {
        _scrollOffset = Math.Max(0, _lines.Count - Height);
        _autoScroll = true;
    }

    protected override void OnRender(Screen screen)
    {
        if (Width <= 0 || Height <= 0)
            return;

        screen.FillRectangle(Bounds, ' ', ForegroundColor, BackgroundColor);

        int visibleLines = Math.Min(Height, _lines.Count - _scrollOffset);

        for (int i = 0; i < visibleLines; i++)
        {
            int lineIndex = _scrollOffset + i;
            if (lineIndex >= _lines.Count)
                break;

            string line = _lines[lineIndex];

            if (_wordWrap && line.Length > Width)
            {
                line = line.Substring(0, Width);
            }
            else if (line.Length > Width)
            {
                line = line.Substring(0, Width);
            }

            int renderY = Y + i;
            screen.WriteText(X, renderY, line, ForegroundColor, BackgroundColor);
        }
    }

    protected internal override bool OnKeyPress(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                ScrollUp(1);
                return true;

            case ConsoleKey.DownArrow:
                ScrollDown(1);
                return true;

            case ConsoleKey.PageUp:
                ScrollUp(Height);
                return true;

            case ConsoleKey.PageDown:
                ScrollDown(Height);
                return true;

            case ConsoleKey.Home:
                ScrollToTop();
                return true;

            case ConsoleKey.End:
                ScrollToBottom();
                return true;
        }

        return base.OnKeyPress(key);
    }

    protected override void OnBoundsChanged()
    {
        base.OnBoundsChanged();

        if (_autoScroll)
        {
            ScrollToBottom();
        }
        else
        {
            int maxOffset = Math.Max(0, _lines.Count - Height);
            _scrollOffset = Math.Min(_scrollOffset, maxOffset);
        }
    }
}
