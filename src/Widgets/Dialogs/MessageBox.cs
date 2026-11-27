using System.Text;
using Ambystech.Elaris.UI.Widgets.Display;
using Ambystech.Elaris.UI.Widgets.Input;

namespace Ambystech.Elaris.UI.Widgets.Dialogs;

/// <summary>
/// Which buttons a <see cref="MessageBox"/> shows.
/// </summary>
public enum MessageBoxButtons
{
    /// <summary>OK.</summary>
    OK,

    /// <summary>OK and Cancel.</summary>
    OKCancel,

    /// <summary>Yes and No.</summary>
    YesNo,

    /// <summary>Yes, No and Cancel.</summary>
    YesNoCancel,
}

/// <summary>
/// Shows a short message with buttons and returns which one was chosen.
/// </summary>
/// <example>
/// <code>
/// var answer = await MessageBox.Show(app, "Discard unsaved changes?", "Close", MessageBoxButtons.YesNo);
/// if (answer == DialogResult.Yes) ...
/// </code>
/// </example>
public static class MessageBox
{
    /// <summary>
    /// Shows a message box and completes with the chosen button. Escape gives
    /// <see cref="DialogResult.Cancel"/> when there is a Cancel button, otherwise the last
    /// button's result (OK, or No). Can be called from any thread.
    /// </summary>
    public static Task<DialogResult> Show(Application app, string message, string title = "",
        MessageBoxButtons buttons = MessageBoxButtons.OK)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.ShowDialog(Create(message, title, buttons));
    }

    /// <summary>
    /// Creates the dialog used by <see cref="Show"/>, for showing it yourself or customizing it.
    /// </summary>
    public static Dialog Create(string message, string title = "", MessageBoxButtons buttons = MessageBoxButtons.OK)
        => new MessageBoxDialog(message ?? string.Empty, title ?? string.Empty, buttons);

    /// <summary>
    /// Word-wraps text to lines no longer than <paramref name="width"/>; words longer than the
    /// width are split. Existing line breaks are kept.
    /// </summary>
    internal static List<string> Wrap(string text, int width)
    {
        var lines = new List<string>();
        foreach (var paragraph in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = new StringBuilder();
            foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var rest = word;
                while (rest.Length > 0)
                {
                    int needed = line.Length == 0 ? rest.Length : line.Length + 1 + rest.Length;
                    if (needed <= width)
                    {
                        if (line.Length > 0) line.Append(' ');
                        line.Append(rest);
                        rest = string.Empty;
                    }
                    else if (line.Length > 0)
                    {
                        lines.Add(line.ToString());
                        line.Clear();
                    }
                    else
                    {
                        lines.Add(rest[..width]);
                        rest = rest[width..];
                    }
                }
            }
            lines.Add(line.ToString());
        }
        return lines;
    }

    private sealed class MessageBoxDialog : Dialog
    {
        private const int MaxTextWidth = 56;
        private const int ButtonWidth = 10;
        private const int ButtonGap = 2;

        private readonly Label _message;
        private readonly List<Button> _buttons = [];
        private readonly int _lineCount;
        private readonly DialogResult _escapeResult;

        public MessageBoxDialog(string message, string title, MessageBoxButtons buttons) : base(title)
        {
            var results = buttons switch
            {
                MessageBoxButtons.OKCancel => new[] { DialogResult.OK, DialogResult.Cancel },
                MessageBoxButtons.YesNo => [DialogResult.Yes, DialogResult.No],
                MessageBoxButtons.YesNoCancel => [DialogResult.Yes, DialogResult.No, DialogResult.Cancel],
                _ => [DialogResult.OK],
            };
            _escapeResult = results.Contains(DialogResult.Cancel) ? DialogResult.Cancel : results[^1];

            var lines = Wrap(message, MaxTextWidth);
            _lineCount = lines.Count;
            _message = new Label(string.Join('\n', lines)) { Height = _lineCount };
            Add(_message);

            foreach (var result in results)
            {
                var button = new Button(result.ToString()) { Width = ButtonWidth, Height = 3 };
                button.Click += () => Close(result);
                _buttons.Add(button);
                Add(button);
            }

            InitialFocus = _buttons[0];

            int textWidth = lines.Max(l => l.Length);
            int buttonsWidth = results.Length * ButtonWidth + (results.Length - 1) * ButtonGap;
            Width = Math.Max(Math.Max(textWidth, buttonsWidth), title.Length + 4) + 4;
            // Border, blank line, message, blank line, buttons (3 rows), border.
            Height = _lineCount + 7;
        }

        protected override DialogResult EscapeResult => _escapeResult;

        protected override void OnBoundsChanged()
        {
            base.OnBoundsChanged();

            _message.Bounds = new System.Drawing.Rectangle(X + 2, Y + 2, Math.Max(0, Width - 4), _lineCount);

            int buttonsWidth = _buttons.Count * ButtonWidth + (_buttons.Count - 1) * ButtonGap;
            int x = X + Math.Max(1, (Width - buttonsWidth) / 2);
            int y = Y + Height - 4;
            foreach (var button in _buttons)
            {
                button.Bounds = new System.Drawing.Rectangle(x, y, ButtonWidth, 3);
                x += ButtonWidth + ButtonGap;
            }
        }

        protected internal override bool OnDialogKey(ConsoleKeyInfo key)
        {
            if (key.Key is ConsoleKey.LeftArrow or ConsoleKey.RightArrow && key.Modifiers == 0 && Owner is { } app)
            {
                app.FocusNext(reverse: key.Key == ConsoleKey.LeftArrow);
                return true;
            }

            return base.OnDialogKey(key);
        }
    }
}
