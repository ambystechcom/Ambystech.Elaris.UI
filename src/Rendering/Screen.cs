using System.Drawing;
using System.Text;
using Ambystech.Elaris.UI.Core;

namespace Ambystech.Elaris.UI.Rendering;

/// <summary>
/// Represents the terminal screen with double-buffered rendering.
/// </summary>
public class Screen
{
    private Cell[,] _frontBuffer;
    private Cell[,] _backBuffer;
    private readonly AnsiRenderer _renderer;
    private readonly object _lock = new();

    public int Width { get; private set; }
    public int Height { get; private set; }

    public Screen(int width, int height)
    {
        Width = width;
        Height = height;
        _frontBuffer = new Cell[height, width];
        _backBuffer = new Cell[height, width];
        _renderer = new AnsiRenderer();

        Clear();
    }

    /// <summary>
    /// Resizes the screen buffer.
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
            // Clear both buffers
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    _frontBuffer[y, x] = Cell.Empty;
                    _backBuffer[y, x] = Cell.Empty;
                }
            }

            // Clear the actual terminal screen
            Console.Write(_renderer.ClearScreen());
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
    /// Renders the back buffer to the console, only updating changed cells.
    /// </summary>
    public void Render()
    {
        lock (_lock)
        {
            var output = new StringBuilder();
            output.Append(_renderer.HideCursor());

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Cell current = _backBuffer[y, x];
                    Cell previous = _frontBuffer[y, x];

                    // Only render if cell has changed
                    if (current != previous)
                    {
                        output.Append(_renderer.MoveCursor(y + 1, x + 1));

                        // Build ANSI sequence for the cell
                        output.Append("\x1b[");

                        if (current.Bold) output.Append("1;");
                        if (current.Italic) output.Append("3;");
                        if (current.Underline) output.Append("4;");

                        output.Append($"38;2;{current.Foreground.R};{current.Foreground.G};{current.Foreground.B}");

                        // Only render background if not transparent (alpha > 0)
                        if (current.Background.A > 0)
                        {
                            output.Append($";48;2;{current.Background.R};{current.Background.G};{current.Background.B}");
                        }

                        output.Append("m");
                        output.Append(current.Character);
                        output.Append("\x1b[0m");

                        _frontBuffer[y, x] = current;
                    }
                }
            }

            output.Append(_renderer.ShowCursor());
            Console.Write(output.ToString());
            Console.Out.Flush();
        }
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
    /// Initializes the terminal for rendering.
    /// </summary>
    public void Initialize()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Write(_renderer.EnableAlternateBuffer());
        Console.Write(_renderer.ClearScreen());
        Console.Write(_renderer.HideCursor());
        Console.CursorVisible = false;
    }

    /// <summary>
    /// Restores the terminal to its original state.
    /// </summary>
    public void Shutdown()
    {
        Console.Write(_renderer.ShowCursor());
        Console.Write(_renderer.DisableAlternateBuffer());
        Console.Write(_renderer.Reset());
        Console.CursorVisible = true;
    }
}
