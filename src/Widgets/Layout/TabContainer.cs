using System.Drawing;
using Ambystech.Elaris.UI.Core;
using Ambystech.Elaris.UI.Rendering;

namespace Ambystech.Elaris.UI.Widgets.Layout;

public class TabContainer : Container
{
    private readonly List<Tab> _tabs = [];
    private int _activeTabIndex = -1;
    private int _tabBarHeight = 1;

    public IReadOnlyList<Tab> Tabs => _tabs.AsReadOnly();

    public int ActiveTabIndex
    {
        get => _activeTabIndex;
        set
        {
            if (value < 0 || value >= _tabs.Count || value == _activeTabIndex)
                return;

            var previousTab = _activeTabIndex >= 0 && _activeTabIndex < _tabs.Count ? _tabs[_activeTabIndex] : null;
            var previousIndex = _activeTabIndex;

            if (previousTab != null)
                previousTab.Content.Visible = false;

            _activeTabIndex = value;
            var activeTab = _tabs[_activeTabIndex];
            activeTab.Content.Visible = true;

            TabChanged?.Invoke(activeTab, previousTab, _activeTabIndex, previousIndex);
        }
    }

    public Tab? ActiveTab
    {
        get => _activeTabIndex >= 0 && _activeTabIndex < _tabs.Count ? _tabs[_activeTabIndex] : null;
        set
        {
            if (value == null)
                return;

            var index = _tabs.IndexOf(value);
            if (index >= 0)
                ActiveTabIndex = index;
        }
    }

    public int TabBarHeight
    {
        get => _tabBarHeight;
        set => _tabBarHeight = Math.Max(1, value);
    }

    public Color ActiveTabBackgroundColor
    {
        get => _activeTabBackgroundColor ?? EffectiveTheme.Accent;
        set => SetColor(ref _activeTabBackgroundColor, value);
    }

    private Color? _activeTabBackgroundColor;
    public Color ActiveTabForegroundColor
    {
        get => _activeTabForegroundColor ?? EffectiveTheme.AccentForeground;
        set => SetColor(ref _activeTabForegroundColor, value);
    }

    private Color? _activeTabForegroundColor;
    public Color InactiveTabBackgroundColor
    {
        get => _inactiveTabBackgroundColor ?? EffectiveTheme.Surface;
        set => SetColor(ref _inactiveTabBackgroundColor, value);
    }

    private Color? _inactiveTabBackgroundColor;
    public Color InactiveTabForegroundColor
    {
        get => _inactiveTabForegroundColor ?? EffectiveTheme.SurfaceForeground;
        set => SetColor(ref _inactiveTabForegroundColor, value);
    }

    private Color? _inactiveTabForegroundColor;
    public Color TabBarBackgroundColor
    {
        get => _tabBarBackgroundColor ?? EffectiveTheme.SurfaceAlt;
        set => SetColor(ref _tabBarBackgroundColor, value);
    }

    private Color? _tabBarBackgroundColor;

    public event Action<Tab, Tab?, int, int>? TabChanged;

    public override bool IsFocusable => true;

    public TabContent AddTab(string title)
    {
        var tab = new Tab(title);
        _tabs.Add(tab);

        Add(tab.Content);
        LayoutTabContent(tab.Content);

        if (_activeTabIndex == -1)
        {
            _activeTabIndex = 0;
            tab.Content.Visible = true;
        }

        return tab.Content;
    }

    public void RemoveTab(Tab tab)
    {
        var index = _tabs.IndexOf(tab);
        if (index < 0)
            return;

        Remove(tab.Content);
        _tabs.RemoveAt(index);

        if (_activeTabIndex == index)
        {
            _activeTabIndex = -1;
            if (_tabs.Count > 0)
            {
                ActiveTabIndex = Math.Min(index, _tabs.Count - 1);
            }
        }
        else if (_activeTabIndex > index)
        {
            _activeTabIndex--;
        }
    }

    public void RemoveTabAt(int index)
    {
        if (index < 0 || index >= _tabs.Count)
            return;

        RemoveTab(_tabs[index]);
    }

    protected override void OnRender(Screen screen)
    {
        RenderTabBar(screen);
    }

    private void RenderTabBar(Screen screen)
    {
        if (Width <= 0 || _tabBarHeight <= 0)
            return;

        screen.FillRectangle(new Rectangle(X, Y, Width, _tabBarHeight), ' ', TabBarBackgroundColor, TabBarBackgroundColor);

        int currentX = X + 1;
        for (int i = 0; i < _tabs.Count; i++)
        {
            var tab = _tabs[i];
            if (!tab.IsEnabled)
                continue;

            bool isActive = i == _activeTabIndex;
            var bgColor = isActive ? ActiveTabBackgroundColor : InactiveTabBackgroundColor;
            var fgColor = isActive ? ActiveTabForegroundColor : InactiveTabForegroundColor;

            string displayText = $" {tab.Title} ";
            int tabWidth = displayText.Length + 2;

            if (currentX + tabWidth > X + Width)
                break;

            screen.WriteText(currentX, Y, $"[{displayText}]", fgColor, bgColor, bold: isActive);
            currentX += tabWidth + 1;
        }
    }

    protected override void OnBoundsChanged()
    {
        base.OnBoundsChanged();

        foreach (var tab in _tabs)
        {
            LayoutTabContent(tab.Content);
        }
    }

    private void LayoutTabContent(TabContent content)
    {
        content.X = X;
        content.Y = Y + _tabBarHeight;
        content.Width = Width;
        content.Height = Math.Max(0, Height - _tabBarHeight);
    }

    protected internal override bool OnKeyPress(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.LeftArrow:
                if (_activeTabIndex > 0)
                    ActiveTabIndex--;
                return true;

            case ConsoleKey.RightArrow:
                if (_activeTabIndex < _tabs.Count - 1)
                    ActiveTabIndex++;
                return true;

            case ConsoleKey.Home:
                if (_tabs.Count > 0)
                    ActiveTabIndex = 0;
                return true;

            case ConsoleKey.End:
                if (_tabs.Count > 0)
                    ActiveTabIndex = _tabs.Count - 1;
                return true;

            case ConsoleKey.Tab when !key.Modifiers.HasFlag(ConsoleModifiers.Shift):
                if (_activeTabIndex < _tabs.Count - 1)
                {
                    ActiveTabIndex++;
                    return true;
                }
                break;

            case ConsoleKey.Tab when key.Modifiers.HasFlag(ConsoleModifiers.Shift):
                if (_activeTabIndex > 0)
                {
                    ActiveTabIndex--;
                    return true;
                }
                break;
        }

        return base.OnKeyPress(key);
    }

    protected internal override void OnFocus()
    {
        base.OnFocus();

        if (_activeTabIndex < 0 && _tabs.Count > 0)
            ActiveTabIndex = 0;
    }
}
