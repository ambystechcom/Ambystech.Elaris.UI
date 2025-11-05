using System.Drawing;
using Ambystech.Elaris.UI;
using Ambystech.Elaris.UI.Core;
using Ambystech.Elaris.UI.Enums;
using Ambystech.Elaris.UI.Widgets.Display;
using Ambystech.Elaris.UI.Widgets.Input;
using Ambystech.Elaris.UI.Widgets.Layout;
using Ambystech.Elaris.UI.Widgets.Layout.Responsive;

var app = new Application();

var statusBar = new StatusBar
{
    Height = 1,
    LeftText = "Ready",
    CenterText = "Tabs Demo - Use Arrow Keys to Switch Tabs",
    RightText = "ESC to exit"
};

var tabContainer = new TabContainer
{
    BackgroundColor = Color.Black,
    TabBarBackgroundColor = ColorHelper.FromRgb(30, 30, 30),
    ActiveTabBackgroundColor = ColorHelper.FromRgb(0, 120, 212),
    ActiveTabForegroundColor = Color.White,
    InactiveTabBackgroundColor = ColorHelper.FromRgb(60, 60, 60),
    InactiveTabForegroundColor = ColorHelper.FromRgb(180, 180, 180)
};

tabContainer.TabChanged += (currentTab, previousTab, currentIndex, previousIndex) =>
{
    statusBar.LeftText = $"Active: {currentTab.Title}";
};

var dashboardTab = tabContainer.AddTab("Dashboard");
dashboardTab.LayoutMode = LayoutMode.Fill;
dashboardTab.BackgroundColor = Color.Black;

var dashboardFrame = new Frame("System Dashboard")
{
    X = 0,
    Y = 0,
    Width = 50,
    Height = 12,
    BorderStyle = BorderStyle.Rounded,
    ForegroundColor = ColorHelper.FromRgb(100, 200, 255)
};

var cpuLabel = new Label("CPU Usage: 42%")
{
    X = 2,
    Y = 2,
    Width = 45,
    Height = 1,
    ForegroundColor = Color.White
};

var cpuProgress = new ProgressBar
{
    X = 2,
    Y = 3,
    Width = 45,
    Height = 1,
    Value = 42,
    ShowPercentage = false,
    FilledColor = ColorHelper.FromRgb(100, 200, 100)
};

var memLabel = new Label("Memory: 8.2 GB / 16 GB")
{
    X = 2,
    Y = 5,
    Width = 45,
    Height = 1,
    ForegroundColor = Color.White
};

var memProgress = new ProgressBar
{
    X = 2,
    Y = 6,
    Width = 45,
    Height = 1,
    Value = 51,
    ShowPercentage = false,
    FilledColor = ColorHelper.FromRgb(255, 200, 100)
};

var diskLabel = new Label("Disk: 245 GB / 512 GB")
{
    X = 2,
    Y = 8,
    Width = 45,
    Height = 1,
    ForegroundColor = Color.White
};

var diskProgress = new ProgressBar
{
    X = 2,
    Y = 9,
    Width = 45,
    Height = 1,
    Value = 48,
    ShowPercentage = false,
    FilledColor = ColorHelper.FromRgb(100, 150, 255)
};

dashboardFrame.Add(cpuLabel);
dashboardFrame.Add(cpuProgress);
dashboardFrame.Add(memLabel);
dashboardFrame.Add(memProgress);
dashboardFrame.Add(diskLabel);
dashboardFrame.Add(diskProgress);
dashboardTab.Add(dashboardFrame);

var actionsTab = tabContainer.AddTab("Quick Actions");
actionsTab.LayoutMode = LayoutMode.Fill;
actionsTab.BackgroundColor = Color.Black;

var actionsFrame = new Frame("Available Actions")
{
    X = 0,
    Y = 0,
    Width = 70,
    Height = 14,
    BorderStyle = BorderStyle.Single,
    ForegroundColor = ColorHelper.FromRgb(150, 150, 200)
};

var clearRamButton = new Button("Clear RAM")
{
    X = 2,
    Y = 2,
    Width = 20,
    Height = 3,
    Style = ButtonStyle.Rounded,
    BackgroundColor = ColorHelper.FromRgb(60, 120, 180),
    ForegroundColor = Color.White
};
clearRamButton.Click += () => statusBar.CenterText = "Clearing RAM...";

var flushDnsButton = new Button("Flush DNS")
{
    X = 24,
    Y = 2,
    Width = 20,
    Height = 3,
    Style = ButtonStyle.Rounded,
    BackgroundColor = ColorHelper.FromRgb(60, 120, 180),
    ForegroundColor = Color.White
};
flushDnsButton.Click += () => statusBar.CenterText = "Flushing DNS cache...";

var resetNetworkButton = new Button("Reset Network")
{
    X = 46,
    Y = 2,
    Width = 20,
    Height = 3,
    Style = ButtonStyle.Rounded,
    BackgroundColor = ColorHelper.FromRgb(60, 120, 180),
    ForegroundColor = Color.White
};
resetNetworkButton.Click += () => statusBar.CenterText = "Resetting network...";

var fixBluetoothButton = new Button("Fix Bluetooth")
{
    X = 2,
    Y = 6,
    Width = 20,
    Height = 3,
    Style = ButtonStyle.Rounded,
    BackgroundColor = ColorHelper.FromRgb(80, 160, 100),
    ForegroundColor = Color.White
};
fixBluetoothButton.Click += () => statusBar.CenterText = "Fixing Bluetooth...";

var fixAudioButton = new Button("Fix Audio")
{
    X = 24,
    Y = 6,
    Width = 20,
    Height = 3,
    Style = ButtonStyle.Rounded,
    BackgroundColor = ColorHelper.FromRgb(80, 160, 100),
    ForegroundColor = Color.White
};
fixAudioButton.Click += () => statusBar.CenterText = "Fixing audio...";

var rebuildButton = new Button("Rebuild Launch")
{
    X = 46,
    Y = 6,
    Width = 20,
    Height = 3,
    Style = ButtonStyle.Rounded,
    BackgroundColor = ColorHelper.FromRgb(80, 160, 100),
    ForegroundColor = Color.White
};
rebuildButton.Click += () => statusBar.CenterText = "Rebuilding launch services...";

actionsFrame.Add(clearRamButton);
actionsFrame.Add(flushDnsButton);
actionsFrame.Add(resetNetworkButton);
actionsFrame.Add(fixBluetoothButton);
actionsFrame.Add(fixAudioButton);
actionsFrame.Add(rebuildButton);
actionsTab.Add(actionsFrame);

var cleanupTab = tabContainer.AddTab("Cleanup");
cleanupTab.LayoutMode = LayoutMode.Fill;
cleanupTab.BackgroundColor = Color.Black;

var cleanupFrame = new Frame("Select Items to Clean")
{
    X = 0,
    Y = 0,
    Width = 50,
    Height = 16,
    BorderStyle = BorderStyle.Single,
    ForegroundColor = ColorHelper.FromRgb(200, 150, 100)
};

var cacheCheckbox = new Checkbox("User Caches (52.57 MB)")
{
    X = 2,
    Y = 2,
    Width = 40,
    Height = 1,
    ForegroundColor = Color.White
};

var trashCheckbox = new Checkbox("Trash (0 B)")
{
    X = 2,
    Y = 3,
    Width = 40,
    Height = 1,
    ForegroundColor = Color.White
};

var homebrewCheckbox = new Checkbox("Homebrew Cache (0 B)")
{
    X = 2,
    Y = 4,
    Width = 40,
    Height = 1,
    ForegroundColor = Color.White
};

var npmCheckbox = new Checkbox("npm Cache (12.00 KB)")
{
    X = 2,
    Y = 5,
    Width = 40,
    Height = 1,
    ForegroundColor = Color.White
};

var yarnCheckbox = new Checkbox("Yarn Cache (0 B)")
{
    X = 2,
    Y = 6,
    Width = 40,
    Height = 1,
    ForegroundColor = Color.White
};

var goBuildCheckbox = new Checkbox("Go Build Cache (193.54 MB)")
{
    X = 2,
    Y = 7,
    Width = 40,
    Height = 1,
    ForegroundColor = Color.White
};

var totalLabel = new Label("Total to clean: 0 B")
{
    X = 2,
    Y = 9,
    Width = 40,
    Height = 1,
    ForegroundColor = Color.Yellow,
    Bold = true
};

var cleanButton = new Button("Clean Selected")
{
    X = 2,
    Y = 11,
    Width = 20,
    Height = 3,
    Style = ButtonStyle.Gradient,
    GradientStartColor = ColorHelper.FromRgb(200, 80, 80),
    GradientEndColor = ColorHelper.FromRgb(150, 40, 40),
    ForegroundColor = Color.White
};
cleanButton.Click += () => statusBar.CenterText = "Cleaning selected items...";

cleanupFrame.Add(cacheCheckbox);
cleanupFrame.Add(trashCheckbox);
cleanupFrame.Add(homebrewCheckbox);
cleanupFrame.Add(npmCheckbox);
cleanupFrame.Add(yarnCheckbox);
cleanupFrame.Add(goBuildCheckbox);
cleanupFrame.Add(totalLabel);
cleanupFrame.Add(cleanButton);
cleanupTab.Add(cleanupFrame);

var helpTab = tabContainer.AddTab("Help");
helpTab.LayoutMode = LayoutMode.Fill;
helpTab.BackgroundColor = Color.Black;

var helpFrame = new Frame("Help & Information")
{
    X = 0,
    Y = 0,
    Width = 70,
    Height = 18,
    BorderStyle = BorderStyle.Rounded,
    ForegroundColor = ColorHelper.FromRgb(150, 200, 150)
};

var helpText = new Label(@"Welcome to Elaris Tabs Demo!

This demo showcases the TabContainer widget for creating
tabbed interfaces in terminal applications.

Navigation:
  • Left/Right Arrow: Switch between tabs
  • Home/End: Jump to first/last tab
  • Tab: Navigate to next tab (or focus next widget)
  • Shift+Tab: Navigate to previous tab

Features:
  ✓ Clean tab bar rendering at top
  ✓ Active tab highlighting
  ✓ Keyboard navigation
  ✓ Content area per tab
  ✓ Supports all widget types
  ✓ Event-driven architecture

Try switching between tabs and interacting with widgets!")
{
    X = 2,
    Y = 2,
    Width = 65,
    Height = 14,
    ForegroundColor = Color.White,
    HorizontalAlignment = HorizontalAlignment.Left,
    VerticalAlignment = VerticalAlignment.Top
};

helpFrame.Add(helpText);
helpTab.Add(helpFrame);

var root = new ResponsiveContainer
{
    BackgroundColor = Color.Black
};

root.Add(tabContainer);
root.Add(statusBar);

root.OnResize((width, height) =>
{
    tabContainer.X = 0;
    tabContainer.Y = 0;
    tabContainer.Width = width;
    tabContainer.Height = height - 1;

    statusBar.X = 0;
    statusBar.Y = height - 1;
    statusBar.Width = width;

});

app.InitialFocusWidget = tabContainer;

try
{
    Console.WriteLine("Starting Elaris Tabs Demo...");
    Console.WriteLine("Press ESC or Ctrl+C to exit");
    Console.WriteLine("Use Arrow Keys to switch between tabs!");
    Thread.Sleep(1500);

    app.Run(root);
}
catch (Exception ex)
{
    Console.WriteLine($"\nError: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}
