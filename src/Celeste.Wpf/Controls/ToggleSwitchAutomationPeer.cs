using System.Windows.Automation.Peers;

namespace Celeste.Wpf.Controls;

/// <summary>
/// Reports a <see cref="ToggleSwitch"/> as a switch rather than as the toggle button it is built on.
/// </summary>
/// <remarks>
/// The control type stays <see cref="AutomationControlType.Button"/> and the toggle pattern comes
/// straight from <see cref="ToggleButtonAutomationPeer"/>: a screen reader still needs to be able to
/// flip it and to read its state. What changes is what the user hears it called, and the class name
/// an assistive tool matches on — a switch applies its change immediately, where a checkbox waits for
/// a Save button, and that is worth telling apart.
/// </remarks>
public class ToggleSwitchAutomationPeer : ToggleButtonAutomationPeer
{
    /// <summary>Initializes a new instance of the <see cref="ToggleSwitchAutomationPeer"/> class.</summary>
    /// <param name="owner">The switch this peer reports on.</param>
    public ToggleSwitchAutomationPeer(ToggleSwitch owner)
        : base(owner)
    {
    }

    /// <inheritdoc />
    protected override string GetClassNameCore() => nameof(ToggleSwitch);

    /// <inheritdoc />
    protected override string GetLocalizedControlTypeCore() => "switch";
}
