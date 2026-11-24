using System.Drawing;
using Ambystech.Elaris.UI;
using Ambystech.Elaris.UI.Enums;
using Ambystech.Elaris.UI.Widgets.Display;
using Ambystech.Elaris.UI.Widgets.Input;
using Ambystech.Elaris.UI.Widgets.Layout;

// No colors are set on any widget here: everything comes from the theme.
// Alt+Left/Right moves focus; Enter or Space presses a button; Esc quits.

var app = new Application { Theme = Theme.Dark };

var custom = Theme.Dark with
{
    Name = "Custom (Dark + orange)",
    Accent = Color.FromArgb(255, 230, 120, 30),
    Selection = Color.FromArgb(255, 120, 60, 10),
    Success = Color.FromArgb(255, 230, 120, 30),
    Warning = Color.FromArgb(255, 255, 170, 60),
};

var root = new Container { LayoutMode = LayoutMode.Vertical, Padding = 1, Spacing = 1 };

var header = new Frame("Elaris Themes") { LayoutMode = LayoutMode.Vertical, Padding = 1, Height = 4 };
var current = new Label($"Current theme: {app.Theme.Name}") { Height = 1 };
header.Add(current);

var buttons = new Container { LayoutMode = LayoutMode.Horizontal, Spacing = 2, Height = 3 };
void AddThemeButton(string text, Theme theme)
{
    var button = new Button(text) { Width = 20, Height = 3 };
    button.Click += () =>
    {
        app.Theme = theme;
        current.Text = $"Current theme: {theme.Name}";
    };
    buttons.Add(button);
}
AddThemeButton("Dark", Theme.Dark);
AddThemeButton("Light", Theme.Light);
AddThemeButton("High Contrast", Theme.HighContrast);
AddThemeButton("Custom", custom);

var inputs = new Frame("Inputs") { LayoutMode = LayoutMode.Vertical, Padding = 1, Spacing = 1, Height = 9 };
inputs.Add(new TextField("Type here (placeholder uses MutedForeground)") { Height = 1 });
inputs.Add(new Checkbox("Checkbox (brackets: Warning, check: Success)") { Height = 1, IsChecked = true });
inputs.Add(new ProgressBar { Height = 1, Value = 65 });

var list = new Frame("ListBox (selection: Selection)") { LayoutMode = LayoutMode.Fill, Padding = 1, Height = 7 };
var listBox = new ListBox();
foreach (var item in new[] { "Apples", "Bananas", "Cherries", "Dates" })
    listBox.AddItem(item);
list.Add(listBox);

var status = new StatusBar { LeftText = "Alt+←/→ focus · Enter select", RightText = "Esc to exit", Height = 1 };

root.Add(header);
root.Add(buttons);
root.Add(inputs);
root.Add(list);
root.Add(status);

app.Run(root);
