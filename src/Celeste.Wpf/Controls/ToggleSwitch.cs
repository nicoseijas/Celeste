using System.Windows;
using System.Windows.Controls.Primitives;

namespace Celeste.Wpf.Controls;

/// <summary>
/// An on/off switch. Prefer it over a check box when the change takes effect immediately.
/// </summary>
/// <remarks>
/// Derives from <see cref="ToggleButton"/>, so <see cref="ToggleButton.IsChecked"/>,
/// <see cref="ToggleButton.Checked"/>, and command binding all work as usual.
/// <see cref="System.Windows.Controls.ContentControl.Content"/> is the label rendered beside the
/// switch; leave it unset for a bare switch.
/// </remarks>
public class ToggleSwitch : ToggleButton
{
    static ToggleSwitch() =>
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ToggleSwitch),
            new FrameworkPropertyMetadata(typeof(ToggleSwitch)));
}
