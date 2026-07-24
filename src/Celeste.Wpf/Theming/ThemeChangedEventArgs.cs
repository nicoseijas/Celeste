namespace Celeste.Wpf.Theming;

/// <summary>
/// Carries the theme that was applied.
/// </summary>
/// <param name="theme">The theme now in effect. Never <see cref="ApplicationTheme.System"/>.</param>
public sealed class ThemeChangedEventArgs(ApplicationTheme theme) : EventArgs
{
    /// <summary>Gets the theme now in effect.</summary>
    public ApplicationTheme Theme { get; } = theme;
}
