namespace Celeste.Wpf.Controls;

/// <summary>
/// How a <see cref="SidebarHost"/> presents its pane.
/// </summary>
public enum SidebarDisplayMode
{
    /// <summary>
    /// Pick the mode from the host's width, using
    /// <see cref="SidebarHost.RailBreakpoint"/> and <see cref="SidebarHost.OffCanvasBreakpoint"/>.
    /// This is an input only: <see cref="SidebarHost.ActualDisplayMode"/> never reports it.
    /// </summary>
    Auto,

    /// <summary>Expanded beside the content, always visible, taking layout space.</summary>
    Docked,

    /// <summary>Beside the content and collapsed to its icon rail.</summary>
    Rail,

    /// <summary>
    /// Out of the layout entirely. The pane slides in over the content when
    /// <see cref="SidebarHost.IsPaneOpen"/> becomes <see langword="true"/>.
    /// </summary>
    OffCanvas,
}
