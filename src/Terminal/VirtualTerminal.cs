using System.Drawing;
using System.Text;
using Ambystech.Elaris.UI.Rendering;

namespace Ambystech.Elaris.UI.Terminal;

/// <summary>
/// An in-memory <see cref="ITerminal"/> for tests. It decodes the ANSI escape sequences
/// written to it into a grid of cells, and returns key presses queued with
/// <see cref="EnqueueKey"/>. All members are thread-safe.
/// </summary>
/// <remarks>
/// Supports the subset of ANSI that Elaris writes: cursor position (<c>CSI row;col H</c>),
/// SGR attributes and 24-bit colors (<c>CSI ... m</c>), clear screen (<c>CSI 2 J</c>),
/// clear to end of line (<c>CSI K</c>), cursor visibility (<c>CSI ?25 h/l</c>) and the
/// alternate screen (<c>CSI ?1049 h/l</c>). Every character advances the cursor by one column.
/// In decoded cells, the terminal's default foreground is <see cref="Color.Empty"/> and the
/// default background is <see cref="Color.Transparent"/>.
/// </remarks>
public sealed class VirtualTerminal : ITerminal
{
    private static readonly Color DefaultForeground = Color.Empty;
    private static readonly Color DefaultBackground = Color.Transparent;

    private readonly object _lock = new();
    private readonly Queue<ConsoleKeyInfo> _keys = new();
    private readonly StringBuilder _pending = new();
    private readonly StringBuilder _output = new();
    private Cell[,] _main;
    private Cell[,] _alternate;
    private int _width;
    private int _height;
    private int _cursorX;
    private int _cursorY;
    private Color _foreground = DefaultForeground;
    private Color _background = DefaultBackground;
    private bool _bold;
    private bool _italic;
    private bool _underline;
    private bool _isAlternateScreen;
    private bool _isCursorVisible = true;
    private bool _isInitialized;
    private bool _treatControlCAsInput;
    private int _flushCount;

    /// <summary>
    /// Creates a virtual terminal with the given size.
    /// </summary>
    public VirtualTerminal(int width = 80, int height = 24)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        _width = width;
        _height = height;
        _main = CreateGrid(width, height);
        _alternate = CreateGrid(width, height);
    }

    /// <inheritdoc />
    public int Width { get { lock (_lock) return _width; } }

    /// <inheritdoc />
    public int Height { get { lock (_lock) return _height; } }

    /// <inheritdoc />
    public bool TreatControlCAsInput
    {
        get { lock (_lock) return _treatControlCAsInput; }
        set { lock (_lock) _treatControlCAsInput = value; }
    }

    /// <summary>
    /// Gets whether the alternate screen is active.
    /// </summary>
    public bool IsAlternateScreen { get { lock (_lock) return _isAlternateScreen; } }

    /// <summary>
    /// Gets whether the cursor is visible.
    /// </summary>
    public bool IsCursorVisible { get { lock (_lock) return _isCursorVisible; } }

    /// <summary>
    /// Gets whether <see cref="Initialize"/> has been called without a matching <see cref="Restore"/>.
    /// </summary>
    public bool IsInitialized { get { lock (_lock) return _isInitialized; } }

    /// <summary>
    /// Gets how many times <see cref="Flush"/> has been called.
    /// </summary>
    public int FlushCount { get { lock (_lock) return _flushCount; } }

    /// <summary>
    /// Changes the terminal size, as if the window had been resized. The screen is cleared.
    /// </summary>
    public void Resize(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        lock (_lock)
        {
            _width = width;
            _height = height;
            _main = CreateGrid(width, height);
            _alternate = CreateGrid(width, height);
            _cursorX = 0;
            _cursorY = 0;
        }
    }

    /// <summary>
    /// Queues a key press to be returned by <see cref="TryReadKey"/>.
    /// </summary>
    public void EnqueueKey(ConsoleKeyInfo key)
    {
        lock (_lock)
            _keys.Enqueue(key);
    }

    /// <summary>
    /// Queues a key press to be returned by <see cref="TryReadKey"/>.
    /// </summary>
    public void EnqueueKey(ConsoleKey key, char keyChar = '\0', bool shift = false, bool alt = false, bool control = false)
        => EnqueueKey(new ConsoleKeyInfo(keyChar, key, shift, alt, control));

    /// <summary>
    /// Queues one key press per character, as if the text had been typed.
    /// </summary>
    public void EnqueueText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        foreach (char c in text)
        {
            var key = c switch
            {
                >= 'a' and <= 'z' => (ConsoleKey)(c - 'a' + (int)ConsoleKey.A),
                >= 'A' and <= 'Z' => (ConsoleKey)(c - 'A' + (int)ConsoleKey.A),
                >= '0' and <= '9' => (ConsoleKey)(c - '0' + (int)ConsoleKey.D0),
                ' ' => ConsoleKey.Spacebar,
                '\n' or '\r' => ConsoleKey.Enter,
                '\t' => ConsoleKey.Tab,
                _ => ConsoleKey.NoName,
            };

            EnqueueKey(new ConsoleKeyInfo(c, key, char.IsUpper(c), false, false));
        }
    }

    /// <summary>
    /// Gets the decoded cell at the given position on the active screen.
    /// </summary>
    public Cell GetCell(int x, int y)
    {
        lock (_lock)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height)
                throw new ArgumentOutOfRangeException(x < 0 || x >= _width ? nameof(x) : nameof(y));

            return ActiveGrid[y, x];
        }
    }

    /// <summary>
    /// Gets the text of one row on the active screen, without trailing spaces.
    /// </summary>
    public string GetLine(int y)
    {
        lock (_lock)
        {
            var sb = new StringBuilder(_width);
            for (int x = 0; x < _width; x++)
                sb.Append(ActiveGrid[y, x].Character);
            return sb.ToString().TrimEnd();
        }
    }

    /// <summary>
    /// Gets the text of the active screen, one line per row, without trailing spaces or blank rows.
    /// </summary>
    public string GetText()
    {
        lock (_lock)
        {
            var lines = new string[_height];
            for (int y = 0; y < _height; y++)
                lines[y] = GetLine(y);
            return string.Join('\n', lines).TrimEnd('\n');
        }
    }

    /// <summary>
    /// Returns everything written since the last call, as raw text including escape sequences.
    /// </summary>
    public string TakeOutput()
    {
        lock (_lock)
        {
            var text = _output.ToString();
            _output.Clear();
            return text;
        }
    }

    /// <inheritdoc />
    public void Initialize()
    {
        lock (_lock)
            _isInitialized = true;
    }

    /// <inheritdoc />
    public void Restore()
    {
        lock (_lock)
            _isInitialized = false;
    }

    /// <inheritdoc />
    public void Write(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        lock (_lock)
        {
            _output.Append(text);
            foreach (char c in text)
                Process(c);
        }
    }

    /// <inheritdoc />
    public void Flush()
    {
        lock (_lock)
            _flushCount++;
    }

    /// <inheritdoc />
    public bool TryReadKey(out ConsoleKeyInfo key)
    {
        lock (_lock)
            return _keys.TryDequeue(out key);
    }

    private Cell[,] ActiveGrid => _isAlternateScreen ? _alternate : _main;

    private static Cell[,] CreateGrid(int width, int height)
    {
        var grid = new Cell[height, width];
        Fill(grid, BlankCell);
        return grid;
    }

    private static Cell BlankCell => new(' ', DefaultForeground, DefaultBackground);

    private static void Fill(Cell[,] grid, Cell cell)
    {
        for (int y = 0; y < grid.GetLength(0); y++)
            for (int x = 0; x < grid.GetLength(1); x++)
                grid[y, x] = cell;
    }

    private void Process(char c)
    {
        if (_pending.Length == 0)
        {
            if (c == '\x1b')
            {
                _pending.Append(c);
                return;
            }

            Print(c);
            return;
        }

        _pending.Append(c);

        if (_pending.Length == 2)
        {
            // Only CSI (ESC [) sequences are supported; drop anything else.
            if (c != '[')
                _pending.Clear();
            return;
        }

        if (c >= '@' && c <= '~')
        {
            ExecuteCsi(_pending.ToString(2, _pending.Length - 3), c);
            _pending.Clear();
        }
    }

    private void Print(char c)
    {
        switch (c)
        {
            case '\r':
                _cursorX = 0;
                return;
            case '\n':
                _cursorY = Math.Min(_height - 1, _cursorY + 1);
                return;
        }

        if (char.IsControl(c))
            return;

        if (_cursorX >= _width)
        {
            // Deferred wrap, as in real terminals.
            _cursorX = 0;
            _cursorY = Math.Min(_height - 1, _cursorY + 1);
        }

        ActiveGrid[_cursorY, _cursorX] = new Cell(c, _foreground, _background, _bold, _italic, _underline);
        _cursorX++;
    }

    private void ExecuteCsi(string parameters, char final)
    {
        bool isPrivate = parameters.StartsWith('?');
        if (isPrivate)
            parameters = parameters[1..];

        var args = parameters.Length == 0
            ? []
            : parameters.Split(';').Select(p => int.TryParse(p, out var n) ? n : 0).ToArray();

        int Arg(int index, int fallback) => index < args.Length && args[index] != 0 ? args[index] : fallback;

        switch (final)
        {
            case 'H' or 'f':
                _cursorY = Math.Clamp(Arg(0, 1) - 1, 0, _height - 1);
                _cursorX = Math.Clamp(Arg(1, 1) - 1, 0, _width - 1);
                break;

            case 'J' when Arg(0, 0) == 2:
                Fill(ActiveGrid, BlankCell);
                break;

            case 'K':
                for (int x = Math.Min(_cursorX, _width); x < _width; x++)
                    ActiveGrid[_cursorY, x] = BlankCell;
                break;

            case 'm':
                ApplySgr(args);
                break;

            case 'h' or 'l' when isPrivate:
                bool enable = final == 'h';
                foreach (var mode in args)
                {
                    if (mode == 25)
                    {
                        _isCursorVisible = enable;
                    }
                    else if (mode == 1049 && enable != _isAlternateScreen)
                    {
                        _isAlternateScreen = enable;
                        if (enable)
                            Fill(_alternate, BlankCell);
                    }
                }
                break;
        }
    }

    private void ApplySgr(int[] args)
    {
        if (args.Length == 0)
        {
            ResetAttributes();
            return;
        }

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case 0: ResetAttributes(); break;
                case 1: _bold = true; break;
                case 3: _italic = true; break;
                case 4: _underline = true; break;
                case 22: _bold = false; break;
                case 23: _italic = false; break;
                case 24: _underline = false; break;
                case 39: _foreground = DefaultForeground; break;
                case 49: _background = DefaultBackground; break;
                case 38 or 48 when i + 4 < args.Length && args[i + 1] == 2:
                    var color = Color.FromArgb(255, args[i + 2], args[i + 3], args[i + 4]);
                    if (args[i] == 38)
                        _foreground = color;
                    else
                        _background = color;
                    i += 4;
                    break;
            }
        }
    }

    private void ResetAttributes()
    {
        _foreground = DefaultForeground;
        _background = DefaultBackground;
        _bold = false;
        _italic = false;
        _underline = false;
    }
}
