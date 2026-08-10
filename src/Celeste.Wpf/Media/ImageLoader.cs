using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;

namespace Celeste.Wpf.Media;

/// <summary>
/// Loads and decodes images off the UI thread, and shares the result between everything that asks
/// for the same image at the same size.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Controls.ImageView"/> and <see cref="Controls.Avatar"/> go through here. It is public
/// so an application can prime the cache before a view opens, and so the <see cref="HttpClient"/>
/// used for remote images can be replaced with one that carries the application's own handler,
/// proxy, or authentication.
/// </para>
/// <para>
/// Decoded images are frozen, so they are safe to hand to any thread, and cached through weak
/// references: an image stays shared for as long as something on screen holds it, and is collected
/// once nothing does. The cache is not a persistent one — it removes duplicate decodes, not
/// duplicate downloads across a session.
/// </para>
/// <para>
/// Concurrent callers for the same key await one load. That shared load does not observe any single
/// caller's <see cref="CancellationToken"/>, because a second caller still wants the result: a
/// cancelled <see cref="LoadAsync"/> stops waiting, it does not stop the download.
/// </para>
/// </remarks>
public static class ImageLoader
{
    /// <summary>Bytes of encoded image data accepted by default, before decoding.</summary>
    private const long DefaultMaxSourceBytes = 32L * 1024 * 1024;

    /// <summary>Dead entries are swept once the cache passes this size.</summary>
    private const int PruneThreshold = 256;

    private const int CopyBufferSize = 81920;

    private const string PackScheme = "pack";
    private const string ApplicationScheme = "application";

    private static readonly TimeSpan DefaultHttpTimeout = TimeSpan.FromSeconds(30);

    // Guards every static field below. Held only around dictionary work, never across an await.
    private static readonly object Gate = new();
    private static readonly Dictionary<ImageKey, WeakReference<ImageSource>> Cache = new();
    private static readonly Dictionary<ImageKey, Task<ImageSource>> InFlight = new();

    private static HttpClient? _httpClient;
    private static long _maxSourceBytes = DefaultMaxSourceBytes;

    /// <summary>
    /// Gets or sets the client used for <c>http</c> and <c>https</c> sources. The default one has a
    /// 30 second timeout and no other configuration. Replace it during startup, before the first
    /// remote image is requested; a load already in flight keeps the client it started with.
    /// </summary>
    public static HttpClient HttpClient
    {
        get
        {
            lock (Gate)
            {
                return _httpClient ??= new HttpClient { Timeout = DefaultHttpTimeout };
            }
        }

        set
        {
            ArgumentNullException.ThrowIfNull(value);

            lock (Gate)
            {
                _httpClient = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the largest encoded image accepted, in bytes. Defaults to 32 MiB. A source that
    /// declares or exceeds this size fails instead of being decoded, so a remote server cannot
    /// exhaust the process's memory with one response.
    /// </summary>
    public static long MaxSourceBytes
    {
        get => Interlocked.Read(ref _maxSourceBytes);
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            Interlocked.Exchange(ref _maxSourceBytes, value);
        }
    }

    /// <summary>
    /// Loads <paramref name="source"/> and returns a frozen image.
    /// </summary>
    /// <param name="source">
    /// An absolute URI. <c>http</c>, <c>https</c>, <c>file</c>, <c>pack</c>, and
    /// <c>application</c> are supported; anything else throws <see cref="NotSupportedException"/>.
    /// </param>
    /// <param name="decodePixelWidth">
    /// The width to decode to, in pixels, or 0 for the image's own resolution. A value wider than
    /// the image is ignored rather than upscaling the decode.
    /// </param>
    /// <param name="cancellationToken">Stops this caller waiting. It does not stop a shared load.</param>
    /// <returns>A frozen <see cref="ImageSource"/>, safe to use from any thread.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="source"/> is a relative URI.</exception>
    /// <exception cref="NotSupportedException">The URI scheme is not one of the supported ones.</exception>
    /// <exception cref="InvalidDataException">The source is larger than <see cref="MaxSourceBytes"/>.</exception>
    public static async Task<ImageSource> LoadAsync(
        Uri source,
        int decodePixelWidth = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegative(decodePixelWidth);

        if (!source.IsAbsoluteUri)
        {
            throw new ArgumentException(
                "The image URI must be absolute. Resolve it against a base URI first.",
                nameof(source));
        }

        var key = new ImageKey(source.AbsoluteUri, decodePixelWidth);

        // WaitAsync, not a token passed into the load: the load may be someone else's too.
        return await GetOrStartAsync(source, key).WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Drops every cached image. Loads already in flight still complete and still populate the
    /// cache; this releases what is held now, it does not stop anything.
    /// </summary>
    public static void ClearCache()
    {
        lock (Gate)
        {
            Cache.Clear();
        }
    }

    private static Task<ImageSource> GetOrStartAsync(Uri source, ImageKey key)
    {
        lock (Gate)
        {
            if (Cache.TryGetValue(key, out WeakReference<ImageSource>? entry))
            {
                if (entry.TryGetTarget(out ImageSource? cached))
                {
                    return Task.FromResult(cached);
                }

                Cache.Remove(key);
            }

            if (InFlight.TryGetValue(key, out Task<ImageSource>? running))
            {
                return running;
            }

            Task<ImageSource> started = LoadCoreAsync(source, key);
            InFlight[key] = started;

            // A continuation rather than a finally inside LoadCoreAsync, so the entry is in the
            // dictionary before anything can remove it. Forget checks identity for the rest.
            _ = started.ContinueWith(
                completed => Forget(key, completed),
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);

            return started;
        }
    }

    private static void Forget(ImageKey key, Task<ImageSource> completed)
    {
        lock (Gate)
        {
            // Only if it is still the task this continuation belongs to.
            if (InFlight.TryGetValue(key, out Task<ImageSource>? current) && ReferenceEquals(current, completed))
            {
                InFlight.Remove(key);
            }
        }
    }

    private static async Task<ImageSource> LoadCoreAsync(Uri source, ImageKey key)
    {
        // Task.Run for the prologue, not for the I/O: opening a file and looking up a resource both
        // block, and this method is started by the caller — usually the UI thread — and started
        // while Gate is held. Neither is a place to block.
        using MemoryStream encoded = await Task.Run(() => ReadAsync(source)).ConfigureAwait(false);

        ImageSource image = Decode(encoded, key.DecodePixelWidth);

        lock (Gate)
        {
            if (Cache.Count >= PruneThreshold)
            {
                Prune();
            }

            Cache[key] = new WeakReference<ImageSource>(image);
        }

        return image;
    }

    /// <summary>
    /// Reads the whole source into memory. Uniform across schemes so the size guard is too, and
    /// because <see cref="BitmapCacheOption.OnLoad"/> reads its stream synchronously — a network
    /// stream handed straight to the decoder would block a thread pool thread on the wire.
    /// </summary>
    private static async Task<MemoryStream> ReadAsync(Uri source)
    {
        long limit = MaxSourceBytes;

        if (source.Scheme == Uri.UriSchemeHttp || source.Scheme == Uri.UriSchemeHttps)
        {
            using HttpResponseMessage response = await HttpClient
                .GetAsync(source, HttpCompletionOption.ResponseHeadersRead)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            long? declared = response.Content.Headers.ContentLength;
            if (declared > limit)
            {
                throw TooLarge(source, declared.Value, limit);
            }

            using Stream content = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            return await CopyAsync(content, source, limit).ConfigureAwait(false);
        }

        if (source.Scheme == Uri.UriSchemeFile)
        {
            using var file = new FileStream(
                source.LocalPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                CopyBufferSize,
                useAsync: true);

            if (file.Length > limit)
            {
                throw TooLarge(source, file.Length, limit);
            }

            return await CopyAsync(file, source, limit).ConfigureAwait(false);
        }

        if (source.Scheme is PackScheme or ApplicationScheme)
        {
            // Throws IOException when the resource is not in the assembly, which is the answer we
            // want: a missing resource is a failed image, not a crash.
            StreamResourceInfo? resource = Application.GetResourceStream(source)
                ?? throw new FileNotFoundException($"No resource at '{source}'.");

            using Stream content = resource.Stream;
            return await CopyAsync(content, source, limit).ConfigureAwait(false);
        }

        throw new NotSupportedException(
            $"Cannot load '{source}': the '{source.Scheme}' scheme is not supported. Use http, https, file, pack, or application.");
    }

    private static async Task<MemoryStream> CopyAsync(Stream content, Uri source, long limit)
    {
        var buffer = new MemoryStream();

        byte[] chunk = new byte[CopyBufferSize];
        int read;

        while ((read = await content.ReadAsync(chunk).ConfigureAwait(false)) > 0)
        {
            long size = buffer.Length + read;

            if (size > limit)
            {
                // The size has to be read before the stream goes, not after.
                await buffer.DisposeAsync().ConfigureAwait(false);
                throw TooLarge(source, size, limit);
            }

            buffer.Write(chunk, 0, read);
        }

        buffer.Position = 0;
        return buffer;
    }

    private static BitmapImage Decode(MemoryStream encoded, int decodePixelWidth)
    {
        // The header alone gives the natural width, which is what keeps DecodePixelWidth from
        // asking a 64-pixel icon to decode at 400 and paying for the upscale in memory.
        var probe = BitmapFrame.Create(encoded, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
        int natural = probe.PixelWidth;

        encoded.Position = 0;

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = encoded;

        if (decodePixelWidth > 0 && decodePixelWidth < natural)
        {
            bitmap.DecodePixelWidth = decodePixelWidth;
        }

        bitmap.EndInit();

        // Frozen: the decode happens on a thread pool thread and the result crosses to the UI one.
        bitmap.Freeze();
        return bitmap;
    }

    private static void Prune()
    {
        var dead = new List<ImageKey>();

        foreach ((ImageKey key, WeakReference<ImageSource> entry) in Cache)
        {
            if (!entry.TryGetTarget(out _))
            {
                dead.Add(key);
            }
        }

        foreach (ImageKey key in dead)
        {
            Cache.Remove(key);
        }
    }

    private static InvalidDataException TooLarge(Uri source, long size, long limit) =>
        new($"'{source}' is {size} bytes, over the {limit} byte limit in {nameof(ImageLoader)}.{nameof(MaxSourceBytes)}.");

    /// <summary>
    /// What makes two requests the same load. Width is part of it: the same image decoded to two
    /// widths is two bitmaps.
    /// </summary>
    private readonly record struct ImageKey(string Source, int DecodePixelWidth);
}
