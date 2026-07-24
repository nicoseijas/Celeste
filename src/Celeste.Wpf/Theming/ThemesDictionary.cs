using System.Windows;

namespace Celeste.Wpf.Theming;

/// <summary>
/// A <see cref="ResourceDictionary"/> that holds the Celeste color tokens for one theme.
/// Merge exactly one of these into your <c>App.xaml</c>:
/// <code language="xml">
/// &lt;celeste:ThemesDictionary Theme="System" /&gt;
/// </code>
/// </summary>
/// <remarks>
/// <see cref="ThemeManager"/> finds this dictionary in the application resources and swaps its
/// <see cref="ResourceDictionary.Source"/> when the theme changes, so every
/// <c>DynamicResource</c> reference updates without restarting the app.
/// </remarks>
public sealed class ThemesDictionary : ResourceDictionary
{
    private ApplicationTheme _theme = ApplicationTheme.System;

    /// <summary>
    /// Gets or sets the theme this dictionary provides. Defaults to <see cref="ApplicationTheme.System"/>.
    /// </summary>
    public ApplicationTheme Theme
    {
        get => _theme;
        set
        {
            _theme = value;
            SetSourceFor(value);
        }
    }

    /// <summary>Initializes a new instance of the <see cref="ThemesDictionary"/> class.</summary>
    public ThemesDictionary() => SetSourceFor(_theme);

    private void SetSourceFor(ApplicationTheme theme)
    {
        ApplicationTheme resolved = theme == ApplicationTheme.System
            ? SystemThemeWatcher.GetCurrentTheme()
            : theme;

        Source = CelesteUi.PackUri($"Themes/{resolved}.xaml");
    }
}
