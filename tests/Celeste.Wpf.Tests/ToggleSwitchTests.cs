using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using Celeste.Wpf.Controls;
using Xunit;

namespace Celeste.Wpf.Tests;

public class ToggleSwitchTests
{
    /// <summary>The same nesting the gallery uses, so layout context is part of the test.</summary>
    private const string NestedXaml = """
        <TabControl xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:celeste="https://celeste-ui.dev/wpf">
            <TabItem Header="Toggles">
                <ScrollViewer>
                    <celeste:Card Header="Toggles">
                        <StackPanel x:Name="Switches">
                            <celeste:ToggleSwitch Content="On by default" IsChecked="True" />
                            <celeste:ToggleSwitch Content="Off by default" />
                            <celeste:ToggleSwitch Content="Disabled" IsEnabled="False" />
                        </StackPanel>
                    </celeste:Card>
                </ScrollViewer>
            </TabItem>
        </TabControl>
        """;

    [Fact]
    public void CheckedSwitchPaintsTheTrackWithThePrimaryBrush()
    {
        StaTestHost.Run(() =>
        {
            var primary = (SolidColorBrush)Application.Current.FindResource("Celeste.Brush.Primary");

            Assert.Equal(primary.Color, TrackColor(Rendered(isChecked: true)));
            Assert.NotEqual(primary.Color, TrackColor(Rendered(isChecked: false)));
        });
    }

    /// <summary>
    /// The thumb has to sit at the on position as soon as a checked switch renders. Driving it from
    /// the animation alone leaves it on the left until the storyboard runs, which reads as a switch
    /// that is half on.
    /// </summary>
    [Fact]
    public void CheckedSwitchPlacesItsThumbAtTheOnPositionBeforeAnimating()
    {
        StaTestHost.Run(() =>
        {
            Assert.True(ThumbOffset(Rendered(isChecked: true)) > ThumbOffset(Rendered(isChecked: false)));
        });
    }

    [Fact]
    public void EachSwitchInAPanelKeepsItsOwnState()
    {
        StaTestHost.Run(() =>
        {
            var tabs = (TabControl)XamlReader.Parse(NestedXaml);
            Layout(tabs, 800, 600);

            var panel = (StackPanel)tabs.FindName("Switches");
            var on = (ToggleSwitch)panel.Children[0];
            var off = (ToggleSwitch)panel.Children[1];

            var primary = (SolidColorBrush)Application.Current.FindResource("Celeste.Brush.Primary");

            Assert.Equal(primary.Color, TrackColor(on));
            Assert.NotEqual(primary.Color, TrackColor(off));
            Assert.True(ThumbOffset(on) > ThumbOffset(off));
        });
    }

    private static ToggleSwitch Rendered(bool isChecked)
    {
        var toggle = new ToggleSwitch { Content = "Label", IsChecked = isChecked };
        Layout(toggle, 400, 100);
        return toggle;
    }

    private static void Layout(FrameworkElement element, double width, double height)
    {
        var host = new Border { Child = element };
        host.Measure(new Size(width, height));
        host.Arrange(new Rect(0, 0, width, height));
        host.UpdateLayout();
    }

    private static Color TrackColor(ToggleSwitch toggle) =>
        ((SolidColorBrush)((Border)toggle.Template.FindName("Track", toggle)).Background).Color;

    private static double ThumbOffset(ToggleSwitch toggle) =>
        ((FrameworkElement)toggle.Template.FindName("Thumb", toggle)).Margin.Left;
}
