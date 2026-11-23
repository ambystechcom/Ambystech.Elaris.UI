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
/// </remarks>
public class Screen
{
    private Cell[,] _frontBuffer;
    private Cell[,] _backBuffer;
    private readonly AnsiRenderer _renderer;
    private readonly ITerminal _terminal;
    private readonly StringBuilder _output = new(4096);
    private readonly object _lock = new();

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
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;

        lock (_lock)
        {
            _backBuffer[y, x] = cell;
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
                if (posX >= Width)
                    break;

                if (posX >= 0 && y >= 0 && y < Height)
                {
                    _backBuffer[y, posX] = new Cell(text[i], foreground, background, bold, italic, underline);
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
            for (int y = rect.Top; y < rect.Bottom && y < Height; y++)
            {
                for (int x = rect.Left; x < rect.Right && x < Width; x++)
                {
                    if (x >= 0 && y >= 0)
                    {
                        _backBuffer[y, x] = new Cell(character, foreground, background);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Draws a rectangle border in the back buffer.
    /// </summary>
    public void DrawRectangle(Rectangle rect, Color foreground, Color background)
    {
        lock (_lock)
        {
            // Top and bottom borders
            for (int x = rect.Left; x < rect.Right && x < Width; x++)
            {
                if (x >= 0)
                {
                    if (rect.Top >= 0 && rect.Top < Height)
                        _backBuffer[rect.Top, x] = new Cell('─', foreground, background);
                    if (rect.Bottom - 1 >= 0 && rect.Bottom - 1 < Height)
                        _backBuffer[rect.Bottom - 1, x] = new Cell('─', foreground, background);
                }
            }

            // Left and right borders
            for (int y = rect.Top; y < rect.Bottom && y < Height; y++)
            {
                if (y >= 0)
                {
                    if (rect.Left >= 0 && rect.Left < Width)
                        _backBuffer[y, rect.Left] = new Cell('│', foreground, background);
                    if (rect.Right - 1 >= 0 && rect.Right - 1 < Width)
                        _backBuffer[y, rect.Right - 1] = new Cell('│', foreground, background);
                }
            }

            // Corners
            if (rect.Left >= 0 && rect.Left < Width && rect.Top >= 0 && rect.Top < Height)
                _backBuffer[rect.Top, rect.Left] = new Cell('┌', foreground, background);
            if (rect.Right - 1 >= 0 && rect.Right - 1 < Width && rect.Top >= 0 && rect.Top < Height)
                _backBuffer[rect.Top, rect.Right - 1] = new Cell('┐', foreground, background);
            if (rect.Left >= 0 && rect.Left < Width && rect.Bottom - 1 >= 0 && rect.Bottom - 1 < Height)
                _backBuffer[rect.Bottom - 1, rect.Left] = new Cell('└', foreground, background);
            if (rect.Right - 1 >= 0 && rect.Right - 1 < Width && rect.Bottom - 1 >= 0 && rect.Bottom - 1 < Height)
                _backBuffer[rect.Bottom - 1, rect.Right - 1] = new Cell('┘', foreground, background);
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
    /// Appends one SGR sequence that resets the attributes and then sets the cell's style.
    /// Transparent colors use the terminal's default.
    /// </summary>
    private static void AppendStyle(StringBuilder sb, Cell cell)
    {
        sb.Append("\x1b[0");

        if (cell.Bold) sb.Append(";1");
        if (cell.Italic) sb.Append(";3");
        if (cell.Underline) sb.Append(";4");

        if (cell.Foreground.A > 0)
        {
            sb.Append(";38;2;").Append(cell.Foreground.R)
              .Append(';').Append(cell.Foreground.G)
              .Append(';').Append(cell.Foreground.B);
        }

        if (cell.Background.A > 0)
        {
            sb.Append(";48;2;").Append(cell.Background.R)
              .Append(';').Append(cell.Background.G)
              .Append(';').Append(cell.Background.B);
        }

        sb.Append('m');
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
