using System.IO;
using System.Windows;
using Celeste.Wpf.Theming;

namespace Celeste.Gallery;

/// <summary>Entry point for the gallery.</summary>
public partial class App : Application
{
    private const string CaptureSwitch = "--capture";

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        base.OnStartup(e);

        if (TryGetCaptureDirectory(e.Args, out string? directory))
        {
            Capture(directory);
            return;
        }

        // Follow Windows until the user picks a theme in the gallery's header.
        ThemeManager.Apply(ApplicationTheme.System);

        // Shown here rather than through StartupUri: the capture path opens windows of its own and
        // must not also be handed one.
        new MainWindow().Show();
    }

    private static bool TryGetCaptureDirectory(string[] args, out string directory)
    {
        int switchIndex = Array.IndexOf(args, CaptureSwitch);

        if (switchIndex < 0)
        {
            directory = string.Empty;
            return false;
        }

        directory = switchIndex + 1 < args.Length
            ? Path.GetFullPath(args[switchIndex + 1])
            : Path.GetFullPath("docs/images");

        return true;
    }

    private void Capture(string directory)
    {
        // The capture closes each window it opens, and the default mode would read the last one
        // closing as the application being over — halfway through the second theme.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        try
        {
            Screenshots.CaptureTo(directory);
            Shutdown(0);
        }
        catch (Exception exception)
        {
            // A WinExe has no console of its own, so this reaches the caller only when one is
            // attached. The exit code is what the script goes by.
            Console.Error.WriteLine(exception);
            Shutdown(1);
        }
    }
}
