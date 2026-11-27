using System.Drawing;
using System.Text;
using Ambystech.Elaris.UI.Terminal;

namespace Ambystech.Elaris.UI.Rendering;

/// <summary>
/// Represents the terminal screen with double-buffered rendering.
/// </summary>
/// <remarks>
/// Widgets draw into the back buffer. <see cref="Render"/> compares it with what is
/// already on the terminal and writes only the cells that changed, moving the cursor
/// and changing colors only when needed.
/// <para>
/// Drawing with a transparent background (alpha 0) keeps the background that is already
/// in the buffer; cells that were never filled show the terminal's own background.
/// </para>
/// </remarks>
public class Screen
{
    private Cell[,] _frontBuffer;
    private Cell[,] _backBuffer;
    private readonly AnsiRenderer _renderer;
    private readonly ITerminal _terminal;
    private readonly StringBuilder _output = new(4096);
    private readonly object _lock = new();
    private readonly Stack<Rectangle> _clipStack = new();
    private Rectangle _clip;
    private ColorMode _colorMode = ColorMode.TrueColor;

    /// <summary>
    /// Gets the screen width in columns.
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// Gets the screen height in rows.
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// Gets the terminal this screen draws to.
    /// </summary>
    public ITerminal Terminal => _terminal;

    /// <summary>
    /// Gets or sets how colors are written: 24-bit, converted to the 256- or 16-color palette,
    /// or left out. Defaults to the terminal's <see cref="ITerminal.ColorMode"/>.
    /// Changing it redraws every cell on the next <see cref="Render"/>.
    /// </summary>
    public ColorMode ColorMode
    {
        get
        {
            lock (_lock)
                return _colorMode;
        }
        set
        {
            lock (_lock)
            {
                if (_colorMode == value)
                    return;

                _colorMode = value;
                _frontBuffer = new Cell[Height, Width];
            }
        }
    }

    /// <summary>
    /// Gets or sets the screen's background color (the theme background; transparent means the
    /// terminal's own, assumed dark). In <see cref="ColorMode.NoColor"/>, cells whose background
    /// clearly differs from it are drawn in reverse video so highlights stay visible.
    /// </summary>
    public Color BaseBackground { get; set; } = Color.Transparent;

    /// <summary>
    /// Gets the area that drawing is currently limited to. Drawing outside it is ignored.
    /// The whole screen unless a clip has been pushed with <see cref="PushClip"/>.
    /// </summary>
    public Rectangle ClipRectangle
    {
        get
        {
            lock (_lock)
                return _clip;
        }
    }

    /// <summary>
    /// Limits drawing to the intersection of <paramref name="area"/> and the current clip,
    /// until the returned scope is disposed (or <see cref="PopClip"/> is called).
    /// </summary>
    /// <example>
    /// <code>
    /// using (screen.PushClip(innerArea))
    /// {
    ///     screen.WriteText(x, y, longText, fg, bg); // cut off at the edge of innerArea
    /// }
    /// </code>
    /// </example>
    public ClipScope PushClip(Rectangle area)
    {
        lock (_lock)
        {
            _clipStack.Push(_clip);
            _clip = Rectangle.Intersect(_clip, area);
            return new ClipScope(this);
        }
    }

    /// <summary>
    /// Lets drawing reach the whole screen, ignoring clips pushed by parent widgets,
    /// until the returned scope is disposed. Use it for popups such as dropdowns and
    /// suggestion lists that extend outside their container.
    /// </summary>
    public ClipScope PushOverlay()
    {
        lock (_lock)
        {
            _clipStack.Push(_clip);
            _clip = FullScreen;
            return new ClipScope(this);
        }
    }

    /// <summary>
    /// Restores the clip that was active before the last <see cref="PushClip"/> or <see cref="PushOverlay"/>.
    /// </summary>
    public void PopClip()
    {
        lock (_lock)
        {
            if (_clipStack.Count == 0)
                throw new InvalidOperationException("PopClip was called more times than PushClip.");

            _clip = _clipStack.Pop();
        }
    }

    private Rectangle FullScreen => new(0, 0, Width, Height);

    /// <summary>
    /// A cell with a transparent background keeps the background already at that position,
    /// so text drawn on a colored panel doesn't cut holes in it.
    /// </summary>
    private Cell Composite(int x, int y, Cell cell)
    {
        if (cell.Background.A == 0)
            cell.Background = _backBuffer[y, x].Background;
        return cell;
    }

    private bool IsInClip(int x, int y)
        => x >= _clip.Left && x < _clip.Right && y >= _clip.Top && y < _clip.Bottom;

    /// <summary>
    /// Restores the previous clip when disposed. Returned by <see cref="PushClip"/> and <see cref="PushOverlay"/>.
    /// </summary>
    public readonly struct ClipScope : IDisposable
    {
        private readonly Screen? _screen;

        internal ClipScope(Screen screen) => _screen = screen;

        /// <summary>
        /// Restores the previous clip.
        /// </summary>
        public void Dispose() => _screen?.PopClip();
    }

    /// <summary>
    /// Creates a screen that draws to the console.
    /// </summary>
    public Screen(int width, int height)
        : this(width, height, new ConsoleTerminal())
    {
    }

    /// <summary>
    /// Creates a screen that draws to the given terminal.
    /// </summary>
    public Screen(int width, int height, ITerminal terminal)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        Width = width;
        Height = height;
        _frontBuffer = new Cell[height, width];
        _backBuffer = new Cell[height, width];
        _renderer = new AnsiRenderer();
        _clip = FullScreen;
        _colorMode = terminal.ColorMode;

        Clear();
    }

    /// <summary>
    /// Resizes the screen buffer. The next <see cref="Render"/> redraws every cell.
    /// </summary>
    public void Resize(int width, int height)
    {
        lock (_lock)
        {
            Width = width;
            Height = height;
            _frontBuffer = new Cell[height, width];
            _backBuffer = new Cell[height, width];
            _clipStack.Clear();
            _clip = FullScreen;
            Clear();
        }
    }

    /// <summary>
    /// Clears the back buffer with the default cell.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    _backBuffer[y, x] = Cell.Empty;
                }
            }
        }
    }

    /// <summary>
    /// Forces a full screen redraw by clearing both buffers and the terminal screen.
    /// Use this when colors or themes change to avoid rendering artifacts.
    /// </summary>
    public void Invalidate()
    {
        lock (_lock)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    _frontBuffer[y, x] = Cell.Empty;
                    _backBuffer[y, x] = Cell.Empty;
                }
            }

            _terminal.Write(_renderer.Reset() + _renderer.ClearScreen());
            _terminal.Flush();
        }
    }

    /// <summary>
    /// Gets the cell in the back buffer at the specified position: what will be on the
    /// terminal after the next <see cref="Render"/>.
    /// </summary>
    public Cell GetCell(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            throw new ArgumentOutOfRangeException(x < 0 || x >= Width ? nameof(x) : nameof(y));

        lock (_lock)
        {
            return _backBuffer[y, x];
        }
    }

    /// <summary>
    /// Sets a cell in the back buffer at the specified position.
    /// </summary>
    public void SetCell(int x, int y, Cell cell)
    {
        lock (_lock)
        {
            if (IsInClip(x, y))
                _backBuffer[y, x] = Composite(x, y, cell);
        }
    }

    /// <summary>
    /// Sets a cell in the back buffer at the specified point.
    /// </summary>
    public void SetCell(Point point, Cell cell)
        => SetCell(point.X, point.Y, cell);

    /// <summary>
    /// Writes text to the back buffer at the specified position.
    /// </summary>
    public void WriteText(int x, int y, string text, Color foreground, Color background,
        bool bold = false, bool italic = false, bool underline = false)
    {
        lock (_lock)
        {
            for (int i = 0; i < text.Length; i++)
            {
                int posX = x + i;
                if (posX >= _clip.Right)
                    break;

                if (IsInClip(posX, y))
                {
                    _backBuffer[y, posX] = Composite(posX, y, new Cell(text[i], foreground, background, bold, italic, underline));
                }
            }
        }
    }

    /// <summary>
    /// Writes text to the back buffer at the specified point.
    /// </summary>
    public void WriteText(Point point, string text, Color foreground, Color background,
        bool bold = false, bool italic = false, bool underline = false)
        => WriteText(point.X, point.Y, text, foreground, background, bold, italic, underline);

    /// <summary>
    /// Draws a filled rectangle in the back buffer.
    /// </summary>
    public void FillRectangle(Rectangle rect, char character, Color foreground, Color background)
    {
        // Skip filling if background is transparent
        if (background.A == 0)
            return;

        lock (_lock)
        {
            var area = Rectangle.Intersect(rect, _clip);
            for (int y = area.Top; y < area.Bottom; y++)
            {
                for (int x = area.Left; x < area.Right; x++)
                {
                    _backBuffer[y, x] = new Cell(character, foreground, background);
                }
            }
        }
    }

    /// <summary>
    /// Draws a rectangle border in the back buffer.
    /// </summary>
    public void DrawRectangle(Rectangle rect, Color foreground, Color background)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        lock (_lock)
        {
            void Put(int x, int y, char c)
            {
                if (IsInClip(x, y))
                    _backBuffer[y, x] = Composite(x, y, new Cell(c, foreground, background));
            }

            int right = rect.Right - 1;
            int bottom = rect.Bottom - 1;

            for (int x = rect.Left; x <= right; x++)
            {
                Put(x, rect.Top, '─');
                Put(x, bottom, '─');
            }

            for (int y = rect.Top; y <= bottom; y++)
            {
                Put(rect.Left, y, '│');
                Put(right, y, '│');
            }

            Put(rect.Left, rect.Top, '┌');
            Put(right, rect.Top, '┐');
            Put(rect.Left, bottom, '└');
            Put(right, bottom, '┘');
        }
    }

    /// <summary>
    /// Renders the back buffer to the terminal, only updating changed cells.
    /// Nothing is written when no cell changed.
    /// </summary>
    public void Render()
    {
        lock (_lock)
        {
            _output.Clear();

            // Where the terminal cursor is after the last character written; -1 when unknown.
            int cursorX = -1;
            int cursorY = -1;
            bool hasStyle = false;
            Cell lastStyle = default;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Cell current = _backBuffer[y, x];

                    if (current == _frontBuffer[y, x])
                        continue;

                    if (cursorX != x || cursorY != y)
                    {
                        _output.Append("\x1b[").Append(y + 1).Append(';').Append(x + 1).Append('H');
                    }

                    if (!hasStyle || !SameStyle(current, lastStyle))
                    {
                        AppendStyle(_output, current);
                        lastStyle = current;
                        hasStyle = true;
                    }

                    char c = Printable(current.Character);
                    _output.Append(c);
                    _frontBuffer[y, x] = current;

                    // The cursor advances one column for narrow characters. For anything whose
                    // width we can't be sure of, forget the position so the next cell moves explicitly.
                    if (IsNarrow(c))
                    {
                        cursorX = x + 1;
                        cursorY = y;
                    }
                    else
                    {
                        cursorX = -1;
                        cursorY = -1;
                    }
                }
            }

            if (_output.Length == 0)
                return;

            _output.Append(_renderer.Reset());
            _terminal.Write(_output.ToString());
            _terminal.Flush();
        }
    }

    private static bool SameStyle(Cell a, Cell b)
        => Cell.ColorKey(a.Foreground) == Cell.ColorKey(b.Foreground)
           && Cell.ColorKey(a.Background) == Cell.ColorKey(b.Background)
           && a.Bold == b.Bold
           && a.Italic == b.Italic
           && a.Underline == b.Underline;

    /// <summary>
    /// Appends one SGR sequence that resets the attributes and then sets the cell's style,
    /// encoding colors for <see cref="ColorMode"/>. Transparent colors use the terminal's default.
    /// </summary>
    private void AppendStyle(StringBuilder sb, Cell cell)
    {
        sb.Append("\x1b[0");

        if (cell.Bold) sb.Append(";1");
        if (cell.Italic) sb.Append(";3");
        if (cell.Underline) sb.Append(";4");

        var fg = cell.Foreground;
        var bg = cell.Background;
        bool hasFg = fg.A > 0;
        bool hasBg = bg.A > 0;

        switch (_colorMode)
        {
            case ColorMode.TrueColor:
                if (hasFg)
                    sb.Append(";38;2;").Append(fg.R).Append(';').Append(fg.G).Append(';').Append(fg.B);
                if (hasBg)
                    sb.Append(";48;2;").Append(bg.R).Append(';').Append(bg.G).Append(';').Append(bg.B);
                break;

            case ColorMode.Palette256:
            {
                int bgIndex = hasBg ? ColorConversion.ToPalette256(bg) : -1;
                if (hasFg)
                {
                    int fgIndex = ColorConversion.ToPalette256(fg);
                    if (hasBg && LostContrast(fg, bg, ColorConversion.FromPalette256(fgIndex), ColorConversion.FromPalette256(bgIndex)))
                        fgIndex = ReadableOn(ColorConversion.FromPalette256(bgIndex)) ? 16 : 231;
                    sb.Append(";38;5;").Append(fgIndex);
                }
                if (hasBg)
                    sb.Append(";48;5;").Append(bgIndex);
                break;
            }

            case ColorMode.Palette16:
            {
                int bgIndex = hasBg ? ColorConversion.ToAnsi16(bg) : -1;
                if (hasFg)
                {
                    int fgIndex = ColorConversion.ToAnsi16(fg);
                    if (hasBg && LostContrast(fg, bg, ColorConversion.Ansi16[fgIndex], ColorConversion.Ansi16[bgIndex]))
                        fgIndex = ReadableOn(ColorConversion.Ansi16[bgIndex]) ? 0 : 15;
                    sb.Append(';').Append(fgIndex < 8 ? 30 + fgIndex : 90 + fgIndex - 8);
                }
                if (hasBg)
                    sb.Append(';').Append(bgIndex < 8 ? 40 + bgIndex : 100 + bgIndex - 8);
                break;
            }

            case ColorMode.NoColor:
                if (hasBg && IsHighlight(bg))
                    sb.Append(";7");
                break;
        }

        sb.Append('m');
    }

    /// <summary>
    /// True when text was readable in 24-bit color (contrast ratio ≥ 2) but isn't after
    /// converting both colors to the palette.
    /// </summary>
    private static bool LostContrast(Color fg, Color bg, Color mappedFg, Color mappedBg)
        => ColorConversion.ContrastRatio(fg, bg) >= 2 && ColorConversion.ContrastRatio(mappedFg, mappedBg) < 2;

    /// <summary>
    /// True when black text contrasts more with <paramref name="background"/> than white text.
    /// </summary>
    private static bool ReadableOn(Color background)
        => ColorConversion.ContrastRatio(Color.Black, background) >= ColorConversion.ContrastRatio(Color.White, background);

    /// <summary>
    /// In <see cref="ColorMode.NoColor"/>: whether a background differs enough from the screen
    /// background to be shown in reverse video.
    /// </summary>
    private bool IsHighlight(Color background)
    {
        var baseColor = BaseBackground.A > 0 ? BaseBackground : Color.Black;
        return Math.Abs(ColorConversion.Luminance(background) - ColorConversion.Luminance(baseColor)) > 0.1;
    }

    /// <summary>
    /// Replaces characters that would move the cursor or start an escape sequence.
    /// </summary>
    private static char Printable(char c)
        => char.IsControl(c) ? ' ' : char.IsSurrogate(c) ? '�' : c;

    /// <summary>
    /// True for characters known to occupy exactly one column.
    /// </summary>
    private static bool IsNarrow(char c)
    {
        if (c < 0x0300)
            return true;

        // Box drawing and block elements, used for borders and progress bars.
        if (c >= 0x2500 && c <= 0x259F)
            return true;

        if (c >= 0x1100)
            return false;

        var category = char.GetUnicodeCategory(c);
        return category is not (System.Globalization.UnicodeCategory.NonSpacingMark
            or System.Globalization.UnicodeCategory.EnclosingMark
            or System.Globalization.UnicodeCategory.Format);
    }

    /// <summary>
    /// Swaps the front and back buffers and clears the new back buffer.
    /// </summary>
    public void Swap()
    {
        lock (_lock)
        {
            (_frontBuffer, _backBuffer) = (_backBuffer, _frontBuffer);
            Clear();
        }
    }

    /// <summary>
    /// Initializes the terminal for rendering: switches to the alternate screen,
    /// clears it and hides the cursor.
    /// </summary>
    public void Initialize()
    {
        _terminal.Initialize();
        _terminal.Write(_renderer.EnableAlternateBuffer() + _renderer.ClearScreen() + _renderer.HideCursor());
        _terminal.Flush();
    }

    /// <summary>
    /// Restores the terminal to its original state.
    /// </summary>
    public void Shutdown()
    {
        _terminal.Write(_renderer.Reset() + _renderer.ShowCursor() + _renderer.DisableAlternateBuffer());
        _terminal.Flush();
        _terminal.Restore();
    }
}
