using Ambystech.Elaris.UI;
using Ambystech.Elaris.UI.Enums;
using Ambystech.Elaris.UI.Widgets.Dialogs;
using Ambystech.Elaris.UI.Widgets.Display;
using Ambystech.Elaris.UI.Widgets.Input;
using Ambystech.Elaris.UI.Widgets.Layout;

// Dialogs are modal: focus stays inside (Tab / Shift+Tab / arrows), clicks outside are
// ignored, and Esc closes the dialog instead of quitting. Esc on the main screen quits.

var app = new Application();

var root = new Container { LayoutMode = LayoutMode.Vertical, Padding = 1, Spacing = 1 };

var header = new Frame("Elaris Dialogs") { LayoutMode = LayoutMode.Vertical, Padding = 1, Height = 5 };
var result = new Label("Choose a dialog below.") { Height = 1 };
header.Add(result);

var buttons = new Container { LayoutMode = LayoutMode.Horizontal, Spacing = 2, Height = 3 };

Button AddButton(string text, Func<Task> onClick)
{
    var button = new Button(text) { Width = 18, Height = 3 };
    button.Click += async () => await onClick();
    buttons.Add(button);
    return button;
}

AddButton("Message", async () =>
{
    await MessageBox.Show(app, "Dialogs are drawn above the rest of the screen and keep focus until they close.", "About");
    result.Text = "Message closed.";
});

AddButton("Yes / No", async () =>
{
    var answer = await MessageBox.Show(app, "Do you like terminal user interfaces?", "Question", MessageBoxButtons.YesNoCancel);
    result.Text = $"You answered: {answer}";
});

AddButton("Rename...", async () =>
{
    var dialog = new Dialog("Rename") { Width = 44, Height = 9, LayoutMode = LayoutMode.Vertical, Padding = 1, Spacing = 1 };
    var name = new TextField("New name") { Height = 1 };
    var actions = new Container { LayoutMode = LayoutMode.Horizontal, Spacing = 2, Height = 3 };
    var ok = new Button("OK") { Width = 10, Height = 3 };
    var cancel = new Button("Cancel") { Width = 10, Height = 3 };
    ok.Click += () => dialog.Close(DialogResult.OK);
    cancel.Click += () => dialog.Close(DialogResult.Cancel);
    actions.Add(ok);
    actions.Add(cancel);
    dialog.Add(name);
    dialog.Add(actions);

    // Enter in the text field confirms.
    name.KeyPress += key =>
    {
        if (key.Key != ConsoleKey.Enter)
            return false;
        dialog.Close(DialogResult.OK);
        return true;
    };

    if (await app.ShowDialog(dialog) == DialogResult.OK)
    {
        var answer = await MessageBox.Show(app, $"Rename to \"{name.Text}\"?", "Confirm", MessageBoxButtons.OKCancel);
        result.Text = answer == DialogResult.OK ? $"Renamed to: {name.Text}" : "Rename cancelled.";
    }
    else
    {
        result.Text = "Rename cancelled.";
    }
});

var status = new StatusBar { LeftText = "Click a button or press Enter · Tab moves inside dialogs", RightText = "Esc to exit", Height = 1 };

root.Add(header);
root.Add(buttons);
root.Add(status);

app.Run(root);
