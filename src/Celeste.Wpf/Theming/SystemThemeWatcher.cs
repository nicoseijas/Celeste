using System.IO;
using System.Security;
using Microsoft.Win32;

namespace Celeste.Wpf.Theming;

/// <summary>
/// Reads the Windows app theme and reports when the user changes it.
/// </summary>
internal static class SystemThemeWatcher
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeValue = "AppsUseLightTheme";

    private static bool _listening;

    /// <summary>Raised when the Windows app theme changes. Not raised on the UI thread.</summary>
    public static event EventHandler? Changed;

    /// <summary>
    /// Returns the current Windows app theme, or <see cref="ApplicationTheme.Light"/> when the
    /// preference cannot be read (older Windows builds, or a policy-restricted registry).
    /// </summary>
    public static ApplicationTheme GetCurrentTheme()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            return key?.GetValue(AppsUseLightThemeValue) is int value && value == 0
                ? ApplicationTheme.Dark
                : ApplicationTheme.Light;
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
        {
            return ApplicationTheme.Light;
        }
    }

    /// <summary>Starts listening for Windows theme changes. Safe to call more than once.</summary>
    public static void Start()
    {
        if (_listening)
        {
            return;
        }

        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        _listening = true;
    }

    /// <summary>Stops listening for Windows theme changes.</summary>
    public static void Stop()
    {
        if (!_listening)
        {
            return;
        }

        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _listening = false;
    }

    private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Color)
        {
            Changed?.Invoke(null, EventArgs.Empty);
        }
    }
}
