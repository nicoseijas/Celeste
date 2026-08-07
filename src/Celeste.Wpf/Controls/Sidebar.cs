using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Celeste.Wpf.Controls;

/// <summary>
/// A vertical navigation list: the column of destinations down the left edge of an application,
/// with an optional header, a pinned footer, and a collapsed rail state.
/// </summary>
/// <remarks>
/// <para>
/// The sidebar derives from <see cref="ListBox"/>, so
/// <see cref="System.Windows.Controls.Primitives.Selector.SelectedItem"/>,
/// <see cref="System.Windows.Controls.Primitives.Selector.SelectedIndex"/>,
/// <see cref="System.Windows.Controls.Primitives.Selector.SelectionChanged"/>, and item binding
/// all work as usual. Selection is the only navigation state the control owns: Celeste ships no
/// router, and the application decides what a selected item means.
/// </para>
/// <para>
/// The sidebar sets its own <see cref="FrameworkElement.Width"/> from
/// <see cref="ExpandedWidth"/> or <see cref="CollapsedWidth"/>, so setting <c>Width</c> on it
/// from the outside has no lasting effect. Put it in a fixed grid column instead if you need to
/// own the width yourself.
/// </para>
/// </remarks>
/// <example>
/// <code language="xml">
/// &lt;celeste:Sidebar Header="Acme" IsCollapsed="{Binding IsRail}" SelectedIndex="0"&gt;
///     &lt;celeste:SidebarGroupHeader Content="Workspace" /&gt;
///     &lt;celeste:SidebarItem Content="Overview" /&gt;
///     &lt;celeste:SidebarItem Content="Reports" Badge="3" /&gt;
/// &lt;/celeste:Sidebar&gt;
/// </code>
/// </example>
public class Sidebar : ListBox
{
    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(Sidebar),
            new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="Footer"/> dependency property.</summary>
    public static readonly DependencyProperty FooterProperty =
        DependencyProperty.Register(
            nameof(Footer),
            typeof(object),
            typeof(Sidebar),
            new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="IsCollapsed"/> dependency property.</summary>
    public static readonly DependencyProperty IsCollapsedProperty =
        DependencyProperty.Register(
            nameof(IsCollapsed),
            typeof(bool),
            typeof(Sidebar),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnPaneWidthSourceChanged));

    /// <summary>Identifies the <see cref="ExpandedWidth"/> dependency property.</summary>
    public static readonly DependencyProperty ExpandedWidthProperty =
        DependencyProperty.Register(
            nameof(ExpandedWidth),
            typeof(double),
            typeof(Sidebar),
            new PropertyMetadata(DefaultExpandedWidth, OnPaneWidthSourceChanged));

    /// <summary>Identifies the <see cref="CollapsedWidth"/> dependency property.</summary>
    public static readonly DependencyProperty CollapsedWidthProperty =
        DependencyProperty.Register(
            nameof(CollapsedWidth),
            typeof(double),
            typeof(Sidebar),
            new PropertyMetadata(DefaultCollapsedWidth, OnPaneWidthSourceChanged));

    /// <summary>Identifies the <see cref="IsToggleButtonVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsToggleButtonVisibleProperty =
        DependencyProperty.Register(
            nameof(IsToggleButtonVisible),
            typeof(bool),
            typeof(Sidebar),
            new PropertyMetadata(true));

    private const double DefaultExpandedWidth = 248d;
    private const double DefaultCollapsedWidth = 56d;

    // Matches Celeste.Duration.Fast. The width is animated from code rather than a storyboard
    // because the target value comes from a property, and a Storyboard cannot bind its To.
    private static readonly Duration CollapseDuration = new(TimeSpan.FromMilliseconds(150));

    static Sidebar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Sidebar), new FrameworkPropertyMetadata(typeof(Sidebar)));

        // A navigation list with two destinations selected at once has no meaning, and the
        // template draws exactly one selection indicator.
        SelectionModeProperty.OverrideMetadata(
            typeof(Sidebar),
            new FrameworkPropertyMetadata(SelectionMode.Single, null, static (_, _) => SelectionMode.Single));
    }

    /// <summary>Gets or sets the content shown above the items — usually a product name or logo.</summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>
    /// Gets or sets the content pinned below the items, where account and settings entries go.
    /// It stays at the bottom while the item list scrolls.
    /// </summary>
    public object? Footer
    {
        get => GetValue(FooterProperty);
        set => SetValue(FooterProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the sidebar is showing its collapsed rail: icons only, labels moved
    /// into tooltips. Binds two-way by default, so the application can drive the state from its
    /// own layout rules instead of the built-in toggle button.
    /// </summary>
    public bool IsCollapsed
    {
        get => (bool)GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    /// <summary>Gets or sets the width of the expanded sidebar. Defaults to 248.</summary>
    public double ExpandedWidth
    {
        get => (double)GetValue(ExpandedWidthProperty);
        set => SetValue(ExpandedWidthProperty, value);
    }

    /// <summary>Gets or sets the width of the collapsed rail. Defaults to 56.</summary>
    public double CollapsedWidth
    {
        get => (double)GetValue(CollapsedWidthProperty);
        set => SetValue(CollapsedWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the built-in collapse toggle is shown in the header.
    /// Defaults to <see langword="true"/>. Turn it off when the application drives
    /// <see cref="IsCollapsed"/> itself.
    /// </summary>
    public bool IsToggleButtonVisible
    {
        get => (bool)GetValue(IsToggleButtonVisibleProperty);
        set => SetValue(IsToggleButtonVisibleProperty, value);
    }

    /// <inheritdoc />
    protected override DependencyObject GetContainerForItemOverride() => new SidebarItem();

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new SidebarAutomationPeer(this);

    /// <inheritdoc />
    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        // The style has been applied by now, so ExpandedWidth holds its token value rather than
        // the property default. Snap rather than animate: nothing has been shown yet.
        UpdatePaneWidth(animate: false);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // OnInitialized does not run for every construction path, and a theme swap reapplies the
        // style. Either way the width has to agree with IsCollapsed before the first measure.
        UpdatePaneWidth(animate: false);
    }

    private static void OnPaneWidthSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((Sidebar)d).UpdatePaneWidth(animate: e.Property == IsCollapsedProperty);

    private void UpdatePaneWidth(bool animate)
    {
        double target = IsCollapsed ? CollapsedWidth : ExpandedWidth;

        if (!animate || !IsLoaded || !SystemParameters.ClientAreaAnimation)
        {
            BeginAnimation(WidthProperty, null);
            Width = target;
            return;
        }

        // Set the local value first and let the animation stop at the end (FillBehavior.Stop):
        // the property then falls back to the value already in place, so no clock is left holding
        // Width and there is no frame where the pane snaps back.
        double from = ActualWidth;
        Width = target;

        var animation = new DoubleAnimation(from, target, CollapseDuration)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            FillBehavior = FillBehavior.Stop,
        };

        BeginAnimation(WidthProperty, animation, HandoffBehavior.SnapshotAndReplace);
    }
}
