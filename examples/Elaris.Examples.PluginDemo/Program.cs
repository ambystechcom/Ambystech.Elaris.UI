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
    X = 0,
    Y = 0,
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
    println!({}, message);
    
    let numbers = vec![1, 2, 3, 4, 5];
    for num in numbers {
        println!({}, num);
    }
}";

var root = new ResponsiveContainer
{
    X = 0,
    Y = 1,
    Width = app.Screen.Width,
    Height = app.Screen.Height - 2,
    LayoutMode = LayoutMode.Horizontal
};

root.OnResize((width, height) =>
{
    int framePadding = 2;
    int lineNumberWidth = codeEditor1.ShowLineNumbers ? 5 : 0;

    leftFrame.X = 0;
    leftFrame.Y = 0;
    leftFrame.Width = width / 2;
    leftFrame.Height = height;

    rightFrame.X = width / 2;
    rightFrame.Y = 0;
    rightFrame.Width = width - (width / 2);
    rightFrame.Height = height;

    codeEditor1.X = leftFrame.X + framePadding;
    codeEditor1.Y = leftFrame.Y + framePadding;
    codeEditor1.Width = leftFrame.Width - (framePadding * 2);
    codeEditor1.Height = leftFrame.Height - (framePadding * 2);

    codeEditor2.X = rightFrame.X + framePadding;
    codeEditor2.Y = rightFrame.Y + framePadding;
    codeEditor2.Width = rightFrame.Width - (framePadding * 2);
    codeEditor2.Height = rightFrame.Height - (framePadding * 2);
});

root.Add(leftFrame);
root.Add(rightFrame);
leftFrame.Add(codeEditor1);
rightFrame.Add(codeEditor2);

var mainContainer = new Container
{
    X = 0,
    Y = 0,
    Width = app.Screen?.Width ?? 80,
    Height = app.Screen?.Height ?? 24
};

mainContainer.Add(menuBar);
mainContainer.Add(root);
mainContainer.Add(statusBar);

app.InitialFocusWidget = codeEditor1;

Console.WriteLine("Plugin Demo - Demonstrating file-based syntax highlighting plugins");
Console.WriteLine("Use Alt+Right/Left Arrow to switch between editors");
Console.WriteLine("Press ESC to exit");
Console.WriteLine();

app.Run(mainContainer);
