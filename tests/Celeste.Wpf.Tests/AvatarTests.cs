using System.Windows;
using System.Windows.Controls;
using Celeste.Wpf.Controls;
using Xunit;

namespace Celeste.Wpf.Tests;

/// <summary>
/// The fallback: whichever of the picture and the initials the reader ends up seeing, and when.
/// </summary>
public class AvatarTests
{
    private const string Present = "pack://application:,,,/Celeste.Wpf.Tests;component/Assets/person.png";
    private const string Missing = "pack://application:,,,/Celeste.Wpf.Tests;component/Assets/not-a-file.png";

    [Fact]
    public void TheInitialsShowWhenThereIsNoPicture()
    {
        StaTestHost.Run(() =>
        {
            var avatar = new Avatar { Initials = "NS" };

            Realize(avatar);

            Assert.Equal(Visibility.Visible, InitialsOf(avatar).Visibility);
        });
    }

    [Fact]
    public void ThePictureReplacesTheInitialsOnceItIsThere()
    {
        StaTestHost.Run(() =>
        {
            var avatar = new Avatar { Initials = "NS" };

            InWindow(avatar, () =>
            {
                Assert.Equal(Visibility.Visible, InitialsOf(avatar).Visibility);

                avatar.Source = new Uri(Present);
                StaTestHost.PumpUntil(
                    () => PictureOf(avatar).State != ImageState.Loading,
                    "the portrait never loaded");

                Assert.Equal(ImageState.Loaded, PictureOf(avatar).State);
                Assert.Equal(Visibility.Collapsed, InitialsOf(avatar).Visibility);
            });
        });
    }

    /// <summary>
    /// The case the fallback exists for. An avatar whose URI is broken has to read as the person it
    /// stands for, not as an empty circle.
    /// </summary>
    [Fact]
    public void APictureThatFailsLeavesTheInitialsShowing()
    {
        StaTestHost.Run(() =>
        {
            var avatar = new Avatar { Initials = "MK" };

            InWindow(avatar, () =>
            {
                avatar.Source = new Uri(Missing);
                StaTestHost.PumpUntil(
                    () => PictureOf(avatar).State != ImageState.Loading,
                    "the load never finished");

                Assert.Equal(ImageState.Failed, PictureOf(avatar).State);
                Assert.Equal(Visibility.Visible, InitialsOf(avatar).Visibility);
            });
        });
    }

    [Theory]
    [InlineData(AvatarSize.Small, 24)]
    [InlineData(AvatarSize.Medium, 32)]
    [InlineData(AvatarSize.Large, 44)]
    public void TheSizeDrivesTheDiameter(AvatarSize size, double expected)
    {
        StaTestHost.Run(() =>
        {
            var avatar = new Avatar { Initials = "NS", Size = size };

            Realize(avatar);

            Assert.Equal(expected, avatar.DesiredSize.Width, 3);
            Assert.Equal(expected, avatar.DesiredSize.Height, 3);
        });
    }

    /// <summary>An explicit size wins: the three named ones are defaults, not a fixed set.</summary>
    [Fact]
    public void AnExplicitWidthOverridesTheNamedSize()
    {
        StaTestHost.Run(() =>
        {
            var avatar = new Avatar { Initials = "NS", Size = AvatarSize.Small, Width = 64, Height = 64 };

            Realize(avatar);

            Assert.Equal(64, avatar.DesiredSize.Width, 3);
        });
    }

    private static void Realize(Avatar avatar)
    {
        var root = new Border { Child = avatar };

        root.Measure(new Size(200, 200));
        root.Arrange(new Rect(0, 0, 200, 200));
        root.UpdateLayout();
    }

    private static void InWindow(Avatar avatar, Action body)
    {
        var window = new Window
        {
            WindowStartupLocation = WindowStartupLocation.Manual,
            ShowInTaskbar = false,
            Left = -4000,
            Top = -4000,
            Width = 200,
            Height = 200,
            Content = avatar,
        };

        try
        {
            window.Show();
            window.UpdateLayout();
            body();
        }
        finally
        {
            window.Close();
        }
    }

    private static FrameworkElement InitialsOf(Avatar avatar) =>
        (FrameworkElement)avatar.Template.FindName("InitialsText", avatar);

    private static ImageView PictureOf(Avatar avatar) =>
        (ImageView)avatar.Template.FindName("Picture", avatar);
}
