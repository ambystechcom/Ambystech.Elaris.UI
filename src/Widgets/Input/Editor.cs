using System.Drawing;
using System.Text;
using Ambystech.Elaris.UI.Core;
using Ambystech.Elaris.UI.Rendering;

namespace Ambystech.Elaris.UI.Widgets.Input;

/// <summary>
/// A multi-line text editor widget with cursor navigation, line numbers, and undo/redo support.
/// </summary>
public class Editor : Widget
{
    private readonly List<string> _lines = [];
    private int _cursorLine = 0;
    private int _cursorColumn = 0;
    protected int _scrollOffsetY = 0;
    protected int _scrollOffsetX = 0;
    private bool _showLineNumbers = false;
    private bool _wordWrap = false;
    private Color _lineNumberColor = Color.Gray;
    private Point? _selectionStart;
    private Point? _selectionEnd;
    private readonly Stack<EditorState> _undoStack = new();
    private readonly Stack<EditorState> _redoStack = new();
    private const int MaxUndoHistory = 50;
    protected bool _hasFocus = false;

    /// <summary>
    /// Gets or sets the full text content.
    /// </summary>
    public string Text
    {
        get => string.Join("\n", _lines);
        set
        {
            _lines.Clear();
            if (!string.IsNullOrEmpty(value))
            {
                _lines.AddRange(value.Split('\n'));
            }
            else
            {
                _lines.Add(string.Empty);
            }
            _cursorLine = 0;
            _cursorColumn = 0;
            _scrollOffsetY = 0;
            _scrollOffsetX = 0;
            EnsureCursorVisible();
        }
    }

    /// <summary>
    /// Gets the individual lines of text.
    /// </summary>
    public IReadOnlyList<string> Lines => _lines.AsReadOnly();

    /// <summary>
    /// Gets or sets the current cursor line (0-based).
    /// </summary>
    public int CursorLine
    {
        get => _cursorLine;
        set
        {
            _cursorLine = Math.Clamp(value, 0, Math.Max(0, _lines.Count - 1));
            EnsureCursorInBounds();
            EnsureCursorVisible();
            CursorMoved?.Invoke(_cursorLine, _cursorColumn);
        }
    }

    /// <summary>
    /// Gets or sets the current cursor column (0-based).
    /// </summary>
    public int CursorColumn
    {
        get => _cursorColumn;
        set
        {
            _cursorColumn = Math.Max(0, value);
            EnsureCursorInBounds();
            EnsureCursorVisible();
            CursorMoved?.Invoke(_cursorLine, _cursorColumn);
        }
    }

    /// <summary>
    /// Gets or sets whether to display line numbers.
    /// </summary>
    public bool ShowLineNumbers
    {
        get => _showLineNumbers;
        set => _showLineNumbers = value;
    }

    /// <summary>
    /// Gets or sets whether to enable word wrapping.
    /// </summary>
    public bool WordWrap
    {
        get => _wordWrap;
        set => _wordWrap = value;
    }

    /// <summary>
    /// Gets or sets the color for line numbers.
    /// </summary>
    public Color LineNumberColor
    {
        get => _lineNumberColor;
        set => _lineNumberColor = value;
    }

    /// <summary>
    /// Gets or sets the selection start position.
    /// </summary>
    public Point? SelectionStart
    {
        get => _selectionStart;
        set
        {
            _selectionStart = value;
            if (value.HasValue)
            {
                _selectionEnd ??= value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the selection end position.
    /// </summary>
    public Point? SelectionEnd
    {
        get => _selectionEnd;
        set => _selectionEnd = value;
    }

    /// <summary>
    /// Gets whether this widget can receive keyboard focus.
    /// </summary>
    public override bool IsFocusable => true;

    /// <summary>
    /// Event raised when the text content changes.
    /// </summary>
    public event Action<string>? TextChanged;

    /// <summary>
    /// Event raised when the cursor position changes.
    /// </summary>
    public event Action<int, int>? CursorMoved;

    /// <summary>
    /// Event raised when a line is modified.
    /// </summary>
    public event Action<int>? LineChanged;

    public Editor()
    {
        _lines.Add(string.Empty);
        BackgroundColor = Color.Black;
        ForegroundColor = Color.White;
    }

    /// <summary>
    /// Inserts text at the current cursor position.
    /// </summary>
    public void InsertText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        SaveState();

        var linesToInsert = text.Split('\n');
        var currentLine = _lines[_cursorLine];

        if (linesToInsert.Length == 1)
        {
            _lines[_cursorLine] = currentLine.Insert(_cursorColumn, linesToInsert[0]);
            _cursorColumn += linesToInsert[0].Length;
        }
        else
        {
            var firstPart = currentLine[.._cursorColumn];
            var lastPart = currentLine[_cursorColumn..];
            _lines[_cursorLine] = firstPart + linesToInsert[0];

            for (int i = 1; i < linesToInsert.Length - 1; i++)
            {
                _lines.Insert(_cursorLine + i, linesToInsert[i]);
            }

            _lines.Insert(_cursorLine + linesToInsert.Length - 1, linesToInsert[^1] + lastPart);
            _cursorLine += linesToInsert.Length - 1;
            _cursorColumn = linesToInsert[^1].Length;
        }

        EnsureCursorInBounds();
        EnsureCursorVisible();
        TextChanged?.Invoke(Text);
        LineChanged?.Invoke(_cursorLine);
        ClearRedoStack();
    }

    /// <summary>
    /// Deletes the character at the current cursor position.
    /// </summary>
    public void DeleteChar()
    {
        if (HasSelection())
        {
            DeleteSelection();
            return;
        }

        if (_cursorColumn < _lines[_cursorLine].Length)
        {
            SaveState();
            _lines[_cursorLine] = _lines[_cursorLine].Remove(_cursorColumn, 1);
            EnsureCursorInBounds();
            TextChanged?.Invoke(Text);
            LineChanged?.Invoke(_cursorLine);
            ClearRedoStack();
        }
        else if (_cursorLine < _lines.Count - 1)
        {
            SaveState();
            var nextLine = _lines[_cursorLine + 1];
            _lines[_cursorLine] += nextLine;
            _lines.RemoveAt(_cursorLine + 1);
            EnsureCursorInBounds();
            TextChanged?.Invoke(Text);
            LineChanged?.Invoke(_cursorLine);
            ClearRedoStack();
        }
    }

    /// <summary>
    /// Deletes the character before the cursor.
    /// </summary>
    public void DeleteBackspace()
    {
        if (HasSelection())
        {
            DeleteSelection();
            return;
        }

        if (_cursorColumn > 0)
        {
            SaveState();
            _lines[_cursorLine] = _lines[_cursorLine].Remove(_cursorColumn - 1, 1);
            _cursorColumn--;
            EnsureCursorInBounds();
            TextChanged?.Invoke(Text);
            LineChanged?.Invoke(_cursorLine);
            ClearRedoStack();
        }
        else if (_cursorLine > 0)
        {
            SaveState();
            var currentLine = _lines[_cursorLine];
            var prevLineLength = _lines[_cursorLine - 1].Length;
            _lines[_cursorLine - 1] += currentLine;
            _lines.RemoveAt(_cursorLine);
            _cursorLine--;
            _cursorColumn = prevLineLength;
            EnsureCursorInBounds();
            TextChanged?.Invoke(Text);
            LineChanged?.Invoke(_cursorLine);
            ClearRedoStack();
        }
    }

    /// <summary>
    /// Deletes the selected text.
    /// </summary>
    public void DeleteSelection()
    {
        if (!HasSelection())
            return;

        SaveState();
        var (start, end) = NormalizeSelection();
        var startLine = start.Y;
        var endLine = end.Y;
        var startCol = start.X;
        var endCol = end.X;

        if (startLine == endLine)
        {
            _lines[startLine] = _lines[startLine].Remove(startCol, endCol - startCol);
            _cursorLine = startLine;
            _cursorColumn = startCol;
        }
        else
        {
            var firstPart = _lines[startLine][..startCol];
            var lastPart = _lines[endLine][endCol..];
            _lines[startLine] = firstPart + lastPart;

            for (int i = endLine; i > startLine; i--)
            {
                _lines.RemoveAt(i);
            }

            _cursorLine = startLine;
            _cursorColumn = startCol;
        }

        ClearSelection();
        EnsureCursorInBounds();
        TextChanged?.Invoke(Text);
        LineChanged?.Invoke(_cursorLine);
        ClearRedoStack();
    }

    /// <summary>
    /// Moves the cursor to the specified position.
    /// </summary>
    public void MoveCursor(int line, int column)
    {
        CursorLine = line;
        CursorColumn = column;
    }

    /// <summary>
    /// Selects all text.
    /// </summary>
    public void SelectAll()
    {
        if (_lines.Count == 0)
            return;

        SelectionStart = new Point(0, 0);
        SelectionEnd = new Point(_lines[^1].Length, _lines.Count - 1);
    }

    /// <summary>
    /// Gets a specific line.
    /// </summary>
    public string GetLine(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= _lines.Count)
            return string.Empty;
        return _lines[lineIndex];
    }

    /// <summary>
    /// Sets a specific line.
    /// </summary>
    public void SetLine(int lineIndex, string text)
    {
        if (lineIndex < 0 || lineIndex >= _lines.Count)
            return;

        SaveState();
        _lines[lineIndex] = text ?? string.Empty;
        EnsureCursorInBounds();
        TextChanged?.Invoke(Text);
        LineChanged?.Invoke(lineIndex);
        ClearRedoStack();
    }

    /// <summary>
    /// Undoes the last operation.
    /// </summary>
    public void Undo()
    {
        if (_undoStack.Count == 0)
            return;

        var currentState = GetCurrentState();
        _redoStack.Push(currentState);

        var previousState = _undoStack.Pop();
        RestoreState(previousState);
    }

    /// <summary>
    /// Redoes the last undone operation.
    /// </summary>
    public void Redo()
    {
        if (_redoStack.Count == 0)
            return;

        var currentState = GetCurrentState();
        _undoStack.Push(currentState);

        var previousState = _redoStack.Pop();
        RestoreState(previousState);
    }

    protected override void OnRender(Screen screen)
    {
        if (Width <= 0 || Height <= 0)
            return;

        screen.FillRectangle(Bounds, ' ', ForegroundColor, BackgroundColor);

        int lineNumberWidth = _showLineNumbers ? GetLineNumberWidth() + 1 : 0;
        int textStartX = X + lineNumberWidth;
        int textWidth = Width - lineNumberWidth;

        int visibleLines = Math.Min(Height, _lines.Count - _scrollOffsetY);

        for (int i = 0; i < visibleLines; i++)
        {
            int lineIndex = _scrollOffsetY + i;
            if (lineIndex >= _lines.Count)
                break;

            int renderY = Y + i;

            if (_showLineNumbers)
            {
                string lineNumber = (lineIndex + 1).ToString().PadLeft(GetLineNumberWidth());
                screen.WriteText(X, renderY, lineNumber, _lineNumberColor, BackgroundColor);
            }

            string line = _lines[lineIndex];
            RenderLine(screen, line, textStartX, renderY, textWidth, lineIndex);
            
            if (string.IsNullOrEmpty(line))
            {
                screen.FillRectangle(new Rectangle(textStartX, renderY, textWidth, 1), ' ', ForegroundColor, BackgroundColor);
            }
        }

        if (_hasFocus)
        {
            RenderCursor(screen, textStartX);
        }
    }

    protected internal override void OnFocus()
    {
        base.OnFocus();
        _hasFocus = true;
    }

    protected internal override void OnBlur()
    {
        base.OnBlur();
        _hasFocus = false;
        ClearSelection();
    }

    protected internal override bool OnKeyPress(ConsoleKeyInfo key)
    {
        bool handled = false;

        if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
        {
            handled = HandleControlKey(key);
        }
        else
        {
            handled = HandleNormalKey(key);
        }

        return handled;
    }

    private bool HandleControlKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Z:
                Undo();
                return true;

            case ConsoleKey.Y:
                Redo();
                return true;

            case ConsoleKey.A:
                SelectAll();
                return true;

            case ConsoleKey.Home:
                MoveCursor(0, 0);
                return true;

            case ConsoleKey.End:
                if (_lines.Count > 0)
                {
                    MoveCursor(_lines.Count - 1, _lines[^1].Length);
                }
                return true;
        }

        return false;
    }

    private bool HandleNormalKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.LeftArrow:
                if (_cursorColumn > 0)
                {
                    CursorColumn--;
                }
                else if (_cursorLine > 0)
                {
                    CursorLine--;
                    CursorColumn = _lines[_cursorLine].Length;
                }
                return true;

            case ConsoleKey.RightArrow:
                if (_cursorColumn < _lines[_cursorLine].Length)
                {
                    CursorColumn++;
                }
                else if (_cursorLine < _lines.Count - 1)
                {
                    CursorLine++;
                    CursorColumn = 0;
                }
                return true;

            case ConsoleKey.UpArrow:
                if (_cursorLine > 0)
                {
                    CursorLine--;
                    EnsureCursorInBounds();
                }
                return true;

            case ConsoleKey.DownArrow:
                if (_cursorLine < _lines.Count - 1)
                {
                    CursorLine++;
                    EnsureCursorInBounds();
                }
                return true;

            case ConsoleKey.Home:
                CursorColumn = 0;
                return true;

            case ConsoleKey.End:
                CursorColumn = _lines[_cursorLine].Length;
                return true;

            case ConsoleKey.PageUp:
                ScrollUp(Height);
                return true;

            case ConsoleKey.PageDown:
                ScrollDown(Height);
                return true;

            case ConsoleKey.Enter:
                InsertText("\n");
                return true;

            case ConsoleKey.Backspace:
                DeleteBackspace();
                return true;

            case ConsoleKey.Delete:
                DeleteChar();
                return true;

            case ConsoleKey.Tab:
                InsertText("    ");
                return true;

            default:
                if (!char.IsControl(key.KeyChar))
                {
                    InsertText(key.KeyChar.ToString());
                    return true;
                }
                break;
        }

        return false;
    }

    private void RenderLine(Screen screen, string line, int x, int y, int width, int lineIndex)
    {
        if (string.IsNullOrEmpty(line))
        {
            if (HasSelection() && IsLineSelected(lineIndex))
            {
                screen.FillRectangle(new Rectangle(x, y, width, 1), ' ', BackgroundColor, ForegroundColor);
            }
            return;
        }

        int displayStart = _scrollOffsetX;
        int displayEnd = Math.Min(line.Length, displayStart + width);

        if (displayStart >= line.Length)
            return;

        string displayLine = line.Substring(displayStart, displayEnd - displayStart);

        if (HasSelection() && IsLineSelected(lineIndex))
        {
            var (start, end) = NormalizeSelection();
            RenderLineWithSelection(screen, displayLine, x, y, width, lineIndex, start, end, displayStart);
        }
        else
        {
            screen.WriteText(x, y, displayLine, ForegroundColor, BackgroundColor);
        }
    }

    private void RenderLineWithSelection(Screen screen, string line, int x, int y, int width, int lineIndex, Point start, Point end, int displayStart)
    {
        int lineStart = lineIndex == start.Y ? Math.Max(0, start.X - displayStart) : 0;
        int lineEnd = lineIndex == end.Y ? Math.Min(line.Length, end.X - displayStart) : line.Length;

        if (lineStart > 0)
        {
            screen.WriteText(x, y, line[..lineStart], ForegroundColor, BackgroundColor);
        }

        if (lineEnd > lineStart)
        {
            string selectedText = line.Substring(lineStart, lineEnd - lineStart);
            screen.WriteText(x + lineStart, y, selectedText, BackgroundColor, ForegroundColor);
        }

        if (lineEnd < line.Length)
        {
            screen.WriteText(x + lineEnd, y, line[lineEnd..], ForegroundColor, BackgroundColor);
        }
    }

    protected virtual void RenderCursor(Screen screen, int textStartX)
    {
        if (_cursorLine < _scrollOffsetY || _cursorLine >= _scrollOffsetY + Height)
            return;

        int renderY = Y + (_cursorLine - _scrollOffsetY);
        int renderX = textStartX + (_cursorColumn - _scrollOffsetX);

        if (renderX < textStartX || renderX >= X + Width)
            return;

        if (renderY >= Y && renderY < Y + Height)
        {
            char cursorChar = ' ';
            if (_cursorLine < _lines.Count && _cursorColumn < _lines[_cursorLine].Length)
            {
                cursorChar = _lines[_cursorLine][_cursorColumn];
            }

            screen.SetCell(renderX, renderY, new Cell(cursorChar, BackgroundColor, ForegroundColor));
        }
    }

    private void EnsureCursorInBounds()
    {
        if (_lines.Count == 0)
        {
            _lines.Add(string.Empty);
        }

        _cursorLine = Math.Clamp(_cursorLine, 0, _lines.Count - 1);
        _cursorColumn = Math.Clamp(_cursorColumn, 0, _lines[_cursorLine].Length);
    }

    private void EnsureCursorVisible()
    {
        if (_cursorLine < _scrollOffsetY)
        {
            _scrollOffsetY = _cursorLine;
        }
        else if (_cursorLine >= _scrollOffsetY + Height)
        {
            _scrollOffsetY = _cursorLine - Height + 1;
        }

        int lineNumberWidth = _showLineNumbers ? GetLineNumberWidth() + 1 : 0;
        int textWidth = Width - lineNumberWidth;
        int textStartX = X + lineNumberWidth;

        if (_cursorColumn < _scrollOffsetX)
        {
            _scrollOffsetX = _cursorColumn;
        }
        else if (_cursorColumn >= _scrollOffsetX + textWidth)
        {
            _scrollOffsetX = _cursorColumn - textWidth + 1;
        }
    }

    protected int GetLineNumberWidth()
    {
        if (_lines.Count == 0)
            return 1;
        return Math.Max(3, _lines.Count.ToString().Length);
    }

    private bool HasSelection()
    {
        return _selectionStart.HasValue && _selectionEnd.HasValue;
    }

    private bool IsLineSelected(int lineIndex)
    {
        if (!HasSelection())
            return false;

        var (start, end) = NormalizeSelection();
        return lineIndex >= start.Y && lineIndex <= end.Y;
    }

    private (Point start, Point end) NormalizeSelection()
    {
        if (!_selectionStart.HasValue || !_selectionEnd.HasValue)
            return (Point.Empty, Point.Empty);

        var start = _selectionStart.Value;
        var end = _selectionEnd.Value;

        if (start.Y > end.Y || (start.Y == end.Y && start.X > end.X))
        {
            (start, end) = (end, start);
        }

        return (start, end);
    }

    private void ClearSelection()
    {
        _selectionStart = null;
        _selectionEnd = null;
    }

    private void ScrollUp(int lines)
    {
        _scrollOffsetY = Math.Max(0, _scrollOffsetY - lines);
        if (_cursorLine > _scrollOffsetY + Height - 1)
        {
            _cursorLine = _scrollOffsetY + Height - 1;
        }
        EnsureCursorInBounds();
    }

    private void ScrollDown(int lines)
    {
        int maxOffset = Math.Max(0, _lines.Count - Height);
        _scrollOffsetY = Math.Min(maxOffset, _scrollOffsetY + lines);
        if (_cursorLine < _scrollOffsetY)
        {
            _cursorLine = _scrollOffsetY;
        }
        EnsureCursorInBounds();
    }

    protected void SaveState()
    {
        var state = GetCurrentState();
        _undoStack.Push(state);

        if (_undoStack.Count > MaxUndoHistory)
        {
            var temp = new Stack<EditorState>();
            for (int i = 0; i < MaxUndoHistory; i++)
            {
                temp.Push(_undoStack.Pop());
            }
            _undoStack.Clear();
            while (temp.Count > 0)
            {
                _undoStack.Push(temp.Pop());
            }
        }
    }

    private EditorState GetCurrentState()
    {
        return new EditorState
        {
            Lines = new List<string>(_lines),
            CursorLine = _cursorLine,
            CursorColumn = _cursorColumn
        };
    }

    private void RestoreState(EditorState state)
    {
        _lines.Clear();
        _lines.AddRange(state.Lines);
        _cursorLine = state.CursorLine;
        _cursorColumn = state.CursorColumn;
        EnsureCursorInBounds();
        EnsureCursorVisible();
        TextChanged?.Invoke(Text);
    }

    protected void ClearRedoStack()
    {
        _redoStack.Clear();
    }

    private class EditorState
    {
        public List<string> Lines { get; set; } = [];
        public int CursorLine { get; set; }
        public int CursorColumn { get; set; }
    }
}

