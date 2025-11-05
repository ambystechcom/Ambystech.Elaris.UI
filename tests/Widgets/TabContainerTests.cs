using Ambystech.Elaris.UI.Widgets.Layout;
using FluentAssertions;

namespace Ambystech.Elaris.UI.Tests.Widgets;

public class TabContainerTests
{
    [Fact]
    public void TabContainer_Should_Initialize_Empty()
    {
        // Arrange & Act
        var tabContainer = new TabContainer();

        // Assert
        tabContainer.Tabs.Should().BeEmpty();
        tabContainer.ActiveTabIndex.Should().Be(-1);
        tabContainer.ActiveTab.Should().BeNull();
    }

    [Fact]
    public void AddTab_Should_Create_Tab_With_Content()
    {
        // Arrange
        var tabContainer = new TabContainer();

        // Act
        var content = tabContainer.AddTab("Dashboard");

        // Assert
        tabContainer.Tabs.Should().HaveCount(1);
        tabContainer.Tabs[0].Title.Should().Be("Dashboard");
        tabContainer.Tabs[0].Content.Should().Be(content);
        content.ParentTab.Should().Be(tabContainer.Tabs[0]);
    }

    [Fact]
    public void AddTab_Should_Auto_Activate_First_Tab()
    {
        // Arrange
        var tabContainer = new TabContainer();

        // Act
        var content = tabContainer.AddTab("First");

        // Assert
        tabContainer.ActiveTabIndex.Should().Be(0);
        tabContainer.ActiveTab.Should().Be(tabContainer.Tabs[0]);
        content.Visible.Should().BeTrue();
    }

    [Fact]
    public void AddTab_Should_Keep_Subsequent_Tabs_Hidden()
    {
        // Arrange
        var tabContainer = new TabContainer();

        // Act
        var content1 = tabContainer.AddTab("First");
        var content2 = tabContainer.AddTab("Second");
        var content3 = tabContainer.AddTab("Third");

        // Assert
        content1.Visible.Should().BeTrue();
        content2.Visible.Should().BeFalse();
        content3.Visible.Should().BeFalse();
    }

    [Fact]
    public void ActiveTabIndex_Should_Switch_Visible_Content()
    {
        // Arrange
        var tabContainer = new TabContainer();
        var content1 = tabContainer.AddTab("First");
        var content2 = tabContainer.AddTab("Second");

        // Act
        tabContainer.ActiveTabIndex = 1;

        // Assert
        content1.Visible.Should().BeFalse();
        content2.Visible.Should().BeTrue();
        tabContainer.ActiveTab.Should().Be(tabContainer.Tabs[1]);
    }

    [Fact]
    public void TabChanged_Event_Should_Fire_When_Switching_Tabs()
    {
        // Arrange
        var tabContainer = new TabContainer();
        tabContainer.AddTab("First");
        tabContainer.AddTab("Second");

        Tab? currentTab = null;
        Tab? previousTab = null;
        int currentIndex = -1;
        int previousIndex = -1;

        tabContainer.TabChanged += (current, prev, currIdx, prevIdx) =>
        {
            currentTab = current;
            previousTab = prev;
            currentIndex = currIdx;
            previousIndex = prevIdx;
        };

        // Act
        tabContainer.ActiveTabIndex = 1;

        // Assert
        currentTab.Should().Be(tabContainer.Tabs[1]);
        previousTab.Should().Be(tabContainer.Tabs[0]);
        currentIndex.Should().Be(1);
        previousIndex.Should().Be(0);
    }

    [Fact]
    public void RemoveTab_Should_Update_Active_Tab()
    {
        // Arrange
        var tabContainer = new TabContainer();
        tabContainer.AddTab("First");
        tabContainer.AddTab("Second");
        tabContainer.AddTab("Third");
        tabContainer.ActiveTabIndex = 1;

        // Act
        tabContainer.RemoveTabAt(1);

        // Assert
        tabContainer.Tabs.Should().HaveCount(2);
        tabContainer.ActiveTabIndex.Should().Be(1);
        tabContainer.ActiveTab?.Title.Should().Be("Third");
    }
}
