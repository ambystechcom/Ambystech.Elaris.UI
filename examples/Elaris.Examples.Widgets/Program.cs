using System.Drawing;
using Ambystech.Elaris.UI;
using Ambystech.Elaris.UI.Core;
using Ambystech.Elaris.UI.Enums;
using Ambystech.Elaris.UI.Widgets.Display;
using Ambystech.Elaris.UI.Widgets.Input;
using Ambystech.Elaris.UI.Widgets.Layout;

var app = new Application();

var container = new Container
{
    LayoutMode = LayoutMode.Vertical,
    Padding = 1,
    Spacing = 1,
    BackgroundColor = Color.Black
};

// Each frame lays out its own children inside its border. Widget positions are absolute
// screen coordinates, so fixed X/Y values would not follow the frame when the outer
// container moves it.
var headerFrame = new Frame("Elaris Widget Showcase")
{
    LayoutMode = LayoutMode.Vertical,
    Padding = 1,
    Height = 5,
    BorderStyle = BorderStyle.Double,
    ForegroundColor = ColorHelper.FromRgb(100, 200, 255),
    BackgroundColor = Color.Black
};

var headerLabel = new Label("Explore all available widgets")
{
    Height = 1,
    ForegroundColor = ColorHelper.FromRgb(200, 200, 200),
    BackgroundColor = Color.Black
};
headerFrame.Add(headerLabel);

var labelsFrame = new Frame("Labels")
{
    LayoutMode = LayoutMode.Vertical,
    Padding = 1,
    Height = 7,
    BorderStyle = BorderStyle.Single,
    ForegroundColor = Color.Yellow,
    BackgroundColor = Color.Black
};

var normalLabel = new Label("Normal Label")
{
    Height = 1,
    ForegroundColor = Color.White,
    BackgroundColor = Color.Black
};

var boldLabel = new Label("Bold Label")
{
    Height = 1,
    Bold = true,
    ForegroundColor = ColorHelper.FromRgb(100, 255, 100),
    BackgroundColor = Color.Black
};

var italicLabel = new Label("Italic Label")
{
    Height = 1,
    Italic = true,
    ForegroundColor = Color.Cyan,
    BackgroundColor = Color.Black
};

labelsFrame.Add(normalLabel);
labelsFrame.Add(boldLabel);
labelsFrame.Add(italicLabel);

var textViewFrame = new Frame("TextView (Scrollable)")
{
    LayoutMode = LayoutMode.Fill,
    Padding = 1,
    Height = 10,
    BorderStyle = BorderStyle.Rounded,
    ForegroundColor = Color.Magenta,
    BackgroundColor = Color.Black
};

var textView = new TextView
{
    ForegroundColor = Color.White,
    BackgroundColor = Color.Black
};

textView.AppendLine("This is a scrollable text view.");
textView.AppendLine("You can add multiple lines of text.");
textView.AppendLine("Use Arrow keys to scroll up/down.");
textView.AppendLine("Page Up/Down for faster scrolling.");
textView.AppendLine("Home/End to jump to top/bottom.");
textView.AppendLine("Line 6");
textView.AppendLine("Line 7");
textView.AppendLine("Line 8");

textViewFrame.Add(textView);

var statusBar = new StatusBar
{
    LeftText = "Elaris v0.1.0",
    CenterText = "Widget Showcase",
    RightText = "Press ESC to exit"
};

container.Add(headerFrame);
container.Add(labelsFrame);
container.Add(textViewFrame);
container.Add(statusBar);

void PositionWidgets()
{
    if (container.Height > 0)
    {
        statusBar.X = container.X;
        statusBar.Y = container.Y + container.Height - 1;
        statusBar.Width = container.Width;
    }
}

PositionWidgets();

try
{
    app.Run(container);
}
catch (Exception ex)
{
    Console.WriteLine($"\nError: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}
