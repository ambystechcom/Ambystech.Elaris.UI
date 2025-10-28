using Ambystech.Elaris.UI.Widgets.Input;
using FluentAssertions;

namespace Ambystech.Elaris.UI.Tests.Widgets;

public class ButtonTests
{
    [Fact]
    public void Button_Should_Initialize_With_Text()
    {
        // Arrange & Act
        var button = new Button("Click Me");

        // Assert
        button.Text.Should().Be("Click Me");
    }

    [Fact]
    public void Button_Should_Fire_Click_Event()
    {
        // Arrange
        var button = new Button("Test");
        var clickCount = 0;
        button.Click += () => clickCount++;

        // Act
        button.PerformClick();

        // Assert
        clickCount.Should().Be(1);
    }
}
