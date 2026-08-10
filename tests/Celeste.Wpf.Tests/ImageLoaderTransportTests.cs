using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Celeste.Wpf.Media;
using Xunit;

namespace Celeste.Wpf.Tests;

/// <summary>
/// How the loader gets its bytes: over HTTP, off the disk, and what it does when the far end
/// misbehaves.
/// </summary>
/// <remarks>
/// No sockets and no files on a server anywhere. The HTTP cases go through
/// <see cref="ImageLoader.HttpClient"/> with a stub handler, which is the seam an application uses to
/// supply its own proxy or authentication — so these tests exercise that seam as well as the code
/// behind it.
/// </remarks>
public class ImageLoaderTransportTests
{
    private const string ImageResource = "pack://application:,,,/Celeste.Wpf.Tests;component/Assets/tile-2.png";

    [Fact]
    public void AnImageIsFetchedThroughTheConfiguredClient()
    {
        StaTestHost.Run(() =>
        {
            byte[] png = ImageBytes();
            var requested = new List<Uri>();

            WithHttpClient(
                request =>
                {
                    requested.Add(request.RequestUri!);
                    return Respond(HttpStatusCode.OK, png);
                },
                () =>
                {
                    var source = new Uri("https://example.invalid/one.png");

                    var image = (BitmapSource)Load(source);

                    Assert.Equal(320, image.PixelWidth);
                    Assert.Equal(source, Assert.Single(requested));
                });
        });
    }

    [Fact]
    public void AResponseThatIsNotSuccessIsAFailedLoad()
    {
        StaTestHost.Run(() =>
        {
            WithHttpClient(
                _ => Respond(HttpStatusCode.NotFound, []),
                () => Assert.Throws<HttpRequestException>(() => Load(new Uri("https://example.invalid/missing.png"))));
        });
    }

    /// <summary>
    /// The point of asking for the headers first: a server that declares a gigabyte should be turned
    /// away before a gigabyte is read, not after.
    /// </summary>
    [Fact]
    public void ADeclaredLengthOverTheLimitIsRefusedBeforeTheBodyIsRead()
    {
        StaTestHost.Run(() =>
        {
            var body = new TripwireStream(ImageBytes());

            long original = ImageLoader.MaxSourceBytes;
            try
            {
                ImageLoader.MaxSourceBytes = 1024;

                WithHttpClient(
                    _ =>
                    {
                        var response = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StreamContent(body),
                        };

                        response.Content.Headers.ContentLength = 8L * 1024 * 1024 * 1024;
                        return response;
                    },
                    () => Assert.Throws<InvalidDataException>(() => Load(new Uri("https://example.invalid/huge.png"))));

                Assert.False(body.WasRead, "The body was read despite the declared length being over the limit.");
            }
            finally
            {
                ImageLoader.MaxSourceBytes = original;
            }
        });
    }

    /// <summary>
    /// A server that lies about the length, or does not declare one, is caught on the way in instead.
    /// </summary>
    [Fact]
    public void AnUndeclaredBodyOverTheLimitIsRefusedWhileItArrives()
    {
        StaTestHost.Run(() =>
        {
            long original = ImageLoader.MaxSourceBytes;
            try
            {
                ImageLoader.MaxSourceBytes = 512;

                WithHttpClient(
                    _ =>
                    {
                        var response = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StreamContent(new MemoryStream(ImageBytes())),
                        };

                        response.Content.Headers.ContentLength = null;
                        return response;
                    },
                    () => Assert.Throws<InvalidDataException>(() => Load(new Uri("https://example.invalid/undeclared.png"))));
            }
            finally
            {
                ImageLoader.MaxSourceBytes = original;
            }
        });
    }

    /// <summary>
    /// Cancelling stops this caller waiting. It does not stop the download, because a second view may
    /// still want the result — which is the documented behaviour, and this is what it looks like.
    /// </summary>
    [Fact]
    public void CancellingStopsTheWaitAndNotTheLoad()
    {
        StaTestHost.Run(() =>
        {
            byte[] png = ImageBytes();
            using var release = new ManualResetEventSlim();
            var source = new Uri("https://example.invalid/slow.png");

            WithHttpClient(
                _ =>
                {
                    release.Wait(TimeSpan.FromSeconds(10));
                    return Respond(HttpStatusCode.OK, png);
                },
                () =>
                {
                    using var cancellation = new CancellationTokenSource();

                    Task<ImageSource> abandoned = ImageLoader.LoadAsync(source, 0, cancellation.Token);
                    cancellation.Cancel();

                    Assert.ThrowsAny<OperationCanceledException>(() => abandoned.GetAwaiter().GetResult());

                    // The handler is still blocked, so the shared load is still in flight.
                    release.Set();

                    ImageSource image = Load(source);
                    Assert.NotNull(image);
                });
        });
    }

    [Fact]
    public void AFileUriIsLoadedFromDisk()
    {
        StaTestHost.Run(() =>
        {
            string path = Path.Combine(Path.GetTempPath(), $"celeste-{Guid.NewGuid():N}.png");
            File.WriteAllBytes(path, ImageBytes());

            try
            {
                var image = (BitmapSource)Load(new Uri(path));

                Assert.Equal(320, image.PixelWidth);
            }
            finally
            {
                File.Delete(path);
            }
        });
    }

    [Fact]
    public void AFileThatIsNotThereIsAFailedLoad()
    {
        StaTestHost.Run(() =>
        {
            var missing = new Uri(Path.Combine(Path.GetTempPath(), $"celeste-{Guid.NewGuid():N}.png"));

            Assert.Throws<FileNotFoundException>(() => Load(missing));
        });
    }

    private static byte[] ImageBytes()
    {
        // The same tile the other picture tests use, read out of this assembly's resources.
        using Stream stream = Application.GetResourceStream(new Uri(ImageResource))!.Stream;
        using var buffer = new MemoryStream();

        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static HttpResponseMessage Respond(HttpStatusCode status, byte[] body) =>
        new(status) { Content = new ByteArrayContent(body) };

    private static ImageSource Load(Uri source) => ImageLoader.LoadAsync(source).GetAwaiter().GetResult();

    /// <summary>
    /// Swaps in a client that answers from <paramref name="respond"/>, and puts the original back.
    /// The cache is cleared on the way out because these URIs are not real and must not outlive the
    /// test that invented them.
    /// </summary>
    private static void WithHttpClient(Func<HttpRequestMessage, HttpResponseMessage> respond, Action body)
    {
        HttpClient original = ImageLoader.HttpClient;

        using var stub = new HttpClient(new StubHandler(respond));
        ImageLoader.HttpClient = stub;

        try
        {
            body();
        }
        finally
        {
            ImageLoader.HttpClient = original;
            ImageLoader.ClearCache();
        }
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(_respond(request));
    }

    /// <summary>A stream that records whether anyone read from it.</summary>
    private sealed class TripwireStream : MemoryStream
    {
        public TripwireStream(byte[] buffer)
            : base(buffer)
        {
        }

        public bool WasRead { get; private set; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            WasRead = true;
            return base.Read(buffer, offset, count);
        }

        public override int Read(Span<byte> buffer)
        {
            WasRead = true;
            return base.Read(buffer);
        }
    }
}
