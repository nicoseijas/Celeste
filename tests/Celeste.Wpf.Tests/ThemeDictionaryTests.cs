using System.Windows;
using Celeste.Wpf.Theming;
using Xunit;

namespace Celeste.Wpf.Tests;

public class ThemeDictionaryTests
{
    private static readonly Uri LightUri = new("pack://application:,,,/Celeste.Wpf;component/Themes/Light.xaml");
    private static readonly Uri DarkUri = new("pack://application:,,,/Celeste.Wpf;component/Themes/Dark.xaml");

    [Fact]
    public void LightAndDarkDefineTheSameKeys()
    {
        StaTestHost.Run(() =>
        {
            HashSet<string> light = KeysOf(LightUri);
            HashSet<string> dark = KeysOf(DarkUri);

            string[] onlyInLight = [.. light.Except(dark).Order()];
            string[] onlyInDark = [.. dark.Except(light).Order()];

            Assert.Empty(onlyInLight);
            Assert.Empty(onlyInDark);
        });
    }

    [Fact]
    public void ThemeDictionaryResolvesToTheRequestedTheme()
    {
        StaTestHost.Run(
            () => Assert.Equal("Dark", Application.Current.TryFindResource("Celeste.ThemeName")),
            ApplicationTheme.Dark);

        StaTestHost.Run(
            () => Assert.Equal("Light", Application.Current.TryFindResource("Celeste.ThemeName")),
            ApplicationTheme.Light);
    }

    [Theory]
    [InlineData("Celeste.Brush.Background")]
    [InlineData("Celeste.Brush.Foreground")]
    [InlineData("Celeste.Brush.Primary")]
    [InlineData("Celeste.Brush.Border")]
    [InlineData("Celeste.Brush.Ring")]
    [InlineData("Celeste.Radius.Md")]
    [InlineData("Celeste.FontFamily")]
    [InlineData("Celeste.FontSize.Body")]
    [InlineData("Celeste.Control.HeightMd")]
    [InlineData("Celeste.Shadow.Md")]
    public void CoreTokensResolveFromApplicationResources(string key)
    {
        StaTestHost.Run(() => Assert.NotNull(Application.Current.TryFindResource(key)));
    }

    [Theory]
    [InlineData("Celeste.Window")]
    [InlineData("Celeste.Button.Primary")]
    [InlineData("Celeste.Button.Destructive")]
    [InlineData("Celeste.Button.Outline")]
    [InlineData("Celeste.Button.Ghost")]
    [InlineData("Celeste.Button.Link")]
    [InlineData("Celeste.Button.Small")]
    [InlineData("Celeste.Button.Large")]
    [InlineData("Celeste.ToggleButton")]
    [InlineData("Celeste.ToggleButton.Segment")]
    [InlineData("Celeste.TextBox.Multiline")]
    [InlineData("Celeste.ScrollViewer")]
    [InlineData("Celeste.TextBlock.Title")]
    [InlineData("Celeste.TextBlock.Muted")]
    [InlineData("Celeste.TextBlock.Code")]
    public void PublicStyleKeysResolveFromApplicationResources(string key)
    {
        StaTestHost.Run(() => Assert.IsType<Style>(Application.Current.TryFindResource(key)));
    }

    private static HashSet<string> KeysOf(Uri source)
    {
        var dictionary = new ResourceDictionary { Source = source };
        return [.. dictionary.Keys.Cast<object>().Select(key => key.ToString()!)];
    }
}
