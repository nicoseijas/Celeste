using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Celeste.Wpf.Media;
using Xunit;

namespace Celeste.Wpf.Tests;

/// <summary>
/// The loader's own contract: what it refuses, and what it only does once.
/// </summary>
public class ImageLoaderTests
{
    private const string Present = "pack://application:,,,/Celeste.Gallery;component/Assets/tile-2.png";

    [Fact]
    public void ARelativeUriIsRejected()
    {
        // The control resolves relative URIs against the element's base URI. The loader has no
        // element and therefore no base to resolve against, so it says so rather than guessing.
        Assert.Throws<ArgumentException>(() => Load(new Uri("Assets/tile-1.png", UriKind.Relative)));
    }

    [Fact]
    public void AnUnsupportedSchemeIsRejected()
    {
        Assert.Throws<NotSupportedException>(() => Load(new Uri("ftp://example.invalid/picture.png")));
    }

    [Fact]
    public void TheSameSourceAtTheSameSizeIsDecodedOnce()
    {
        StaTestHost.Run(() =>
        {
            var source = new Uri(Present);

            ImageSource first = Load(source, decodePixelWidth: 120);
            ImageSource second = Load(source, decodePixelWidth: 120);

            // The same instance, not merely an equal one: a grid showing one picture in ten places
            // should hold one bitmap.
            Assert.Same(first, second);
        });
    }

    [Fact]
    public void ADifferentDecodeWidthIsADifferentBitmap()
    {
        StaTestHost.Run(() =>
        {
            var source = new Uri(Present);

            ImageSource narrow = Load(source, decodePixelWidth: 80);
            ImageSource wide = Load(source, decodePixelWidth: 160);

            Assert.NotSame(narrow, wide);
            Assert.True(((BitmapSource)narrow).PixelWidth < ((BitmapSource)wide).PixelWidth);
        });
    }

    /// <summary>
    /// The guard exists so a remote server cannot spend the process's memory for it. The limit is
    /// global state, restored here because <see cref="StaTestHost.Run"/> is what keeps tests from
    /// overlapping.
    /// </summary>
    [Fact]
    public void ASourceOverTheSizeLimitIsRejected()
    {
        StaTestHost.Run(() =>
        {
            long original = ImageLoader.MaxSourceBytes;

            try
            {
                ImageLoader.MaxSourceBytes = 512;

                Assert.Throws<InvalidDataException>(() => Load(new Uri(Present), decodePixelWidth: 64));
            }
            finally
            {
                ImageLoader.MaxSourceBytes = original;
                ImageLoader.ClearCache();
            }
        });
    }

    /// <summary>
    /// Blocking is safe here: nothing inside the loader needs the calling thread back, so a test can
    /// wait on it without pumping a dispatcher.
    /// </summary>
    private static ImageSource Load(Uri source, int decodePixelWidth = 0) =>
        ImageLoader.LoadAsync(source, decodePixelWidth).GetAwaiter().GetResult();
}
