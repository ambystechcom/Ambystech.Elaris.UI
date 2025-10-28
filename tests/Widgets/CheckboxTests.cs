using Ambystech.Elaris.UI.Widgets.Input;
using FluentAssertions;

namespace Ambystech.Elaris.UI.Tests.Widgets;

public class CheckboxTests
{
    [Fact]
    public void Checkbox_Should_Initialize_Unchecked()
    {
        // Arrange & Act
        var checkbox = new Checkbox("Test");

        // Assert
        checkbox.IsChecked.Should().BeFalse();
    }

    [Fact]
    public void Checkbox_Should_Toggle()
    {
        // Arrange
        var checkbox = new Checkbox("Test");

        // Act
        checkbox.Toggle();

        // Assert
        checkbox.IsChecked.Should().BeTrue();
    }

    [Fact]
    public void Checkbox_Should_Fire_CheckedChanged_Event()
    {
        // Arrange
        var checkbox = new Checkbox("Test");
        var eventFired = false;
        var eventValue = false;

        checkbox.CheckedChanged += (value) =>
        {
            eventFired = true;
            eventValue = value;
        };

        // Act
        checkbox.IsChecked = true;

        // Assert
        eventFired.Should().BeTrue();
        eventValue.Should().BeTrue();
    }

    [Fact]
    public void Checkbox_Should_Not_Fire_Event_When_Value_Unchanged()
    {
        // Arrange
        var checkbox = new Checkbox("Test");
        var eventCount = 0;

        checkbox.CheckedChanged += (_) => eventCount++;

        // Act
        checkbox.IsChecked = false;
        checkbox.IsChecked = false;

        // Assert
        eventCount.Should().Be(0);
    }
}
