using System.Windows;
using System.Windows.Controls;

namespace Celeste.Wpf.Controls;

/// <summary>
/// One destination in a <see cref="Sidebar"/>: an icon, a label, and an optional trailing badge.
/// </summary>
/// <remarks>
/// Derives from <see cref="ListBoxItem"/>, so <see cref="ListBoxItem.IsSelected"/> and the
/// sidebar's selection binding drive it. <see cref="ContentControl.Content"/> is the label; it
/// becomes the tooltip while the sidebar is collapsed, which is the only thing identifying the
/// item once the label is hidden.
/// </remarks>
public class SidebarItem : ListBoxItem
{
    /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(object),
            typeof(SidebarItem),
            new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="Badge"/> dependency property.</summary>
    public static readonly DependencyProperty BadgeProperty =
        DependencyProperty.Register(
            nameof(Badge),
            typeof(object),
            typeof(SidebarItem),
            new PropertyMetadata(null));

    static SidebarItem() =>
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(SidebarItem),
            new FrameworkPropertyMetadata(typeof(SidebarItem)));

    /// <summary>
    /// Gets or sets the leading visual. Any element works — a <see cref="System.Windows.Shapes.Path"/>,
    /// an <see cref="Image"/>, or a glyph in a <see cref="TextBlock"/>. Celeste ships no icon set.
    /// </summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Gets or sets the trailing content, usually a count. A bare value is rendered as a
    /// <see cref="Controls.Badge"/>; pass a <see cref="UIElement"/> to control it yourself.
    /// </summary>
    public object? Badge
    {
        get => GetValue(BadgeProperty);
        set => SetValue(BadgeProperty, value);
    }
}
