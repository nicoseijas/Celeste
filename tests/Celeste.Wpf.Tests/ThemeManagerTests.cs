using System.Windows;
using Celeste.Wpf.Theming;
using Xunit;

namespace Celeste.Wpf.Tests;

/// <summary>
/// The manager's contract: what is painted, what it says is painted, and that those cannot disagree.
/// </summary>
/// <remarks>
/// Every test restores the theme it found, because <see cref="ThemeManager"/> is static and the host
/// application is shared. <see cref="StaTestHost.Run"/> is what keeps two of them from overlapping.
/// </remarks>
public class ThemeManagerTests
{
    [Fact]
    public void ApplyingAThemePaintsItAndReportsIt()
    {
        InApplication(() =>
        {
            ThemeManager.Apply(ApplicationTheme.Dark);

            Assert.Equal(ApplicationTheme.Dark, ThemeManager.CurrentTheme);
            Assert.Equal(ApplicationTheme.Dark, ThemeManager.RequestedTheme);
            Assert.Equal(ApplicationTheme.Dark, Dictionary().Theme);

            ThemeManager.Apply(ApplicationTheme.Light);

            Assert.Equal(ApplicationTheme.Light, ThemeManager.CurrentTheme);
            Assert.Equal(ApplicationTheme.Light, Dictionary().Theme);
        });
    }

    /// <summary>
    /// The dictionary's own <see cref="ThemesDictionary.Theme"/> is writable, so the two can be set
    /// from two places. Whatever happens, the theme the manager reports has to be the one loaded.
    /// </summary>
    [Fact]
    public void TheReportedThemeCannotDisagreeWithTheDictionary()
    {
        InApplication(() =>
        {
            ThemeManager.Apply(ApplicationTheme.Dark);
            Assert.Equal(ApplicationTheme.Dark, ThemeManager.CurrentTheme);

            // Straight to the dictionary, behind the manager's back.
            Dictionary().Theme = ApplicationTheme.Light;
            Assert.Equal(ApplicationTheme.Light, ThemeManager.CurrentTheme);

            // The manager last recorded Dark, so a guard that trusted its own record would decide
            // there was nothing to do and leave the application painted Light.
            ThemeManager.Apply(ApplicationTheme.Dark);

            Assert.Equal(ApplicationTheme.Dark, Dictionary().Theme);
            Assert.Equal(ApplicationTheme.Dark, ThemeManager.CurrentTheme);
        });
    }

    [Fact]
    public void ApplyingTheThemeAlreadyPaintedAnnouncesNothing()
    {
        InApplication(() =>
        {
            ThemeManager.Apply(ApplicationTheme.Dark);

            int announcements = 0;
            void Count(object? sender, ThemeChangedEventArgs e) => announcements++;

            ThemeManager.ThemeChanged += Count;
            try
            {
                ThemeManager.Apply(ApplicationTheme.Dark);
                Assert.Equal(0, announcements);

                ThemeManager.Apply(ApplicationTheme.Light);
                Assert.Equal(1, announcements);
            }
            finally
            {
                ThemeManager.ThemeChanged -= Count;
            }
        });
    }

    [Fact]
    public void SystemResolvesToAConcreteTheme()
    {
        InApplication(() =>
        {
            ThemeManager.Apply(ApplicationTheme.System);

            Assert.Equal(ApplicationTheme.System, ThemeManager.RequestedTheme);

            // Whatever Windows is set to, what gets painted is one of the two real dictionaries.
            Assert.Contains(ThemeManager.CurrentTheme, new[] { ApplicationTheme.Light, ApplicationTheme.Dark });
        });
    }

    /// <summary>
    /// Applying a theme with nothing merged is a setup mistake, and saying so beats painting nothing.
    /// </summary>
    [Fact]
    public void ApplyingWithNoDictionaryMergedSaysWhat()
    {
        StaTestHost.Run(() =>
        {
            ResourceDictionary resources = HostResources;
            var merged = resources.MergedDictionaries.ToList();

            ApplicationTheme restore = ThemeManager.CurrentTheme;
            resources.MergedDictionaries.Clear();

            try
            {
                InvalidOperationException failure =
                    Assert.Throws<InvalidOperationException>(() => ThemeManager.Apply(ApplicationTheme.Dark));

                Assert.Contains(nameof(ThemesDictionary), failure.Message, StringComparison.Ordinal);
            }
            finally
            {
                foreach (ResourceDictionary dictionary in merged)
                {
                    resources.MergedDictionaries.Add(dictionary);
                }

                ThemeManager.Apply(restore);
            }
        });
    }

    /// <summary>The host application's resources. Always there inside <see cref="StaTestHost.Run"/>.</summary>
    private static ResourceDictionary HostResources =>
        (Application.Current ?? throw new InvalidOperationException("No host application.")).Resources;

    private static ThemesDictionary Dictionary() =>
        HostResources.MergedDictionaries.OfType<ThemesDictionary>().Single();

    /// <summary>
    /// Runs on the host application and puts the theme back, so the next test starts where this one
    /// found things.
    /// </summary>
    private static void InApplication(Action body)
    {
        StaTestHost.Run(() =>
        {
            ApplicationTheme requested = ThemeManager.RequestedTheme;
            ApplicationTheme painted = ThemeManager.CurrentTheme;

            try
            {
                body();
            }
            finally
            {
                ThemeManager.Apply(requested == ApplicationTheme.System ? ApplicationTheme.System : painted);
                Dictionary().Theme = painted;
            }
        });
    }
}
