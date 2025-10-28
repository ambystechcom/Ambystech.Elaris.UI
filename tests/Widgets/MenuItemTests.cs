using Ambystech.Elaris.UI.Widgets.Menu;
using FluentAssertions;

namespace Ambystech.Elaris.UI.Tests.Widgets;

public class MenuItemTests
{
    [Fact]
    public void MenuItem_Should_Initialize_With_Text()
    {
        // Arrange & Act
        var item = new MenuItem("File");

        // Assert
        item.Text.Should().Be("File");
        item.Enabled.Should().BeTrue();
        item.IsSeparator.Should().BeFalse();
    }

    [Fact]
    public void MenuItem_Should_Fire_Selected_Event()
    {
        // Arrange
        var eventFired = false;
        var item = new MenuItem("Open", () => eventFired = true);

        // Act
        item.Invoke();

        // Assert
        eventFired.Should().BeTrue();
    }

    [Fact]
    public void MenuItem_Should_Not_Fire_When_Disabled()
    {
        // Arrange
        var eventFired = false;
        var item = new MenuItem("Save", () => eventFired = true) { Enabled = false };

        // Act
        item.Invoke();

        // Assert
        eventFired.Should().BeFalse();
    }

    [Fact]
    public void MenuItem_Separator_Should_Be_Marked()
    {
        // Arrange & Act
        var separator = MenuItem.Separator();

        // Assert
        separator.IsSeparator.Should().BeTrue();
    }

    [Fact]
    public void MenuItem_Should_Support_Submenu()
    {
        // Arrange
        var item = new MenuItem("File");
        item.SubItems.Add(new MenuItem("New"));
        item.SubItems.Add(new MenuItem("Open"));

        // Assert
        item.HasSubmenu.Should().BeTrue();
        item.SubItems.Should().HaveCount(2);
    }

    [Fact]
    public void MenuItem_Should_Support_Hotkey()
    {
        // Arrange & Act
        var item = new MenuItem("Save", null, 'S');

        // Assert
        item.Hotkey.Should().Be('S');
    }

    [Fact]
    public void MenuItem_Should_Support_Checked_State()
    {
        // Arrange
        var item = new MenuItem("Show Toolbar") { Checked = true };

        // Act
        item.Checked = false;

        // Assert
        item.Checked.Should().BeFalse();
    }
}
