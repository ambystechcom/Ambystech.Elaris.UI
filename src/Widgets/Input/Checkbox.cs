using System.Drawing;
using Ambystech.Elaris.UI.Rendering;

namespace Ambystech.Elaris.UI.Widgets.Input;

/// <summary>
/// A checkbox widget with a label.
/// </summary>
public class Checkbox : Widget
{
    private bool _isChecked = false;
    private string _label = string.Empty;
    private bool _hasFocus = false;

    /// <summary>
    /// Gets or sets whether the checkbox is checked.
    /// </summary>
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                _isChecked = value;
                CheckedChanged?.Invoke(_isChecked);
            }
        }
    }

    /// <summary>
    /// Gets or sets the checkbox label text.
    /// </summary>
    public string Label
    {
        get => _label;
        set => _label = value ?? string.Empty;
    }

    /// <summary>
    /// Event raised when the checked state changes.
    /// </summary>
    public event Action<bool>? CheckedChanged;

    /// <summary>
    /// Gets or sets the color of the checkbox brackets.
    /// </summary>
    public Color BracketColor { get; set; } = Color.Yellow;

    /// <summary>
    /// Gets or sets the color of the checkmark when checked.
    /// </summary>
    public Color CheckmarkColor { get; set; } = Color.Green;

    /// <summary>
    /// Gets whether this widget can receive keyboard focus.
    /// </summary>
    public override bool IsFocusable => true;

    public Checkbox()
    {
    }

    public Checkbox(string label)
    {
        _label = label ?? string.Empty;
    }

    protected override void OnRender(Screen screen)
    {
        if (Width <= 0 || Height <= 0)
            return;

        Color fgColor = ForegroundColor;
        Color bgColor = BackgroundColor;

        int boxX = X;
        int boxY = Y;

        if (_hasFocus)
        {
            screen.SetCell(boxX, boxY, new Cell('[', BracketColor, bgColor, bold: true));
            screen.SetCell(boxX + 2, boxY, new Cell(']', BracketColor, bgColor, bold: true));
        }
        else
        {
            screen.SetCell(boxX, boxY, new Cell('[', fgColor, bgColor));
            screen.SetCell(boxX + 2, boxY, new Cell(']', fgColor, bgColor));
        }

        char checkChar = _isChecked ? 'X' : ' ';
        Color checkColor = _isChecked ? CheckmarkColor : fgColor;
        screen.SetCell(boxX + 1, boxY, new Cell(checkChar, checkColor, bgColor, bold: _isChecked));

        // Draw label if present
        if (!string.IsNullOrEmpty(_label) && Width > 4)
        {
            int labelX = boxX + 4;
            int labelLength = Math.Min(_label.Length, Width - 4);
            string displayLabel = labelLength < _label.Length ? _label.Substring(0, labelLength) : _label;

            screen.WriteText(labelX, boxY, displayLabel, fgColor, bgColor);
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
    }

    protected internal override bool OnKeyPress(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.Spacebar || key.Key == ConsoleKey.Enter)
        {
            Toggle();
            return true;
        }

        return base.OnKeyPress(key);
    }

    /// <summary>
    /// Toggles the checkbox state.
    /// </summary>
    public void Toggle()
    {
        IsChecked = !IsChecked;
    }
}
