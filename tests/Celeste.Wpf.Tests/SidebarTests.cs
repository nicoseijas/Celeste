using System.Windows;
using System.Windows.Controls;
using Celeste.Wpf.Controls;
using Celeste.Wpf.Theming;
using Xunit;

namespace Celeste.Wpf.Tests;

/// <summary>
/// Behaviour the sidebar's templates cannot express on their own: the pane width follows the
/// collapsed state, plain items get sidebar containers, and the rail keeps the destinations
/// identifiable once their labels are gone.
/// </summary>
public class SidebarTests
{
    private static readonly string[] Destinations = ["Overview", "Reports"];

    [Fact]
    public void CollapsingSwitchesToTheRailWidthAndBack()
    {
        StaTestHost.Run(() =>
        {
            var sidebar = new Sidebar
            {
                ExpandedWidth = 200,
                CollapsedWidth = 48,
                Items = { new SidebarItem { Content = "Overview" } },
            };

            Realize(sidebar);
            Assert.Equal(200, sidebar.ActualWidth);

            sidebar.IsCollapsed = true;
            Realize(sidebar);
            Assert.Equal(48, sidebar.ActualWidth);

            sidebar.IsCollapsed = false;
            Realize(sidebar);
            Assert.Equal(200, sidebar.ActualWidth);
        });
    }

    [Fact]
    public void BoundItemsGetSidebarItemContainers()
    {
        StaTestHost.Run(() =>
        {
            var sidebar = new Sidebar { ItemsSource = Destinations };

            Realize(sidebar);

            Assert.IsType<SidebarItem>(sidebar.ItemContainerGenerator.ContainerFromIndex(0));
            Assert.IsType<SidebarItem>(sidebar.ItemContainerGenerator.ContainerFromIndex(1));
        });
    }

    [Fact]
    public void SelectionStaysSingleEvenWhenMultipleIsRequested()
    {
        StaTestHost.Run(() =>
        {
            var sidebar = new Sidebar { SelectionMode = SelectionMode.Multiple };

            Assert.Equal(SelectionMode.Single, sidebar.SelectionMode);
        });
    }

    [Fact]
    public void GroupHeadersAndSeparatorsStayOutOfTheKeyboardPath()
    {
        StaTestHost.Run(() =>
        {
            var header = new SidebarGroupHeader { Content = "Workspace" };
            var separator = new SidebarSeparator();

            // Focusable=false is what makes ListBox's arrow-key navigation step over them; without
            // IsHitTestVisible=false a click would still select the row underneath the caption.
            Assert.False(header.Focusable);
            Assert.False(header.IsHitTestVisible);
            Assert.False(separator.Focusable);
            Assert.False(separator.IsHitTestVisible);
        });
    }

    [Fact]
    public void TheRailEnablesItemTooltipsBecauseTheLabelsAreHidden()
    {
        StaTestHost.Run(() =>
        {
            var item = new SidebarItem { Content = "Overview" };
            var sidebar = new Sidebar { Items = { item } };

            Realize(sidebar);
            Assert.False(ToolTipService.GetIsEnabled(ItemRootOf(item)));

            sidebar.IsCollapsed = true;
            Realize(sidebar);

            FrameworkElement root = ItemRootOf(item);
            Assert.True(ToolTipService.GetIsEnabled(root));
            Assert.Equal("Overview", root.ToolTip);
        });
    }

    [Theory]
    [InlineData(ApplicationTheme.Light)]
    [InlineData(ApplicationTheme.Dark)]
    public void AFullSidebarRealizes(ApplicationTheme theme)
    {
        StaTestHost.Run(
            () =>
            {
                var sidebar = new Sidebar
                {
                    Header = "Acme",
                    Footer = new SidebarItem { Content = "Settings" },
                    Items =
                    {
                        new SidebarGroupHeader { Content = "Workspace" },
                        new SidebarItem { Content = "Overview", Badge = "3" },
                        new SidebarSeparator(),
                        new SidebarItem { Content = "Reports" },
                    },
                };

                Realize(sidebar);

                Assert.True(sidebar.ActualHeight > 0);
            },
            theme);
    }

    private static void Realize(FrameworkElement element)
    {
        // A parented element that has been measured and arranged has run its template, its item
        // container generator, and its triggers — which is everything under test here.
        FrameworkElement host = element.Parent as FrameworkElement ?? new Border { Child = element };

        host.Measure(new Size(600, 400));
        host.Arrange(new Rect(0, 0, 600, 400));
        host.UpdateLayout();
    }

    private static FrameworkElement ItemRootOf(SidebarItem item) =>
        (FrameworkElement)item.Template.FindName("Root", item);
}
