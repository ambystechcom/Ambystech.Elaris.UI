using System.Drawing;
using Ambystech.Elaris.UI.Rendering;

namespace Ambystech.Elaris.UI.Widgets.Layout;

/// <summary>
/// A status bar widget typically shown at the bottom of the screen.
/// </summary>
public class StatusBar : Widget
{
    private string _leftText = string.Empty;
    private string _centerText = string.Empty;
    private string _rightText = string.Empty;
    private Color _statusBarBackground = Color.DarkGray;
    private Color _statusBarForeground = Color.White;

    /// <summary>
    /// Gets or sets the text displayed on the left side.
    /// </summary>
    public string LeftText
    {
        get => _leftText;
        set => _leftText = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the text displayed in the center.
    /// </summary>
    public string CenterText
    {
        get => _centerText;
        set => _centerText = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the text displayed on the right side.
    /// </summary>
    public string RightText
    {
        get => _rightText;
        set => _rightText = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the status bar background color.
    /// </summary>
    public Color StatusBarBackground
    {
        get => _statusBarBackground;
        set => _statusBarBackground = value;
    }

    /// <summary>
    /// Gets or sets the status bar foreground color.
    /// </summary>
    public Color StatusBarForeground
    {
        get => _statusBarForeground;
        set => _statusBarForeground = value;
    }

    public StatusBar()
    {
        Height = 1; // Status bar is typically 1 line tall
    }

    protected override void OnRender(Screen screen)
    {
        if (Width <= 0 || Height <= 0)
            return;

        screen.FillRectangle(Bounds, ' ', _statusBarForeground, _statusBarBackground);

        if (!string.IsNullOrEmpty(_leftText))
        {
            string leftDisplay = _leftText.Length > Width / 3
                ? _leftText[..(Width / 3)]
                : _leftText;
            screen.WriteText(X, Y, leftDisplay, _statusBarForeground, _statusBarBackground);
        }

        if (!string.IsNullOrEmpty(_centerText))
        {
            int centerX = X + (Width - _centerText.Length) / 2;
            centerX = Math.Max(X, Math.Min(centerX, X + Width - _centerText.Length));

            string centerDisplay = _centerText.Length > Width / 3
                ? _centerText.Substring(0, Width / 3)
                : _centerText;

            screen.WriteText(centerX, Y, centerDisplay, _statusBarForeground, _statusBarBackground);
        }

        if (!string.IsNullOrEmpty(_rightText))
        {
            string rightDisplay = _rightText.Length > Width / 3
                ? _rightText.Substring(0, Width / 3)
                : _rightText;

            int rightX = X + Width - rightDisplay.Length;
            screen.WriteText(rightX, Y, rightDisplay, _statusBarForeground, _statusBarBackground);
        }
    }

    /// <summary>
    /// Sets all status bar text at once.
    /// </summary>
    public void SetStatus(string? left = null, string? center = null, string? right = null)
    {
        if (left != null) _leftText = left;
        if (center != null) _centerText = center;
        if (right != null) _rightText = right;
    }
}
