using System.Windows;
using System.Windows.Controls;
using Celeste.Wpf.Controls;
using Celeste.Wpf.Theming;
using Xunit;

namespace Celeste.Wpf.Tests;

/// <summary>
/// Every Celeste style suppresses the dotted system focus rectangle, which means every one of them
/// owes the keyboard user a replacement. These are the controls that draw their own ring, and this is
/// what stops one of them from losing it.
/// </summary>
/// <remarks>
/// Absent on purpose: <c>ListBoxItem</c>, <c>ComboBoxItem</c>, and <c>TabItem</c> show keyboard focus
/// through their selected and highlighted states rather than through a ring, and a <c>RepeatButton</c>
/// inside a slider track is not focusable at all.
/// </remarks>
public class FocusVisualTests
{
    private const string RingPartName = "FocusRing";

    public static TheoryData<string> RingedControls => new()
    {
        nameof(Button),
        nameof(CheckBox),
        nameof(RadioButton),
        nameof(TextBox),
        nameof(PasswordBox),
        nameof(ComboBox),
        nameof(ToggleSwitch),
        nameof(Slider),
    };

    [Theory]
    [MemberData(nameof(RingedControls))]
    public void KeyboardFocusIsVisibleInLight(string controlName) =>
        AssertRingFollowsFocus(controlName, ApplicationTheme.Light);

    [Theory]
    [MemberData(nameof(RingedControls))]
    public void KeyboardFocusIsVisibleInDark(string controlName) =>
        AssertRingFollowsFocus(controlName, ApplicationTheme.Dark);

    private static void AssertRingFollowsFocus(string controlName, ApplicationTheme theme)
    {
        StaTestHost.Run(
            () =>
            {
                Control control = Create(controlName);

                // Something else focusable, so focus has somewhere to be that is not the control.
                var other = new Button { Content = "elsewhere" };
                var root = new StackPanel();
                root.Children.Add(other);
                root.Children.Add(control);

                var window = new Window
                {
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    ShowInTaskbar = false,
                    Left = -4000,
                    Top = -4000,
                    Width = 300,
                    Height = 200,
                    Content = root,
                };

                try
                {
                    window.Show();
                    window.UpdateLayout();

                    other.Focus();
                    window.UpdateLayout();
                    Assert.Equal(0d, RingOpacity(control));

                    Assert.True(control.Focus(), $"{controlName} refused keyboard focus.");
                    window.UpdateLayout();

                    Assert.True(
                        RingOpacity(control) > 0d,
                        $"{controlName} has keyboard focus in the {theme} theme and shows no focus ring.");
                }
                finally
                {
                    window.Close();
                }
            },
            theme);
    }

    private static double RingOpacity(Control control)
    {
        var ring = control.Template.FindName(RingPartName, control) as FrameworkElement;

        Assert.NotNull(ring);
        return ring.Opacity;
    }

    private static Control Create(string controlName) => controlName switch
    {
        nameof(Button) => new Button { Content = "Save" },
        nameof(CheckBox) => new CheckBox { Content = "Ship it" },
        nameof(RadioButton) => new RadioButton { Content = "Daily" },
        nameof(TextBox) => new TextBox { Text = "value" },
        nameof(PasswordBox) => new PasswordBox(),
        nameof(ComboBox) => new ComboBox { Items = { new ComboBoxItem { Content = "One" } } },
        nameof(ToggleSwitch) => new ToggleSwitch { Content = "Public" },
        nameof(Slider) => new Slider { Value = 30 },
        _ => throw new ArgumentOutOfRangeException(nameof(controlName), controlName, "Unknown control."),
    };
}
