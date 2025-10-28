using System.Drawing;
using Ambystech.Elaris.UI;
using Ambystech.Elaris.UI.Core;
using Ambystech.Elaris.UI.Enums;
using Ambystech.Elaris.UI.Widgets.Display;
using Ambystech.Elaris.UI.Widgets.Input;
using Ambystech.Elaris.UI.Widgets.Layout;
using Ambystech.Elaris.UI.Widgets.Layout.Responsive;

var app = new Application();

var mainFrame = new Frame("Interactive Widgets Demo")
{
    BorderStyle = BorderStyle.Single,
    ForegroundColor = ColorHelper.FromRgb(100, 200, 255),
};

var statusBar = new StatusBar
{
    Height = 1,
    LeftText = "Ready",
    CenterText = "Interactive Widgets Demo",
    RightText = "ESC to exit"
};

var button1 = new Button("Button 1")
{
    Width = 15,
    Height = 3,
    ForegroundColor = Color.White,
    BackgroundColor = ColorHelper.FromRgb(66, 135, 245),
    BorderColor = ColorHelper.FromRgb(66, 135, 245),
    PressedBackgroundColor = ColorHelper.FromRgb(40, 90, 180),
    PressedForegroundColor = Color.White,
    FocusedBackgroundColor = ColorHelper.FromRgb(80, 150, 255),
    Style = ButtonStyle.Rounded
};

var button2 = new Button("Button 2")
{
    Width = 15,
    Height = 3,
    ForegroundColor = ColorHelper.FromRgb(66, 135, 245),
    BorderColor = ColorHelper.FromRgb(66, 135, 245),
    PressedForegroundColor = ColorHelper.FromRgb(40, 90, 180),
    FocusedForegroundColor = ColorHelper.FromRgb(80, 150, 255),
    Style = ButtonStyle.Outline
};

var button3 = new Button("Button 3")
{
    Width = 15,
    Height = 3,
    ForegroundColor = Color.White,
    BorderColor = ColorHelper.FromRgb(91, 192, 235),
    PressedForegroundColor = Color.White,
    FocusedForegroundColor = Color.White,
    Style = ButtonStyle.Gradient,
    GradientStartColor = ColorHelper.FromRgb(91, 192, 235),
    GradientEndColor = ColorHelper.FromRgb(76, 162, 213),
    PressedGradientStartColor = ColorHelper.FromRgb(60, 130, 170),
    PressedGradientEndColor = ColorHelper.FromRgb(50, 110, 150)
};

int clickCount = 0;
button1.Click += () =>
{
    clickCount++;
    statusBar.CenterText = $"Button 1 clicked {clickCount} times!";
};

button2.Click += () =>
{
    statusBar.CenterText = "Button 2 clicked!";
};

button3.Click += () =>
{
    statusBar.CenterText = "Button 3 clicked!";
};

var checkbox1 = new Checkbox("Enable feature A")
{
    Width = 30,
    Height = 1,
    ForegroundColor = Color.White,
};

var checkbox2 = new Checkbox("Enable feature B")
{
    Width = 30,
    Height = 1,
    ForegroundColor = Color.White,
};

var checkbox3 = new Checkbox("Enable feature C")
{
    Width = 30,
    Height = 1,
    ForegroundColor = Color.White,
    IsChecked = true
};

checkbox1.CheckedChanged += (isChecked) =>
{
    statusBar.LeftText = $"Feature A: {(isChecked ? "ON" : "OFF")}";
};

checkbox2.CheckedChanged += (isChecked) =>
{
    statusBar.LeftText = $"Feature B: {(isChecked ? "ON" : "OFF")}";
};

checkbox3.CheckedChanged += (isChecked) =>
{
    statusBar.LeftText = $"Feature C: {(isChecked ? "ON" : "OFF")}";
};

var listBox = new ListBox
{
    Width = 30,
    ForegroundColor = Color.White,
    BackgroundColor = ColorHelper.FromRgb(30, 30, 30)
};

listBox.AddItem("Apple");
listBox.AddItem("Banana");
listBox.AddItem("Cherry");
listBox.AddItem("Date");
listBox.AddItem("Elderberry");
listBox.AddItem("Fig");
listBox.AddItem("Grape");
listBox.AddItem("Honeydew");
listBox.AddItem("Kiwi");
listBox.AddItem("Lemon");
listBox.AddItem("Mango");
listBox.AddItem("Orange");
listBox.AddItem("Papaya");
listBox.AddItem("Quince");
listBox.AddItem("Raspberry");

listBox.SelectionChanged += (index) =>
{
    if (listBox.SelectedItem != null)
    {
        statusBar.RightText = $"Selected: {listBox.SelectedItem}";
    }
};

var progressBar1 = new ProgressBar
{
    Width = 30,
    Height = 1,
    ForegroundColor = Color.White,
    BackgroundColor = Color.Black,
    Value = 35,
    ShowPercentage = true
};

var progressBar2 = new ProgressBar
{
    Width = 30,
    Height = 3,
    ForegroundColor = Color.White,
    BackgroundColor = Color.Black,
    Value = 75,
    ShowPercentage = true,
    FilledColor = ColorHelper.FromRgb(50, 200, 50),
    PercentageVerticalAlignment = VerticalAlignment.Middle,
    PercentageHorizontalAlignment = HorizontalAlignment.Center
};

var infoLabel = new Label("Use Tab to navigate between widgets")
{
    Height = 1,
    ForegroundColor = Color.Yellow
};

var keysLabel = new Label("Space/Enter: Activate | Arrow keys: Navigate list")
{
    Height = 1,
    ForegroundColor = Color.Gray
};

mainFrame.Add(button1);
mainFrame.Add(button2);
mainFrame.Add(button3);
mainFrame.Add(checkbox1);
mainFrame.Add(checkbox2);
mainFrame.Add(checkbox3);
mainFrame.Add(listBox);
mainFrame.Add(progressBar1);
mainFrame.Add(progressBar2);
mainFrame.Add(infoLabel);
mainFrame.Add(keysLabel);

var root = new ResponsiveContainer
{
    BackgroundColor = Color.Black
};

root.Add(mainFrame);
root.Add(statusBar);

root.OnResize((width, height) =>
{
    statusBar.X = 0;
    statusBar.Y = height - 1;
    statusBar.Width = width;

    int margin = 1;
    mainFrame.X = margin;
    mainFrame.Y = margin;
    mainFrame.Width = width - (margin * 2);
    mainFrame.Height = height - margin - 2; 

    int innerWidth = mainFrame.Width - 4; 
    int innerHeight = mainFrame.Height - 3; 

    int leftColumnX = 2;
    int rightColumnX = Math.Max(40, innerWidth - 32);
    int currentY = 2;

    button1.X = leftColumnX;
    button1.Y = currentY;

    button2.X = leftColumnX + 17;
    button2.Y = currentY;

    button3.X = leftColumnX + 34;
    button3.Y = currentY;

    currentY += 4;

    checkbox1.X = leftColumnX;
    checkbox1.Y = currentY;

    checkbox2.X = leftColumnX;
    checkbox2.Y = currentY + 1;

    checkbox3.X = leftColumnX;
    checkbox3.Y = currentY + 2;

    currentY += 4;

    progressBar1.X = leftColumnX;
    progressBar1.Y = currentY;

    progressBar2.X = leftColumnX;
    progressBar2.Y = currentY + 2;

    currentY += 5;

    infoLabel.X = leftColumnX;
    infoLabel.Y = currentY;
    infoLabel.Width = innerWidth - 4;

    keysLabel.X = leftColumnX;
    keysLabel.Y = currentY + 1;
    keysLabel.Width = innerWidth - 4;

    listBox.X = rightColumnX;
    listBox.Y = 2;
    listBox.Height = Math.Min(15, innerHeight - 4);
    listBox.Width = Math.Min(30, innerWidth - rightColumnX + leftColumnX);
});

app.InitialFocusWidget = button1;

try
{
    Console.WriteLine("Starting Elaris Interactive Widgets Demo...");
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
