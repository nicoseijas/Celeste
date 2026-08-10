using System.Windows;

namespace Celeste.Wpf.Theming;

/// <summary>
/// Switches the Celeste theme at runtime.
/// </summary>
/// <remarks>
/// The manager rewrites the <see cref="ThemesDictionary"/> that the application merged in
/// <c>App.xaml</c>. Controls that reference tokens with <c>DynamicResource</c> — every built-in
/// Celeste style does — repaint immediately.
/// </remarks>
public static class ThemeManager
{
    private static ApplicationTheme _requestedTheme = ApplicationTheme.System;

    // What ThemeChanged last reported. Seeded with what is painted before anyone applies anything,
    // so applying the theme the application already has does not announce a change.
    private static ApplicationTheme _announcedTheme = SystemThemeWatcher.GetCurrentTheme();

    /// <summary>
    /// Raised on the UI thread after the effective theme changes.
    /// </summary>
    public static event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    /// <summary>
    /// Gets the theme the application asked for, which may be <see cref="ApplicationTheme.System"/>.
    /// </summary>
    public static ApplicationTheme RequestedTheme => _requestedTheme;

    /// <summary>
    /// Gets the theme currently painted, which is never <see cref="ApplicationTheme.System"/>.
    /// </summary>
    /// <remarks>
    /// Read from the merged <see cref="ThemesDictionary"/> rather than from a field of this class.
    /// The dictionary's own <see cref="ThemesDictionary.Theme"/> is writable, so a copy kept here
    /// could report a theme other than the one on screen. Before a dictionary is merged there is
    /// nothing painted yet, and this reports the Windows theme.
    /// </remarks>
    public static ApplicationTheme CurrentTheme =>
        FindThemesDictionary()?.ResolvedTheme ?? SystemThemeWatcher.GetCurrentTheme();

    /// <summary>
    /// Applies <paramref name="theme"/> to the current application.
    /// </summary>
    /// <param name="theme">
    /// The theme to apply. <see cref="ApplicationTheme.System"/> resolves to the Windows app theme
    /// and keeps tracking it until a different theme is applied.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// No <see cref="ThemesDictionary"/> is merged into <see cref="Application.Current"/>'s resources.
    /// </exception>
    public static void Apply(ApplicationTheme theme)
    {
        _requestedTheme = theme;

        if (theme == ApplicationTheme.System)
        {
            SystemThemeWatcher.Changed -= OnSystemThemeChanged;
            SystemThemeWatcher.Changed += OnSystemThemeChanged;
            SystemThemeWatcher.Start();
        }
        else
        {
            SystemThemeWatcher.Changed -= OnSystemThemeChanged;

            // Nothing else listens, so the watcher's own hook into SystemEvents can go too.
            SystemThemeWatcher.Stop();
        }

        ApplyResolved(theme == ApplicationTheme.System ? SystemThemeWatcher.GetCurrentTheme() : theme);
    }

    private static void ApplyResolved(ApplicationTheme theme)
    {
        ThemesDictionary dictionary = FindThemesDictionary()
            ?? throw new InvalidOperationException(
                "No ThemesDictionary was found in Application.Current.Resources. " +
                "Merge <celeste:ThemesDictionary /> into App.xaml before calling ThemeManager.Apply.");

        // Compared against what the dictionary actually holds. Comparing against a theme recorded
        // here would skip the assignment for a dictionary that had been pointed elsewhere directly,
        // and leave this class reporting a theme the application is not painting.
        if (dictionary.ResolvedTheme != theme)
        {
            dictionary.Theme = theme;
        }
        else if (_announcedTheme == theme)
        {
            return;
        }

        _announcedTheme = theme;
        ThemeChanged?.Invoke(null, new ThemeChangedEventArgs(theme));
    }

    private static void OnSystemThemeChanged(object? sender, EventArgs e)
    {
        // SystemEvents raises this on its own thread; resource dictionaries belong to the UI thread.
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            // The application may have pinned a theme between the notification and this callback
            // running, and a queued Windows change must not overrule an explicit choice.
            if (_requestedTheme == ApplicationTheme.System)
            {
                ApplyResolved(SystemThemeWatcher.GetCurrentTheme());
            }
        });
    }

    private static ThemesDictionary? FindThemesDictionary()
    {
        ResourceDictionary? resources = Application.Current?.Resources;
        return resources is null ? null : Find(resources);

        static ThemesDictionary? Find(ResourceDictionary dictionary)
        {
            foreach (ResourceDictionary merged in dictionary.MergedDictionaries)
            {
                if (merged is ThemesDictionary themes)
                {
                    return themes;
                }

                if (Find(merged) is { } nested)
                {
                    return nested;
                }
            }

            return null;
        }
    }
}
