using System.Collections.ObjectModel;

namespace Ambystech.Elaris.UI.Widgets.Display;

/// <summary>
/// Represents a node in a tree structure with support for hierarchical data.
/// </summary>
public class TreeNode
{
    private readonly List<TreeNode> _children = [];
    private bool _isExpanded;

    /// <summary>
    /// Gets or sets the text displayed for this node.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets the icon displayed before the text (optional).
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets the collection of child nodes.
    /// </summary>
    public ReadOnlyCollection<TreeNode> Children => _children.AsReadOnly();

    /// <summary>
    /// Gets or sets whether this node is currently expanded.
    /// Only applicable for nodes with children.
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                OnExpandedChanged?.Invoke(this, value);
            }
        }
    }

    /// <summary>
    /// Gets whether this node is a leaf (has no children).
    /// </summary>
    public bool IsLeaf => _children.Count == 0;

    /// <summary>
    /// Gets or sets the parent node (null if this is a root node).
    /// </summary>
    public TreeNode? Parent { get; private set; }

    /// <summary>
    /// Gets or sets custom data associated with this node.
    /// Useful for storing domain-specific objects (e.g., file paths, IDs).
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Gets or sets whether this node is selectable.
    /// </summary>
    public bool IsSelectable { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this node is enabled (can be interacted with).
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Event raised when the node's expanded state changes.
    /// </summary>
    public event Action<TreeNode, bool>? OnExpandedChanged;

    /// <summary>
    /// Creates a new tree node with the specified text.
    /// </summary>
    public TreeNode(string text, string? icon = null, object? data = null)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Icon = icon;
        Data = data;
    }

    /// <summary>
    /// Adds a child node to this node.
    /// </summary>
    public void AddChild(TreeNode child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (child == this)
            throw new InvalidOperationException("Cannot add a node as its own child");

        if (child.Parent != null)
            throw new InvalidOperationException("Child node already has a parent. Remove it first.");

        child.Parent = this;
        _children.Add(child);
    }

    /// <summary>
    /// Inserts a child node at the specified index.
    /// </summary>
    public void InsertChild(int index, TreeNode child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (index < 0 || index > _children.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (child == this)
            throw new InvalidOperationException("Cannot add a node as its own child");

        if (child.Parent != null)
            throw new InvalidOperationException("Child node already has a parent. Remove it first.");

        child.Parent = this;
        _children.Insert(index, child);
    }

    /// <summary>
    /// Removes a child node from this node.
    /// </summary>
    public bool RemoveChild(TreeNode child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (_children.Remove(child))
        {
            child.Parent = null;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes all child nodes.
    /// </summary>
    public void ClearChildren()
    {
        foreach (var child in _children)
        {
            child.Parent = null;
        }
        _children.Clear();
    }

    /// <summary>
    /// Expands this node to show its children.
    /// </summary>
    public void Expand()
    {
        if (!IsLeaf)
        {
            IsExpanded = true;
        }
    }

    /// <summary>
    /// Collapses this node to hide its children.
    /// </summary>
    public void Collapse()
    {
        if (!IsLeaf)
        {
            IsExpanded = false;
        }
    }

    /// <summary>
    /// Toggles the expanded state of this node.
    /// </summary>
    public void Toggle()
    {
        if (!IsLeaf)
        {
            IsExpanded = !IsExpanded;
        }
    }

    /// <summary>
    /// Expands this node and all its descendants recursively.
    /// </summary>
    public void ExpandAll()
    {
        if (!IsLeaf)
        {
            IsExpanded = true;
            foreach (var child in _children)
            {
                child.ExpandAll();
            }
        }
    }

    /// <summary>
    /// Collapses this node and all its descendants recursively.
    /// </summary>
    public void CollapseAll()
    {
        if (!IsLeaf)
        {
            IsExpanded = false;
            foreach (var child in _children)
            {
                child.CollapseAll();
            }
        }
    }

    /// <summary>
    /// Gets the depth of this node in the tree (0 for root nodes).
    /// </summary>
    public int GetDepth()
    {
        int depth = 0;
        TreeNode? current = Parent;
        while (current != null)
        {
            depth++;
            current = current.Parent;
        }
        return depth;
    }

    /// <summary>
    /// Gets the path from the root to this node as a list of nodes.
    /// </summary>
    public List<TreeNode> GetPath()
    {
        var path = new List<TreeNode>();
        TreeNode? current = this;
        while (current != null)
        {
            path.Insert(0, current);
            current = current.Parent;
        }
        return path;
    }

    /// <summary>
    /// Gets all descendants of this node (children, grandchildren, etc.).
    /// </summary>
    public List<TreeNode> GetAllDescendants()
    {
        var descendants = new List<TreeNode>();
        foreach (var child in _children)
        {
            descendants.Add(child);
            descendants.AddRange(child.GetAllDescendants());
        }
        return descendants;
    }

    /// <summary>
    /// Finds the first descendant node that matches the predicate.
    /// </summary>
    public TreeNode? FindDescendant(Func<TreeNode, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        foreach (var child in _children)
        {
            if (predicate(child))
                return child;

            var found = child.FindDescendant(predicate);
            if (found != null)
                return found;
        }

        return null;
    }

    /// <summary>
    /// Finds all descendant nodes that match the predicate.
    /// </summary>
    public List<TreeNode> FindAllDescendants(Func<TreeNode, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var results = new List<TreeNode>();
        foreach (var child in _children)
        {
            if (predicate(child))
                results.Add(child);

            results.AddRange(child.FindAllDescendants(predicate));
        }

        return results;
    }

    /// <summary>
    /// Returns a string representation of this node.
    /// </summary>
    public override string ToString()
    {
        return Icon != null ? $"{Icon} {Text}" : Text;
    }
}
