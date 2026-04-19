using System.Drawing;
using Ambystech.Elaris.UI;
using Ambystech.Elaris.UI.Core;
using Ambystech.Elaris.UI.Enums;
using Ambystech.Elaris.UI.Widgets.Input;
using Ambystech.Elaris.UI.Widgets.Layout;
using Ambystech.Elaris.UI.Widgets.Layout.Responsive;
using Ambystech.Elaris.UI.Widgets.Menu;

var app = new Application();

var statusBar = new StatusBar
{
    Height = 1,
    LeftText = "Editor Demo",
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

var leftFrame = new Frame("Plain Text Editor")
{
    BorderStyle = BorderStyle.Single,
    ForegroundColor = ColorHelper.FromRgb(100, 200, 255),
    BackgroundColor = Color.Black
};

Editor editor = new()
{
    ShowLineNumbers = true,
    WordWrap = false,
    ForegroundColor = Color.White,
    BackgroundColor = Color.Black,
    LineNumberColor = ColorHelper.FromRgb(100, 100, 100),
    Text = @"Welcome to the Elaris Editor Demo!

    This is a plain text editor with the following features:
    - Multi-line text editing
    - Cursor navigation with arrow keys
    - Line numbers display
    - Undo/Redo support (Ctrl+Z / Ctrl+Y)
    - Text selection (Ctrl+A to select all)
    - Home/End for line navigation
    - Page Up/Down for scrolling

    Try editing this text! Use the arrow keys to move around,
    and type to insert text. Press Backspace or Delete to remove characters.

    The editor supports full keyboard navigation and editing capabilities."
};

editor.CursorMoved += (line, col) =>
{
    statusBar.RightText = $"Line {line + 1}, Col {col + 1}";
};

leftFrame.Add(editor);

Frame rightFrame = new("Code Editor (C#)")
{
    BorderStyle = BorderStyle.Single,
    ForegroundColor = ColorHelper.FromRgb(255, 200, 100),
    BackgroundColor = Color.Black
};

CodeEditor codeEditor = new()
{
    ShowLineNumbers = true,
    SyntaxHighlighting = true,
    ShowBracketMatching = true,
    AutoIndent = true,
    TabSize = 4,
    Language = "csharp",
    ForegroundColor = Color.White,
    BackgroundColor = Color.Black,
    LineNumberColor = ColorHelper.FromRgb(100, 100, 100),
    Text = @"using System;

        namespace Elaris.Examples
        {
            public class Program
            {
                public static void Main(string[] args)
                {
                    Console.WriteLine(""Hello, Elaris!"");
            
                    var editor = new CodeEditor
                    {
                        Language = ""csharp"",
                        SyntaxHighlighting = true
                    };
            
                    editor.Text = ""// Your code here"";
                }
            }
        }"
};

codeEditor.CursorMoved += (line, col) =>
{
    statusBar.CenterText = $"Code Editor - Line {line + 1}, Col {col + 1} | Language: {codeEditor.Language}";
};

rightFrame.Add(codeEditor);

var root = new ResponsiveContainer
{
    BackgroundColor = Color.Black
};

root.Add(menuBar);
root.Add(leftFrame);
root.Add(rightFrame);
root.Add(statusBar);

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

    int framePadding = 2;
    int lineNumberWidth = editor.ShowLineNumbers ? 5 : 0;

    editor.X = leftFrame.X + framePadding;
    editor.Y = leftFrame.Y + framePadding;
    editor.Width = leftFrame.Width - (framePadding * 2);
    editor.Height = leftFrame.Height - (framePadding * 2);

    codeEditor.X = rightFrame.X + framePadding;
    codeEditor.Y = rightFrame.Y + framePadding;
    codeEditor.Width = rightFrame.Width - (framePadding * 2);
    codeEditor.Height = rightFrame.Height - (framePadding * 2);
});

app.InitialFocusWidget = editor;

try
{
    Console.WriteLine("Starting Elaris Editor Demo...");
    Console.WriteLine("Press ESC or Ctrl+C to exit");
    Console.WriteLine("Use Alt+Right/Left Arrow to switch between editors");
    Console.WriteLine("Left: Plain Text Editor | Right: Code Editor with Syntax Highlighting");
    Thread.Sleep(1500);

    app.Run(root);
}
catch (Exception ex)
{
    Console.WriteLine($"\nError: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}

