using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Celeste.Wpf.Controls;
using Xunit;

namespace Celeste.Wpf.Tests;

/// <summary>
/// The rules the host owns rather than the template: which presentation a width resolves to, and
/// what an off-canvas pane does to the content it covers.
/// </summary>
public class SidebarHostTests
{
    [Theory]
    [InlineData(1200, SidebarDisplayMode.Docked)]
    [InlineData(800, SidebarDisplayMode.Rail)]
    [InlineData(480, SidebarDisplayMode.OffCanvas)]
    public void AutoPicksThePresentationFromTheHostWidth(double width, SidebarDisplayMode expected)
    {
        StaTestHost.Run(() =>
        {
            var host = new SidebarHost { Pane = new Sidebar() };

            Realize(host, width);

            Assert.Equal(expected, host.ActualDisplayMode);
        });
    }

    [Fact]
    public void AnExplicitModeIgnoresTheBreakpoints()
    {
        StaTestHost.Run(() =>
        {
            var host = new SidebarHost { DisplayMode = SidebarDisplayMode.Docked, Pane = new Sidebar() };

            Border root = Realize(host, 400);
            Assert.Equal(SidebarDisplayMode.Docked, host.ActualDisplayMode);

            Resize(root, 1200);
            Assert.Equal(SidebarDisplayMode.Docked, host.ActualDisplayMode);
        });
    }

    [Fact]
    public void TheRailPresentationCollapsesTheSidebar()
    {
        StaTestHost.Run(() =>
        {
            var sidebar = new Sidebar { Items = { new SidebarItem { Content = "Overview" } } };
            var host = new SidebarHost { Pane = sidebar };

            Border root = Realize(host, 1200);
            Assert.False(sidebar.IsCollapsed);

            Resize(root, 800);
            Assert.True(sidebar.IsCollapsed);

            // Off-canvas shows the full pane: there is no space to save once it is out of the layout.
            Resize(root, 480);
            Assert.False(sidebar.IsCollapsed);
        });
    }

    [Fact]
    public void GrowingPastTheBreakpointClosesTheOverlay()
    {
        StaTestHost.Run(() =>
        {
            var host = new SidebarHost { Pane = new Sidebar() };

            Border root = Realize(host, 480);
            host.IsPaneOpen = true;

            Resize(root, 1200);

            // The pane is on screen again as part of the layout, so an "open overlay" it is not.
            Assert.False(host.IsPaneOpen);
        });
    }

    [Fact]
    public void TheScrimOnlyExistsWhileTheOverlayIsOpen()
    {
        StaTestHost.Run(() =>
        {
            var host = new SidebarHost { Pane = new Sidebar() };
            Border root = Realize(host, 480);

            // Collapsed rather than transparent: a scrim that is merely invisible still swallows
            // every click meant for the content underneath it.
            Assert.Equal(Visibility.Collapsed, PartOf(host, "PART_Scrim").Visibility);

            host.IsPaneOpen = true;
            root.UpdateLayout();
            Assert.Equal(Visibility.Visible, PartOf(host, "PART_Scrim").Visibility);

            host.IsPaneOpen = false;
            root.UpdateLayout();
            Assert.Equal(Visibility.Collapsed, PartOf(host, "PART_Scrim").Visibility);
        });
    }

    [Fact]
    public void TheClosedOverlayKeepsItsWidthSoItCanSlideIn()
    {
        StaTestHost.Run(() =>
        {
            var host = new SidebarHost { Pane = new Sidebar { ExpandedWidth = 200 } };
            Realize(host, 480);

            FrameworkElement pane = PartOf(host, "PART_Pane");

            // Hidden, not Collapsed: a collapsed pane measures to nothing, and the slide distance
            // is the pane's own width.
            Assert.Equal(Visibility.Hidden, pane.Visibility);
            Assert.Equal(200, pane.ActualWidth);
        });
    }

    [Fact]
    public void TheOpenOverlayTakesTheContentOutOfTheTabOrder()
    {
        StaTestHost.Run(() =>
        {
            var host = new SidebarHost { Pane = new Sidebar(), Content = new Button { Content = "Save" } };
            Border root = Realize(host, 480);

            FrameworkElement content = PartOf(host, "PART_Content");
            Assert.Equal(KeyboardNavigationMode.Continue, KeyboardNavigation.GetTabNavigation(content));

            host.IsPaneOpen = true;
            root.UpdateLayout();

            // Tab must not walk through content the user cannot see or click.
            Assert.Equal(KeyboardNavigationMode.None, KeyboardNavigation.GetTabNavigation(content));

            host.IsPaneOpen = false;
            root.UpdateLayout();
            Assert.Equal(KeyboardNavigationMode.Continue, KeyboardNavigation.GetTabNavigation(content));
        });
    }

    [Theory]
    [InlineData(480, true)]
    [InlineData(1200, false)]
    public void TheToggleCommandOnlyRunsWhereThereIsSomethingToToggle(double width, bool expected)
    {
        StaTestHost.Run(() =>
        {
            var host = new SidebarHost { Pane = new Sidebar() };

            Realize(host, width);

            Assert.Equal(expected, SidebarHost.TogglePaneCommand.CanExecute(null, host));
        });
    }

    /// <summary>
    /// The presentation follows the window, not just a container the test resized by hand: a host
    /// that only re-reads its width on load would stay docked in a window the user narrowed.
    /// </summary>
    [Fact]
    public void ResizingTheWindowChangesThePresentation()
    {
        StaTestHost.Run(() =>
        {
            var host = new SidebarHost { Pane = new Sidebar() };
            var window = new Window
            {
                WindowStartupLocation = WindowStartupLocation.Manual,
                ShowInTaskbar = false,
                Left = -4000,
                Top = -4000,
                Width = 1200,
                Height = 400,
                Content = host,
            };

            try
            {
                window.Show();
                window.UpdateLayout();
                Assert.Equal(SidebarDisplayMode.Docked, host.ActualDisplayMode);

                window.Width = 800;
                window.UpdateLayout();
                Assert.Equal(SidebarDisplayMode.Rail, host.ActualDisplayMode);

                window.Width = 480;
                window.UpdateLayout();
                Assert.Equal(SidebarDisplayMode.OffCanvas, host.ActualDisplayMode);

                window.Width = 1200;
                window.UpdateLayout();
                Assert.Equal(SidebarDisplayMode.Docked, host.ActualDisplayMode);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void EscapeClosesTheOverlay()
    {
        StaTestHost.Run(() =>
        {
            var host = new SidebarHost { Pane = new Sidebar(), Content = new Button { Content = "Save" } };

            // KeyEventArgs needs a real PresentationSource, so this one runs in a window.
            var window = new Window
            {
                WindowStartupLocation = WindowStartupLocation.Manual,
                ShowInTaskbar = false,
                Left = -4000,
                Top = -4000,
                Width = 480,
                Height = 400,
                Content = host,
            };

            try
            {
                window.Show();
                window.UpdateLayout();

                Assert.Equal(SidebarDisplayMode.OffCanvas, host.ActualDisplayMode);
                host.IsPaneOpen = true;

                host.RaiseEvent(new KeyEventArgs(
                    Keyboard.PrimaryDevice,
                    PresentationSource.FromVisual(window),
                    0,
                    Key.Escape)
                {
                    RoutedEvent = Keyboard.KeyDownEvent,
                });

                Assert.False(host.IsPaneOpen);
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static Border Realize(SidebarHost host, double width)
    {
        var root = new Border { Child = host };

        Resize(root, width);
        return root;
    }

    private static void Resize(Border root, double width)
    {
        root.Width = width;
        root.Height = 400;
        root.Measure(new Size(width, 400));
        root.Arrange(new Rect(0, 0, width, 400));
        root.UpdateLayout();
    }

    private static FrameworkElement PartOf(SidebarHost host, string partName) =>
        (FrameworkElement)host.Template.FindName(partName, host);
}
