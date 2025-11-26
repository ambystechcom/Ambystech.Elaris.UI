using System.Drawing;
using Ambystech.Elaris.UI.Rendering;
using Ambystech.Elaris.UI.Widgets.Display;

namespace Ambystech.Elaris.UI.Widgets.Data;

public class ProgressBarCell : TableCell
{
    private readonly ProgressBar _progressBar;

    public ProgressBarCell(TableColumn column, TableRow row, object? value) : base(column, row, value)
    {
        _progressBar = new ProgressBar
        {
            ShowPercentage = true,
            Height = 1
        };
        Widget = _progressBar;
    }

    public ProgressBar ProgressBar => _progressBar;

    public override void Render(Screen screen, int x, int y, int width, bool isSelected,
        Color foreground, Color background)
    {
        double progressValue = Value switch
        {
            double d => d,
            float f => f,
            int i => i,
            decimal dec => (double)dec,
            _ => 0
        };

        _progressBar.Value = progressValue;
        _progressBar.BackgroundColor = background;
        UpdateBounds(x, y, width, 1);
        _progressBar.Render(screen);
    }
}
