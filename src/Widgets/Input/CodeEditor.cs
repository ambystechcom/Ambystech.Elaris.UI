using System.Drawing;
using System.Text.RegularExpressions;
using Ambystech.Elaris.UI.Core;
using Ambystech.Elaris.UI.Rendering;
using Ambystech.Elaris.UI.Widgets.Input.SyntaxHighlighting;

namespace Ambystech.Elaris.UI.Widgets.Input;

/// <summary>
/// A code editor widget with syntax highlighting, bracket matching, and auto-indentation.
/// </summary>
public class CodeEditor : Editor
{
    private string _language = "csharp";
    private string _theme = "default";
    private bool _syntaxHighlighting = true;
    private bool _showBracketMatching = true;
    private int _tabSize = 4;
    private bool _useTabs = false;
    private bool _autoIndent = true;

    /// <summary>
    /// Gets or sets the programming language for syntax highlighting.
    /// </summary>
    public string Language
    {
        get => _language;
        set
        {
            _language = value ?? "csharp";
            var supportedLanguages = PluginManager.Instance.GetSupportedLanguages();
            if (!supportedLanguages.Contains(_language))
            {
                _language = "csharp";
            }
        }
    }

    /// <summary>
    /// Gets or sets the theme name for syntax highlighting colors.
    /// </summary>
    public string Theme
    {
        get => _theme;
        set
        {
            _theme = value ?? "default";
            var supportedThemes = PluginManager.Instance.GetSupportedThemes();
            if (!supportedThemes.Contains(_theme))
            {
                _theme = "default";
            }
        }
    }

    /// <summary>
    /// Gets or sets whether syntax highlighting is enabled.
    /// </summary>
    public bool SyntaxHighlighting
    {
        get => _syntaxHighlighting;
        set => _syntaxHighlighting = value;
    }

    /// <summary>
    /// Gets or sets whether to highlight matching brackets.
    /// </summary>
    public bool ShowBracketMatching
    {
        get => _showBracketMatching;
        set => _showBracketMatching = value;
    }

    /// <summary>
    /// Gets or sets the number of spaces per tab.
    /// </summary>
    public int TabSize
    {
        get => _tabSize;
        set => _tabSize = Math.Max(1, value);
    }

    /// <summary>
    /// Gets or sets whether to use tabs instead of spaces.
    /// </summary>
    public bool UseTabs
    {
        get => _useTabs;
        set => _useTabs = value;
    }

    /// <summary>
    /// Gets or sets whether to enable auto-indentation.
    /// </summary>
    public bool AutoIndent
    {
        get => _autoIndent;
        set => _autoIndent = value;
    }

    /// <summary>
    /// Sets the programming language for syntax highlighting.
    /// </summary>
    public void SetLanguage(string language)
    {
        Language = language;
    }

    /// <summary>
    /// Finds the matching bracket for the bracket at the specified position.
    /// </summary>
    public Point? FindMatchingBracket(int line, int column)
    {
        var rules = PluginManager.Instance.GetRules(_language);
        if (!_showBracketMatching || rules == null)
            return null;

        if (line < 0 || line >= Lines.Count)
            return null;

        string lineText = Lines[line];
        if (column < 0 || column >= lineText.Length)
            return null;

        char bracket = lineText[column];
        if (!rules.BracketPairs.ContainsKey(bracket) && !rules.BracketPairs.ContainsValue(bracket))
            return null;

        bool isOpening = rules.BracketPairs.ContainsKey(bracket);
        char matchingBracket = isOpening ? rules.BracketPairs[bracket] : rules.BracketPairs.First(kvp => kvp.Value == bracket).Key;
        int direction = isOpening ? 1 : -1;
        int depth = 1;

        int currentLine = line;
        int currentCol = column + direction;

        while (currentLine >= 0 && currentLine < Lines.Count)
        {
            string currentLineText = Lines[currentLine];

            while (currentCol >= 0 && currentCol < currentLineText.Length)
            {
                char ch = currentLineText[currentCol];

                if (ch == bracket)
                {
                    depth++;
                }
                else if (ch == matchingBracket)
                {
                    depth--;
                    if (depth == 0)
                    {
                        return new Point(currentCol, currentLine);
                    }
                }

                currentCol += direction;
            }

            currentLine += direction;
            if (currentLine >= 0 && currentLine < Lines.Count)
            {
                currentCol = direction > 0 ? 0 : Lines[currentLine].Length - 1;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the indentation level for a specific line.
    /// </summary>
    public int GetIndentationLevel(int line)
    {
        if (line < 0 || line >= Lines.Count)
            return 0;

        string lineText = Lines[line];
        int indent = 0;

        for (int i = 0; i < lineText.Length; i++)
        {
            if (lineText[i] == ' ')
            {
                indent++;
            }
            else if (lineText[i] == '\t')
            {
                indent += _tabSize;
            }
            else
            {
                break;
            }
        }

        return indent;
    }

    protected override void OnRender(Screen screen)
    {
        if (!_syntaxHighlighting)
        {
            base.OnRender(screen);
            return;
        }

        if (Width <= 0 || Height <= 0)
            return;

        screen.FillRectangle(Bounds, ' ', ForegroundColor, BackgroundColor);

        int lineNumberWidth = ShowLineNumbers ? GetLineNumberWidth() + 1 : 0;
        int textStartX = X + lineNumberWidth;
        int textWidth = Width - lineNumberWidth;

        int visibleLines = Math.Min(Height, Lines.Count - _scrollOffsetY);

        for (int i = 0; i < visibleLines; i++)
        {
            int lineIndex = _scrollOffsetY + i;
            if (lineIndex >= Lines.Count)
                break;

            int renderY = Y + i;

            if (ShowLineNumbers)
            {
                string lineNumber = (lineIndex + 1).ToString().PadLeft(GetLineNumberWidth());
                screen.WriteText(X, renderY, lineNumber, LineNumberColor, BackgroundColor);
            }

            string line = Lines[lineIndex];
            RenderSyntaxHighlightedLine(screen, line, textStartX, renderY, textWidth, lineIndex);
            
            if (string.IsNullOrEmpty(line))
            {
                screen.FillRectangle(new Rectangle(textStartX, renderY, textWidth, 1), ' ', ForegroundColor, BackgroundColor);
            }
        }

        if (_hasFocus)
        {
            RenderBracketMatching(screen, textStartX);
            RenderCursor(screen, textStartX);
        }
    }

    protected internal override void OnFocus()
    {
        base.OnFocus();
    }

    protected internal override void OnBlur()
    {
        base.OnBlur();
    }

    protected internal override bool OnKeyPress(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.Tab)
        {
            if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
            {
                RemoveIndentation();
                return true;
            }

            if (_autoIndent)
            {
                InsertIndentation();
            }
            else
            {
                InsertText(_useTabs ? "\t" : new string(' ', _tabSize));
            }
            return true;
        }

        if (key.Key == ConsoleKey.Enter)
        {
            if (_autoIndent)
            {
                InsertTextWithIndent();
            }
            else
            {
                InsertText("\n");
            }
            return true;
        }

        if (key.KeyChar != '\0' && (key.KeyChar == '{' || key.KeyChar == '[' || key.KeyChar == '('))
        {
            var rules = PluginManager.Instance.GetRules(_language);
            if (_autoIndent && rules != null && rules.BracketPairs.ContainsKey(key.KeyChar))
            {
                InsertText(key.KeyChar.ToString());
                InsertText(rules.BracketPairs[key.KeyChar].ToString());
                MoveCursor(CursorLine, CursorColumn - 1);
                return true;
            }
        }

        return base.OnKeyPress(key);
    }

    private void RenderSyntaxHighlightedLine(Screen screen, string line, int x, int y, int width, int lineIndex)
    {
        if (string.IsNullOrEmpty(line))
            return;

        int displayStart = _scrollOffsetX;
        int displayEnd = Math.Min(line.Length, displayStart + width);

        if (displayStart >= line.Length)
            return;

        var rules = PluginManager.Instance.GetRules(_language);
        if (rules == null)
        {
            string displayLine = line.Substring(displayStart, displayEnd - displayStart);
            screen.WriteText(x, y, displayLine, ForegroundColor, BackgroundColor);
            return;
        }

        var tokens = TokenizeLine(line, rules);
        int lastPos = displayStart;

        foreach (var token in tokens)
        {
            if (token.Start >= displayEnd)
                break;

            if (token.End <= displayStart)
                continue;

            if (token.Start > lastPos)
            {
                int gapStart = Math.Max(displayStart, lastPos);
                int gapEnd = Math.Min(displayEnd, token.Start);
                if (gapEnd > gapStart)
                {
                    int gapX = x + (gapStart - displayStart);
                    string gapText = line.Substring(gapStart, gapEnd - gapStart);
                    screen.WriteText(gapX, y, gapText, ForegroundColor, BackgroundColor);
                }
            }

            int visibleStart = Math.Max(displayStart, token.Start);
            int visibleEnd = Math.Min(displayEnd, token.End);
            int visibleLength = visibleEnd - visibleStart;

            if (visibleLength > 0)
            {
                int renderX = x + (visibleStart - displayStart);
                string tokenText = line.Substring(visibleStart, visibleLength);

                if (renderX >= x && renderX < x + width && tokenText.Length > 0)
                {
                    var theme = PluginManager.Instance.GetTheme(_theme);
                    Color tokenColor = theme.Colors.GetValueOrDefault(token.Type, ForegroundColor);
                    screen.WriteText(renderX, y, tokenText, tokenColor, BackgroundColor);
                }
            }

            lastPos = Math.Max(lastPos, token.End);
        }

        if (lastPos < displayEnd)
        {
            int gapStart = Math.Max(displayStart, lastPos);
            int gapEnd = displayEnd;
            if (gapEnd > gapStart)
            {
                int gapX = x + (gapStart - displayStart);
                string gapText = line.Substring(gapStart, gapEnd - gapStart);
                screen.WriteText(gapX, y, gapText, ForegroundColor, BackgroundColor);
            }
        }
    }

    private List<Token> TokenizeLine(string line, SyntaxRules rules)
    {
        var tokens = new List<Token>();
        int position = 0;

        while (position < line.Length)
        {
            bool matched = false;

            foreach (var pattern in rules.CommentPatterns)
            {
                var match = Regex.Match(line, pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100));
                if (match.Success && match.Index == position)
                {
                    tokens.Add(new Token { Type = TokenType.Comment, Start = position, End = position + match.Length });
                    position += match.Length;
                    matched = true;
                    break;
                }
            }

            if (matched)
                continue;

            if (Regex.IsMatch(line, rules.NumberPattern))
            {
                var match = Regex.Match(line, rules.NumberPattern, RegexOptions.None, TimeSpan.FromMilliseconds(100));
                if (match.Success && match.Index == position)
                {
                    tokens.Add(new Token { Type = TokenType.Number, Start = position, End = position + match.Length });
                    position += match.Length;
                    continue;
                }
            }

            foreach (var delimiter in rules.StringDelimiters)
            {
                if (position + delimiter.Length <= line.Length && line.Substring(position, delimiter.Length) == delimiter)
                {
                    int endPos = FindStringEnd(line, position + delimiter.Length, delimiter);
                    tokens.Add(new Token { Type = TokenType.String, Start = position, End = endPos });
                    position = endPos;
                    matched = true;
                    break;
                }
            }

            if (matched)
                continue;

            string remaining = line[position..];
            string? keyword = rules.Keywords.FirstOrDefault(k => remaining.StartsWith(k + " ") || remaining.StartsWith(k + "\t") || remaining == k);

            if (keyword != null && (position + keyword.Length >= line.Length || !char.IsLetterOrDigit(line[position + keyword.Length])))
            {
                tokens.Add(new Token { Type = TokenType.Keyword, Start = position, End = position + keyword.Length });
                position += keyword.Length;
                continue;
            }

            if (char.IsLetterOrDigit(line[position]) || line[position] == '_')
            {
                int start = position;
                while (position < line.Length && (char.IsLetterOrDigit(line[position]) || line[position] == '_'))
                {
                    position++;
                }
                tokens.Add(new Token { Type = TokenType.Identifier, Start = start, End = position });
                continue;
            }

            tokens.Add(new Token { Type = TokenType.Operator, Start = position, End = position + 1 });
            position++;
        }

        return tokens;
    }

    private int FindStringEnd(string line, int start, string delimiter)
    {
        int pos = start;
        while (pos < line.Length)
        {
            if (pos + delimiter.Length <= line.Length && line.Substring(pos, delimiter.Length) == delimiter)
            {
                return pos + delimiter.Length;
            }
            pos++;
        }
        return line.Length;
    }

    private void RenderBracketMatching(Screen screen, int textStartX)
    {
        if (!_showBracketMatching)
            return;

        var match = FindMatchingBracket(CursorLine, CursorColumn);
        if (match.HasValue)
        {
            int matchY = Y + (match.Value.Y - _scrollOffsetY);
            int matchX = textStartX + (match.Value.X - _scrollOffsetX);

            if (matchY >= Y && matchY < Y + Height && matchX >= textStartX && matchX < X + Width)
            {
                if (match.Value.Y < Lines.Count && match.Value.X < Lines[match.Value.Y].Length)
                {
                    char bracketChar = Lines[match.Value.Y][match.Value.X];
                    screen.SetCell(matchX, matchY, new Cell(bracketChar, Color.Yellow, BackgroundColor, bold: true));
                }
            }
        }
    }

    private void InsertIndentation()
    {
        int indentLevel = GetIndentationLevel(CursorLine);
        string indent = _useTabs ? "\t" : new string(' ', _tabSize);
        InsertText(indent);
    }

    private void RemoveIndentation()
    {
        if (CursorLine >= Lines.Count || CursorLine < 0)
            return;

        string line = Lines[CursorLine];
        if (string.IsNullOrEmpty(line) || CursorColumn == 0)
            return;

        int spacesToRemove = 0;
        if (_useTabs && CursorColumn > 0 && line[CursorColumn - 1] == '\t')
        {
            spacesToRemove = 1;
        }
        else
        {
            int leadingSpaces = 0;
            int checkPos = CursorColumn - 1;
            while (checkPos >= 0 && checkPos < line.Length && line[checkPos] == ' ')
            {
                leadingSpaces++;
                checkPos--;
            }
            spacesToRemove = Math.Min(_tabSize, leadingSpaces);
        }

        if (spacesToRemove > 0)
        {
            SaveState();
            for (int i = 0; i < spacesToRemove && CursorColumn > 0; i++)
            {
                DeleteBackspace();
            }
            ClearRedoStack();
        }
    }

    private void InsertTextWithIndent()
    {
        int currentIndent = GetIndentationLevel(CursorLine);
        string currentLine = Lines[CursorLine];

        string indent = new string(' ', currentIndent);

        if (currentLine.TrimEnd().EndsWith('{') || currentLine.TrimEnd().EndsWith('[') || currentLine.TrimEnd().EndsWith('('))
        {
            indent += new string(' ', _tabSize);
        }

        InsertText("\n" + indent);
    }

    private class Token
    {
        public TokenType Type { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
    }
}

