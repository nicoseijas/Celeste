using System.Windows.Automation.Peers;

namespace Celeste.Wpf.Controls;

/// <summary>
/// Reports a <see cref="Sidebar"/> to assistive technology as a navigation list rather than a
/// generic list box.
/// </summary>
/// <remarks>
/// The control type stays <see cref="AutomationControlType.List"/>: a screen reader still needs
/// the selection and item-count semantics of a list to announce "3 of 7". Only the spoken
/// description of the control changes, so the user hears which list they landed in.
/// </remarks>
public class SidebarAutomationPeer : ListBoxAutomationPeer
{
    /// <summary>Initializes a new instance of the <see cref="SidebarAutomationPeer"/> class.</summary>
    /// <param name="owner">The sidebar this peer reports on.</param>
    public SidebarAutomationPeer(Sidebar owner)
        : base(owner)
    {
    }

    /// <inheritdoc />
    protected override string GetClassNameCore() => nameof(Sidebar);

    /// <inheritdoc />
    protected override string GetLocalizedControlTypeCore() => "navigation";
}
