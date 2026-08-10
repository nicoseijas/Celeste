using System.Windows;
using System.Windows.Threading;
using Celeste.Wpf.Theming;

namespace Celeste.Wpf.Tests;

/// <summary>
/// Owns the one <see cref="Application"/> the test process is allowed to have, on a dedicated STA
/// thread, and runs test bodies on it.
/// </summary>
/// <remarks>
/// WPF objects require an STA thread, resource lookup for merged theme dictionaries goes through
/// <see cref="Application.Current"/>, and WPF refuses to create a second <see cref="Application"/>
/// in a process. xUnit worker threads are MTA, so every test that touches a control hops here.
/// </remarks>
internal static class StaTestHost
{
    private static readonly TimeSpan PumpTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(5);

    private static readonly Lock Gate = new();
    private static Dispatcher? _dispatcher;
    private static ThemesDictionary? _themes;

    /// <summary>
    /// Runs <paramref name="body"/> on the host thread under <paramref name="theme"/> and rethrows
    /// whatever it threw.
    /// </summary>
    public static void Run(Action body, ApplicationTheme theme = ApplicationTheme.Light)
    {
        ArgumentNullException.ThrowIfNull(body);

        Dispatcher dispatcher = EnsureStarted();

        // One test at a time: the shared Application resources carry the theme under test.
        lock (Gate)
        {
            dispatcher.Invoke(() =>
            {
                _themes!.Theme = theme;
                body();
            });
        }
    }

    /// <summary>
    /// Drains the dispatcher queue until <paramref name="condition"/> holds, the way a running
    /// application would. Call it from inside <see cref="Run"/>: work that a background operation
    /// posts back to the UI thread cannot run while a test body is sitting on that thread.
    /// </summary>
    /// <param name="condition">What the test is waiting for.</param>
    /// <param name="because">What to say if it never happens.</param>
    /// <exception cref="TimeoutException">The condition did not hold within the timeout.</exception>
    public static void PumpUntil(Func<bool> condition, string because)
    {
        ArgumentNullException.ThrowIfNull(condition);

        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        DateTime deadline = DateTime.UtcNow + PumpTimeout;

        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException($"Waited {PumpTimeout.TotalSeconds:0} s: {because}");
            }

            // Invoking at the lowest priority runs everything queued above it first.
            dispatcher.Invoke(static () => { }, DispatcherPriority.SystemIdle);

            if (!condition())
            {
                Thread.Sleep(PollInterval);
            }
        }
    }

    private static Dispatcher EnsureStarted()
    {
        lock (Gate)
        {
            if (_dispatcher is not null)
            {
                return _dispatcher;
            }

            using var ready = new ManualResetEventSlim();

            var thread = new Thread(() =>
            {
                var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                _themes = new ThemesDictionary { Theme = ApplicationTheme.Light };
                application.Resources.MergedDictionaries.Add(_themes);
                application.Resources.MergedDictionaries.Add(new ControlsDictionary());

                _dispatcher = Dispatcher.CurrentDispatcher;
                ready.Set();
                Dispatcher.Run();
            })
            {
                IsBackground = true,
                Name = "Celeste STA test host",
            };

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            ready.Wait();

            return _dispatcher!;
        }
    }
}
