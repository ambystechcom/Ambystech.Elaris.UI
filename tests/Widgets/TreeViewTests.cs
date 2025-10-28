using System.Drawing;
using Ambystech.Elaris.UI.Core;
using Ambystech.Elaris.UI.Rendering;
using Ambystech.Elaris.UI.Widgets.Display;
using FluentAssertions;

namespace Ambystech.Elaris.UI.Tests.Widgets;

public class TreeViewTests
{
    #region Helper Methods

    private TreeView CreateTreeView()
    {
        var treeView = new TreeView
        {
            Width = 40,
            Height = 10,
            X = 0,
            Y = 0
        };
        return treeView;
    }

    private TreeNode CreateSimpleTree()
    {
        var root = new TreeNode("Root");
        var child1 = new TreeNode("Child 1");
        var child2 = new TreeNode("Child 2");
        root.AddChild(child1);
        root.AddChild(child2);
        return root;
    }

    private TreeNode CreateDeepTree()
    {
        var root = new TreeNode("Level 0");
        var level1 = new TreeNode("Level 1");
        var level2 = new TreeNode("Level 2");
        var level3 = new TreeNode("Level 3");
        var level4 = new TreeNode("Level 4");
        var level5 = new TreeNode("Level 5");

        root.AddChild(level1);
        level1.AddChild(level2);
        level2.AddChild(level3);
        level3.AddChild(level4);
        level4.AddChild(level5);

        return root;
    }

    private List<TreeNode> CreateLargeTree(int count)
    {
        var nodes = new List<TreeNode>();
        for (int i = 0; i < count; i++)
        {
            nodes.Add(new TreeNode($"Node {i}"));
        }
        return nodes;
    }

    private Screen CreateTestScreen()
    {
        return new Screen(80, 24);
    }

    private ConsoleKeyInfo CreateKey(ConsoleKey key)
    {
        return new ConsoleKeyInfo('\0', key, false, false, false);
    }

    #endregion

    #region Initialization Tests

    [Fact]
    public void TreeView_Should_Initialize_Empty()
    {
        // Arrange & Act
        var treeView = new TreeView();

        // Assert
        treeView.RootNodes.Should().BeEmpty();
        treeView.SelectedNode.Should().BeNull();
        treeView.ShowIcons.Should().BeTrue();
        treeView.IndentSize.Should().Be(2);
    }

    [Fact]
    public void TreeView_Should_Accept_RootNodes()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root1 = new TreeNode("Root 1");
        var root2 = new TreeNode("Root 2");

        // Act
        treeView.RootNodes = new List<TreeNode> { root1, root2 };

        // Assert
        treeView.RootNodes.Should().HaveCount(2);
        treeView.RootNodes[0].Should().Be(root1);
        treeView.RootNodes[1].Should().Be(root2);
    }

    [Fact]
    public void TreeView_Should_Reset_Selection_When_Node_No_Longer_In_Tree()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root1 = new TreeNode("Root 1");
        treeView.RootNodes = new List<TreeNode> { root1 };
        treeView.SelectedNode = root1;

        // Act
        var root2 = new TreeNode("Root 2");
        treeView.RootNodes = new List<TreeNode> { root2 };

        // Assert
        treeView.SelectedNode.Should().BeNull();
    }

    #endregion

    #region Navigation Tests - Arrow Keys

    [Fact]
    public void TreeView_Should_Navigate_Down_Between_Nodes()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = CreateSimpleTree();
        root.Expand(); // Expand to make children visible
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.SelectedNode = root;

        // Act
        var handled = treeView.OnKeyPress(CreateKey(ConsoleKey.DownArrow));

        // Assert
        handled.Should().BeTrue();
        treeView.SelectedNode.Should().Be(root.Children[0]);
    }

    [Fact]
    public void TreeView_Should_Navigate_Up_Between_Nodes()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = CreateSimpleTree();
        root.Expand(); // Expand to make children visible
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.SelectedNode = root.Children[0];

        // Act
        var handled = treeView.OnKeyPress(CreateKey(ConsoleKey.UpArrow));

        // Assert
        handled.Should().BeTrue();
        treeView.SelectedNode.Should().Be(root);
    }

    [Fact]
    public void TreeView_Should_Not_Navigate_Up_When_At_First_Node()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = new TreeNode("Root");
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.SelectedNode = root;

        // Act
        var handled = treeView.OnKeyPress(CreateKey(ConsoleKey.UpArrow));

        // Assert
        handled.Should().BeTrue();
        treeView.SelectedNode.Should().Be(root);
    }

    [Fact]
    public void TreeView_Should_Not_Navigate_Down_When_At_Last_Node()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = CreateSimpleTree();
        root.Expand(); // Expand to make children visible
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.SelectedNode = root.Children[1];

        // Act
        var handled = treeView.OnKeyPress(CreateKey(ConsoleKey.DownArrow));

        // Assert
        handled.Should().BeTrue();
        treeView.SelectedNode.Should().Be(root.Children[1]);
    }

    [Fact]
    public void TreeView_Should_Select_First_Node_On_Down_When_Nothing_Selected()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = new TreeNode("Root");
        treeView.RootNodes = new List<TreeNode> { root };

        // Act
        var handled = treeView.OnKeyPress(CreateKey(ConsoleKey.DownArrow));

        // Assert
        handled.Should().BeTrue();
        treeView.SelectedNode.Should().Be(root);
    }

    [Fact]
    public void TreeView_Should_Navigate_Only_Visible_Nodes()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = CreateSimpleTree();
        // Don't expand the root, so children are not visible
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.SelectedNode = root;

        // Act
        var handled = treeView.OnKeyPress(CreateKey(ConsoleKey.DownArrow));

        // Assert
        handled.Should().BeTrue();
        treeView.SelectedNode.Should().Be(root); // Should stay on root since children are collapsed
    }

    #endregion

    #region Navigation Tests - Home/End

    [Fact]
    public void TreeView_Should_Navigate_To_First_Node_On_Home()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = CreateSimpleTree();
        root.Expand();
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.SelectedNode = root.Children[1];

        // Act
        var handled = treeView.OnKeyPress(CreateKey(ConsoleKey.Home));

        // Assert
        handled.Should().BeTrue();
        treeView.SelectedNode.Should().Be(root);
    }

    [Fact]
    public void TreeView_Should_Navigate_To_Last_Visible_Node_On_End()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = CreateSimpleTree();
        root.Expand();
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.SelectedNode = root;

        // Act
        var handled = treeView.OnKeyPress(CreateKey(ConsoleKey.End));

        // Assert
        handled.Should().BeTrue();
        treeView.SelectedNode.Should().Be(root.Children[1]);
    }

    [Fact]
    public void TreeView_Home_Should_Handle_Empty_Tree()
    {
        // Arrange
        var treeView = CreateTreeView();

        // Act
        var handled = treeView.OnKeyPress(CreateKey(ConsoleKey.Home));

        // Assert - Should NOT handle the key when tree is empty
        handled.Should().BeFalse();
        treeView.SelectedNode.Should().BeNull();
    }

    #endregion

    #region Navigation Tests - PageUp/PageDown

    [Fact]
    public void TreeView_Should_Navigate_PageDown()
    {
        // Arrange
        var treeView = CreateTreeView();
        treeView.Height = 5;
        var nodes = CreateLargeTree(20);
        nodes.ForEach(n => n.Expand());
        treeView.RootNodes = nodes;
        treeView.SelectedNode = nodes[0];

        // Act
        var handled = treeView.OnKeyPress(CreateKey(ConsoleKey.PageDown));

        // Assert
        handled.Should().BeTrue();
        treeView.SelectedNode.Should().Be(nodes[5]); // Height = 5, so jump 5 nodes
    }

    [Fact]
    public void TreeView_Should_Navigate_PageUp()
    {
        // Arrange
        var treeView = CreateTreeView();
        treeView.Height = 5;
        var nodes = CreateLargeTree(20);
        treeView.RootNodes = nodes;
        treeView.SelectedNode = nodes[10];

        // Act
        var handled = treeView.OnKeyPress(CreateKey(ConsoleKey.PageUp));

        // Assert
        handled.Should().BeTrue();
        treeView.SelectedNode.Should().Be(nodes[5]); // Jump back 5 nodes
    }

    [Fact]
    public void TreeView_PageDown_Should_Not_Go_Beyond_Last_Node()
    {
        // Arrange
        var treeView = CreateTreeView();
        treeView.Height = 10;
        var nodes = CreateLargeTree(5);
        treeView.RootNodes = nodes;
        treeView.SelectedNode = nodes[3];

        // Act
        var handled = treeView.OnKeyPress(CreateKey(ConsoleKey.PageDown));

        // Assert
        handled.Should().BeTrue();
        treeView.SelectedNode.Should().Be(nodes[4]); // Should clamp to last node
    }

    [Fact]
    public void TreeView_PageUp_Should_Not_Go_Before_First_Node()
    {
        // Arrange
        var treeView = CreateTreeView();
        treeView.Height = 10;
        var nodes = CreateLargeTree(20);
        treeView.RootNodes = nodes;
        treeView.SelectedNode = nodes[2];

        // Act
        var handled = treeView.OnKeyPress(CreateKey(ConsoleKey.PageUp));

        // Assert
        handled.Should().BeTrue();
        treeView.SelectedNode.Should().Be(nodes[0]); // Should clamp to first node
    }

    #endregion

    #region Selection Tests

    [Fact]
    public void TreeView_Should_Fire_SelectionChanged_Event()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = CreateSimpleTree();
        treeView.RootNodes = new List<TreeNode> { root };

        TreeNode? selectedNode = null;
        var eventFired = false;

        treeView.SelectionChanged += (node) =>
        {
            eventFired = true;
            selectedNode = node;
        };

        // Act
        treeView.SelectedNode = root;

        // Assert
        eventFired.Should().BeTrue();
        selectedNode.Should().Be(root);
    }

    [Fact]
    public void TreeView_Should_Not_Fire_SelectionChanged_When_Setting_Same_Node()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = new TreeNode("Root");
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.SelectedNode = root;

        var eventCount = 0;
        treeView.SelectionChanged += (node) => eventCount++;

        // Act
        treeView.SelectedNode = root;

        // Assert
        eventCount.Should().Be(0);
    }

    [Fact]
    public void TreeView_Selection_Should_Adjust_Scroll_To_Keep_Visible()
    {
        // Arrange
        var treeView = CreateTreeView();
        treeView.Height = 5;
        var nodes = CreateLargeTree(20);
        treeView.RootNodes = nodes;

        // Act - Select a node beyond the visible area
        treeView.SelectedNode = nodes[10];

        // Assert - The selection should be set (scroll offset is internal, but selection should work)
        treeView.SelectedNode.Should().Be(nodes[10]);
    }

    #endregion

    #region Expansion Tests

    [Fact]
    public void TreeView_Should_Collapse_Node_On_Left_Arrow()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = CreateSimpleTree();
        root.Expand();
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.SelectedNode = root;

        // Act
        var handled = treeView.OnKeyPress(CreateKey(ConsoleKey.LeftArrow));

        // Assert
        handled.Should().BeTrue();
        root.IsExpanded.Should().BeFalse();
    }

    [Fact]
    public void TreeView_Should_Fire_NodeCollapsed_Event()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = CreateSimpleTree();
        root.Expand();
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.SelectedNode = root;

        TreeNode? collapsedNode = null;
        var eventFired = false;

        treeView.NodeCollapsed += (node) =>
        {
            eventFired = true;
            collapsedNode = node;
        };

        // Act
        treeView.OnKeyPress(CreateKey(ConsoleKey.LeftArrow));

        // Assert
        eventFired.Should().BeTrue();
        collapsedNode.Should().Be(root);
    }

    [Fact]
    public void TreeView_Should_Navigate_To_Parent_On_Left_When_Already_Collapsed()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = CreateSimpleTree();
        root.Expand();
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.SelectedNode = root.Children[0];

        // Act
        var handled = treeView.OnKeyPress(CreateKey(ConsoleKey.LeftArrow));

        // Assert
        handled.Should().BeTrue();
        treeView.SelectedNode.Should().Be(root);
    }

    [Fact]
    public void TreeView_Should_Expand_Node_On_Right_Arrow()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = CreateSimpleTree();
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.SelectedNode = root;

        // Act
        var handled = treeView.OnKeyPress(CreateKey(ConsoleKey.RightArrow));

        // Assert
        handled.Should().BeTrue();
        root.IsExpanded.Should().BeTrue();
    }

    [Fact]
    public void TreeView_Should_Fire_NodeExpanded_Event()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = CreateSimpleTree();
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.SelectedNode = root;

        TreeNode? expandedNode = null;
        var eventFired = false;

        treeView.NodeExpanded += (node) =>
        {
            eventFired = true;
            expandedNode = node;
        };

        // Act
        treeView.OnKeyPress(CreateKey(ConsoleKey.RightArrow));

        // Assert
        eventFired.Should().BeTrue();
        expandedNode.Should().Be(root);
    }

    [Fact]
    public void TreeView_Should_Navigate_To_First_Child_On_Right_When_Already_Expanded()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = CreateSimpleTree();
        root.Expand();
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.SelectedNode = root;

        // Act
        var handled = treeView.OnKeyPress(CreateKey(ConsoleKey.RightArrow));

        // Assert
        handled.Should().BeTrue();
        treeView.SelectedNode.Should().Be(root.Children[0]);
    }

    [Fact]
    public void TreeView_RightArrow_Should_Not_Expand_Leaf_Nodes()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = new TreeNode("Root");
        var leaf = new TreeNode("Leaf");
        root.AddChild(leaf);
        root.Expand();
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.SelectedNode = leaf;

        // Act
        var handled = treeView.OnKeyPress(CreateKey(ConsoleKey.RightArrow));

        // Assert
        handled.Should().BeTrue();
        leaf.IsLeaf.Should().BeTrue();
        treeView.SelectedNode.Should().Be(leaf); // Should stay on leaf
    }

    [Fact]
    public void TreeView_Should_Toggle_Expansion_On_Enter()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = CreateSimpleTree();
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.SelectedNode = root;

        // Act - Expand
        treeView.OnKeyPress(CreateKey(ConsoleKey.Enter));

        // Assert
        root.IsExpanded.Should().BeTrue();

        // Act - Collapse
        treeView.OnKeyPress(CreateKey(ConsoleKey.Enter));

        // Assert
        root.IsExpanded.Should().BeFalse();
    }

    [Fact]
    public void TreeView_Should_Fire_NodeActivated_Event_On_Enter()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = new TreeNode("Root");
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.SelectedNode = root;

        TreeNode? activatedNode = null;
        var eventFired = false;

        treeView.NodeActivated += (node) =>
        {
            eventFired = true;
            activatedNode = node;
        };

        // Act
        treeView.OnKeyPress(CreateKey(ConsoleKey.Enter));

        // Assert
        eventFired.Should().BeTrue();
        activatedNode.Should().Be(root);
    }

    [Fact]
    public void TreeView_Enter_Should_Fire_Both_Expanded_And_Activated_Events()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = CreateSimpleTree();
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.SelectedNode = root;

        var expandedFired = false;
        var activatedFired = false;

        treeView.NodeExpanded += (node) => expandedFired = true;
        treeView.NodeActivated += (node) => activatedFired = true;

        // Act
        treeView.OnKeyPress(CreateKey(ConsoleKey.Enter));

        // Assert
        expandedFired.Should().BeTrue();
        activatedFired.Should().BeTrue();
    }

    #endregion

    #region Focus Tests

    [Fact]
    public void TreeView_Should_Select_First_Node_On_Focus_When_Nothing_Selected()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = new TreeNode("Root");
        treeView.RootNodes = new List<TreeNode> { root };

        // Act
        treeView.OnFocus();

        // Assert
        treeView.SelectedNode.Should().Be(root);
    }

    [Fact]
    public void TreeView_Should_Not_Change_Selection_On_Focus_When_Already_Selected()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = CreateSimpleTree();
        root.Expand();
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.SelectedNode = root.Children[0];

        // Act
        treeView.OnFocus();

        // Assert
        treeView.SelectedNode.Should().Be(root.Children[0]);
    }

    [Fact]
    public void TreeView_OnFocus_Should_Handle_Empty_Tree()
    {
        // Arrange
        var treeView = CreateTreeView();

        // Act
        treeView.OnFocus();

        // Assert
        treeView.SelectedNode.Should().BeNull();
    }

    [Fact]
    public void TreeView_OnBlur_Should_Not_Clear_Selection()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = new TreeNode("Root");
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.SelectedNode = root;

        // Act
        treeView.OnBlur();

        // Assert
        treeView.SelectedNode.Should().Be(root);
    }

    #endregion

    #region Edge Cases - Empty Tree

    [Fact]
    public void TreeView_Should_Handle_Empty_Tree_Navigation()
    {
        // Arrange
        var treeView = CreateTreeView();

        // Act & Assert - Keys should NOT be handled (return false) for empty tree
        treeView.OnKeyPress(CreateKey(ConsoleKey.UpArrow)).Should().BeFalse();
        treeView.OnKeyPress(CreateKey(ConsoleKey.DownArrow)).Should().BeFalse();
        treeView.OnKeyPress(CreateKey(ConsoleKey.LeftArrow)).Should().BeFalse();
        treeView.OnKeyPress(CreateKey(ConsoleKey.RightArrow)).Should().BeFalse();
        treeView.OnKeyPress(CreateKey(ConsoleKey.Home)).Should().BeFalse();
        treeView.OnKeyPress(CreateKey(ConsoleKey.End)).Should().BeFalse();
        treeView.SelectedNode.Should().BeNull();
    }

    [Fact]
    public void TreeView_Should_Render_Empty_Tree_Without_Error()
    {
        // Arrange
        var treeView = CreateTreeView();
        var screen = CreateTestScreen();

        // Act
        Action render = () => treeView.Render(screen);

        // Assert
        render.Should().NotThrow();
    }

    #endregion

    #region Edge Cases - Single Node

    [Fact]
    public void TreeView_Should_Handle_Single_Node_Tree()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = new TreeNode("Single");
        treeView.RootNodes = new List<TreeNode> { root };

        // Act
        treeView.OnFocus();

        // Assert
        treeView.SelectedNode.Should().Be(root);
    }

    [Fact]
    public void TreeView_Single_Node_Should_Not_Navigate_Up_Or_Down()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = new TreeNode("Single");
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.SelectedNode = root;

        // Act & Assert
        treeView.OnKeyPress(CreateKey(ConsoleKey.UpArrow));
        treeView.SelectedNode.Should().Be(root);

        treeView.OnKeyPress(CreateKey(ConsoleKey.DownArrow));
        treeView.SelectedNode.Should().Be(root);
    }

    #endregion

    #region Edge Cases - Deep Tree

    [Fact]
    public void TreeView_Should_Handle_Deeply_Nested_Tree()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = CreateDeepTree();
        root.ExpandAll();
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.SelectedNode = root;

        // Act - Navigate to the deepest node
        for (int i = 0; i < 5; i++)
        {
            treeView.OnKeyPress(CreateKey(ConsoleKey.DownArrow));
        }

        // Assert
        treeView.SelectedNode.Should().NotBeNull();
        treeView.SelectedNode!.Text.Should().Be("Level 5");
        treeView.SelectedNode.GetDepth().Should().Be(5);
    }

    [Fact]
    public void TreeView_Should_Navigate_Up_Deep_Tree()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = CreateDeepTree();
        root.ExpandAll();
        treeView.RootNodes = new List<TreeNode> { root };

        // Start at deepest node
        var deepestNode = root.Children[0].Children[0].Children[0].Children[0].Children[0];
        treeView.SelectedNode = deepestNode;

        // Act - Navigate all the way up
        for (int i = 0; i < 5; i++)
        {
            treeView.OnKeyPress(CreateKey(ConsoleKey.UpArrow));
        }

        // Assert
        treeView.SelectedNode.Should().Be(root);
    }

    [Fact]
    public void TreeView_Should_Render_Deep_Tree_Without_Error()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = CreateDeepTree();
        root.ExpandAll();
        treeView.RootNodes = new List<TreeNode> { root };
        var screen = CreateTestScreen();

        // Act
        Action render = () => treeView.Render(screen);

        // Assert
        render.Should().NotThrow();
    }

    #endregion

    #region Edge Cases - Large Tree

    [Fact]
    public void TreeView_Should_Handle_Large_Tree_With_Scrolling()
    {
        // Arrange
        var treeView = CreateTreeView();
        treeView.Height = 10;
        var nodes = CreateLargeTree(100);
        treeView.RootNodes = nodes;

        // Act - Select a node far down
        treeView.SelectedNode = nodes[50];

        // Assert
        treeView.SelectedNode.Should().Be(nodes[50]);
    }

    [Fact]
    public void TreeView_Should_Navigate_Through_Large_Tree()
    {
        // Arrange
        var treeView = CreateTreeView();
        treeView.Height = 5;
        var nodes = CreateLargeTree(20);
        treeView.RootNodes = nodes;
        treeView.SelectedNode = nodes[0];

        // Act - Navigate down 10 times
        for (int i = 0; i < 10; i++)
        {
            treeView.OnKeyPress(CreateKey(ConsoleKey.DownArrow));
        }

        // Assert
        treeView.SelectedNode.Should().Be(nodes[10]);
    }

    [Fact]
    public void TreeView_Should_Render_Large_Tree_Without_Error()
    {
        // Arrange
        var treeView = CreateTreeView();
        var nodes = CreateLargeTree(100);
        treeView.RootNodes = nodes;
        var screen = CreateTestScreen();

        // Act
        Action render = () => treeView.Render(screen);

        // Assert
        render.Should().NotThrow();
    }

    #endregion

    #region Rendering Tests

    [Fact]
    public void TreeView_Should_Render_Without_Error()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = CreateSimpleTree();
        treeView.RootNodes = new List<TreeNode> { root };
        var screen = CreateTestScreen();

        // Act
        Action render = () => treeView.Render(screen);

        // Assert
        render.Should().NotThrow();
    }

    [Fact]
    public void TreeView_Should_Not_Render_Collapsed_Children()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = CreateSimpleTree();
        // Don't expand root, children should not be visible
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.SelectedNode = root;

        // Act - Try to navigate to children (should not be possible)
        treeView.OnKeyPress(CreateKey(ConsoleKey.DownArrow));

        // Assert - Should still be on root since children are collapsed
        treeView.SelectedNode.Should().Be(root);
    }

    [Fact]
    public void TreeView_Should_Render_Expanded_Children()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = CreateSimpleTree();
        root.Expand();
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.SelectedNode = root;

        // Act - Navigate to first child
        treeView.OnKeyPress(CreateKey(ConsoleKey.DownArrow));

        // Assert - Should be on first child
        treeView.SelectedNode.Should().Be(root.Children[0]);
    }

    [Fact]
    public void TreeView_Should_Handle_Icons_When_Enabled()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = new TreeNode("Root", "📁");
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.ShowIcons = true;
        var screen = CreateTestScreen();

        // Act
        Action render = () => treeView.Render(screen);

        // Assert
        render.Should().NotThrow();
    }

    [Fact]
    public void TreeView_Should_Handle_Icons_When_Disabled()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = new TreeNode("Root", "📁");
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.ShowIcons = false;
        var screen = CreateTestScreen();

        // Act
        Action render = () => treeView.Render(screen);

        // Assert
        render.Should().NotThrow();
    }

    [Fact]
    public void TreeView_Should_Render_Selection_Highlighting()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = new TreeNode("Root");
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.SelectedNode = root;
        treeView.SelectionBackgroundColor = ColorHelper.FromRgb(50, 100, 150);
        treeView.SelectionForegroundColor = Color.White;
        var screen = CreateTestScreen();

        // Act
        Action render = () => treeView.Render(screen);

        // Assert
        render.Should().NotThrow();
    }

    [Fact]
    public void TreeView_Should_Render_With_Custom_Indentation()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = CreateDeepTree();
        root.ExpandAll();
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.IndentSize = 4;
        var screen = CreateTestScreen();

        // Act
        Action render = () => treeView.Render(screen);

        // Assert
        render.Should().NotThrow();
    }

    [Fact]
    public void TreeView_Should_Render_With_Custom_Indicators()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = CreateSimpleTree();
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.ExpandedIndicator = "[-]";
        treeView.CollapsedIndicator = "[+]";
        var screen = CreateTestScreen();

        // Act
        Action render = () => treeView.Render(screen);

        // Assert
        render.Should().NotThrow();
    }

    [Fact]
    public void TreeView_Should_Handle_Zero_Width()
    {
        // Arrange
        var treeView = CreateTreeView();
        treeView.Width = 0;
        var root = new TreeNode("Root");
        treeView.RootNodes = new List<TreeNode> { root };
        var screen = CreateTestScreen();

        // Act
        Action render = () => treeView.Render(screen);

        // Assert
        render.Should().NotThrow();
    }

    [Fact]
    public void TreeView_Should_Handle_Zero_Height()
    {
        // Arrange
        var treeView = CreateTreeView();
        treeView.Height = 0;
        var root = new TreeNode("Root");
        treeView.RootNodes = new List<TreeNode> { root };
        var screen = CreateTestScreen();

        // Act
        Action render = () => treeView.Render(screen);

        // Assert
        render.Should().NotThrow();
    }

    #endregion

    #region Multiple Root Nodes Tests

    [Fact]
    public void TreeView_Should_Handle_Multiple_Root_Nodes()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root1 = new TreeNode("Root 1");
        var root2 = new TreeNode("Root 2");
        var root3 = new TreeNode("Root 3");
        treeView.RootNodes = new List<TreeNode> { root1, root2, root3 };

        // Act
        treeView.OnFocus();

        // Assert
        treeView.SelectedNode.Should().Be(root1);
    }

    [Fact]
    public void TreeView_Should_Navigate_Between_Root_Nodes()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root1 = new TreeNode("Root 1");
        var root2 = new TreeNode("Root 2");
        treeView.RootNodes = new List<TreeNode> { root1, root2 };
        treeView.SelectedNode = root1;

        // Act
        treeView.OnKeyPress(CreateKey(ConsoleKey.DownArrow));

        // Assert
        treeView.SelectedNode.Should().Be(root2);
    }

    #endregion

    #region Bounds Change Tests

    [Fact]
    public void TreeView_Should_Handle_Bounds_Change()
    {
        // Arrange
        var treeView = CreateTreeView();
        var nodes = CreateLargeTree(50);
        treeView.RootNodes = nodes;
        treeView.SelectedNode = nodes[20];

        // Act
        treeView.Height = 5; // Reduce height

        // Assert - Should not throw
        treeView.SelectedNode.Should().Be(nodes[20]);
    }

    #endregion

    #region Null/Empty Handling

    [Fact]
    public void TreeView_Should_Handle_Null_RootNodes()
    {
        // Arrange
        var treeView = CreateTreeView();

        // Act
        treeView.RootNodes = null!;

        // Assert
        treeView.RootNodes.Should().NotBeNull();
        treeView.RootNodes.Should().BeEmpty();
    }

    [Fact]
    public void TreeView_Should_Handle_Selection_Of_Null()
    {
        // Arrange
        var treeView = CreateTreeView();
        var root = new TreeNode("Root");
        treeView.RootNodes = new List<TreeNode> { root };
        treeView.SelectedNode = root;

        var eventFired = false;
        treeView.SelectionChanged += (node) => eventFired = true;

        // Act
        treeView.SelectedNode = null;

        // Assert
        eventFired.Should().BeTrue();
        treeView.SelectedNode.Should().BeNull();
    }

    #endregion
}
