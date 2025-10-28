using System.Drawing;
using Ambystech.Elaris.UI.Core;
using Ambystech.Elaris.UI.Enums;
using Ambystech.Elaris.UI.Rendering;

namespace Ambystech.Elaris.UI.Widgets.Display;

/// <summary>
/// A progress bar widget that displays completion percentage.
/// </summary>
public class ProgressBar : Widget
{
    private double _value = 0;
    private double _minimum = 0;
    private double _maximum = 100;
    private bool _showPercentage = true;
    private VerticalAlignment _percentageVerticalAlignment = VerticalAlignment.Middle;
    private HorizontalAlignment _percentageHorizontalAlignment = HorizontalAlignment.Center;

    /// <summary>
    /// Gets or sets the current value.
    /// </summary>
    public double Value
    {
        get => _value;
        set
        {
            double oldValue = _value;
            _value = Math.Max(_minimum, Math.Min(_maximum, value));

            if (oldValue != _value)
            {
                ValueChanged?.Invoke(_value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the minimum value.
    /// </summary>
    public double Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            if (_value < _minimum)
                _value = _minimum;
        }
    }

    /// <summary>
    /// Gets or sets the maximum value.
    /// </summary>
    public double Maximum
    {
        get => _maximum;
        set
        {
            _maximum = value;
            if (_value > _maximum)
                _value = _maximum;
        }
    }

    /// <summary>
    /// Gets or sets whether to show percentage text.
    /// </summary>
    public bool ShowPercentage
    {
        get => _showPercentage;
        set => _showPercentage = value;
    }

    /// <summary>
    /// Gets or sets the color of the filled portion of the progress bar.
    /// </summary>
    public Color FilledColor { get; set; }

    /// <summary>
    /// Gets or sets the color of the unfilled portion of the progress bar.
    /// </summary>
    public Color UnfilledColor { get; set; }

    /// <summary>
    /// Gets or sets the text color for percentage display over filled portion.
    /// </summary>
    public Color FilledTextColor { get; set; } = Color.White;

    /// <summary>
    /// Gets or sets the vertical alignment of the percentage text.
    /// </summary>
    public VerticalAlignment PercentageVerticalAlignment
    {
        get => _percentageVerticalAlignment;
        set => _percentageVerticalAlignment = value;
    }

    /// <summary>
    /// Gets or sets the horizontal alignment of the percentage text.
    /// </summary>
    public HorizontalAlignment PercentageHorizontalAlignment
    {
        get => _percentageHorizontalAlignment;
        set => _percentageHorizontalAlignment = value;
    }

    /// <summary>
    /// Event raised when the value changes.
    /// </summary>
    public event Action<double>? ValueChanged;

    public ProgressBar()
    {
        FilledColor = Color.Green;
        UnfilledColor = Color.DarkGray;
        Height = 1;
    }

    /// <summary>
    /// Gets the current progress as a percentage (0-100).
    /// </summary>
    public double Percentage
    {
        get
        {
            double range = _maximum - _minimum;
            if (range <= 0)
                return 0;

            return ((_value - _minimum) / range) * 100.0;
        }
    }

    protected override void OnRender(Screen screen)
    {
        if (Width <= 0 || Height <= 0)
            return;

        // Calculate filled width
        double range = _maximum - _minimum;
        double progress = range > 0 ? (_value - _minimum) / range : 0;
        int filledWidth = (int)(Width * progress);

        // Render progress bar
        for (int y = Y; y < Y + Height; y++)
        {
            for (int x = X; x < X + Width; x++)
            {
                bool isFilled = (x - X) < filledWidth;
                Color bgColor = isFilled ? FilledColor : UnfilledColor;
                char fillChar = isFilled ? '█' : '░';

                screen.SetCell(x, y, new Cell(fillChar, bgColor, BackgroundColor));
            }
        }

        if (_showPercentage && Width >= 5 && Height > 0)
        {
            string percentText = $"{Percentage:F0}%";

            int textX = _percentageHorizontalAlignment switch
            {
                HorizontalAlignment.Left => X,
                HorizontalAlignment.Center => X + (Width - percentText.Length) / 2,
                HorizontalAlignment.Right => X + Width - percentText.Length,
                _ => X + (Width - percentText.Length) / 2
            };

            int textY = _percentageVerticalAlignment switch
            {
                VerticalAlignment.Top => Y,
                VerticalAlignment.Middle => Y + (Height - 1) / 2,
                VerticalAlignment.Bottom => Y + Height - 1,
                _ => Y + (Height - 1) / 2
            };

            for (int i = 0; i < percentText.Length && textX + i < X + Width; i++)
            {
                bool isFilled = (textX + i - X) < filledWidth;
                Color textColor = isFilled ? FilledTextColor : ForegroundColor;
                Color bgColor = isFilled ? FilledColor : UnfilledColor;

                var cell = new Cell(percentText[i], textColor, bgColor, bold: true);
                screen.SetCell(textX + i, textY, cell);
            }
        }
    }

    /// <summary>
    /// Increments the value by the specified amount.
    /// </summary>
    public void Increment(double amount = 1)
    {
        Value += amount;
    }

    /// <summary>
    /// Decrements the value by the specified amount.
    /// </summary>
    public void Decrement(double amount = 1)
    {
        Value -= amount;
    }

    /// <summary>
    /// Sets the value to a specific percentage (0-100).
    /// </summary>
    public void SetPercentage(double percentage)
    {
        percentage = Math.Max(0, Math.Min(100, percentage));
        double range = _maximum - _minimum;
        Value = _minimum + (range * percentage / 100.0);
    }
}
