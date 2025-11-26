namespace Ambystech.Elaris.UI.Widgets.Data;

public class ProgressColumn : TableColumn
{
    public double Minimum { get; set; } = 0;
    public double Maximum { get; set; } = 100;
    public bool ShowPercentage { get; set; } = true;

    public ProgressColumn(string name, int width = 15) : base(name, width) { }

    public override TableCell CreateCell(TableRow row, object? value)
    {
        var cell = new ProgressBarCell(this, row, value);
        cell.ProgressBar.Minimum = Minimum;
        cell.ProgressBar.Maximum = Maximum;
        cell.ProgressBar.ShowPercentage = ShowPercentage;
        return cell;
    }
}
