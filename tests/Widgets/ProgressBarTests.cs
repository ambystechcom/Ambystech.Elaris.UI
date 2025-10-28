using Ambystech.Elaris.UI.Widgets.Display;
using FluentAssertions;

namespace Ambystech.Elaris.UI.Tests.Widgets;

public class ProgressBarTests
{
    [Fact]
    public void ProgressBar_Should_Initialize_At_Zero()
    {
        // Arrange & Act
        var progressBar = new ProgressBar();

        // Assert
        progressBar.Value.Should().Be(0);
        progressBar.Minimum.Should().Be(0);
        progressBar.Maximum.Should().Be(100);
    }

    [Fact]
    public void ProgressBar_Should_Set_Value()
    {
        // Arrange
        var progressBar = new ProgressBar();

        // Act
        progressBar.Value = 50;

        // Assert
        progressBar.Value.Should().Be(50);
    }

    [Fact]
    public void ProgressBar_Should_Clamp_Value_To_Range()
    {
        // Arrange
        var progressBar = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100
        };

        // Act
        progressBar.Value = 150;

        // Assert
        progressBar.Value.Should().Be(100);
    }

    [Fact]
    public void ProgressBar_Should_Increment()
    {
        // Arrange
        var progressBar = new ProgressBar { Value = 25 };

        // Act
        progressBar.Increment(10);

        // Assert
        progressBar.Value.Should().Be(35);
    }

    [Fact]
    public void ProgressBar_Should_Decrement()
    {
        // Arrange
        var progressBar = new ProgressBar { Value = 50 };

        // Act
        progressBar.Decrement(15);

        // Assert
        progressBar.Value.Should().Be(35);
    }

    [Fact]
    public void ProgressBar_Should_Fire_ValueChanged_Event()
    {
        // Arrange
        var progressBar = new ProgressBar();
        var eventFired = false;
        var newValue = 0.0;

        progressBar.ValueChanged += (value) =>
        {
            eventFired = true;
            newValue = value;
        };

        // Act
        progressBar.Value = 75;

        // Assert
        eventFired.Should().BeTrue();
        newValue.Should().Be(75);
    }

    [Fact]
    public void ProgressBar_Should_Set_Percentage()
    {
        // Arrange
        var progressBar = new ProgressBar
        {
            Minimum = 0,
            Maximum = 200
        };

        // Act
        progressBar.SetPercentage(50);

        // Assert
        progressBar.Value.Should().Be(100); // 50% of 200
    }
}
