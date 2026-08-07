using System.Windows;
using System.Windows.Controls;

namespace Celeste.Wpf.Controls;

/// <summary>
/// A caption that labels a run of <see cref="SidebarItem"/>s inside a <see cref="Sidebar"/>.
/// </summary>
/// <remarks>
/// Derives from <see cref="ListBoxItem"/> so the sidebar's item machinery treats it like any
/// other row, but it is neither focusable nor hit-testable: keyboard navigation steps over it and
/// a click passes through instead of selecting it. While the sidebar is collapsed the caption is
/// replaced by a rule, because a label that no longer fits should still leave the grouping visible.
/// </remarks>
public class SidebarGroupHeader : ListBoxItem
{
    static SidebarGroupHeader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(SidebarGroupHeader),
            new FrameworkPropertyMetadata(typeof(SidebarGroupHeader)));

        FocusableProperty.OverrideMetadata(
            typeof(SidebarGroupHeader),
            new FrameworkPropertyMetadata(false));

        IsHitTestVisibleProperty.OverrideMetadata(
            typeof(SidebarGroupHeader),
            new FrameworkPropertyMetadata(false));
    }
}
