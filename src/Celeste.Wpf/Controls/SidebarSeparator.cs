using System.Windows;
using System.Windows.Controls;

namespace Celeste.Wpf.Controls;

/// <summary>
/// A horizontal rule between runs of <see cref="SidebarItem"/>s in a <see cref="Sidebar"/>.
/// Use it where a group needs separating but not naming.
/// </summary>
/// <remarks>
/// Derives from <see cref="ListBoxItem"/> for the same reason <see cref="SidebarGroupHeader"/>
/// does, and is likewise neither focusable nor hit-testable.
/// <see cref="ContentControl.Content"/> is ignored — the template draws a line.
/// </remarks>
public class SidebarSeparator : ListBoxItem
{
    static SidebarSeparator()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(SidebarSeparator),
            new FrameworkPropertyMetadata(typeof(SidebarSeparator)));

        FocusableProperty.OverrideMetadata(
            typeof(SidebarSeparator),
            new FrameworkPropertyMetadata(false));

        IsHitTestVisibleProperty.OverrideMetadata(
            typeof(SidebarSeparator),
            new FrameworkPropertyMetadata(false));
    }
}
