namespace Ambystech.Elaris.UI.Widgets.Layout;

public class Tab
{
    public string Title { get; set; }
    public bool IsEnabled { get; set; }
    public TabContent Content { get; internal set; }

    public Tab(string title)
    {
        Title = title ?? string.Empty;
        IsEnabled = true;
        Content = new TabContent(this);
    }

    internal Tab(string title, TabContent content)
    {
        Title = title ?? string.Empty;
        IsEnabled = true;
        Content = content;
    }
}
