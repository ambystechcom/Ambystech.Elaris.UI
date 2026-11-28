using System.Drawing;
using Ambystech.Elaris.UI.Enums;
using Ambystech.Elaris.UI.Rendering;
using Ambystech.Elaris.UI.Widgets.Layout;

namespace Ambystech.Elaris.UI.Widgets.Dialogs;

/// <summary>
/// How a dialog was closed.
/// </summary>
public enum DialogResult
{
    /// <summary>Closed without a specific result.</summary>
    None,

    /// <summary>OK was chosen.</summary>
    OK,

    /// <summary>Cancel was chosen, or the dialog was dismissed with Escape.</summary>
    Cancel,

    /// <summary>Yes was chosen.</summary>
    Yes,

    /// <summary>No was chosen.</summary>
    No,
}

/// <summary>
/// A modal window shown above the rest of the application with <see cref="Application.ShowDialog"/>.
/// </summary>
/// <remarks>
/// <para>
/// While a dialog is open, keyboard focus stays inside it (Tab, Shift+Tab and Alt+←/→ cycle
/// through its focusable widgets), clicks outside it are ignored, and Escape closes it with
/// <see cref="DialogResult.Cancel"/> instead of quitting the application. When it closes, focus
/// returns to the widget that had it before.
/// </para>
/// <para>
/// A dialog is a <see cref="Frame"/>: add widgets to it and lay them out as in any container.
/// Set <see cref="Widget.Width"/> and <see cref="Widget.Height"/>; it is centered on the screen
/// unless <see cref="CenterOnScreen"/> is false. Do not add a dialog to the widget tree.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var dialog = new Dialog("Rename") { Width = 40, Height = 8, LayoutMode = LayoutMode.Vertical, Padding = 1 };
/// var name = new TextField("New name") { Height = 1 };
/// var ok = new Button("OK") { Height = 3 };
/// ok.Click += () => dialog.Close(DialogResult.OK);
/// dialog.Add(name);
/// dialog.Add(ok);
///
/// if (await app.ShowDialog(dialog) == DialogResult.OK)
///     Rename(name.Text);
/// </code>
/// </example>
public class Dialog : Frame
{
    /// <summary>Creates a dialog without a title.</summary>
    public Dialog() : this(string.Empty)
    {
    }

    /// <summary>Creates a dialog with a title.</summary>
    public Dialog(string title) : base(title)
    {
        BorderStyle = BorderStyle.Rounded;
    }

    /// <summary>
    /// Gets or sets whether Escape (when the focused widget doesn't use it) closes the dialog with
    /// <see cref="DialogResult.Cancel"/>. Defaults to <c>true</c>.
    /// </summary>
    public bool CloseOnEscape { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the dialog is centered on the screen when shown and when the
    /// terminal is resized. Defaults to <c>true</c>.
    /// </summary>
    public bool CenterOnScreen { get; set; } = true;

    /// <summary>
    /// Gets or sets the widget focused when the dialog opens. Defaults to the first focusable widget.
    /// </summary>
    public Widget? InitialFocus { get; set; }

    /// <summary>Gets whether the dialog is currently shown.</summary>
    public bool IsOpen => Owner != null;

    /// <summary>Gets the result of the last time the dialog was closed, or null.</summary>
    public DialogResult? Result { get; private set; }

    /// <summary>Raised after the dialog closes, with its result.</summary>
    public event Action<DialogResult>? Closed;

    internal Application? Owner { get; set; }

    internal Widget? PreviousFocus { get; set; }

    internal TaskCompletionSource<DialogResult>? Completion { get; set; }

    /// <summary>
    /// Closes the dialog. Does nothing if it is not open. Can be called from any thread.
    /// </summary>
    public void Close(DialogResult result = DialogResult.None)
        => Owner?.CloseDialog(this, result);

    /// <summary>
    /// Called with key presses that the focused widget did not handle. The default moves focus
    /// with Tab / Shift+Tab and closes the dialog on Escape (see <see cref="CloseOnEscape"/>).
    /// </summary>
    /// <returns><c>true</c> if the key was handled.</returns>
    protected internal virtual bool OnDialogKey(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.Tab && Owner is { } app)
        {
            app.FocusNext(reverse: key.Modifiers.HasFlag(ConsoleModifiers.Shift));
            return true;
        }

        if (key.Key == ConsoleKey.Escape && key.Modifiers == 0 && CloseOnEscape)
        {
            Close(EscapeResult);
            return true;
        }

        return false;
    }

    /// <summary>
    /// The result used when the dialog is closed with Escape. Defaults to <see cref="DialogResult.Cancel"/>.
    /// </summary>
    protected virtual DialogResult EscapeResult => DialogResult.Cancel;

    /// <summary>Dialogs are opaque: the theme's <see cref="Theme.SurfaceAlt"/>.</summary>
    protected override Color DefaultBackgroundColor => EffectiveTheme.SurfaceAlt;

    /// <inheritdoc />
    protected override void OnRender(Screen screen)
    {
        screen.FillRectangle(Bounds, ' ', ForegroundColor, BackgroundColor);
        base.OnRender(screen);
    }

    internal void Complete(DialogResult result)
    {
        Result = result;
        var completion = Completion;
        Completion = null;
        Closed?.Invoke(result);
        completion?.TrySetResult(result);
    }

    internal void Cancel()
    {
        var completion = Completion;
        Completion = null;
        completion?.TrySetCanceled();
    }
}
