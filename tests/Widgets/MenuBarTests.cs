using Ambystech.Elaris.UI.Widgets.Menu;
using FluentAssertions;

namespace Ambystech.Elaris.UI.Tests.Widgets;

public class MenuBarTests
{
    [Fact]
    public void MenuBar_Should_Initialize_Empty()
    {
        // Arrange & Act
        var menuBar = new MenuBar();

        // Assert
        menuBar.Items.Should().BeEmpty();
    }

    [Fact]
    public void MenuBar_Should_Add_Menu_Items()
    {
        // Arrange
        var menuBar = new MenuBar();

        // Act
        menuBar.AddItem(new MenuItem("File"));
        menuBar.AddItem(new MenuItem("Edit"));

        // Assert
        menuBar.Items.Should().HaveCount(2);
        menuBar.Items[0].Text.Should().Be("File");
        menuBar.Items[1].Text.Should().Be("Edit");
    }

    [Fact]
    public void MenuBar_Should_Add_Menu_With_Submenu()
    {
        // Arrange
        var menuBar = new MenuBar();

        // Act
        menuBar.AddMenu("File",
            new MenuItem("New"),
            new MenuItem("Open")
        );

        // Assert
        menuBar.Items.Should().HaveCount(1);
        menuBar.Items[0].HasSubmenu.Should().BeTrue();
        menuBar.Items[0].SubItems.Should().HaveCount(2);
    }

    [Fact]
    public void MenuBar_Should_Support_ItemSelected_Event()
    {
        // Arrange
        var menuBar = new MenuBar();
        var eventWasRegistered = false;

        // Act
        try
        {
            menuBar.ItemSelected += (item) => { };
            eventWasRegistered = true;
        }
        catch
        {
            eventWasRegistered = false;
        }

        // Assert - event handler should be registerable
        eventWasRegistered.Should().BeTrue();
    }
}
