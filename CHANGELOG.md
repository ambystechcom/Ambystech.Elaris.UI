# Changelog

All notable changes to Ambystech.Elaris.UI and Ambystech.Elaris.UI.CodeEditor.Plugins are
documented here. The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and the project follows [Semantic Versioning](https://semver.org/) (while on 0.x, minor versions
may contain breaking changes).

## [0.2.0] - 2026-11-27

A large update: themes, mouse support, dialogs, color fallback for terminals without 24-bit
color, much smaller terminal output, near-zero CPU use while idle, and a terminal abstraction
that makes applications testable without a console. Test count went from 136 to 474.

**Before upgrading, read [Behavior changes](#behavior-changes) and [Upgrade notes](#upgrade-notes).**

### Added

- **Themes** (#18): `Theme` with 16 color roles and built-in `Theme.Dark` (default), `Theme.Light`
  and `Theme.HighContrast`; derive custom themes with `with { … }`. Set `Application.Theme` for
  the whole app (also while running) or `Widget.Theme` for a widget and its children. Every widget
  color property falls back to the theme unless set explicitly. New `ThemesDemo` example.
- **Mouse support** on macOS and Linux (#19): click to focus; clicks for Button, Checkbox,
  TextField, Editor, ListBox, TreeView, Table, TabContainer, MenuBar, AutoComplete suggestions and
  Panel headers; double click activates list items, tree nodes and table rows; the wheel scrolls
  lists, trees, tables, text views and the editor. `Application.MouseEnabled`,
  `Application.DoubleClickTime`, `Widget.OnMouse`, `Widget.HitTest`. Custom widgets can override
  `OnMouse` (or the existing `OnMouseClick`, which is now called).
- **Dialogs** (#21): `Dialog` and `Application.ShowDialog` (returns `Task<DialogResult>`), and
  `MessageBox.Show` with OK, OK/Cancel, Yes/No and Yes/No/Cancel. Dialogs are modal: focus stays
  inside, clicks outside are ignored, Esc closes them, and focus returns when they close.
  New `DialogsDemo` example.
- **Color fallback** (#20): `ColorMode` (`TrueColor`, `Palette256`, `Palette16`, `NoColor`),
  detected from `NO_COLOR`, `COLORTERM`, `TERM`, `TERM_PROGRAM` and `WT_SESSION`; colors are
  converted to the nearest palette entry, keeping text readable. Override with
  `Application.ColorMode`. `ColorConversion` exposes the conversions and detection.
- **Terminal abstraction** (#15): `ITerminal`, `ConsoleTerminal` and `VirtualTerminal`.
  `VirtualTerminal` decodes everything Elaris draws into a cell grid and accepts queued keys and
  mouse events, so applications can be tested without a console. `new Application(terminal)`,
  `new Screen(width, height, terminal)`.
- **UI-thread dispatcher** (#16): `Application.Post`, `InvokeAsync`, `CheckAccess`; a
  `SynchronizationContext` on the UI thread so `await` resumes there; `Application.UnhandledException`
  for exceptions from key handlers and posted actions.
- **Clipping** (#17): containers clip their children (`Container.ClipChildren`); a `Frame` clips to
  the area inside its border. `Screen.PushClip`, `PushOverlay`, `PopClip`, `ClipRectangle`.
- `Widget.Invalidate()` to request a redraw; `Application.AlwaysRender`.
- `Application.QuitKey`, `QuitOnCtrlC`, `FocusNext`, `FocusedWidget`, `MaxKeysPerFrame` (#13).
- `ListBox.ItemActivated` event, raised by Enter and double click (#19).
- `Screen.GetCell`, `Screen.ColorMode`, `Screen.BaseBackground`.
- `InputHandler.TryReadKey`.

### Behavior changes

These can change how existing applications look or behave:

- **Esc quits only if the focused widget doesn't use it** (#13). It now closes an open menu,
  suggestion popup or dialog first. Set `QuitKey = null` to disable quitting with Esc.
- **The screen is redrawn only when something may have changed** (#16): after input, posted
  actions, a resize, or `Widget.Invalidate()`. Code that changes widgets directly from background
  threads should use `Application.Post` / `InvokeAsync`, or set `Application.AlwaysRender = true`
  to redraw every frame as before.
- **Containers clip their children** (#17). Content positioned outside its container is no longer
  drawn. Containers without a size don't clip; set `ClipChildren = false` to turn it off.
- **Transparent backgrounds keep what is underneath** (#18) instead of resetting to the
  terminal's default background, so text on a colored panel no longer cuts holes in it.
- **Default colors are unified by role** (#18). With the default theme: the MenuBar and menu
  dropdown selection and the selected table row use the same selection blue as lists; table
  headers and inactive tabs use `70,70,70`; inactive tab text is white; button borders and grid
  lines use gray `128`; pressed buttons and suggestion popups use `40,40,40`.
- **Colors adapt to the terminal** (#20). Terminals that don't advertise 24-bit color (for
  example macOS Terminal) now get 256-color output; `NO_COLOR` turns colors off.
- **Mouse reporting is on by default** on macOS and Linux (#19). While it's on, most terminals
  select text only with Shift held down. Set `Application.MouseEnabled = false` to turn it off.
- **Focus order contains only focusable widgets** (#13). Non-focusable children of the root
  (such as labels) are no longer tab stops; widgets added after `Run` can receive focus.
- `Panel` is focusable (#13).
- **Children are ordered by `ZIndex` when added** (#19). Previously a widget created with a
  `ZIndex` and then added was drawn in insertion order.
- Enter in a `ListBox` raises `ItemActivated` and is marked as handled (#19).

### Deprecated

- `ListBox.Clear()`: use `ClearItems()` (#14). `Clear()` hid `Widget.Clear()`, which removes
  child widgets.
- `CodeEditor.Theme` (string): use `SyntaxThemeName` (#18). `Theme` now means the UI color theme.

### Fixed

- Typing and pasting lagged: only one key was handled per frame (#13).
- The terminal was left in the alternate screen with a hidden cursor after Ctrl+C, SIGTERM,
  SIGHUP, SIGQUIT or an unhandled exception (#13).
- `Widget.HasFocus` always returned `false` for Button, Checkbox, TextField, ListBox, Editor and
  TreeView (#13).
- The terminal cursor flashed at the last drawn cell after every frame (#15).
- Control characters in widget text (tab, newline, escape) could move the cursor or start an
  escape sequence (#15).
- Equal colors created differently (`Color.White` vs `Color.FromArgb(255, 255, 255)`) caused
  needless redraws (#15).
- `AutoCompleteTextField` changed its widgets from a timer thread, and a slow earlier query could
  replace newer suggestions (#16).
- Frame timing measured the previous frame instead of the current one (#13).
- The Widgets example drew its labels and text view over the header instead of inside their
  frames (#17).
- A focused, empty `TextField` hid the first character of its placeholder (#21).
- The README quickstart didn't compile (#13).

### Performance

- Terminal output for a typical first screen dropped from about 204 KB to 9.8 KB, and to zero
  while idle: unchanged cells, repeated styles and redundant cursor moves are no longer written (#15).
- Idle CPU use dropped about 7× because frames are drawn only when needed (#16).

### Removed

- The `System.Drawing.Primitives` package reference (core) and the `System.Text.Json` 8.0.0
  reference (plugins), which had a known high-severity vulnerability (#13). Both ship with .NET 8+;
  the core package now has no dependencies.

### Upgrade notes

- Replace `listBox.Clear()` with `listBox.ClearItems()`, and `codeEditor.Theme = "…"` with
  `codeEditor.SyntaxThemeName = "…"`. The old names still compile, with a warning.
- If your app updates widgets from timers or background tasks, wrap the changes in
  `app.Post(() => …)` (or set `app.AlwaysRender = true`).
- If parts of your layout disappear, they were positioned outside their container. Let the
  container lay them out (`Frame` is a `Container`: set `LayoutMode` and `Padding`), or set
  `ClipChildren = false`.
- If you relied on Esc always quitting, it still does unless a focused widget or dialog handles it.
- Widget positions are absolute screen coordinates; see the README's "Layout and Clipping".

### Known limitations

- Mouse input is not available on Windows yet: .NET does not deliver mouse events to console
  applications there.

## [0.1.7] and earlier

See the [GitHub releases](https://github.com/ambystechcom/Ambystech.Elaris.UI/releases).

[0.2.0]: https://github.com/ambystechcom/Ambystech.Elaris.UI/compare/v0.1.7...v0.2.0
[0.1.7]: https://github.com/ambystechcom/Ambystech.Elaris.UI/releases/tag/v0.1.7
