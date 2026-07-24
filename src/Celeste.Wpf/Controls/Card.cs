using System.Windows;
using System.Windows.Controls;

namespace Celeste.Wpf.Controls;

/// <summary>
/// A raised surface that groups related content, with an optional header, description, and footer.
/// </summary>
/// <example>
/// <code language="xml">
/// &lt;celeste:Card Header="Billing" Description="Manage your subscription."&gt;
///     &lt;StackPanel&gt;...&lt;/StackPanel&gt;
/// &lt;/celeste:Card&gt;
/// </code>
/// </example>
public class Card : HeaderedContentControl
{
    /// <summary>Identifies the <see cref="Description"/> dependency property.</summary>
    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(
            nameof(Description),
            typeof(object),
            typeof(Card),
            new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="Footer"/> dependency property.</summary>
    public static readonly DependencyProperty FooterProperty =
        DependencyProperty.Register(
            nameof(Footer),
            typeof(object),
            typeof(Card),
            new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="Elevation"/> dependency property.</summary>
    public static readonly DependencyProperty ElevationProperty =
        DependencyProperty.Register(
            nameof(Elevation),
            typeof(CardElevation),
            typeof(Card),
            new PropertyMetadata(CardElevation.Flat));

    static Card() =>
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Card), new FrameworkPropertyMetadata(typeof(Card)));

    /// <summary>Gets or sets the secondary line rendered under the header.</summary>
    public object? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <summary>Gets or sets the content rendered in the card's footer bar.</summary>
    public object? Footer
    {
        get => GetValue(FooterProperty);
        set => SetValue(FooterProperty, value);
    }

    /// <summary>Gets or sets how far the card lifts off the page. Defaults to <see cref="CardElevation.Flat"/>.</summary>
    public CardElevation Elevation
    {
        get => (CardElevation)GetValue(ElevationProperty);
        set => SetValue(ElevationProperty, value);
    }
}
