using System.Drawing;
using Ambystech.Elaris.UI;
using Ambystech.Elaris.UI.Core;
using Ambystech.Elaris.UI.Enums;
using Ambystech.Elaris.UI.Widgets.Display;
using Ambystech.Elaris.UI.Widgets.Layout;
using Ambystech.Elaris.UI.Widgets.Layout.Responsive;
using Ambystech.Elaris.UI.Widgets.Menu;

var app = new Application();

var statusBar = new StatusBar
{
    Height = 1,
    LeftText = "Ready",
    CenterText = "Menu Demo - Use Arrow Keys to Navigate",
    RightText = "ESC to exit"
};

var menuBar = new MenuBar
{
    X = 0,
    Y = 0,
    Height = 1,
    ForegroundColor = Color.Black,
    BackgroundColor = ColorHelper.FromRgb(200, 200, 200)
};

menuBar.AddMenu("File",
    new MenuItem("New", () => statusBar.LeftText = "New file created", 'N'),
    new MenuItem("Open", () => statusBar.LeftText = "Open file dialog", 'O'),
    new MenuItem("Save", () => statusBar.LeftText = "File saved", 'S'),
    new MenuItem("Save As...", () => statusBar.LeftText = "Save As dialog", 'A'),
    MenuItem.Separator(),
    new MenuItem("Exit", () => app.Stop(), 'x')
);

menuBar.AddMenu("Edit",
    new MenuItem("Undo", () => statusBar.LeftText = "Undo last action", 'U'),
    new MenuItem("Redo", () => statusBar.LeftText = "Redo last action", 'R'),
    MenuItem.Separator(),
    new MenuItem("Cut", () => statusBar.LeftText = "Cut to clipboard", 't'),
    new MenuItem("Copy", () => statusBar.LeftText = "Copy to clipboard", 'C'),
    new MenuItem("Paste", () => statusBar.LeftText = "Paste from clipboard", 'P'),
    MenuItem.Separator(),
    new MenuItem("Select All", () => statusBar.LeftText = "All selected", 'A')
);

var showToolbarItem = new MenuItem("Show Toolbar", null, 'T') { Checked = true };
showToolbarItem.Selected += () =>
{
    showToolbarItem.Checked = !showToolbarItem.Checked;
    statusBar.LeftText = $"Toolbar: {(showToolbarItem.Checked ? "Visible" : "Hidden")}";
};

var showStatusBarItem = new MenuItem("Show Status Bar", null, 'S') { Checked = true };
showStatusBarItem.Selected += () =>
{
    showStatusBarItem.Checked = !showStatusBarItem.Checked;
    statusBar.LeftText = $"Status Bar: {(showStatusBarItem.Checked ? "Visible" : "Hidden")}";
};

menuBar.AddMenu("View",
    showToolbarItem,
    showStatusBarItem,
    MenuItem.Separator(),
    new MenuItem("Zoom In", () => statusBar.LeftText = "Zoomed in", '+'),
    new MenuItem("Zoom Out", () => statusBar.LeftText = "Zoomed out", '-'),
    new MenuItem("Reset Zoom", () => statusBar.LeftText = "Zoom reset to 100%", '0')
);

menuBar.AddMenu("Help",
    new MenuItem("Documentation", () => statusBar.LeftText = "Opening documentation...", 'D'),
    new MenuItem("Keyboard Shortcuts", () => statusBar.LeftText = "Showing keyboard shortcuts", 'K'),
    MenuItem.Separator(),
    new MenuItem("About", () => statusBar.LeftText = "Elaris Menu Demo v1.0", 'A')
);

var contentFrame = new Frame("Menu Demo")
{
    Y = 1,
    BorderStyle = BorderStyle.Single,
    ForegroundColor = ColorHelper.FromRgb(100, 200, 255),
    BackgroundColor = Color.Black
};

var contentLabel = new Label(@"Welcome to the Elaris Menu Demo!

This demo showcases the MenuBar widget with dropdown menus.

Navigation:
  • Tab: Move focus to menu bar
  • Arrow Keys: Navigate menus and items
  • Enter/Space: Select menu item
  • ESC: Close open menu

Features:
  ✓ Dropdown menus
  ✓ Menu separators
  ✓ Checkable items (see View menu)
  ✓ Keyboard shortcuts
  ✓ Event handling

Try selecting items from the menus above!
Watch the status bar for feedback.")
{
    X = 2,
    Y = 2,
    ForegroundColor = Color.White,
    BackgroundColor = Color.Black,
    HorizontalAlignment = HorizontalAlignment.Left,
    VerticalAlignment = VerticalAlignment.Top
};

contentFrame.Add(contentLabel);

var root = new ResponsiveContainer
{
    BackgroundColor = Color.Black
};

root.Add(menuBar);
root.Add(contentFrame);
root.Add(statusBar);

root.OnResize((width, height) =>
{
    menuBar.X = 0;
    menuBar.Y = 0;
    menuBar.Width = width;
    menuBar.Height = 1;

    contentFrame.X = 0;
    contentFrame.Y = 1;
    contentFrame.Width = width;
    contentFrame.Height = height - 2;

    contentLabel.Width = width - 4;
    contentLabel.Height = height - 6;

    statusBar.X = 0;
    statusBar.Y = height - 1;
    statusBar.Width = width;
});

app.InitialFocusWidget = menuBar;

try
{
    Console.WriteLine("Starting Elaris Menu Demo...");
    Console.WriteLine("Press ESC or Ctrl+C to exit");
    Console.WriteLine("Resize the window to see the responsive layout!");
    Thread.Sleep(1500);

    app.Run(root);
}
catch (Exception ex)
{
    Console.WriteLine($"\nError: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}
