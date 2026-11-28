using System.Drawing;
using Ambystech.Elaris.UI.Widgets;
using Ambystech.Elaris.UI.Widgets.Dialogs;

namespace Ambystech.Elaris.UI;

public partial class Application
{
    private readonly List<Dialog> _dialogs = [];

    /// <summary>
    /// Gets the dialogs currently shown, from bottom to top. The last one has focus.
    /// </summary>
    public IReadOnlyList<Dialog> OpenDialogs => _dialogs;

    /// <summary>
    /// The widget that keyboard focus and mouse input are limited to: the top dialog, or the root.
    /// </summary>
    private Widget? FocusRoot => _dialogs.Count > 0 ? _dialogs[^1] : _rootWidget;

    /// <summary>
    /// Shows a modal dialog above the application and completes with its result when it closes
    /// (see <see cref="Dialog"/>). Can be called from any thread, and before <see cref="Run"/>
    /// (the dialog then opens when the application starts). If the application stops while the
    /// dialog is open, the task is cancelled.
    /// </summary>
    public Task<DialogResult> ShowDialog(Dialog dialog)
    {
        ArgumentNullException.ThrowIfNull(dialog);

        if (MustMarshal || _rootWidget == null)
            return InvokeAsync(() => ShowDialog(dialog)).Unwrap();

        if (dialog.IsOpen)
            throw new InvalidOperationException("The dialog is already open.");

        if (dialog.Parent != null)
            throw new InvalidOperationException("A dialog must not be added to the widget tree; show it with ShowDialog.");

        var completion = new TaskCompletionSource<DialogResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        dialog.Owner = this;
        dialog.Completion = completion;
        dialog.PreviousFocus = _focusedWidget;
        dialog.LogicalParent = _rootWidget;
        dialog.RenderRequested = RequestRender;
        _dialogs.Add(dialog);

        if (dialog.CenterOnScreen)
            CenterDialog(dialog);

        var focusable = GetFocusableWidgets();
        Widget initial = dialog.InitialFocus is { } preferred && focusable.Contains(preferred)
            ? preferred
            : focusable.Count > 0 ? focusable[0] : dialog;
        SetFocus(initial);

        RequestRender();
        return completion.Task;
    }

    /// <summary>
    /// Closes a dialog: removes it, returns focus to where it was before the dialog opened, and
    /// completes its task.
    /// </summary>
    internal void CloseDialog(Dialog dialog, DialogResult result)
    {
        if (MustMarshal)
        {
            Post(() => CloseDialog(dialog, result));
            return;
        }

        if (dialog.Owner != this || !_dialogs.Remove(dialog))
            return;

        Detach(dialog);

        if (_mouseCapture != null && IsInside(_mouseCapture, dialog))
            _mouseCapture = null;

        if (_focusedWidget == null || IsInside(_focusedWidget, dialog))
        {
            var previous = dialog.PreviousFocus;
            if (previous != null && IsReachable(previous))
            {
                SetFocus(previous);
            }
            else
            {
                var focusable = GetFocusableWidgets();
                var fallback = focusable.Count > 0 ? focusable[0] : FocusRoot;
                if (fallback != null)
                    SetFocus(fallback);
            }
        }

        dialog.PreviousFocus = null;
        RequestRender();
        dialog.Complete(result);
    }

    /// <summary>
    /// True when the application is running on another thread, so UI work must be queued to it.
    /// </summary>
    private bool MustMarshal => _uiThreadId != -1 && !CheckAccess();

    private static void Detach(Dialog dialog)
    {
        dialog.Owner = null;
        dialog.RenderRequested = null;
        dialog.LogicalParent = null;
    }

    private static bool IsInside(Widget widget, Widget container)
    {
        for (var current = widget; current != null; current = current.Parent)
        {
            if (current == container)
                return true;
        }
        return false;
    }

    private void CenterDialog(Dialog dialog)
    {
        if (_rootWidget == null)
            return;

        var area = _rootWidget.Bounds;
        int width = Math.Min(dialog.Width, area.Width);
        int height = Math.Min(dialog.Height, area.Height);
        dialog.Bounds = new Rectangle(
            area.X + Math.Max(0, (area.Width - width) / 2),
            area.Y + Math.Max(0, (area.Height - height) / 2),
            width,
            height);
    }

    private void RecenterDialogs()
    {
        foreach (var dialog in _dialogs)
        {
            if (dialog.CenterOnScreen)
                CenterDialog(dialog);
        }
    }

    private void RenderDialogs()
    {
        if (_screen == null)
            return;

        foreach (var dialog in _dialogs)
        {
            using (_screen.PushOverlay())
            {
                dialog.Render(_screen);
            }
        }
    }

    /// <summary>
    /// Cancels dialogs still open when the application stops.
    /// </summary>
    private void CancelOpenDialogs()
    {
        var open = _dialogs.ToList();
        _dialogs.Clear();
        foreach (var dialog in open)
        {
            Detach(dialog);
            dialog.PreviousFocus = null;
            dialog.Cancel();
        }
    }

    /// <summary>
    /// Offers a key the focused widget didn't handle to the top dialog.
    /// </summary>
    private bool HandleDialogKey(ConsoleKeyInfo key)
        => _dialogs.Count > 0 && _dialogs[^1].OnDialogKey(key);
}
