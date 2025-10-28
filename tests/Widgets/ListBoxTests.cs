using Ambystech.Elaris.UI.Widgets.Input;
using FluentAssertions;

namespace Ambystech.Elaris.UI.Tests.Widgets;

public class ListBoxTests
{
    [Fact]
    public void ListBox_Should_Initialize_Empty()
    {
        // Arrange & Act
        var listBox = new ListBox();

        // Assert
        listBox.Items.Should().BeEmpty();
        listBox.SelectedIndex.Should().Be(-1);
        listBox.SelectedItem.Should().BeNull();
    }

    [Fact]
    public void ListBox_Should_Add_Items()
    {
        // Arrange
        var listBox = new ListBox();

        // Act
        listBox.AddItem("Item 1");
        listBox.AddItem("Item 2");

        // Assert
        listBox.Items.Should().HaveCount(2);
        listBox.Items[0].Should().Be("Item 1");
        listBox.Items[1].Should().Be("Item 2");
    }

    [Fact]
    public void ListBox_Should_Select_Item_By_Index()
    {
        // Arrange
        var listBox = new ListBox();
        listBox.AddItem("Apple");
        listBox.AddItem("Banana");
        listBox.AddItem("Cherry");

        // Act
        listBox.SelectedIndex = 1;

        // Assert
        listBox.SelectedIndex.Should().Be(1);
        listBox.SelectedItem.Should().Be("Banana");
    }

    [Fact]
    public void ListBox_Should_Fire_SelectionChanged_Event()
    {
        // Arrange
        var listBox = new ListBox();
        listBox.AddItem("Item 1");
        listBox.AddItem("Item 2");

        var eventFired = false;
        var selectedIndex = -1;

        listBox.SelectionChanged += (index) =>
        {
            eventFired = true;
            selectedIndex = index;
        };

        // Act
        listBox.SelectedIndex = 1;

        // Assert
        eventFired.Should().BeTrue();
        selectedIndex.Should().Be(1);
    }

    [Fact]
    public void ListBox_Should_Clear_All_Items()
    {
        // Arrange
        var listBox = new ListBox();
        listBox.AddItem("Item 1");
        listBox.AddItem("Item 2");
        listBox.SelectedIndex = 0;

        // Act
        listBox.Clear();

        // Assert
        listBox.Items.Should().BeEmpty();
        listBox.SelectedIndex.Should().Be(-1);
    }

    [Fact]
    public void ListBox_Should_Remove_Item_At_Index()
    {
        // Arrange
        var listBox = new ListBox();
        listBox.AddItem("Item 1");
        listBox.AddItem("Item 2");
        listBox.AddItem("Item 3");

        // Act
        listBox.RemoveItemAt(1);

        // Assert
        listBox.Items.Should().HaveCount(2);
        listBox.Items[0].Should().Be("Item 1");
        listBox.Items[1].Should().Be("Item 3");
    }
}
