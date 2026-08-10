using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Celeste.Wpf.Theming;
using Xunit;

namespace Celeste.Wpf.Tests;

/// <summary>
/// Holds every token pair the control styles put together as background and text above the WCAG 2.2
/// AA minimum of 4.5:1, in both themes.
/// </summary>
/// <remarks>
/// <para>
/// This is the test that makes the palette's accessibility a property of the repository rather than
/// an observation someone once made: a token cannot be nudged for looks without the ratio being
/// rechecked here.
/// </para>
/// <para>
/// Scope is success criterion 1.4.3, text. Non-text contrast (1.4.11) — the borders that identify an
/// input, the focus ring against its surroundings — is not enforced yet, and Celeste does not meet it
/// everywhere. Nor is 11px text treated as large: none of it is, at any weight the styles use.
/// </para>
/// <para>
/// Disabled pairs are absent on purpose. WCAG exempts an inactive control, and a disabled colour that
/// has to meet the same bar as an enabled one cannot look disabled.
/// </para>
/// </remarks>
public class ContrastTests
{
    /// <summary>WCAG 2.2 success criterion 1.4.3 for text below 18.66px bold or 24px regular.</summary>
    private const double TextMinimum = 4.5;

    /// <summary>
    /// Background and foreground token names the styles set together. A pair belongs here as soon as
    /// some style puts that foreground on that background.
    /// </summary>
    public static TheoryData<string, string> TextPairs => new()
    {
        // Buttons and badges: a filled surface carrying its own foreground.
        { "Primary", "PrimaryForeground" },
        { "PrimaryHover", "PrimaryForeground" },
        { "PrimaryPressed", "PrimaryForeground" },
        { "Secondary", "SecondaryForeground" },
        { "SecondaryHover", "SecondaryForeground" },
        { "SecondaryPressed", "SecondaryForeground" },
        { "Destructive", "DestructiveForeground" },
        { "DestructiveHover", "DestructiveForeground" },
        { "DestructivePressed", "DestructiveForeground" },
        { "Success", "SuccessForeground" },
        { "Warning", "WarningForeground" },
        { "Accent", "AccentForeground" },
        { "Muted", "MutedForeground" },

        // Page and card text, including the muted variant on each surface it can land on.
        { "Background", "Foreground" },
        { "Background", "MutedForeground" },
        { "Surface", "SurfaceForeground" },
        { "Surface", "MutedForeground" },
        { "SurfaceRaised", "Foreground" },
        { "SurfaceSunken", "MutedForeground" },
    };

    [Theory]
    [MemberData(nameof(TextPairs))]
    public void TextMeetsAaInLight(string background, string foreground) =>
        AssertContrast(background, foreground, ApplicationTheme.Light);

    [Theory]
    [MemberData(nameof(TextPairs))]
    public void TextMeetsAaInDark(string background, string foreground) =>
        AssertContrast(background, foreground, ApplicationTheme.Dark);

    /// <summary>
    /// The ratio for a pair the styles are known to fail on, so a broken calculation cannot make the
    /// theory above pass by returning something large for everything.
    /// </summary>
    [Fact]
    public void TheRatioIsComputedTheWayWcagDefinesIt()
    {
        Assert.Equal(21, Ratio(Colors.Black, Colors.White), 2);
        Assert.Equal(1, Ratio(Colors.White, Colors.White), 2);

        // #767676 on white is the canonical "just passes 4.5" grey.
        Assert.Equal(4.54, Ratio(Color.FromRgb(0x76, 0x76, 0x76), Colors.White), 2);
    }

    private static void AssertContrast(string background, string foreground, ApplicationTheme theme)
    {
        StaTestHost.Run(
            () =>
            {
                Color back = TokenColor(background);
                Color front = TokenColor(foreground);
                double ratio = Ratio(back, front);

                Assert.True(
                    ratio >= TextMinimum,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} theme: {1} ({2}) on {3} ({4}) is {5:N2}:1, under the {6:N1}:1 minimum for text.",
                        theme,
                        foreground,
                        front,
                        background,
                        back,
                        ratio,
                        TextMinimum));
            },
            theme);
    }

    private static Color TokenColor(string name) =>
        (Color)(Application.Current ?? throw new InvalidOperationException("No host application."))
            .FindResource($"Celeste.Color.{name}");

    /// <summary>
    /// The WCAG 2.2 contrast ratio. Alpha is ignored: every pair here is an opaque foreground on an
    /// opaque surface, which is what the styles actually set.
    /// </summary>
    private static double Ratio(Color first, Color second)
    {
        double a = RelativeLuminance(first);
        double b = RelativeLuminance(second);

        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    private static double RelativeLuminance(Color color) =>
        (0.2126 * Linear(color.R)) + (0.7152 * Linear(color.G)) + (0.0722 * Linear(color.B));

    private static double Linear(byte channel)
    {
        double value = channel / 255d;

        return value <= 0.03928
            ? value / 12.92
            : Math.Pow((value + 0.055) / 1.055, 2.4);
    }
}
