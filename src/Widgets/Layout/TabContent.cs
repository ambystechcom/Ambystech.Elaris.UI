namespace Ambystech.Elaris.UI.Widgets.Layout;

public class TabContent : Container
{
    public Tab ParentTab { get; }

    internal TabContent(Tab parentTab)
    {
        ParentTab = parentTab;
        Visible = false;
    }
}
