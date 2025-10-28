using System.Drawing;
using Ambystech.Elaris.UI;
using Ambystech.Elaris.UI.Core;
using Ambystech.Elaris.UI.Enums;
using Ambystech.Elaris.UI.Widgets;
using Ambystech.Elaris.UI.Widgets.Input;
using Ambystech.Elaris.UI.Widgets.Layout;

var app = new Application();

var statusBar = new StatusBar
{
    LeftText = "Elaris Chat v0.1.0",
    CenterText = "Chat Demo",
    RightText = "ESC to exit",
    Height = 1
};

var root = new RootContainer(statusBar);

var chatView = new TextView
{
    AutoScroll = true,
    ForegroundColor = Color.White,
    BackgroundColor = Color.Black
};

var inputField = new TextField("Type a message...")
{
    ForegroundColor = Color.White,
    BackgroundColor = ColorHelper.FromRgb(30, 30, 30),
    PlaceholderColor = Color.Gray,
    Height = 1
};

var mainFrame = new ChatFrame("Elaris Chat Demo", chatView, inputField)
{
    BorderStyle = BorderStyle.Single,
    ForegroundColor = ColorHelper.FromRgb(100, 200, 255),
    BackgroundColor = Color.Black,
    BorderColor = ColorHelper.FromHex("#f92672")
};

chatView.AppendLine("[System] Welcome to Elaris Chat Demo!");
chatView.AppendLine("[System] This demonstrates a chat-like interface.");
chatView.AppendLine("[System] Type a message and press Enter to send.");
chatView.AppendLine("[User] Hello, Elaris!");
chatView.AppendLine("[Bot] Hi there! How can I help you today?");
chatView.AppendLine("[User] What features do you support?");
chatView.AppendLine("[Bot] I support scrolling, text input, and more!");
chatView.AppendLine("");

inputField.KeyPress += (key) =>
{
    if (key.Key == ConsoleKey.Enter)
    {
        chatView.AppendLine($"[User] {inputField.Text}");
        inputField.Text = string.Empty;
        return true; // Mark as handled
    }
    return false; // Let default handler process it
};

root.Add(mainFrame);

app.InitialFocusWidget = inputField;

try
{
    Console.WriteLine("Starting Elaris Chat Demo...");
    Console.WriteLine("Type in the text field and press Enter");
    Console.WriteLine("Press ESC or Ctrl+C to exit");
    Thread.Sleep(1000);

    app.Run(root);
}
catch (Exception ex)
{
    Console.WriteLine($"\nError: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}

class ChatFrame : Frame
{
    private readonly TextView _chatView;
    private readonly TextField _inputField;

    public ChatFrame(string title, TextView chatView, TextField inputField) : base(title)
    {
        _chatView = chatView;
        _inputField = inputField;

        Add(_chatView);
        Add(_inputField);
    }

    protected override void OnBoundsChanged()
    {
        base.OnBoundsChanged();

        if (Width <= 2 || Height <= 2)
            return;

        int contentX = X + 1 + 1;
        int contentY = Y + 1 + 1;
        int contentWidth = Width - 2 - 2;
        int contentHeight = Height - 2 - 2;

        if (contentHeight < 2 || contentWidth < 1)
            return;

        _inputField.X = contentX;
        _inputField.Y = contentY + contentHeight - 1;
        _inputField.Width = contentWidth;
        _inputField.Height = 1;

        _chatView.X = contentX;
        _chatView.Y = contentY;
        _chatView.Width = contentWidth;
        _chatView.Height = contentHeight - 1;
    }
}

class RootContainer : Container
{
    private readonly StatusBar _statusBar;
    private Frame? _mainFrame;

    public RootContainer(StatusBar statusBar)
    {
        _statusBar = statusBar;
        BackgroundColor = Color.Black;
        LayoutMode = LayoutMode.Absolute;

        Add(_statusBar);
    }

    public new void Add(Widget widget)
    {
        base.Add(widget);
        if (widget is Frame frame)
        {
            _mainFrame = frame;
        }
    }

    protected override void OnBoundsChanged()
    {
        base.OnBoundsChanged();

        if (Width <= 0 || Height <= 0)
            return;

        _statusBar.X = 0;
        _statusBar.Y = Height - 1;
        _statusBar.Width = Width;
        _statusBar.Height = 1;

        if (_mainFrame != null)
        {
            _mainFrame.X = 0;
            _mainFrame.Y = 0;
            _mainFrame.Width = Width;
            _mainFrame.Height = Height - 1;
        }
    }
}
