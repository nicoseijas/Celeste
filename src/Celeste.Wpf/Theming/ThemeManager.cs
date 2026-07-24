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
    public static ApplicationTheme CurrentTheme { get; private set; } = SystemThemeWatcher.GetCurrentTheme();

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
        }

        ApplyResolved(theme == ApplicationTheme.System ? SystemThemeWatcher.GetCurrentTheme() : theme);
    }

    private static void ApplyResolved(ApplicationTheme theme)
    {
        ThemesDictionary dictionary = FindThemesDictionary()
            ?? throw new InvalidOperationException(
                "No ThemesDictionary was found in Application.Current.Resources. " +
                "Merge <celeste:ThemesDictionary /> into App.xaml before calling ThemeManager.Apply.");

        if (CurrentTheme == theme && dictionary.Source is not null)
        {
            return;
        }

        dictionary.Theme = theme;
        CurrentTheme = theme;
        ThemeChanged?.Invoke(null, new ThemeChangedEventArgs(theme));
    }

    private static void OnSystemThemeChanged(object? sender, EventArgs e)
    {
        // SystemEvents raises this on its own thread; resource dictionaries belong to the UI thread.
        Application.Current?.Dispatcher.BeginInvoke(
            () => ApplyResolved(SystemThemeWatcher.GetCurrentTheme()));
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
