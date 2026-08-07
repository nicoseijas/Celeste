using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
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

    /// <summary>
    /// An icon has to be painted, and has to keep following the item once hover or selection
    /// changes the foreground, or it vanishes against the accent fill.
    /// </summary>
    /// <remarks>
    /// This guards the contract, not the defect that produced it. Icons once went unpainted in a
    /// real application because the style resolved the colour by walking up to the owning item: an
    /// icon is the value of <see cref="SidebarItem.Icon"/>, so it is styled while still detached,
    /// the ancestor walk finds nothing, and the binding never recovers. This host resolves that
    /// same binding, whichever construction order the test uses, so the failure was confirmed and
    /// the fix verified against the running gallery instead.
    /// </remarks>
    [Fact]
    public void AnItemIconIsPaintedAndFollowsTheItemsForeground()
    {
        StaTestHost.Run(() =>
        {
            var icon = new Path
            {
                Data = Geometry.Parse("M1,1 H17 V17 H1 Z"),
                Style = (Style)Application.Current.FindResource("Celeste.SidebarItem.Icon"),
            };

            // Styled before it has any parent, the order XAML uses.
            Assert.Null(icon.Parent);

            var item = new SidebarItem { Content = "Overview", Icon = icon };
            var sidebar = new Sidebar { Items = { item } };

            Realize(sidebar);
            Assert.NotNull(icon.Stroke);
            Brush idle = icon.Stroke;

            item.IsSelected = true;
            Realize(sidebar);

            Assert.NotEqual(idle, icon.Stroke);
            Assert.Equal(((SolidColorBrush)item.Foreground).Color, ((SolidColorBrush)icon.Stroke).Color);
        });
    }

    [Fact]
    public void TheRailSwapsTheHeaderForTheMark()
    {
        StaTestHost.Run(() =>
        {
            var sidebar = new Sidebar
            {
                Header = "Acme",
                CollapsedHeader = new Border { Width = 20, Height = 20 },
                Items = { new SidebarItem { Content = "Overview" } },
            };

            Realize(sidebar);
            Assert.Equal(Visibility.Visible, PartOf(sidebar, "HeaderContent").Visibility);
            Assert.Equal(Visibility.Collapsed, PartOf(sidebar, "CollapsedHeaderContent").Visibility);

            sidebar.IsCollapsed = true;
            Realize(sidebar);

            // A wordmark squeezed into a 56px column is unreadable, so the rail shows the mark
            // instead of scaling the header down.
            Assert.Equal(Visibility.Collapsed, PartOf(sidebar, "HeaderContent").Visibility);
            Assert.Equal(Visibility.Visible, PartOf(sidebar, "CollapsedHeaderContent").Visibility);
        });
    }

    [Fact]
    public void TheRailDropsTheHeaderBandWhenNothingIsLeftToPutInIt()
    {
        StaTestHost.Run(() =>
        {
            var sidebar = new Sidebar
            {
                Header = "Acme",
                IsToggleButtonVisible = false,
                Items = { new SidebarItem { Content = "Overview" } },
            };

            Realize(sidebar);
            Assert.Equal(Visibility.Visible, PartOf(sidebar, "HeaderArea").Visibility);

            sidebar.IsCollapsed = true;
            Realize(sidebar);

            // The expanded header is hidden rather than removed, so without this the rail keeps an
            // empty band above the first destination.
            Assert.Equal(Visibility.Collapsed, PartOf(sidebar, "HeaderArea").Visibility);
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

    private static FrameworkElement PartOf(Sidebar sidebar, string partName) =>
        (FrameworkElement)sidebar.Template.FindName(partName, sidebar);

    private static FrameworkElement ItemRootOf(SidebarItem item) =>
        (FrameworkElement)item.Template.FindName("Root", item);
}
