using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Celeste.Wpf.Controls;

/// <summary>
/// A small pill that labels or counts something: a status, a tag, an unread total.
/// </summary>
public class Badge : ContentControl
{
    /// <summary>Identifies the <see cref="Variant"/> dependency property.</summary>
    public static readonly DependencyProperty VariantProperty =
        DependencyProperty.Register(
            nameof(Variant),
            typeof(BadgeVariant),
            typeof(Badge),
            new PropertyMetadata(BadgeVariant.Neutral));

    static Badge() =>
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Badge), new FrameworkPropertyMetadata(typeof(Badge)));

    /// <summary>Gets or sets the badge's semantic color. Defaults to <see cref="BadgeVariant.Neutral"/>.</summary>
    public BadgeVariant Variant
    {
        get => (BadgeVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Text, not a button: a badge is a piece of status a reader needs to hear, and nothing happens
    /// when it is clicked. Its content is that text, so that is the name.
    /// </remarks>
    protected override AutomationPeer OnCreateAutomationPeer() =>
        new CelesteAutomationPeer(this, AutomationControlType.Text, () => Content?.ToString());
}
