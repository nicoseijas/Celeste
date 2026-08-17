using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Celeste.Wpf.Controls;
using Celeste.Wpf.Theming;

namespace Celeste.Gallery;

/// <summary>
/// Renders the gallery to the PNG files the documentation links to.
/// </summary>
/// <remarks>
/// The pictures come from the running sample rather than from a test host, for the same reason a
/// visual change is verified by running it: a host that builds elements the application never
/// builds can show something the application does not.
/// </remarks>
internal static class Screenshots
{
    private const int WindowWidth = 1180;
    private const int WindowHeight = 820;

    /// <summary>
    /// The tab the two themes are cut together from, by header. It has to be one whose content
    /// reaches both edges, or the half on the right is a picture of empty surface.
    /// </summary>
    private const string HeroTab = "Display";

    private static readonly TimeSpan PictureTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(20);

    /// <summary>Colour of the seam between the two halves of the hero shot, readable on both.</summary>
    private static readonly SolidColorBrush Seam = new(Color.FromRgb(0x94, 0xA3, 0xB8));

    public static void CaptureTo(string directory)
    {
        Directory.CreateDirectory(directory);

        Dictionary<string, RenderTargetBitmap> light = RenderEveryTab(ApplicationTheme.Light);
        Dictionary<string, RenderTargetBitmap> dark = RenderEveryTab(ApplicationTheme.Dark);

        foreach ((string tab, BitmapSource bitmap) in light)
        {
            Save(bitmap, Path.Combine(directory, $"{Slug(tab)}-light.png"));
        }

        foreach ((string tab, BitmapSource bitmap) in dark)
        {
            Save(bitmap, Path.Combine(directory, $"{Slug(tab)}-dark.png"));
        }

        Save(CutTogether(light[HeroTab], dark[HeroTab]), Path.Combine(directory, "hero.png"));
    }

    /// <summary>
    /// Shows the gallery under <paramref name="theme"/> and renders every tab, keyed by header.
    /// </summary>
    private static Dictionary<string, RenderTargetBitmap> RenderEveryTab(ApplicationTheme theme)
    {
        // Before the window exists: MainWindow reads the requested theme to set its own selector,
        // and picking a theme in that selector is what would otherwise apply one.
        ThemeManager.Apply(theme);

        var window = new MainWindow
        {
            WindowStartupLocation = WindowStartupLocation.Manual,
            ShowInTaskbar = false,
            Left = -4000,
            Top = -4000,
            Width = WindowWidth,
            Height = WindowHeight,
        };

        var captures = new Dictionary<string, RenderTargetBitmap>(StringComparer.Ordinal);

        try
        {
            window.Show();
            window.UpdateLayout();

            var tabs = (TabControl)window.FindName("Sections");

            for (int index = 0; index < tabs.Items.Count; index++)
            {
                var tab = (TabItem)tabs.Items[index];
                tabs.SelectedIndex = index;
                window.UpdateLayout();

                WaitForPictures(window);

                captures[(string)tab.Header] = Render(window);
            }
        }
        finally
        {
            window.Close();
        }

        return captures;
    }

    /// <summary>
    /// Renders the window's client area at 96 dpi, so the file is the same size whatever the
    /// monitor the capture ran on is scaled to.
    /// </summary>
    private static RenderTargetBitmap Render(Window window)
    {
        // The template child, not Content: the Celeste.Window style paints the background there, and
        // rendering Content alone would cut it out.
        var root = (FrameworkElement)VisualTreeHelper.GetChild(window, 0);

        var bitmap = new RenderTargetBitmap(
            (int)Math.Round(root.ActualWidth),
            (int)Math.Round(root.ActualHeight),
            96,
            96,
            PixelFormats.Pbgra32);

        bitmap.Render(root);
        bitmap.Freeze();

        return bitmap;
    }

    /// <summary>
    /// Puts the left half of <paramref name="left"/> beside the right half of
    /// <paramref name="right"/>. Both are the same tab at the same size, so the seam shows the
    /// theme changing and nothing else.
    /// </summary>
    private static RenderTargetBitmap CutTogether(BitmapSource left, BitmapSource right)
    {
        int width = Math.Min(left.PixelWidth, right.PixelWidth);
        int height = Math.Min(left.PixelHeight, right.PixelHeight);
        int seam = width / 2;

        var visual = new DrawingVisual();

        using (DrawingContext context = visual.RenderOpen())
        {
            var full = new Rect(0, 0, width, height);

            context.PushClip(new RectangleGeometry(new Rect(0, 0, seam, height)));
            context.DrawImage(left, full);
            context.Pop();

            context.PushClip(new RectangleGeometry(new Rect(seam, 0, width - seam, height)));
            context.DrawImage(right, full);
            context.Pop();

            context.DrawRectangle(Seam, pen: null, new Rect(seam, 0, 1, height));
        }

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();

        return bitmap;
    }

    /// <summary>
    /// Drains the dispatcher until no picture on screen is still loading. An
    /// <see cref="ImageView"/> decodes off the UI thread, so a tab captured the moment it is
    /// selected is a tab full of placeholders.
    /// </summary>
    private static void WaitForPictures(DependencyObject root)
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        DateTime deadline = DateTime.UtcNow + PictureTimeout;

        while (Descendants(root).OfType<ImageView>().Any(view => view.State == ImageState.Loading))
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException(
                    $"A picture was still loading after {PictureTimeout.TotalSeconds:0} s.");
            }

            // Lowest priority runs everything queued above it, which is where a finished decode
            // posts itself back to.
            dispatcher.Invoke(static () => { }, DispatcherPriority.SystemIdle);
            Thread.Sleep(PollInterval);
        }

        dispatcher.Invoke(static () => { }, DispatcherPriority.SystemIdle);
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        int count = VisualTreeHelper.GetChildrenCount(root);

        for (int index = 0; index < count; index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, index);
            yield return child;

            foreach (DependencyObject descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    private static void Save(BitmapSource bitmap, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using FileStream file = File.Create(path);
        encoder.Save(file);
    }

    private static string Slug(string header) =>
        header.Replace(' ', '-').ToLowerInvariant();
}
