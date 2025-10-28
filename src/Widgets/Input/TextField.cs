using System.Drawing;
using Ambystech.Elaris.UI.Core;
using Ambystech.Elaris.UI.Rendering;

namespace Ambystech.Elaris.UI.Widgets.Input;

/// <summary>
/// A single-line text input widget.
/// </summary>
public class TextField : Widget
{
    private string _text = string.Empty;
    private int _cursorPosition = 0;
    private bool _hasFocus = false;
    private string _placeholder = string.Empty;
    private Color _placeholderColor;

    /// <summary>
    /// Gets or sets the text content.
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            _text = value ?? string.Empty;
            _cursorPosition = Math.Min(_cursorPosition, _text.Length);
        }
    }

    /// <summary>
    /// Gets or sets the placeholder text shown when empty.
    /// </summary>
    public string Placeholder
    {
        get => _placeholder;
        set => _placeholder = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the placeholder text color.
    /// </summary>
    public Color PlaceholderColor
    {
        get => _placeholderColor.A == 0 ? Color.Gray : _placeholderColor;
        set => _placeholderColor = value;
    }

    /// <summary>
    /// Gets the current cursor position.
    /// </summary>
    public int CursorPosition => _cursorPosition;

    /// <summary>
    /// Event raised when a key is pressed. Return true to mark as handled and prevent default behavior.
    /// </summary>
    public event Func<ConsoleKeyInfo, bool>? KeyPress;

    /// <summary>
    /// Event raised when a key is pressed down (fires before default handling).
    /// </summary>
    public event Action<ConsoleKeyInfo>? KeyDown;

    /// <summary>
    /// Event raised when a key is released (fires after default handling).
    /// </summary>
    public event Action<ConsoleKeyInfo>? KeyUp;

    /// <summary>
    /// Event raised when the text field receives focus.
    /// </summary>
    public event Action? Focused;

    /// <summary>
    /// Event raised when the text field loses focus.
    /// </summary>
    public event Action? Blurred;

    /// <summary>
    /// Event raised when the text content changes.
    /// </summary>
    public event Action<string>? TextChanged;

    /// <summary>
    /// Gets whether this widget can receive keyboard focus.
    /// </summary>
    public override bool IsFocusable => true;

    public TextField() { }

    public TextField(string placeholder)
    {
        _placeholder = placeholder ?? string.Empty;
    }

    protected override void OnRender(Screen screen)
    {
        if (Width <= 0 || Height <= 0)
            return;

        screen.FillRectangle(Bounds, ' ', ForegroundColor, BackgroundColor);

        string displayText;
        Color textColor;

        if (string.IsNullOrEmpty(_text) && !string.IsNullOrEmpty(_placeholder))
        {
            displayText = _placeholder;
            textColor = PlaceholderColor;
        }
        else
        {
            displayText = _text;
            textColor = ForegroundColor;
        }

        int maxLength = Width;
        if (displayText.Length > maxLength)
        {
            displayText = displayText.Substring(0, maxLength);
        }

        screen.WriteText(X, Y, displayText, textColor, BackgroundColor);

        if (_hasFocus && _cursorPosition <= Width)
        {
            int cursorX = X + _cursorPosition;
            if (cursorX < X + Width)
            {
                char cursorChar = _cursorPosition < _text.Length ? _text[_cursorPosition] : ' ';
                screen.SetCell(cursorX, Y, new Cell(cursorChar, BackgroundColor, ForegroundColor));
            }
        }
    }

    protected internal override void OnFocus()
    {
        base.OnFocus();
        _hasFocus = true;
        Focused?.Invoke();
    }

    protected internal override void OnBlur()
    {
        base.OnBlur();
        _hasFocus = false;
        Blurred?.Invoke();
    }

    protected internal override bool OnKeyPress(ConsoleKeyInfo key)
    {
        KeyDown?.Invoke(key);

        var userHandled = KeyPress?.Invoke(key);
        if (userHandled == true)
        {
            KeyUp?.Invoke(key);
            return true;
        }

        string oldText = _text;
        bool handled = false;

        switch (key.Key)
        {
            case ConsoleKey.LeftArrow:
                _cursorPosition = Math.Max(0, _cursorPosition - 1);
                handled = true;
                break;

            case ConsoleKey.RightArrow:
                _cursorPosition = Math.Min(_text.Length, _cursorPosition + 1);
                handled = true;
                break;

            case ConsoleKey.Home:
                _cursorPosition = 0;
                handled = true;
                break;

            case ConsoleKey.End:
                _cursorPosition = _text.Length;
                handled = true;
                break;

            case ConsoleKey.Backspace:
                if (_cursorPosition > 0)
                {
                    _text = _text.Remove(_cursorPosition - 1, 1);
                    _cursorPosition--;
                    handled = true;
                }
                break;

            case ConsoleKey.Delete:
                if (_cursorPosition < _text.Length)
                {
                    _text = _text.Remove(_cursorPosition, 1);
                    handled = true;
                }
                break;
            case ConsoleKey.Enter:
                _cursorPosition = _text.Length;
                handled = true;
                break;
            default:
                if (!char.IsControl(key.KeyChar))
                {
                    _text = _text.Insert(_cursorPosition, key.KeyChar.ToString());
                    _cursorPosition++;
                    handled = true;
                }
                break;
        }

        if (oldText != _text)
        {
            TextChanged?.Invoke(_text);
        }

        if (handled)
        {
            KeyUp?.Invoke(key);
            return true;
        }

        return base.OnKeyPress(key);
    }
}
