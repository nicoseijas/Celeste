using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Celeste.Wpf.Theming;
using Xunit;

namespace Celeste.Wpf.Tests;

/// <summary>
/// Realizes every tab of the gallery. A tab whose content only fails when it is first selected —
/// a resource the styles do not define, a template that cannot measure — surfaces here instead of
/// crashing the sample.
/// </summary>
public class GalleryTests
{
    public static TheoryData<int> TabIndexes => new() { 0, 1, 2, 3 };

    [Theory]
    [MemberData(nameof(TabIndexes))]
    public void EveryGalleryTabRealizesInLight(int tabIndex) => AssertTabRealizes(tabIndex, ApplicationTheme.Light);

    [Theory]
    [MemberData(nameof(TabIndexes))]
    public void EveryGalleryTabRealizesInDark(int tabIndex) => AssertTabRealizes(tabIndex, ApplicationTheme.Dark);

    /// <summary>
    /// The range switch is a segmented choice: exactly one of Day / Week / Month is the current
    /// range at any time. Three independent <see cref="ToggleButton"/> instances look segmented but
    /// are not — each one checks and unchecks on its own, so the sample could show two ranges at
    /// once, or none.
    /// </summary>
    [Fact]
    public void TheRangeSwitchKeepsExactlyOneOptionChecked()
    {
        StaTestHost.Run(() =>
        {
            var window = new Gallery.MainWindow();
            var toggles = ((Panel)window.FindName("RangeToggles")).Children
                .OfType<ToggleButton>()
                .ToList();

            Assert.Equal(3, toggles.Count);
            Assert.Single(toggles, toggle => toggle.IsChecked == true);

            Click(toggles[1]);
            Assert.Single(toggles, toggle => toggle.IsChecked == true);
            Assert.True(toggles[1].IsChecked);

            // Clicking the range that is already current cannot leave the switch empty.
            Click(toggles[1]);
            Assert.True(toggles[1].IsChecked);
        });
    }

    /// <summary>
    /// Presses a toggle the way a user does, through the automation pattern the control exposes:
    /// a plain <see cref="ToggleButton"/> flips, an option in a group selects itself. Assigning
    /// <see cref="ToggleButton.IsChecked"/> directly would skip exactly the behavior under test.
    /// </summary>
    private static void Click(ToggleButton toggle)
    {
        var peer = UIElementAutomationPeer.CreatePeerForElement(toggle);

        switch (peer?.GetPattern(PatternInterface.SelectionItem) ?? peer?.GetPattern(PatternInterface.Toggle))
        {
            case ISelectionItemProvider selection:
                selection.Select();
                break;
            case IToggleProvider toggleProvider:
                toggleProvider.Toggle();
                break;
            default:
                throw new InvalidOperationException($"'{toggle.Content}' exposes no way to be pressed.");
        }
    }

    private static void AssertTabRealizes(int tabIndex, ApplicationTheme theme)
    {
        StaTestHost.Run(
            () =>
            {
                // A Window only lays its content out once it is shown, and realizing a tab is
                // exactly what crashed the sample — so the window has to be real, just off-screen.
                var window = new Gallery.MainWindow
                {
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    ShowInTaskbar = false,
                    Left = -4000,
                    Top = -4000,
                };

                try
                {
                    window.Show();
                    window.UpdateLayout();

                    var tabs = (TabControl)window.FindName("Sections");
                    tabs.SelectedIndex = tabIndex;
                    window.UpdateLayout();

                    var content = (FrameworkElement)tabs.SelectedContent;
                    Assert.True(content.ActualHeight > 0, $"Tab {tabIndex} produced no content.");
                }
                finally
                {
                    window.Close();
                }
            },
            theme);
    }
}
