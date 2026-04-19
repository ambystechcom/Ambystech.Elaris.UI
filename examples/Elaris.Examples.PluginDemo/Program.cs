using System.Drawing;
using Ambystech.Elaris.UI;
using Ambystech.Elaris.UI.Core;
using Ambystech.Elaris.UI.Enums;
using Ambystech.Elaris.UI.Widgets.Display;
using Ambystech.Elaris.UI.Widgets.Input;
using Ambystech.Elaris.UI.Widgets.Layout;
using Ambystech.Elaris.UI.Widgets.Layout.Responsive;
using Ambystech.Elaris.UI.Widgets.Menu;
using Ambystech.Elaris.UI.CodeEditor.Plugins;

var app = new Application();

var pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
var syntaxRulesPath = Path.Combine(pluginsPath, "syntax");
var themesPath = Path.Combine(pluginsPath, "themes");

Directory.CreateDirectory(syntaxRulesPath);
Directory.CreateDirectory(themesPath);

PluginInitializer.Initialize(syntaxRulesPath, themesPath);

var statusBar = new StatusBar
{
    Height = 1,
    LeftText = "Plugin Demo",
    CenterText = "Use Alt+Right/Left Arrow to switch between editors",
    RightText = "ESC to exit"
};

var menuBar = new MenuBar
{
    Height = 1,
    ForegroundColor = Color.Black,
    BackgroundColor = ColorHelper.FromRgb(200, 200, 200)
};

menuBar.AddMenu("File",
    new MenuItem("New", () => statusBar.LeftText = "New file", 'N'),
    new MenuItem("Open", () => statusBar.LeftText = "Open file", 'O'),
    new MenuItem("Save", () => statusBar.LeftText = "File saved", 'S'),
    MenuItem.Separator(),
    new MenuItem("Exit", () => app.Stop(), 'x')
);

menuBar.AddMenu("Edit",
    new MenuItem("Undo", () => statusBar.LeftText = "Undo (Ctrl+Z)", 'U'),
    new MenuItem("Redo", () => statusBar.LeftText = "Redo (Ctrl+Y)", 'R'),
    MenuItem.Separator(),
    new MenuItem("Select All", () => statusBar.LeftText = "Select All (Ctrl+A)", 'A')
);

var leftFrame = new Frame("Built-in Language (C#)")
{
    BorderStyle = BorderStyle.Single,
    ForegroundColor = ColorHelper.FromRgb(100, 200, 255),
    BackgroundColor = Color.Black
};

var rightFrame = new Frame("Custom Language (Rust)")
{
    BorderStyle = BorderStyle.Single,
    ForegroundColor = ColorHelper.FromRgb(100, 200, 255),
    BackgroundColor = Color.Black
};

var codeEditor1 = new CodeEditor
{
    Language = "csharp",
    Theme = "default",
    ShowLineNumbers = true,
    ForegroundColor = Color.White,
    BackgroundColor = Color.Black,
    LineNumberColor = ColorHelper.FromRgb(100, 100, 100)
};

var codeEditor2 = new CodeEditor
{
    Language = "rust",
    Theme = "default",
    ShowLineNumbers = true,
    ForegroundColor = Color.White,
    BackgroundColor = Color.Black,
    LineNumberColor = ColorHelper.FromRgb(100, 100, 100)
};

codeEditor1.Text = @"using System;

namespace Example
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(""Hello, World!"");
        }
    }
}";

codeEditor2.Text = @"fn main() {
    let message = ""Hello, World!"";
    println!(""{}"", message);

    let numbers = vec![1, 2, 3, 4, 5];
    for num in numbers {
        println!(""{}"", num);
    }
}";

var root = new ResponsiveContainer
{
    BackgroundColor = Color.Black
};

root.Add(menuBar);
root.Add(leftFrame);
root.Add(rightFrame);
root.Add(statusBar);
leftFrame.Add(codeEditor1);
rightFrame.Add(codeEditor2);

root.OnResize((width, height) =>
{
    menuBar.X = 0;
    menuBar.Y = 0;
    menuBar.Width = width;
    menuBar.Height = 1;

    int contentY = 1;
    int contentHeight = height - 2;
    int splitX = width / 2;

    leftFrame.X = 0;
    leftFrame.Y = contentY;
    leftFrame.Width = splitX;
    leftFrame.Height = contentHeight;

    rightFrame.X = splitX;
    rightFrame.Y = contentY;
    rightFrame.Width = width - splitX;
    rightFrame.Height = contentHeight;

    statusBar.X = 0;
    statusBar.Y = height - 1;
    statusBar.Width = width;
    statusBar.Height = 1;

    int framePadding = 2;

    codeEditor1.X = leftFrame.X + framePadding;
    codeEditor1.Y = leftFrame.Y + framePadding;
    codeEditor1.Width = leftFrame.Width - (framePadding * 2);
    codeEditor1.Height = leftFrame.Height - (framePadding * 2);

    codeEditor2.X = rightFrame.X + framePadding;
    codeEditor2.Y = rightFrame.Y + framePadding;
    codeEditor2.Width = rightFrame.Width - (framePadding * 2);
    codeEditor2.Height = rightFrame.Height - (framePadding * 2);
});

app.InitialFocusWidget = codeEditor1;

try
{
    Console.WriteLine("Plugin Demo - Demonstrating file-based syntax highlighting plugins");
    Console.WriteLine("Use Alt+Right/Left Arrow to switch between editors");
    Console.WriteLine("Press ESC to exit");
    Thread.Sleep(1500);

    app.Run(root);
}
catch (Exception ex)
{
    Console.WriteLine($"\nError: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}
