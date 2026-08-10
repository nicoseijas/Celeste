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

    /// <summary>
    /// <c>application:</c> parses as a URI scheme, which makes it look like one WPF resolves. It is
    /// not: WPF understands the word only as the authority of a pack URI, and
    /// <c>Application.GetResourceStream</c> rejects the bare scheme. Refusing it with a message that
    /// names the form that works beats accepting it and failing deeper down.
    /// </summary>
    [Fact]
    public void TheBareApplicationSchemeIsRejectedAndPointsAtPack()
    {
        NotSupportedException failure = Assert.Throws<NotSupportedException>(
            () => Load(new Uri("application:///Celeste.Gallery;component/Assets/tile-1.png")));

        Assert.Contains("pack://application:,,,/", failure.Message, StringComparison.Ordinal);
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
    /// The byte limit bounds the file; this one bounds what the file expands into. They are different
    /// numbers, and only the second one stands between a small file describing a huge image and the
    /// allocation that decoding it would ask for.
    /// </summary>
    [Fact]
    public void AnImageThatDecodesToTooManyPixelsIsRejected()
    {
        StaTestHost.Run(() =>
        {
            long original = ImageLoader.MaxDecodedPixels;

            try
            {
                // tile-2 is 320x480, so 1000 pixels is well under what it decodes to.
                ImageLoader.MaxDecodedPixels = 1000;

                // A cached picture is never decoded again, so the guard would never run. The gallery
                // test realizes tiles at a width the loader reads as "full resolution", which is the
                // same cache key this test uses.
                ImageLoader.ClearCache();

                InvalidDataException failure = Assert.Throws<InvalidDataException>(() => Load(new Uri(Present)));
                Assert.Contains(nameof(ImageLoader.MaxDecodedPixels), failure.Message, StringComparison.Ordinal);
            }
            finally
            {
                ImageLoader.MaxDecodedPixels = original;
                ImageLoader.ClearCache();
            }
        });
    }

    /// <summary>
    /// The limit is about the decode, not the file: asking for a thumbnail of a large picture is a
    /// small allocation and has to stay allowed.
    /// </summary>
    [Fact]
    public void TheLimitIsJudgedAgainstTheSizeBeingDecoded()
    {
        StaTestHost.Run(() =>
        {
            long original = ImageLoader.MaxDecodedPixels;

            try
            {
                // 320x480 whole is 153,600 pixels and would fail. Decoded to 20 wide it is 20x30.
                ImageLoader.MaxDecodedPixels = 1000;
                ImageLoader.ClearCache();

                var thumbnail = (BitmapSource)Load(new Uri(Present), decodePixelWidth: 20);

                Assert.Equal(20, thumbnail.PixelWidth);
            }
            finally
            {
                ImageLoader.MaxDecodedPixels = original;
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
