using System.Windows;
using System.Windows.Controls;

namespace Celeste.Wpf.Controls;

/// <summary>
/// A round portrait for a person or an account, with initials standing in whenever the picture is
/// missing, still loading, or unavailable.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Initials"/> is shown as given. Celeste does not derive it from a name: which letters
/// abbreviate a person is a question about their language and their name order, and a two-letter
/// rule that reads well in one script mangles another.
/// </para>
/// <para>
/// The picture goes through <see cref="ImageView"/>, so it is loaded and decoded off the UI thread
/// and a broken URI degrades to the initials instead of an empty circle.
/// </para>
/// </remarks>
/// <example>
/// <code language="xml">
/// &lt;celeste:Avatar Source="https://example.com/me.png" Initials="NS" Size="Large" /&gt;
/// </code>
/// </example>
public class Avatar : Control
{
    /// <summary>Identifies the <see cref="Source"/> dependency property.</summary>
    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register(
            nameof(Source),
            typeof(Uri),
            typeof(Avatar),
            new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="Initials"/> dependency property.</summary>
    public static readonly DependencyProperty InitialsProperty =
        DependencyProperty.Register(
            nameof(Initials),
            typeof(string),
            typeof(Avatar),
            new PropertyMetadata(string.Empty));

    /// <summary>Identifies the <see cref="Size"/> dependency property.</summary>
    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(
            nameof(Size),
            typeof(AvatarSize),
            typeof(Avatar),
            new PropertyMetadata(AvatarSize.Medium));

    static Avatar() =>
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Avatar), new FrameworkPropertyMetadata(typeof(Avatar)));

    /// <summary>
    /// Gets or sets the portrait. Relative URIs are resolved against the element's base URI. With no
    /// source, or a source that fails, the avatar shows <see cref="Initials"/>.
    /// </summary>
    public Uri? Source
    {
        get => (Uri?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>Gets or sets the letters shown when there is no picture. Rendered exactly as given.</summary>
    public string Initials
    {
        get => (string)GetValue(InitialsProperty);
        set => SetValue(InitialsProperty, value);
    }

    /// <summary>Gets or sets the diameter. Defaults to <see cref="AvatarSize.Medium"/>.</summary>
    public AvatarSize Size
    {
        get => (AvatarSize)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }
}
