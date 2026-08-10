using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Celeste.Wpf.Controls;
using Xunit;

namespace Celeste.Wpf.Tests;

/// <summary>
/// The host's whole job is what it remembers: where the application has been, and where each of
/// those pages was scrolled to.
/// </summary>
public class NavigationHostTests
{
    [Fact]
    public void ChangingTheContentPushesThePreviousPage()
    {
        StaTestHost.Run(() =>
        {
            var host = new NavigationHost();
            Realize(host);

            // The first page is not a place to go back to; nothing preceded it.
            host.Content = "Overview";
            Assert.False(host.CanGoBack);
            Assert.Equal(0, host.BackStackDepth);

            host.Content = "Reports";
            Assert.True(host.CanGoBack);
            Assert.Equal(1, host.BackStackDepth);
        });
    }

    [Fact]
    public void GoingBackShowsThePreviousPageWithoutStackingIt()
    {
        StaTestHost.Run(() =>
        {
            var host = new NavigationHost();
            Realize(host);

            host.Content = "Overview";
            host.Content = "Reports";

            Assert.True(host.GoBack());
            Assert.Equal("Overview", host.Content);

            // Going back is not a navigation of its own, or the two pages would trade places for
            // ever and Back would never run out.
            Assert.False(host.CanGoBack);
            Assert.Equal(0, host.BackStackDepth);
        });
    }

    [Fact]
    public void GoingBackWithNothingBehindIsRefused()
    {
        StaTestHost.Run(() =>
        {
            var host = new NavigationHost { Content = "Overview" };
            Realize(host);

            Assert.False(host.GoBack());
            Assert.Equal("Overview", host.Content);
        });
    }

    [Fact]
    public void TheStackStopsAtItsLimitAndDropsTheOldest()
    {
        StaTestHost.Run(() =>
        {
            var host = new NavigationHost { MaxBackStackDepth = 2 };
            Realize(host);

            host.Content = "A";
            host.Content = "B";
            host.Content = "C";
            host.Content = "D";

            Assert.Equal(2, host.BackStackDepth);

            host.GoBack();
            Assert.Equal("C", host.Content);
            host.GoBack();
            Assert.Equal("B", host.Content);

            // "A" fell off the bottom rather than the stack growing without end.
            Assert.False(host.CanGoBack);
        });
    }

    [Fact]
    public void LoweringTheLimitTrimsAStackThatIsAlreadyTooDeep()
    {
        StaTestHost.Run(() =>
        {
            var host = new NavigationHost();
            Realize(host);

            host.Content = "A";
            host.Content = "B";
            host.Content = "C";
            Assert.Equal(2, host.BackStackDepth);

            host.MaxBackStackDepth = 1;
            Assert.Equal(1, host.BackStackDepth);

            host.GoBack();
            Assert.Equal("B", host.Content);
        });
    }

    [Fact]
    public void ClearingHistoryLeavesThePageOnScreen()
    {
        StaTestHost.Run(() =>
        {
            var host = new NavigationHost();
            Realize(host);

            host.Content = "Overview";
            host.Content = "Reports";

            host.ClearHistory();

            Assert.Equal("Reports", host.Content);
            Assert.False(host.CanGoBack);
            Assert.Equal(0, host.BackStackDepth);
        });
    }

    [Fact]
    public void BackIsTheStandardBrowseBackCommand()
    {
        StaTestHost.Run(() =>
        {
            var host = new NavigationHost();
            Realize(host);
            host.Content = "Overview";

            Assert.False(NavigationCommands.BrowseBack.CanExecute(null, host));

            host.Content = "Reports";
            Assert.True(NavigationCommands.BrowseBack.CanExecute(null, host));

            NavigationCommands.BrowseBack.Execute(null, host);
            Assert.Equal("Overview", host.Content);
        });
    }

    [Fact]
    public void ReturningToAPageRestoresWhereItWasScrolledTo()
    {
        StaTestHost.Run(() =>
        {
            var host = new NavigationHost { Padding = new Thickness(0) };
            Realize(host);

            var tall = new Border { Height = 2000, Width = 100 };
            host.Content = tall;
            Realize(host);

            ScrollViewer scroller = ScrollViewerOf(host);
            scroller.ScrollToVerticalOffset(500);
            scroller.UpdateLayout();
            Assert.Equal(500, scroller.VerticalOffset);

            host.Content = new Border { Height = 2000, Width = 100 };
            Realize(host);

            // Forward is a fresh look at a page, so it starts at the top.
            Assert.Equal(0, scroller.VerticalOffset);

            GoBackAndSettle(host);

            Assert.Same(tall, host.Content);
            Assert.Equal(500, scroller.VerticalOffset);
        });
    }

    [Fact]
    public void EachPageKeepsItsOwnScrollPosition()
    {
        StaTestHost.Run(() =>
        {
            var host = new NavigationHost { Padding = new Thickness(0) };
            Realize(host);
            ScrollViewer scroller = ScrollViewerOf(host);

            var first = new Border { Height = 2000, Width = 100 };
            var second = new Border { Height = 2000, Width = 100 };

            host.Content = first;
            Realize(host);
            scroller.ScrollToVerticalOffset(300);
            scroller.UpdateLayout();

            host.Content = second;
            Realize(host);
            scroller.ScrollToVerticalOffset(700);
            scroller.UpdateLayout();

            host.Content = new Border { Height = 2000, Width = 100 };
            Realize(host);

            // One remembered offset for the whole host would give both pages the same answer.
            GoBackAndSettle(host);
            Assert.Same(second, host.Content);
            Assert.Equal(700, scroller.VerticalOffset);

            GoBackAndSettle(host);
            Assert.Same(first, host.Content);
            Assert.Equal(300, scroller.VerticalOffset);
        });
    }

    /// <summary>
    /// <see cref="NavigationHost.MaxBackStackDepth"/> has to bound the pages the host holds, not only
    /// the length of a list. Nothing can return to a page left behind by going back — there is no
    /// forward stack, and reaching it again from a selection starts at the top — so remembering where
    /// it was scrolled to would keep the page, and its whole visual tree, for as long as the host.
    /// </summary>
    [Fact]
    public void GoingBackDoesNotHoldOnToThePagesItLeaves()
    {
        StaTestHost.Run(() =>
        {
            var host = new NavigationHost { Padding = new Thickness(0) };
            Realize(host);

            var first = new Border { Height = 2000, Width = 100 };
            host.Content = first;
            Realize(host);

            (WeakReference second, WeakReference third) = VisitTwoMorePages(host);

            GoBackAndSettle(host);
            GoBackAndSettle(host);

            Assert.Same(first, host.Content);
            Assert.Equal(0, host.BackStackDepth);

            Collect();

            Assert.False(second.IsAlive, "The host is still holding the second page.");
            Assert.False(third.IsAlive, "The host is still holding the third page.");
        });
    }

    /// <summary>
    /// Its own method so that the pages it visits have no local variable left pointing at them when
    /// it returns, which would keep them alive whatever the host does.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference Second, WeakReference Third) VisitTwoMorePages(NavigationHost host)
    {
        var second = new Border { Height = 2000, Width = 100 };
        host.Content = second;
        Realize(host);

        // Somewhere other than the top, so the host has a position worth remembering for it.
        ScrollViewerOf(host).ScrollToVerticalOffset(200);
        Realize(host);

        var third = new Border { Height = 2000, Width = 100 };
        host.Content = third;
        Realize(host);

        return (new WeakReference(second), new WeakReference(third));
    }

    private static void Collect()
    {
        // The deferred scroll restore captures the page it is restoring, so the dispatcher queue has
        // to drain before anything the host let go of is actually unreachable.
        Dispatcher.CurrentDispatcher.Invoke(static () => { }, DispatcherPriority.SystemIdle);

        for (int attempt = 0; attempt < 3; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    private static void Realize(FrameworkElement element)
    {
        FrameworkElement host = element.Parent as FrameworkElement ?? new Border { Child = element };

        host.Measure(new Size(400, 300));
        host.Arrange(new Rect(0, 0, 400, 300));
        host.UpdateLayout();
    }

    /// <summary>
    /// Goes back and lets the deferred scroll restore run. The restore waits for layout, because
    /// until the new page has been measured the scrollable extent is still the old page's.
    /// </summary>
    private static void GoBackAndSettle(NavigationHost host)
    {
        host.GoBack();
        Realize(host);
        Dispatcher.CurrentDispatcher.Invoke(static () => { }, DispatcherPriority.Loaded);
        Realize(host);
    }

    private static ScrollViewer ScrollViewerOf(NavigationHost host) =>
        (ScrollViewer)host.Template.FindName("PART_ScrollViewer", host);
}
