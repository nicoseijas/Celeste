using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Celeste.Wpf.Controls;

/// <summary>
/// Pairs a navigation pane with the content beside it, and decides how the pane is presented:
/// docked, collapsed to its rail, or off-canvas as an overlay that slides in over the content.
/// </summary>
/// <remarks>
/// <para>
/// The pane is normally a <see cref="Sidebar"/>, and the host drives its
/// <see cref="Sidebar.IsCollapsed"/> when the mode changes. Any content works, but only a
/// <see cref="Sidebar"/> gets the rail.
/// </para>
/// <para>
/// In <see cref="SidebarDisplayMode.OffCanvas"/> the open pane traps keyboard focus and hands it
/// back to the element that opened it. Clicking the scrim or pressing <c>Esc</c> closes it. An
/// overlay that leaves <c>Tab</c> walking through the content underneath is worse than no overlay.
/// </para>
/// <para>
/// Nothing in the host opens the pane on its own — an off-canvas pane needs a button somewhere in
/// the application's own chrome. Bind one to <see cref="TogglePaneCommand"/>; the command routes,
/// so the button has to sit inside the host.
/// </para>
/// </remarks>
/// <example>
/// <code language="xml">
/// &lt;celeste:SidebarHost&gt;
///     &lt;celeste:SidebarHost.Pane&gt;
///         &lt;celeste:Sidebar Header="Acme" SelectedIndex="0"&gt;
///             &lt;celeste:SidebarItem Content="Overview" /&gt;
///         &lt;/celeste:Sidebar&gt;
///     &lt;/celeste:SidebarHost.Pane&gt;
///
///     &lt;Button Command="{x:Static celeste:SidebarHost.TogglePaneCommand}" Content="Menu" /&gt;
/// &lt;/celeste:SidebarHost&gt;
/// </code>
/// </example>
[TemplatePart(Name = PanePartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = ScrimPartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = ContentPartName, Type = typeof(FrameworkElement))]
public class SidebarHost : ContentControl
{
    /// <summary>
    /// Opens the pane if it is closed and closes it if it is open. Only executable while
    /// <see cref="ActualDisplayMode"/> is <see cref="SidebarDisplayMode.OffCanvas"/>: in the other
    /// modes the pane is already on screen, so a button bound to this command disables itself.
    /// </summary>
    public static readonly RoutedCommand TogglePaneCommand =
        new(nameof(TogglePaneCommand), typeof(SidebarHost));

    /// <summary>Identifies the <see cref="Pane"/> dependency property.</summary>
    public static readonly DependencyProperty PaneProperty =
        DependencyProperty.Register(
            nameof(Pane),
            typeof(object),
            typeof(SidebarHost),
            new PropertyMetadata(null, OnPaneChanged));

    /// <summary>Identifies the <see cref="DisplayMode"/> dependency property.</summary>
    public static readonly DependencyProperty DisplayModeProperty =
        DependencyProperty.Register(
            nameof(DisplayMode),
            typeof(SidebarDisplayMode),
            typeof(SidebarHost),
            new PropertyMetadata(SidebarDisplayMode.Auto, OnDisplayModeSourceChanged));

    private static readonly DependencyPropertyKey ActualDisplayModePropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(ActualDisplayMode),
            typeof(SidebarDisplayMode),
            typeof(SidebarHost),
            new PropertyMetadata(SidebarDisplayMode.Docked, OnActualDisplayModeChanged));

    /// <summary>Identifies the <see cref="ActualDisplayMode"/> dependency property.</summary>
    public static readonly DependencyProperty ActualDisplayModeProperty =
        ActualDisplayModePropertyKey.DependencyProperty;

    /// <summary>Identifies the <see cref="IsPaneOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsPaneOpenProperty =
        DependencyProperty.Register(
            nameof(IsPaneOpen),
            typeof(bool),
            typeof(SidebarHost),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnIsPaneOpenChanged));

    /// <summary>Identifies the <see cref="RailBreakpoint"/> dependency property.</summary>
    public static readonly DependencyProperty RailBreakpointProperty =
        DependencyProperty.Register(
            nameof(RailBreakpoint),
            typeof(double),
            typeof(SidebarHost),
            new PropertyMetadata(DefaultRailBreakpoint, OnDisplayModeSourceChanged));

    /// <summary>Identifies the <see cref="OffCanvasBreakpoint"/> dependency property.</summary>
    public static readonly DependencyProperty OffCanvasBreakpointProperty =
        DependencyProperty.Register(
            nameof(OffCanvasBreakpoint),
            typeof(double),
            typeof(SidebarHost),
            new PropertyMetadata(DefaultOffCanvasBreakpoint, OnDisplayModeSourceChanged));

    private const string PanePartName = "PART_Pane";
    private const string ScrimPartName = "PART_Scrim";
    private const string ContentPartName = "PART_Content";

    private const double DefaultRailBreakpoint = 1008d;
    private const double DefaultOffCanvasBreakpoint = 640d;

    // Matches Celeste.Duration.Fast. Animated from code rather than a storyboard because the
    // slide distance is the pane's measured width, which a Storyboard cannot bind to.
    private static readonly Duration SlideDuration = new(TimeSpan.FromMilliseconds(150));

    private FrameworkElement? _pane;
    private FrameworkElement? _scrim;
    private FrameworkElement? _content;
    private TranslateTransform? _paneTransform;
    private IInputElement? _restoreFocusTo;

    // A close transition hides the pane when it finishes. If a newer transition started in the
    // meantime, the stale Completed handler must not touch anything.
    private int _transitionGeneration;

    static SidebarHost()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(SidebarHost),
            new FrameworkPropertyMetadata(typeof(SidebarHost)));

        CommandManager.RegisterClassCommandBinding(
            typeof(SidebarHost),
            new CommandBinding(TogglePaneCommand, OnTogglePaneExecuted, OnTogglePaneCanExecute));
    }

    /// <summary>
    /// Gets or sets the navigation pane, normally a <see cref="Sidebar"/>. The host's
    /// <see cref="ContentControl.Content"/> is everything beside it.
    /// </summary>
    public object? Pane
    {
        get => GetValue(PaneProperty);
        set => SetValue(PaneProperty, value);
    }

    /// <summary>
    /// Gets or sets how the pane is presented. Defaults to <see cref="SidebarDisplayMode.Auto"/>,
    /// which resolves against the host's width; set an explicit mode to drive it yourself and the
    /// breakpoints stop being consulted.
    /// </summary>
    public SidebarDisplayMode DisplayMode
    {
        get => (SidebarDisplayMode)GetValue(DisplayModeProperty);
        set => SetValue(DisplayModeProperty, value);
    }

    /// <summary>
    /// Gets the mode the pane is actually presented in — <see cref="DisplayMode"/> with
    /// <see cref="SidebarDisplayMode.Auto"/> already resolved. Bind to this rather than to
    /// <see cref="DisplayMode"/> when the application reacts to the presentation.
    /// </summary>
    public SidebarDisplayMode ActualDisplayMode => (SidebarDisplayMode)GetValue(ActualDisplayModeProperty);

    /// <summary>
    /// Gets or sets whether the off-canvas pane is showing. Binds two way by default. It has no
    /// effect in the other modes, where the pane is always on screen; leaving
    /// <see cref="SidebarDisplayMode.OffCanvas"/> closes it.
    /// </summary>
    public bool IsPaneOpen
    {
        get => (bool)GetValue(IsPaneOpenProperty);
        set => SetValue(IsPaneOpenProperty, value);
    }

    /// <summary>
    /// Gets or sets the width below which <see cref="SidebarDisplayMode.Auto"/> collapses the pane
    /// to its rail. Defaults to 1008.
    /// </summary>
    public double RailBreakpoint
    {
        get => (double)GetValue(RailBreakpointProperty);
        set => SetValue(RailBreakpointProperty, value);
    }

    /// <summary>
    /// Gets or sets the width below which <see cref="SidebarDisplayMode.Auto"/> takes the pane out
    /// of the layout. Defaults to 640.
    /// </summary>
    public double OffCanvasBreakpoint
    {
        get => (double)GetValue(OffCanvasBreakpointProperty);
        set => SetValue(OffCanvasBreakpointProperty, value);
    }

    /// <inheritdoc />
    /// <remarks>
    /// A group holding the pane and the content beside it. The pane reports itself: a
    /// <see cref="Sidebar"/> has its own peer. An open off-canvas pane is still not announced as
    /// modal, which UIA has no non-window way to express.
    /// </remarks>
    protected override AutomationPeer OnCreateAutomationPeer() =>
        new CelesteAutomationPeer(this, AutomationControlType.Group);

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        if (_scrim is not null)
        {
            _scrim.MouseLeftButtonDown -= OnScrimPressed;
        }

        base.OnApplyTemplate();

        _pane = GetTemplateChild(PanePartName) as FrameworkElement;
        _scrim = GetTemplateChild(ScrimPartName) as FrameworkElement;
        _content = GetTemplateChild(ContentPartName) as FrameworkElement;

        if (_scrim is not null)
        {
            _scrim.MouseLeftButtonDown += OnScrimPressed;
        }

        // The transform is created here rather than in the template because a Setter cannot target
        // a Freezable by name, and the slide has to animate one the control can reach.
        _paneTransform = new TranslateTransform();
        if (_pane is not null)
        {
            _pane.RenderTransform = _paneTransform;
        }

        UpdateActualDisplayMode();
        ApplyPaneState(animate: false);
    }

    /// <inheritdoc />
    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);

        if (sizeInfo.WidthChanged)
        {
            UpdateActualDisplayMode();
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape && IsPaneOpen && ActualDisplayMode == SidebarDisplayMode.OffCanvas)
        {
            IsPaneOpen = false;
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private static void OnTogglePaneExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        var host = (SidebarHost)sender;
        host.IsPaneOpen = !host.IsPaneOpen;
        e.Handled = true;
    }

    private static void OnTogglePaneCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = ((SidebarHost)sender).ActualDisplayMode == SidebarDisplayMode.OffCanvas;
        e.Handled = true;
    }

    private static void OnPaneChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var host = (SidebarHost)d;

        if (host._pane is not null && host._paneTransform is not null)
        {
            host._pane.RenderTransform = host._paneTransform;
        }

        host.SyncPaneCollapsedState();
        host.ApplyPaneState(animate: false);
    }

    private static void OnDisplayModeSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((SidebarHost)d).UpdateActualDisplayMode();

    private static void OnActualDisplayModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var host = (SidebarHost)d;
        host.SyncPaneCollapsedState();

        // A pane that is no longer an overlay is on screen whatever IsPaneOpen said.
        if (host.ActualDisplayMode != SidebarDisplayMode.OffCanvas)
        {
            host.SetContentReachableByTab(true);
            host.IsPaneOpen = false;
        }

        host.ApplyPaneState(animate: false);
    }

    private static void OnIsPaneOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((SidebarHost)d).OnIsPaneOpenChanged((bool)e.NewValue);

    private void OnIsPaneOpenChanged(bool isOpen)
    {
        if (ActualDisplayMode != SidebarDisplayMode.OffCanvas)
        {
            ApplyPaneState(animate: false);
            return;
        }

        if (isOpen)
        {
            _restoreFocusTo = Keyboard.FocusedElement;
        }

        ApplyPaneState(animate: true);
        SetContentReachableByTab(!isOpen);

        if (isOpen)
        {
            // The pane has just been made visible; it can only take focus once layout has run.
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(FocusPane));
        }
        else
        {
            RestoreFocus();
        }
    }

    private void FocusPane()
    {
        if (IsPaneOpen)
        {
            _pane?.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
        }
    }

    private void RestoreFocus()
    {
        IInputElement? target = _restoreFocusTo;
        _restoreFocusTo = null;

        // Focus is somewhere inside a pane that is on its way off screen, so it has to move
        // whether or not the original element is still around to take it.
        if (target is UIElement { IsVisible: true, Focusable: true, IsEnabled: true })
        {
            target.Focus();
        }
        else
        {
            _content?.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
        }
    }

    private void SetContentReachableByTab(bool reachable)
    {
        if (_content is not null)
        {
            KeyboardNavigation.SetTabNavigation(
                _content,
                reachable ? KeyboardNavigationMode.Continue : KeyboardNavigationMode.None);
        }
    }

    private void SyncPaneCollapsedState()
    {
        if (Pane is Sidebar sidebar)
        {
            sidebar.IsCollapsed = ActualDisplayMode == SidebarDisplayMode.Rail;
        }
    }

    private void UpdateActualDisplayMode()
    {
        SidebarDisplayMode mode = DisplayMode;

        if (mode == SidebarDisplayMode.Auto)
        {
            double width = ActualWidth;

            // Before the first measure there is no width to judge by. Docked is the mode a
            // desktop window lands in, and the first size change corrects it either way.
            mode = width <= 0 ? SidebarDisplayMode.Docked
                : width < OffCanvasBreakpoint ? SidebarDisplayMode.OffCanvas
                : width < RailBreakpoint ? SidebarDisplayMode.Rail
                : SidebarDisplayMode.Docked;
        }

        SetValue(ActualDisplayModePropertyKey, mode);
    }

    private void OnScrimPressed(object sender, MouseButtonEventArgs e)
    {
        IsPaneOpen = false;
        e.Handled = true;
    }

    private void ApplyPaneState(bool animate)
    {
        if (_pane is null || _scrim is null || _paneTransform is null)
        {
            return;
        }

        bool overlay = ActualDisplayMode == SidebarDisplayMode.OffCanvas;
        bool open = !overlay || IsPaneOpen;

        // Hidden rather than Collapsed while closed: the pane keeps being measured, so the slide
        // distance is its real width instead of a guess, and it stays out of the tab order.
        double distance = _pane.ActualWidth;

        unchecked
        {
            _transitionGeneration++;
        }

        _paneTransform.BeginAnimation(TranslateTransform.XProperty, null);
        _scrim.BeginAnimation(OpacityProperty, null);

        if (!animate || !overlay || !IsLoaded || !SystemParameters.ClientAreaAnimation || distance <= 0)
        {
            _pane.Visibility = open ? Visibility.Visible : Visibility.Hidden;
            _paneTransform.X = open ? 0 : -distance;
            _scrim.Visibility = overlay && open ? Visibility.Visible : Visibility.Collapsed;
            _scrim.Opacity = open ? 1 : 0;
            return;
        }

        int generation = _transitionGeneration;

        _pane.Visibility = Visibility.Visible;
        _scrim.Visibility = Visibility.Visible;

        // Set the local values first and stop the animations at the end (FillBehavior.Stop), so no
        // clock is left holding X or Opacity and there is no frame where the pane snaps back.
        double fromX = _paneTransform.X;
        double toX = open ? 0 : -distance;
        double fromOpacity = _scrim.Opacity;
        double toOpacity = open ? 1 : 0;

        _paneTransform.X = toX;
        _scrim.Opacity = toOpacity;

        var slide = new DoubleAnimation(fromX, toX, SlideDuration)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            FillBehavior = FillBehavior.Stop,
        };

        if (!open)
        {
            slide.Completed += (_, _) =>
            {
                if (generation != _transitionGeneration)
                {
                    return;
                }

                _pane.Visibility = Visibility.Hidden;
                _scrim.Visibility = Visibility.Collapsed;
            };
        }

        _paneTransform.BeginAnimation(TranslateTransform.XProperty, slide, HandoffBehavior.SnapshotAndReplace);
        _scrim.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(fromOpacity, toOpacity, SlideDuration) { FillBehavior = FillBehavior.Stop },
            HandoffBehavior.SnapshotAndReplace);
    }
}
