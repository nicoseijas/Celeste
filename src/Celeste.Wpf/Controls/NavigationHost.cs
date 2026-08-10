using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Celeste.Wpf.Controls;

/// <summary>
/// Holds the page a <see cref="Sidebar"/> selected, keeps a back stack, and restores each page's
/// scroll position when the user returns to it.
/// </summary>
/// <remarks>
/// <para>
/// The host does not navigate. It watches <see cref="ContentControl.Content"/> and records where
/// the application has been, so binding the content to a selection is all that is needed and
/// Celeste never becomes the router:
/// </para>
/// <code language="xml">
/// &lt;celeste:NavigationHost Content="{Binding SelectedItem, ElementName=Nav, Mode=TwoWay}" /&gt;
/// </code>
/// <para>
/// Bind it two ways when the pages come from a selection. <see cref="GoBack"/> writes the previous
/// page back into <see cref="ContentControl.Content"/>, and a one-way binding would leave the
/// sidebar highlighting a page the host is no longer showing.
/// </para>
/// <para>
/// Back is <see cref="NavigationCommands.BrowseBack"/> rather than a command of Celeste's own, so
/// the gestures Windows already assigns to it work without the application wiring anything. The
/// command routes, so the button has to sit inside the host.
/// </para>
/// </remarks>
[TemplatePart(Name = ScrollViewerPartName, Type = typeof(ScrollViewer))]
public class NavigationHost : ContentControl
{
    /// <summary>Identifies the <see cref="MaxBackStackDepth"/> dependency property.</summary>
    public static readonly DependencyProperty MaxBackStackDepthProperty =
        DependencyProperty.Register(
            nameof(MaxBackStackDepth),
            typeof(int),
            typeof(NavigationHost),
            new PropertyMetadata(DefaultMaxBackStackDepth, OnMaxBackStackDepthChanged),
            static depth => depth is int value && value >= 0);

    /// <summary>Identifies the <see cref="IsScrollEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty IsScrollEnabledProperty =
        DependencyProperty.Register(
            nameof(IsScrollEnabled),
            typeof(bool),
            typeof(NavigationHost),
            new PropertyMetadata(true));

    private static readonly DependencyPropertyKey CanGoBackPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(CanGoBack),
            typeof(bool),
            typeof(NavigationHost),
            new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="CanGoBack"/> dependency property.</summary>
    public static readonly DependencyProperty CanGoBackProperty = CanGoBackPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey BackStackDepthPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(BackStackDepth),
            typeof(int),
            typeof(NavigationHost),
            new PropertyMetadata(0));

    /// <summary>Identifies the <see cref="BackStackDepth"/> dependency property.</summary>
    public static readonly DependencyProperty BackStackDepthProperty = BackStackDepthPropertyKey.DependencyProperty;

    private const string ScrollViewerPartName = "PART_ScrollViewer";
    private const int DefaultMaxBackStackDepth = 10;

    private readonly List<object> _backStack = [];

    // Keyed by reference: two pages that compare equal are still two places the user has been, and
    // a page rebuilt on every visit legitimately starts at the top.
    //
    // Never holds more than the back stack plus the page on screen. An entry arrives with a push,
    // and leaves when its page falls off the bottom of the stack or is left behind by going back.
    // That bound is what makes MaxBackStackDepth a bound on retained pages rather than only on the
    // length of a list.
    private readonly Dictionary<object, ScrollOffset> _scrollOffsets = new(ReferenceEqualityComparer.Instance);

    private ScrollViewer? _scrollViewer;

    // GoBack writes to Content, which comes straight back through OnContentChanged. Without this
    // the page being returned to would be pushed onto the stack it just came off.
    private bool _isGoingBack;

    static NavigationHost()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NavigationHost),
            new FrameworkPropertyMetadata(typeof(NavigationHost)));

        CommandManager.RegisterClassCommandBinding(
            typeof(NavigationHost),
            new CommandBinding(NavigationCommands.BrowseBack, OnBrowseBackExecuted, OnBrowseBackCanExecute));
    }

    /// <summary>
    /// Gets whether there is a page to go back to.
    /// </summary>
    public bool CanGoBack => (bool)GetValue(CanGoBackProperty);

    /// <summary>
    /// Gets how many pages are on the back stack.
    /// </summary>
    public int BackStackDepth => (int)GetValue(BackStackDepthProperty);

    /// <summary>
    /// Gets or sets how many pages the back stack keeps. Defaults to 10; the oldest entry is
    /// dropped once the stack is full, along with the scroll position remembered for it.
    /// </summary>
    /// <remarks>
    /// An unbounded stack is an unbounded reference to every page an application has shown, which
    /// in a session that runs for days is a leak. Set it to 0 to keep no history at all.
    /// </remarks>
    public int MaxBackStackDepth
    {
        get => (int)GetValue(MaxBackStackDepthProperty);
        set => SetValue(MaxBackStackDepthProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the host scrolls its content. Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// Turn it off when the pages scroll themselves, otherwise they end up inside a second
    /// scrolling region. Scroll positions are then no longer restored, because the host is no
    /// longer the thing that scrolls.
    /// </remarks>
    public bool IsScrollEnabled
    {
        get => (bool)GetValue(IsScrollEnabledProperty);
        set => SetValue(IsScrollEnabledProperty, value);
    }

    /// <summary>
    /// Shows the previous page and restores where it was scrolled to.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if a page was shown, <see langword="false"/> if the stack was empty.
    /// </returns>
    public bool GoBack()
    {
        if (_backStack.Count == 0)
        {
            return false;
        }

        object previous = _backStack[^1];
        _backStack.RemoveAt(_backStack.Count - 1);

        // The page being left is not remembered, and anything remembered about it is dropped: there
        // is no forward stack, so nothing can return to it, and reaching it again from a selection
        // is forward navigation, which starts at the top. Keeping its offset would be keeping the
        // page itself, which is how the host would hold every page a long session ever showed.
        Forget(Content, keeping: previous);

        _isGoingBack = true;
        try
        {
            Content = previous;
        }
        finally
        {
            _isGoingBack = false;
        }

        UpdateBackStackProperties();
        RestoreScrollOffset(previous);
        return true;
    }

    /// <summary>
    /// Drops the back stack and everything remembered about where its pages were scrolled to.
    /// The page on screen is left alone.
    /// </summary>
    public void ClearHistory()
    {
        _backStack.Clear();
        _scrollOffsets.Clear();
        UpdateBackStackProperties();
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _scrollViewer = GetTemplateChild(ScrollViewerPartName) as ScrollViewer;
    }

    /// <inheritdoc />
    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);

        if (_isGoingBack || ReferenceEquals(oldContent, newContent))
        {
            return;
        }

        if (oldContent is not null)
        {
            RememberScrollOffset(oldContent);
            _backStack.Add(oldContent);
            TrimBackStack();
            UpdateBackStackProperties();
        }

        // Forward navigation is a fresh look at a page, so it starts at the top. Only GoBack
        // restores a position, which is the distinction between following a link and returning.
        _scrollViewer?.ScrollToTop();
        _scrollViewer?.ScrollToLeftEnd();
    }

    private static void OnBrowseBackExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        ((NavigationHost)sender).GoBack();
        e.Handled = true;
    }

    private static void OnBrowseBackCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = ((NavigationHost)sender).CanGoBack;
        e.Handled = true;
    }

    private static void OnMaxBackStackDepthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var host = (NavigationHost)d;
        host.TrimBackStack();
        host.UpdateBackStackProperties();
    }

    private void TrimBackStack()
    {
        int excess = _backStack.Count - MaxBackStackDepth;

        if (excess <= 0)
        {
            return;
        }

        for (int i = 0; i < excess; i++)
        {
            _scrollOffsets.Remove(_backStack[i]);
        }

        _backStack.RemoveRange(0, excess);
    }

    private void UpdateBackStackProperties()
    {
        SetValue(BackStackDepthPropertyKey, _backStack.Count);
        SetValue(CanGoBackPropertyKey, _backStack.Count > 0);

        // CanGoBack drives BrowseBack's CanExecute, and nothing else tells the command manager.
        CommandManager.InvalidateRequerySuggested();
    }

    /// <summary>
    /// Drops what was remembered about <paramref name="page"/>, unless it is the page being returned
    /// to — the stack top is never the current content, but a caller should not have to rely on it.
    /// </summary>
    private void Forget(object? page, object keeping)
    {
        if (page is not null && !ReferenceEquals(page, keeping))
        {
            _scrollOffsets.Remove(page);
        }
    }

    private void RememberScrollOffset(object? page)
    {
        if (page is not null && _scrollViewer is not null)
        {
            _scrollOffsets[page] = new ScrollOffset(_scrollViewer.HorizontalOffset, _scrollViewer.VerticalOffset);
        }
    }

    private void RestoreScrollOffset(object page)
    {
        if (_scrollViewer is null || !_scrollOffsets.TryGetValue(page, out ScrollOffset offset))
        {
            return;
        }

        // The page has only just been given to the presenter; until it has been measured the
        // scrollable extent is still the previous page's, and an offset past it would be clamped.
        Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            new Action(() =>
            {
                if (_scrollViewer is null || !ReferenceEquals(Content, page))
                {
                    return;
                }

                _scrollViewer.UpdateLayout();
                _scrollViewer.ScrollToHorizontalOffset(offset.Horizontal);
                _scrollViewer.ScrollToVerticalOffset(offset.Vertical);
            }));
    }

    private readonly record struct ScrollOffset(double Horizontal, double Vertical);
}
