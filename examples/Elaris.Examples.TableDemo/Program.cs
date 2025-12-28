using System.Drawing;
using Ambystech.Elaris.UI;
using Ambystech.Elaris.UI.Core;
using Ambystech.Elaris.UI.Enums;
using Ambystech.Elaris.UI.Widgets.Data;
using Ambystech.Elaris.UI.Widgets.Display;
using Ambystech.Elaris.UI.Widgets.Layout;
using Ambystech.Elaris.UI.Widgets.Layout.Responsive;

var app = new Application();

var statusBar = new StatusBar
{
    Height = 1,
    LeftText = "Ready",
    CenterText = "Table Demo - Arrow Keys to Navigate, Enter to Activate",
    RightText = "ESC to exit"
};

var table = new Table
{
    BackgroundColor = Color.Black,
    ForegroundColor = Color.White,
    HeaderBackgroundColor = ColorHelper.FromRgb(40, 60, 90),
    HeaderForegroundColor = Color.White,
    SelectedRowBackgroundColor = ColorHelper.FromRgb(0, 120, 212),
    SelectedRowForegroundColor = Color.White,
    GridLineColor = ColorHelper.FromRgb(60, 60, 60),
    ShowHeader = true,
    ShowGridLines = true,
    EmptyStateMessage = "No tasks to display"
};

table.AddColumn("Task", 30).BindTo<TaskItem>(t => t.Name);
table.AddColumn("Status", 12).BindTo<TaskItem>(t => t.Status);
table.AddColumn(new ProgressColumn("Progress", 20) { ShowPercentage = true }.BindTo<TaskItem>(t => t.Progress));
table.AddColumn(new CheckboxColumn("Done", 6).BindTo<TaskItem>(t => t.IsComplete));
table.AddColumn("Priority", 10).BindTo<TaskItem>(t => t.Priority);
table.AddColumn("Assignee", 15).BindTo<TaskItem>(t => t.Assignee);

var tasks = new List<TaskItem>
{
    new("Implement login page", "In Progress", 65, false, "High", "Alice"),
    new("Design database schema", "Complete", 100, true, "High", "Bob"),
    new("Write unit tests", "In Progress", 30, false, "Medium", "Carol"),
    new("Setup CI/CD pipeline", "Pending", 0, false, "High", "Dave"),
    new("Code review PR #42", "In Progress", 80, false, "Medium", "Alice"),
    new("Update documentation", "Pending", 0, false, "Low", "Eve"),
    new("Fix memory leak", "Complete", 100, true, "Critical", "Bob"),
    new("Optimize queries", "In Progress", 45, false, "Medium", "Carol"),
    new("Add logging", "Pending", 10, false, "Low", "Dave"),
    new("Security audit", "In Progress", 25, false, "Critical", "Eve"),
    new("Performance testing", "Pending", 0, false, "High", "Alice"),
    new("Refactor auth module", "Complete", 100, true, "Medium", "Bob")
};

table.DataSource = tasks;

table.RowSelected += row =>
{
    if (row.Data is TaskItem task)
        statusBar.LeftText = $"Selected: {task.Name}";
};

table.RowActivated += row =>
{
    if (row.Data is TaskItem task)
        statusBar.CenterText = $"Activated: {task.Name} ({task.Status})";
};

var root = new ResponsiveContainer { BackgroundColor = Color.Black };

var frame = new Frame("Task Manager")
{
    BorderStyle = BorderStyle.Rounded,
    ForegroundColor = ColorHelper.FromRgb(100, 200, 255)
};

frame.Add(table);
root.Add(frame);
root.Add(statusBar);

root.OnResize((width, height) =>
{
    frame.X = 0;
    frame.Y = 0;
    frame.Width = width;
    frame.Height = height - 1;

    table.X = 1;
    table.Y = 1;
    table.Width = width - 2;
    table.Height = height - 3;

    statusBar.X = 0;
    statusBar.Y = height - 1;
    statusBar.Width = width;
});

app.InitialFocusWidget = table;

try
{
    Console.WriteLine("Starting Elaris Table Demo...");
    Console.WriteLine("Press ESC or Ctrl+C to exit");
    Console.WriteLine("Use Arrow Keys to navigate rows!");
    Thread.Sleep(1500);

    app.Run(root);
}
catch (Exception ex)
{
    Console.WriteLine($"\nError: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}

record TaskItem(string Name, string Status, double Progress, bool IsComplete, string Priority, string Assignee);
