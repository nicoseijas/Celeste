using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using Celeste.Wpf.Media;

namespace Celeste.Wpf.Controls;

/// <summary>
/// Shows a picture from a URI: loaded and decoded off the UI thread, clipped to the control's corner
/// radius, with a placeholder while it arrives and a visible failure when it does not.
/// </summary>
/// <remarks>
/// <para>
/// <b>An <see cref="ImageView"/> takes the width its layout gives it</b> and derives its height from
/// the aspect ratio — the one in <see cref="AspectRatio"/>, or the picture's own once it is known.
/// That is what makes it usable in a <see cref="MasonryPanel"/> column, where the width is the
/// column and the height has to come from the image. For a picture that should be its own natural
/// size instead, set <see cref="FrameworkElement.Width"/> and
/// <see cref="FrameworkElement.Height"/>, or use a plain <see cref="Image"/>.
/// </para>
/// <para>
/// Until the size is known the control reserves a square, so a grid of tiles does not collapse to
/// nothing and then jump. Setting <see cref="AspectRatio"/> removes that reflow entirely, which is
/// worth doing whenever the ratio is known in advance.
/// </para>
/// <para>
/// The load starts once the control has been laid out, so the decode can be sized to the width the
/// picture will be shown at rather than the file's full resolution. The width is read once: a
/// control that grows later keeps the bitmap it decoded.
/// </para>
/// <para>
/// A failure sets <see cref="State"/> to <see cref="ImageState.Failed"/> and raises
/// <see cref="ImageFailed"/>. Nothing is written to a log by the library.
/// </para>
/// </remarks>
/// <example>
/// <code language="xml">
/// &lt;celeste:ImageView Source="https://example.com/cover.jpg"
///                    AspectRatio="1.5"
///                    Stretch="UniformToFill" /&gt;
/// </code>
/// </example>
public class ImageView : Control
{
    /// <summary>Identifies the <see cref="Source"/> dependency property.</summary>
    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register(
            nameof(Source),
            typeof(Uri),
            typeof(ImageView),
            new PropertyMetadata(null, OnSourceChanged));

    /// <summary>Identifies the <see cref="Stretch"/> dependency property.</summary>
    public static readonly DependencyProperty StretchProperty =
        DependencyProperty.Register(
            nameof(Stretch),
            typeof(Stretch),
            typeof(ImageView),
            new FrameworkPropertyMetadata(Stretch.UniformToFill, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="AspectRatio"/> dependency property.</summary>
    public static readonly DependencyProperty AspectRatioProperty =
        DependencyProperty.Register(
            nameof(AspectRatio),
            typeof(double),
            typeof(ImageView),
            new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsMeasure),
            IsValidAspectRatio);

    /// <summary>Identifies the <see cref="DecodeWidth"/> dependency property.</summary>
    public static readonly DependencyProperty DecodeWidthProperty =
        DependencyProperty.Register(
            nameof(DecodeWidth),
            typeof(double),
            typeof(ImageView),
            new PropertyMetadata(double.NaN));

    /// <summary>Identifies the <see cref="Placeholder"/> dependency property.</summary>
    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.Register(
            nameof(Placeholder),
            typeof(object),
            typeof(ImageView),
            new PropertyMetadata(null));

    private static readonly DependencyPropertyKey StatePropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(State),
            typeof(ImageState),
            typeof(ImageView),
            new PropertyMetadata(ImageState.None));

    /// <summary>Identifies the <see cref="State"/> dependency property.</summary>
    public static readonly DependencyProperty StateProperty = StatePropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey DecodedImagePropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(DecodedImage),
            typeof(ImageSource),
            typeof(ImageView),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>Identifies the <see cref="DecodedImage"/> dependency property.</summary>
    public static readonly DependencyProperty DecodedImageProperty = DecodedImagePropertyKey.DependencyProperty;

    /// <summary>The ratio a tile reserves while nothing better is known.</summary>
    private const double UnknownAspectRatio = 1d;

    /// <summary>What a relative URI resolves against when the element has no base URI of its own.</summary>
    private static readonly Uri ApplicationBaseUri = new("pack://application:,,,/");

    // A superseded load must not write its result. Same shape as SidebarHost's transition
    // generation: the download itself is shared with other views, so it is abandoned, not cancelled.
    private int _loadGeneration;
    private bool _loadPending;

    static ImageView() =>
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageView), new FrameworkPropertyMetadata(typeof(ImageView)));

    /// <summary>Initializes a new instance of the <see cref="ImageView"/> class.</summary>
    public ImageView() => Loaded += OnLoaded;

    /// <summary>Raised when the source could not be loaded or decoded.</summary>
    public event EventHandler<ImageFailedEventArgs>? ImageFailed;

    /// <summary>
    /// Gets or sets the picture to show. Relative URIs are resolved against the element's base URI,
    /// so <c>Assets/cover.png</c> works the same as it does on <see cref="Image"/>.
    /// </summary>
    public Uri? Source
    {
        get => (Uri?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>
    /// Gets or sets how the picture fills the control. Defaults to
    /// <see cref="System.Windows.Media.Stretch.UniformToFill"/>, which crops rather than distorts —
    /// the right default for a tile, since the control's own shape comes from its layout.
    /// </summary>
    public Stretch Stretch
    {
        get => (Stretch)GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    /// <summary>
    /// Gets or sets the width-to-height ratio the control measures to. <see cref="double.NaN"/>, the
    /// default, means the picture's own ratio, and a square until that is known.
    /// </summary>
    public double AspectRatio
    {
        get => (double)GetValue(AspectRatioProperty);
        set => SetValue(AspectRatioProperty, value);
    }

    /// <summary>
    /// Gets or sets the width to decode to, in device-independent pixels.
    /// <see cref="double.NaN"/>, the default, means the width the control was laid out at. A value
    /// wider than the picture never upscales the decode.
    /// </summary>
    public double DecodeWidth
    {
        get => (double)GetValue(DecodeWidthProperty);
        set => SetValue(DecodeWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the content shown while there is no picture — during the load, and after a
    /// failure. The default template shows a sunken surface, and a glyph once the load has failed.
    /// </summary>
    public object? Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>Gets how far along the load is.</summary>
    public ImageState State => (ImageState)GetValue(StateProperty);

    /// <summary>
    /// Gets the decoded picture, or <see langword="null"/> while there is none. Frozen, and shared
    /// with every other view that asked for the same URI at the same size.
    /// </summary>
    public ImageSource? DecodedImage => (ImageSource?)GetValue(DecodedImageProperty);

    /// <inheritdoc />
    /// <remarks>
    /// An image, with no name unless the application sets <c>AutomationProperties.Name</c>. That is
    /// deliberate: a picture with no alternative text is either decorative, in which case an unnamed
    /// image is right, or it is missing one, and inventing a name from the file would hide that.
    /// </remarks>
    protected override AutomationPeer OnCreateAutomationPeer() =>
        new CelesteAutomationPeer(this, AutomationControlType.Image);

    /// <inheritdoc />
    protected override Size MeasureOverride(Size constraint)
    {
        Size desired = base.MeasureOverride(constraint);
        double ratio = LayoutAspectRatio;

        // With no width to work from there is no height to derive. The control then measures to
        // whatever its placeholder wants, which for the default template is nothing.
        if (double.IsNaN(ratio) || double.IsInfinity(constraint.Width) || constraint.Width <= 0)
        {
            return desired;
        }

        return new Size(constraint.Width, constraint.Width / ratio);
    }

    private static bool IsValidAspectRatio(object value)
    {
        double ratio = (double)value;
        return double.IsNaN(ratio) || (ratio > 0 && !double.IsInfinity(ratio));
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((ImageView)d).OnSourceChanged((Uri?)e.NewValue);

    /// <summary>
    /// The ratio the control measures to: the explicit one, the picture's own, or a square while
    /// neither is available.
    /// </summary>
    private double LayoutAspectRatio
    {
        get
        {
            double requested = AspectRatio;
            if (!double.IsNaN(requested))
            {
                return requested;
            }

            // Width and Height rather than PixelWidth and PixelHeight: an image with non-square
            // pixels or its own DPI is the size it says it is, not the size of its pixel grid.
            if (DecodedImage is { Width: > 0, Height: > 0 } image)
            {
                return image.Width / image.Height;
            }

            return UnknownAspectRatio;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_loadPending)
        {
            BeginLoad();
        }
    }

    private void OnSourceChanged(Uri? source)
    {
        // Abandon whatever the previous source was doing before showing anything of the new one.
        unchecked
        {
            _loadGeneration++;
        }

        SetValue(DecodedImagePropertyKey, null);

        if (source is null)
        {
            _loadPending = false;
            SetValue(StatePropertyKey, ImageState.None);
            return;
        }

        SetValue(StatePropertyKey, ImageState.Loading);
        _loadPending = true;

        // Before the first layout pass there is no width to decode against, so the load waits for
        // Loaded rather than decoding the file at full resolution.
        if (IsLoaded)
        {
            BeginLoad();
        }
    }

    private void BeginLoad()
    {
        _loadPending = false;

        Uri? source = Source;
        if (source is null)
        {
            return;
        }

        Uri absolute;
        try
        {
            absolute = source.IsAbsoluteUri
                ? source
                : new Uri(BaseUriHelper.GetBaseUri(this) ?? ApplicationBaseUri, source);
        }
        catch (UriFormatException exception)
        {
            Fail(source, exception, _loadGeneration);
            return;
        }

        _ = LoadAsync(absolute, DecodePixelWidth(), _loadGeneration);
    }

    /// <summary>
    /// Awaited on the UI thread, so the result lands back on it. Every failure is handled here:
    /// nothing this returns is ever observed.
    /// </summary>
    private async Task LoadAsync(Uri source, int decodePixelWidth, int generation)
    {
        try
        {
            ImageSource image = await ImageLoader.LoadAsync(source, decodePixelWidth).ConfigureAwait(true);

            if (generation != _loadGeneration)
            {
                return;
            }

            SetValue(DecodedImagePropertyKey, image);
            SetValue(StatePropertyKey, ImageState.Loaded);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // Any reason a URI does not become a picture — bad scheme, 404, unreadable file,
            // unknown format, oversized payload — is the same failure to the control. The
            // application gets the exception through ImageFailed.
            Fail(source, exception, generation);
        }
    }

    private void Fail(Uri source, Exception exception, int generation)
    {
        if (generation != _loadGeneration)
        {
            return;
        }

        SetValue(StatePropertyKey, ImageState.Failed);
        ImageFailed?.Invoke(this, new ImageFailedEventArgs(source, exception));
    }

    /// <summary>
    /// The decode width in physical pixels: the explicit <see cref="DecodeWidth"/>, else the width
    /// the control was laid out at, scaled for the monitor. Zero asks for the full resolution.
    /// </summary>
    private int DecodePixelWidth()
    {
        double width = DecodeWidth;

        if (double.IsNaN(width) || width <= 0)
        {
            width = ActualWidth;
        }

        if (double.IsNaN(width) || width <= 0 || double.IsInfinity(width))
        {
            return 0;
        }

        double scale = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1d;
        if (scale <= 0)
        {
            scale = 1d;
        }

        return (int)Math.Ceiling(width * scale);
    }
}
