using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Celeste.Wpf.Controls;
using Xunit;

namespace Celeste.Wpf.Tests;

/// <summary>
/// What the control owns rather than the template: the state it reports, the height it derives from
/// an aspect ratio, and the fact that a picture is decoded to the size it is shown at.
/// </summary>
/// <remarks>
/// The pictures are the gallery's own resources. The test project already references the gallery as
/// the reference consumer, and a pack URI keeps every case here off the network and off the disk.
/// </remarks>
public class ImageViewTests
{
    private const string Present = "pack://application:,,,/Celeste.Gallery;component/Assets/tile-1.png";
    private const string Missing = "pack://application:,,,/Celeste.Gallery;component/Assets/not-a-file.png";

    /// <summary>The natural width of <see cref="Present"/>.</summary>
    private const int PresentPixelWidth = 480;

    [Fact]
    public void APictureThatResolvesEndsUpDecoded()
    {
        StaTestHost.Run(() =>
        {
            var view = new ImageView { Width = 100, Height = 100 };

            InWindow(view, () =>
            {
                view.Source = new Uri(Present);
                StaTestHost.PumpUntil(() => view.State != ImageState.Loading, "the picture never loaded");

                Assert.Equal(ImageState.Loaded, view.State);
                Assert.NotNull(view.DecodedImage);
            });
        });
    }

    /// <summary>
    /// The reason the control does the loading instead of handing the URI to an <see cref="Image"/>:
    /// a 480 pixel file shown in a 100 pixel box is decoded small, not decoded whole and scaled down.
    /// </summary>
    [Fact]
    public void ThePictureIsDecodedToTheWidthItIsShownAt()
    {
        StaTestHost.Run(() =>
        {
            var view = new ImageView { Width = 100, Height = 100 };

            InWindow(view, () =>
            {
                view.Source = new Uri(Present);
                StaTestHost.PumpUntil(() => view.State != ImageState.Loading, "the picture never loaded");

                var decoded = (BitmapSource)view.DecodedImage!;
                Assert.True(
                    decoded.PixelWidth < PresentPixelWidth,
                    $"Decoded {decoded.PixelWidth} pixels wide for a 100 pixel box; the file is {PresentPixelWidth}.");
            });
        });
    }

    [Fact]
    public void ASourceThatCannotBeFoundFailsAndSaysWhy()
    {
        StaTestHost.Run(() =>
        {
            var view = new ImageView { Width = 100, Height = 100 };
            ImageFailedEventArgs? failure = null;
            view.ImageFailed += (_, e) => failure = e;

            InWindow(view, () =>
            {
                view.Source = new Uri(Missing);
                StaTestHost.PumpUntil(() => view.State != ImageState.Loading, "the load never finished");

                Assert.Equal(ImageState.Failed, view.State);
                Assert.Null(view.DecodedImage);

                // A failure the application cannot see is a failure the library swallowed.
                Assert.NotNull(failure);
                Assert.Equal(Missing, failure.Source.AbsoluteUri);
                Assert.NotNull(failure.Exception);
            });
        });
    }

    [Fact]
    public void ClearingTheSourceDropsThePicture()
    {
        StaTestHost.Run(() =>
        {
            var view = new ImageView { Width = 100, Height = 100 };

            InWindow(view, () =>
            {
                view.Source = new Uri(Present);
                StaTestHost.PumpUntil(() => view.State == ImageState.Loaded, "the picture never loaded");

                view.Source = null;

                Assert.Equal(ImageState.None, view.State);
                Assert.Null(view.DecodedImage);
            });
        });
    }

    [Fact]
    public void TheHeightComesFromTheAspectRatio()
    {
        StaTestHost.Run(() =>
        {
            var view = new ImageView { AspectRatio = 2 };

            Measure(view, width: 300);

            Assert.Equal(300, view.DesiredSize.Width, 3);
            Assert.Equal(150, view.DesiredSize.Height, 3);
        });
    }

    /// <summary>
    /// A tile with no ratio yet reserves a square, so a masonry grid does not collapse to nothing and
    /// then jump once the pictures arrive.
    /// </summary>
    [Fact]
    public void AnUnknownRatioReservesASquare()
    {
        StaTestHost.Run(() =>
        {
            var view = new ImageView();

            Measure(view, width: 240);

            Assert.Equal(240, view.DesiredSize.Width, 3);
            Assert.Equal(240, view.DesiredSize.Height, 3);
        });
    }

    private static void Measure(ImageView view, double width)
    {
        var root = new Border { Child = view, Width = width };

        root.Measure(new Size(width, double.PositiveInfinity));
        root.Arrange(new Rect(0, 0, width, root.DesiredSize.Height));
        root.UpdateLayout();
    }

    /// <summary>
    /// A real window, off screen: the control waits for its first layout pass before loading, so a
    /// detached element never gets as far as a decode.
    /// </summary>
    private static void InWindow(ImageView view, Action body)
    {
        var window = new Window
        {
            WindowStartupLocation = WindowStartupLocation.Manual,
            ShowInTaskbar = false,
            Left = -4000,
            Top = -4000,
            Width = 300,
            Height = 300,
            Content = view,
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
}
