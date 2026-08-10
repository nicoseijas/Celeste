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
    private ApplicationTheme _resolvedTheme;

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

    /// <summary>
    /// The theme whose tokens are actually loaded, with <see cref="ApplicationTheme.System"/>
    /// already resolved. This is what is painted, and <see cref="ThemeManager"/> reports it rather
    /// than keeping a second copy that can drift from it.
    /// </summary>
    internal ApplicationTheme ResolvedTheme => _resolvedTheme;

    /// <summary>Initializes a new instance of the <see cref="ThemesDictionary"/> class.</summary>
    public ThemesDictionary() => SetSourceFor(_theme);

    private void SetSourceFor(ApplicationTheme theme)
    {
        ApplicationTheme resolved = theme == ApplicationTheme.System
            ? SystemThemeWatcher.GetCurrentTheme()
            : theme;

        _resolvedTheme = resolved;
        Source = CelesteUi.PackUri($"Themes/{resolved}.xaml");
    }
}
