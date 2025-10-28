using System.Drawing;
using Ambystech.Elaris.UI.Core;
using Ambystech.Elaris.UI.Rendering;

namespace Ambystech.Elaris.UI.Widgets.Display;

/// <summary>
/// A tree view widget for displaying hierarchical data with keyboard navigation and scrolling.
/// </summary>
public class TreeView : Widget
{
    private List<TreeNode> _rootNodes = [];
    private TreeNode? _selectedNode;
    private int _scrollOffset = 0;

    /// <summary>
    /// Gets or sets the root nodes of the tree.
    /// </summary>
    public List<TreeNode> RootNodes
    {
        get => _rootNodes;
        set
        {
            _rootNodes = value ?? [];
            if (_selectedNode != null && !IsNodeInTree(_selectedNode))
            {
                _selectedNode = null;
            }
        }
    }

    /// <summary>
    /// Gets or sets the currently selected node.
    /// </summary>
    public TreeNode? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (_selectedNode != value)
            {
                var oldNode = _selectedNode;
                _selectedNode = value;

                if (_selectedNode != null && Height > 0)
                {
                    var visibleNodes = GetVisibleNodes();
                    int selectedIndex = visibleNodes.FindIndex(v => v.node == _selectedNode);

                    if (selectedIndex >= 0)
                    {
                        if (selectedIndex < _scrollOffset)
                        {
                            _scrollOffset = selectedIndex;
                        }
                        else if (selectedIndex >= _scrollOffset + Height)
                        {
                            _scrollOffset = selectedIndex - Height + 1;
                        }
                    }
                }

                if (oldNode != _selectedNode)
                {
                    SelectionChanged?.Invoke(_selectedNode);
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets whether to show node icons.
    /// </summary>
    public bool ShowIcons { get; set; } = true;

    /// <summary>
    /// Gets or sets the indentation size per level.
    /// </summary>
    public int IndentSize { get; set; } = 2;

    /// <summary>
    /// Gets or sets the background color for the selected node.
    /// </summary>
    public Color SelectionBackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the foreground color for the selected node.
    /// </summary>
    public Color SelectionForegroundColor { get; set; }

    /// <summary>
    /// Gets or sets the color for tree connectors.
    /// </summary>
    public Color ConnectorColor { get; set; } = Color.Gray;

    /// <summary>
    /// Gets or sets the expand/collapse indicator character for expanded nodes.
    /// </summary>
    public string ExpandedIndicator { get; set; } = "▼";

    /// <summary>
    /// Gets or sets the expand/collapse indicator character for collapsed nodes.
    /// </summary>
    public string CollapsedIndicator { get; set; } = "▶";

    /// <summary>
    /// Event raised when the selected node changes.
    /// </summary>
    public event Action<TreeNode?>? SelectionChanged;

    /// <summary>
    /// Event raised when a node is expanded.
    /// </summary>
    public event Action<TreeNode>? NodeExpanded;

    /// <summary>
    /// Event raised when a node is collapsed.
    /// </summary>
    public event Action<TreeNode>? NodeCollapsed;

    /// <summary>
    /// Event raised when a node is activated (Enter key pressed).
    /// </summary>
    public event Action<TreeNode>? NodeActivated;

    /// <summary>
    /// Gets whether this widget can receive keyboard focus.
    /// </summary>
    public override bool IsFocusable => true;

    public TreeView()
    {
        SelectionBackgroundColor = ColorHelper.FromRgb(50, 100, 150);
        SelectionForegroundColor = Color.White;
    }

    protected override void OnRender(Screen screen)
    {
        if (Width <= 0 || Height <= 0)
            return;

        screen.FillRectangle(Bounds, ' ', ForegroundColor, BackgroundColor);

        var visibleNodes = GetVisibleNodes();
        int renderCount = Math.Min(Height, visibleNodes.Count - _scrollOffset);

        for (int i = 0; i < renderCount; i++)
        {
            int nodeIndex = _scrollOffset + i;
            if (nodeIndex >= visibleNodes.Count)
                break;

            var (node, depth, isLast, ancestorStates) = visibleNodes[nodeIndex];
            int nodeY = Y + i;

            bool isSelected = node == _selectedNode;
            var bgColor = isSelected ? SelectionBackgroundColor : BackgroundColor;
            var fgColor = isSelected ? SelectionForegroundColor : ForegroundColor;

            RenderTreeLine(screen, X, nodeY, node, depth, isLast, ancestorStates, fgColor, bgColor);
        }
    }

    private void RenderTreeLine(Screen screen, int x, int y, TreeNode node, int depth, bool isLast,
        List<bool> ancestorStates, Color fgColor, Color bgColor)
    {
        int currentX = x;
        if (depth > 0)
        {
            for (int level = 0; level < depth - 1; level++)
            {
                int indentX = currentX + level * IndentSize;

                if (!ancestorStates[level])
                {
                    if (indentX < x + Width)
                    {
                        screen.SetCell(indentX, y, new Cell('│', ConnectorColor, bgColor));
                    }
                }
                else
                {
                    if (indentX < x + Width)
                    {
                        screen.SetCell(indentX, y, new Cell(' ', fgColor, bgColor));
                    }
                }
            }

            int connectorX = currentX + (depth - 1) * IndentSize;
            if (connectorX < x + Width)
            {
                string connector = isLast ? "└" : "├";
                screen.SetCell(connectorX, y, new Cell(connector[0], ConnectorColor, bgColor));

                for (int i = 1; i < IndentSize; i++)
                {
                    if (connectorX + i < x + Width)
                    {
                        screen.SetCell(connectorX + i, y, new Cell('─', ConnectorColor, bgColor));
                    }
                }
            }

            currentX += depth * IndentSize;
        }

        if (!node.IsLeaf)
        {
            string indicator = node.IsExpanded ? ExpandedIndicator : CollapsedIndicator;
            if (currentX < x + Width)
            {
                screen.WriteText(currentX, y, indicator, fgColor, bgColor);
                currentX += indicator.Length + 1;
            }
        }
        else
        {
            currentX += 2;
        }

        if (ShowIcons && !string.IsNullOrEmpty(node.Icon))
        {
            if (currentX < x + Width)
            {
                screen.WriteText(currentX, y, node.Icon + " ", fgColor, bgColor);
                currentX += node.Icon.Length + 1;
            }
        }

        if (currentX < x + Width)
        {
            int maxTextLength = x + Width - currentX;
            string text = node.Text.Length > maxTextLength
                ? node.Text.Substring(0, maxTextLength)
                : node.Text;

            screen.WriteText(currentX, y, text, fgColor, bgColor, bold: node == _selectedNode);
            currentX += text.Length;

            while (currentX < x + Width)
            {
                screen.SetCell(currentX, y, new Cell(' ', fgColor, bgColor));
                currentX++;
            }
        }
    }

    /// <summary>
    /// Gets a flattened list of visible nodes (only expanded branches).
    /// Returns tuples of (node, depth, isLastChild, ancestorIsLastStates).
    /// </summary>
    private List<(TreeNode node, int depth, bool isLast, List<bool> ancestorStates)> GetVisibleNodes()
    {
        var result = new List<(TreeNode, int, bool, List<bool>)>();

        void TraverseNodes(List<TreeNode> nodes, int depth, List<bool> ancestorStates)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                bool isLast = i == nodes.Count - 1;

                result.Add((node, depth, isLast, new List<bool>(ancestorStates)));

                if (node.IsExpanded && node.Children.Count > 0)
                {
                    var newAncestorStates = new List<bool>(ancestorStates) { isLast };
                    TraverseNodes(node.Children.ToList(), depth + 1, newAncestorStates);
                }
            }
        }

        TraverseNodes(_rootNodes, 0, new List<bool>());
        return result;
    }

    /// <summary>
    /// Checks if a node is present in the current tree structure.
    /// </summary>
    private bool IsNodeInTree(TreeNode node)
    {
        bool SearchNodes(List<TreeNode> nodes)
        {
            foreach (var n in nodes)
            {
                if (n == node)
                    return true;

                if (SearchNodes([.. n.Children]))
                    return true;
            }
            return false;
        }

        return SearchNodes(_rootNodes);
    }

    protected internal override void OnFocus()
    {
        base.OnFocus();
        _hasFocus = true;

        if (_selectedNode == null)
        {
            var visibleNodes = GetVisibleNodes();
            if (visibleNodes.Count > 0)
            {
                SelectedNode = visibleNodes[0].node;
            }
        }
    }

    protected internal override void OnBlur()
    {
        base.OnBlur();
        _hasFocus = false;
    }

    protected internal override bool OnKeyPress(ConsoleKeyInfo key)
    {
        var visibleNodes = GetVisibleNodes();
        if (visibleNodes.Count == 0)
            return base.OnKeyPress(key);

        int currentIndex = _selectedNode != null
            ? visibleNodes.FindIndex(v => v.node == _selectedNode)
            : -1;

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                if (currentIndex > 0)
                {
                    SelectedNode = visibleNodes[currentIndex - 1].node;
                }
                return true;

            case ConsoleKey.DownArrow:
                if (currentIndex < visibleNodes.Count - 1)
                {
                    SelectedNode = visibleNodes[currentIndex + 1].node;
                }
                else if (currentIndex == -1 && visibleNodes.Count > 0)
                {
                    SelectedNode = visibleNodes[0].node;
                }
                return true;

            case ConsoleKey.LeftArrow:
                if (_selectedNode != null)
                {
                    // If expanded, collapse
                    if (_selectedNode.IsExpanded)
                    {
                        _selectedNode.Collapse();
                        NodeCollapsed?.Invoke(_selectedNode);
                    }
                    // Otherwise, move to parent
                    else if (_selectedNode.Parent != null)
                    {
                        SelectedNode = _selectedNode.Parent;
                    }
                }
                return true;

            case ConsoleKey.RightArrow:
                if (_selectedNode != null && !_selectedNode.IsLeaf)
                {
                    // If collapsed, expand
                    if (!_selectedNode.IsExpanded)
                    {
                        _selectedNode.Expand();
                        NodeExpanded?.Invoke(_selectedNode);
                    }
                    // Otherwise, move to first child
                    else if (_selectedNode.Children.Count > 0)
                    {
                        SelectedNode = _selectedNode.Children[0];
                    }
                }
                return true;

            case ConsoleKey.Enter:
                if (_selectedNode != null)
                {
                    NodeActivated?.Invoke(_selectedNode);

                    // Toggle expansion on Enter
                    if (!_selectedNode.IsLeaf)
                    {
                        if (_selectedNode.IsExpanded)
                        {
                            _selectedNode.Collapse();
                            NodeCollapsed?.Invoke(_selectedNode);
                        }
                        else
                        {
                            _selectedNode.Expand();
                            NodeExpanded?.Invoke(_selectedNode);
                        }
                    }
                }
                return true;

            case ConsoleKey.Home:
                if (visibleNodes.Count > 0)
                {
                    SelectedNode = visibleNodes[0].node;
                }
                return true;

            case ConsoleKey.End:
                if (visibleNodes.Count > 0)
                {
                    SelectedNode = visibleNodes[^1].node;
                }
                return true;

            case ConsoleKey.PageUp:
                if (visibleNodes.Count > 0 && currentIndex >= 0)
                {
                    int newIndex = Math.Max(0, currentIndex - Height);
                    SelectedNode = visibleNodes[newIndex].node;
                }
                return true;

            case ConsoleKey.PageDown:
                if (visibleNodes.Count > 0 && currentIndex >= 0)
                {
                    int newIndex = Math.Min(visibleNodes.Count - 1, currentIndex + Height);
                    SelectedNode = visibleNodes[newIndex].node;
                }
                return true;
        }

        return base.OnKeyPress(key);
    }

    protected override void OnBoundsChanged()
    {
        base.OnBoundsChanged();

        // Adjust scroll offset if height changed
        if (_selectedNode != null && Height > 0)
        {
            var visibleNodes = GetVisibleNodes();
            int selectedIndex = visibleNodes.FindIndex(v => v.node == _selectedNode);

            if (selectedIndex >= 0 && selectedIndex >= _scrollOffset + Height)
            {
                _scrollOffset = selectedIndex - Height + 1;
            }
        }
    }
}
