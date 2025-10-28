using System.Drawing;
using Ambystech.Elaris.UI.Core;
using Ambystech.Elaris.UI.Enums;
using Ambystech.Elaris.UI.Rendering;

namespace Ambystech.Elaris.UI.Widgets.Input;

/// <summary>
/// A clickable button widget.
/// </summary>
public class Button : Widget
{
    private string _text = string.Empty;
    private bool _isPressed = false;
    private bool _hasFocus = false;

    /// <summary>
    /// Gets or sets the button text.
    /// </summary>
    public string Text
    {
        get => _text;
        set => _text = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the background color when button is pressed.
    /// </summary>
    public Color PressedBackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the foreground color when button is pressed.
    /// </summary>
    public Color PressedForegroundColor { get; set; }

    /// <summary>
    /// Gets or sets the background color when button has focus.
    /// </summary>
    public Color FocusedBackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the foreground color when button has focus.
    /// </summary>
    public Color FocusedForegroundColor { get; set; }

    /// <summary>
    /// Gets or sets the border color. If not set, uses ForegroundColor.
    /// </summary>
    public Color BorderColor { get; set; }

    /// <summary>
    /// Gets or sets the button visual style.
    /// </summary>
    public ButtonStyle Style { get; set; } = ButtonStyle.Standard;

    /// <summary>
    /// Gets or sets the gradient start color (used when Style is Gradient).
    /// </summary>
    public Color? GradientStartColor { get; set; }

    /// <summary>
    /// Gets or sets the gradient end color (used when Style is Gradient).
    /// </summary>
    public Color? GradientEndColor { get; set; }

    /// <summary>
    /// Gets or sets the gradient start color when pressed (used when Style is Gradient).
    /// </summary>
    public Color? PressedGradientStartColor { get; set; }

    /// <summary>
    /// Gets or sets the gradient end color when pressed (used when Style is Gradient).
    /// </summary>
    public Color? PressedGradientEndColor { get; set; }

    /// <summary>
    /// Event raised when the button is clicked.
    /// </summary>
    public event Action? Click;

    /// <summary>
    /// Event raised when the button is pressed down.
    /// </summary>
    public event Action? Pressed;

    /// <summary>
    /// Event raised when the button is released.
    /// </summary>
    public event Action? Released;

    /// <summary>
    /// Gets whether this widget can receive keyboard focus.
    /// </summary>
    public override bool IsFocusable => true;

    public Button()
    {
        BackgroundColor = ColorHelper.FromRgb(70, 70, 70);
        ForegroundColor = Color.White;
        PressedBackgroundColor = ColorHelper.FromRgb(50, 50, 50);
        PressedForegroundColor = Color.White;
        FocusedBackgroundColor = ColorHelper.FromRgb(100, 150, 200);
        FocusedForegroundColor = Color.White;
        BorderColor = ColorHelper.FromRgb(150, 150, 150);
    }

    public Button(string text) : this()
    {
        _text = text ?? string.Empty;
    }

    protected override void OnRender(Screen screen)
    {
        if (Width <= 0 || Height <= 0)
            return;

        // Render based on style
        switch (Style)
        {
            case ButtonStyle.Standard:
                RenderStandardButton(screen);
                break;
            case ButtonStyle.Rounded:
                RenderRoundedButton(screen);
                break;
            case ButtonStyle.Outline:
                RenderOutlineButton(screen);
                break;
            case ButtonStyle.Gradient:
                RenderGradientButton(screen);
                break;
        }
    }

    private void RenderStandardButton(Screen screen)
    {
        var (bgColor, fgColor) = GetStateColors();

        FillInnerArea(screen, bgColor, fgColor);

        RenderText(screen, fgColor, bgColor);

        RenderBorder(screen, '─', '│', '┌', '┐', '└', '┘', Color.Transparent);
    }

    private void RenderRoundedButton(Screen screen)
    {
        var (bgColor, fgColor) = GetStateColors();

        FillInnerArea(screen, bgColor, fgColor);

        RenderText(screen, fgColor, bgColor);

        RenderBorder(screen, '─', '│', '╭', '╮', '╰', '╯', Color.Transparent);
    }

    private void RenderOutlineButton(Screen screen)
    {
        var (_, fgColor) = GetStateColors();

        Color transparentBg = Color.Transparent;

        RenderText(screen, fgColor, transparentBg);

        RenderBorder(screen, '─', '│', '╭', '╮', '╰', '╯', transparentBg);
    }

    private void RenderGradientButton(Screen screen)
    {
        var (bgColor, fgColor) = GetStateColors();

        Color startColor, endColor;
        if (_isPressed && PressedGradientStartColor.HasValue && PressedGradientEndColor.HasValue)
        {
            startColor = PressedGradientStartColor.Value;
            endColor = PressedGradientEndColor.Value;
        }
        else if (_isPressed && (GradientStartColor.HasValue || GradientEndColor.HasValue))
        {
            Color baseStart = GradientStartColor ?? bgColor;
            Color baseEnd = GradientEndColor ?? ColorHelper.Lerp(bgColor, Color.White, 0.3f);
            startColor = ColorHelper.Lerp(baseStart, Color.Black, 0.3f);
            endColor = ColorHelper.Lerp(baseEnd, Color.Black, 0.3f);
        }
        else
        {
            startColor = GradientStartColor ?? bgColor;
            endColor = GradientEndColor ?? ColorHelper.Lerp(bgColor, Color.White, 0.3f);
        }

        if (Width >= 3 && Height >= 3)
        {
            for (int x = X + 1; x < X + Width - 1; x++)
            {
                float t = (Width - 2) > 1 ? (float)(x - X - 1) / (Width - 3) : 0f;
                Color gradientColor = ColorHelper.Lerp(startColor, endColor, t);

                for (int y = Y + 1; y < Y + Height - 1; y++)
                {
                    screen.SetCell(x, y, new Cell(' ', fgColor, gradientColor));
                }
            }
        }

        Color middleBgColor = ColorHelper.Lerp(startColor, endColor, 0.5f);
        RenderText(screen, fgColor, middleBgColor);

        RenderBorder(screen, '─', '│', '┌', '┐', '└', '┘', Color.Transparent);
    }

    private (Color bgColor, Color fgColor) GetStateColors()
    {
        Color bgColor = BackgroundColor;
        Color fgColor = ForegroundColor;

        if (_isPressed)
        {
            bgColor = PressedBackgroundColor.A > 0 ? PressedBackgroundColor : bgColor;
            fgColor = PressedForegroundColor.A > 0 ? PressedForegroundColor : fgColor;
        }
        else if (_hasFocus)
        {
            bgColor = FocusedBackgroundColor.A > 0 ? FocusedBackgroundColor : bgColor;
            fgColor = FocusedForegroundColor.A > 0 ? FocusedForegroundColor : fgColor;
        }

        return (bgColor, fgColor);
    }

    private void FillInnerArea(Screen screen, Color bgColor, Color fgColor)
    {
        // Only fill the inner area, not the border cells
        if (Width < 3 || Height < 3)
            return;

        for (int y = Y + 1; y < Y + Height - 1; y++)
        {
            for (int x = X + 1; x < X + Width - 1; x++)
            {
                screen.SetCell(x, y, new Cell(' ', fgColor, bgColor));
            }
        }
    }

    private void RenderText(Screen screen, Color fgColor, Color bgColor)
    {
        if (Width <= 2 || Height < 3)
            return;

        int textY = Y + (Height - 1) / 2;
        if (textY < Y || textY >= Y + Height)
            return;

        string displayText = string.Empty;
        int textX = X + 1;

        if (!string.IsNullOrEmpty(_text))
        {
            int textLength = Math.Min(_text.Length, Width - 2);
            displayText = textLength < _text.Length ? _text.Substring(0, textLength) : _text;
            textX = X + (Width - displayText.Length) / 2;
        }

        if (Style == ButtonStyle.Gradient && GradientStartColor.HasValue && GradientEndColor.HasValue)
        {
            Color startColor, endColor;
            if (_isPressed && PressedGradientStartColor.HasValue && PressedGradientEndColor.HasValue)
            {
                startColor = PressedGradientStartColor.Value;
                endColor = PressedGradientEndColor.Value;
            }
            else if (_isPressed)
            {
                Color baseStart = GradientStartColor.Value;
                Color baseEnd = GradientEndColor.Value;
                startColor = ColorHelper.Lerp(baseStart, Color.Black, 0.3f);
                endColor = ColorHelper.Lerp(baseEnd, Color.Black, 0.3f);
            }
            else
            {
                startColor = GradientStartColor.Value;
                endColor = GradientEndColor.Value;
            }

            int textCharIndex = 0;
            for (int x = X + 1; x < X + Width - 1; x++)
            {
                float t = (Width - 2) > 1 ? (float)(x - X - 1) / (Width - 3) : 0f;
                Color cellBgColor = ColorHelper.Lerp(startColor, endColor, t);

                char displayChar = ' ';
                if (!string.IsNullOrEmpty(displayText) && x >= textX && x < textX + displayText.Length)
                {
                    displayChar = displayText[x - textX];
                }

                screen.SetCell(x, textY, new Cell(displayChar, fgColor, cellBgColor, bold: true));
            }
        }
        else
        {
            for (int x = X + 1; x < X + Width - 1; x++)
            {
                char displayChar = ' ';
                if (!string.IsNullOrEmpty(displayText) && x >= textX && x < textX + displayText.Length)
                {
                    displayChar = displayText[x - textX];
                }

                screen.SetCell(x, textY, new Cell(displayChar, fgColor, bgColor, bold: true));
            }
        }
    }

    private void RenderBorder(Screen screen, char horizontal, char vertical,
        char topLeft, char topRight, char bottomLeft, char bottomRight, Color bgColor)
    {
        if (Width < 3 || Height < 3)
            return;

        var (_, fgColor) = GetStateColors();
        Color borderColor = BorderColor.A > 0 ? BorderColor : fgColor;

        for (int x = X + 1; x < X + Width - 1; x++)
        {
            screen.SetCell(x, Y, new Cell(horizontal, borderColor, bgColor));
            screen.SetCell(x, Y + Height - 1, new Cell(horizontal, borderColor, bgColor));
        }

        for (int y = Y + 1; y < Y + Height - 1; y++)
        {
            screen.SetCell(X, y, new Cell(vertical, borderColor, bgColor));
            screen.SetCell(X + Width - 1, y, new Cell(vertical, borderColor, bgColor));
        }

        screen.SetCell(X, Y, new Cell(topLeft, borderColor, bgColor));
        screen.SetCell(X + Width - 1, Y, new Cell(topRight, borderColor, bgColor));
        screen.SetCell(X, Y + Height - 1, new Cell(bottomLeft, borderColor, bgColor));
        screen.SetCell(X + Width - 1, Y + Height - 1, new Cell(bottomRight, borderColor, bgColor));
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
        _isPressed = false;
    }

    protected internal override bool OnKeyPress(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Spacebar)
        {
            _isPressed = true;
            Pressed?.Invoke();

            _isPressed = false;
            Released?.Invoke();
            Click?.Invoke();

            return true;
        }

        return base.OnKeyPress(key);
    }

    /// <summary>
    /// Programmatically clicks the button.
    /// </summary>
    public void PerformClick()
    {
        Click?.Invoke();
    }
}
