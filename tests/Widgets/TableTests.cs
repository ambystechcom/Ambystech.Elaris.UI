using Ambystech.Elaris.UI.Enums;
using Ambystech.Elaris.UI.Widgets.Data;
using FluentAssertions;

namespace Ambystech.Elaris.UI.Tests.Widgets;

public class TableTests
{
    [Fact]
    public void Table_Should_Initialize_Empty()
    {
        var table = new Table();

        table.Columns.Should().BeEmpty();
        table.Rows.Should().BeEmpty();
        table.SelectedRowIndex.Should().Be(-1);
        table.SelectedRow.Should().BeNull();
    }

    [Fact]
    public void AddColumn_Should_Create_Column_With_Defaults()
    {
        var table = new Table();

        var column = table.AddColumn("Name", 20);

        table.Columns.Should().HaveCount(1);
        column.Name.Should().Be("Name");
        column.Width.Should().Be(20);
        column.IsVisible.Should().BeTrue();
        column.Alignment.Should().Be(HorizontalAlignment.Left);
    }

    [Fact]
    public void AddRow_Should_Create_Row_With_Index()
    {
        var table = new Table();
        table.AddColumn("Name");

        var row1 = table.AddRow("Data1");
        var row2 = table.AddRow("Data2");

        table.Rows.Should().HaveCount(2);
        row1.Index.Should().Be(0);
        row2.Index.Should().Be(1);
        row1.Data.Should().Be("Data1");
    }

    [Fact]
    public void DataSource_Should_Rebuild_Rows()
    {
        var table = new Table();
        table.AddColumn("Name");

        var items = new[] { "Item1", "Item2", "Item3" };
        table.DataSource = items;

        table.Rows.Should().HaveCount(3);
        table.Rows[0].Data.Should().Be("Item1");
        table.Rows[1].Data.Should().Be("Item2");
        table.Rows[2].Data.Should().Be("Item3");
    }

    [Fact]
    public void DataSource_Should_Select_First_Row()
    {
        var table = new Table();
        table.AddColumn("Name");

        table.DataSource = new[] { "A", "B", "C" };

        table.SelectedRowIndex.Should().Be(0);
        table.SelectedRow?.Data.Should().Be("A");
    }

    [Fact]
    public void ClearRows_Should_Reset_State()
    {
        var table = new Table();
        table.AddColumn("Name");
        table.AddRow("Data1");
        table.AddRow("Data2");

        table.ClearRows();

        table.Rows.Should().BeEmpty();
        table.SelectedRowIndex.Should().Be(-1);
    }

    [Fact]
    public void SelectedRowIndex_Should_Clamp_To_Valid_Range()
    {
        var table = new Table();
        table.AddColumn("Name");
        table.AddRow("A");
        table.AddRow("B");
        table.AddRow("C");

        table.SelectedRowIndex = 100;
        table.SelectedRowIndex.Should().Be(2);

        table.SelectedRowIndex = -5;
        table.SelectedRowIndex.Should().Be(-1);
    }

    [Fact]
    public void RowSelected_Event_Should_Fire_On_Selection()
    {
        var table = new Table();
        table.AddColumn("Name");
        table.AddRow("A");
        table.AddRow("B");

        TableRow? selectedRow = null;
        table.RowSelected += row => selectedRow = row;

        table.SelectedRowIndex = 1;

        selectedRow.Should().NotBeNull();
        selectedRow!.Data.Should().Be("B");
    }

    [Fact]
    public void Column_BindTo_PropertyName_Should_Get_Value()
    {
        var column = new TableColumn("Name");
        column.BindTo("Length");

        var value = column.GetValue("Hello");

        value.Should().Be(5);
    }

    [Fact]
    public void Column_BindTo_Lambda_Should_Get_Value()
    {
        var column = new TableColumn("Doubled");
        column.BindTo<int>(n => n * 2);

        var value = column.GetValue(5);

        value.Should().Be(10);
    }

    [Fact]
    public void TableRow_GetCell_Should_Cache_Cells()
    {
        var column = new TableColumn("Name");
        var row = new TableRow("Data");

        var cell1 = row.GetCell(column);
        var cell2 = row.GetCell(column);

        cell1.Should().BeSameAs(cell2);
    }

    [Fact]
    public void TableRow_InvalidateCells_Should_Clear_Cache()
    {
        var column = new TableColumn("Name");
        var row = new TableRow("Data");

        var cell1 = row.GetCell(column);
        row.InvalidateCells();
        var cell2 = row.GetCell(column);

        cell1.Should().NotBeSameAs(cell2);
    }

    [Fact]
    public void ProgressColumn_Should_Create_ProgressBarCell()
    {
        var column = new ProgressColumn("Progress", 15)
        {
            Minimum = 0,
            Maximum = 100,
            ShowPercentage = true
        };
        var row = new TableRow(50.0);

        var cell = column.CreateCell(row, 50.0);

        cell.Should().BeOfType<ProgressBarCell>();
        var progressCell = (ProgressBarCell)cell;
        progressCell.ProgressBar.Minimum.Should().Be(0);
        progressCell.ProgressBar.Maximum.Should().Be(100);
        progressCell.ProgressBar.ShowPercentage.Should().BeTrue();
    }

    [Fact]
    public void CheckboxColumn_Should_Create_CheckboxCell()
    {
        var column = new CheckboxColumn("Done", 5);
        var row = new TableRow(true);

        var cell = column.CreateCell(row, true);

        cell.Should().BeOfType<CheckboxCell>();
    }
}
