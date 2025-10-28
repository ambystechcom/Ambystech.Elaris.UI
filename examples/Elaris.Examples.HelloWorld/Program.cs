using System.Drawing;
using Ambystech.Elaris.UI;
using Ambystech.Elaris.UI.Core;
using Ambystech.Elaris.UI.Enums;
using Ambystech.Elaris.UI.Widgets.Display;
using Ambystech.Elaris.UI.Widgets.Layout;

var app = new Application();

var mainFrame = new Frame("Hello Elaris!")
{
    BorderStyle = BorderStyle.Rounded,
    ForegroundColor = ColorHelper.FromRgb(100, 200, 255),
};

var label = new Label("Welcome to Elaris CLI UI Library!")
{
    X = 2,
    Y = 2,
    Width = 50,
    Height = 1,
    ForegroundColor = ColorHelper.FromRgb(255, 200, 100),
    Bold = true
};

var infoLabel = new Label("Press ESC or Ctrl+C to exit")
{
    X = 2,
    Y = 4,
    Width = 50,
    Height = 1,
    ForegroundColor = Color.Gray
};

mainFrame.Add(label);
mainFrame.Add(infoLabel);

try
{
    app.Run(mainFrame);
}
catch (Exception ex)
{
    Console.WriteLine($"\nError: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}
