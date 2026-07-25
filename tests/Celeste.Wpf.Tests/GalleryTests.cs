using System.Windows;
using System.Windows.Controls;
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
