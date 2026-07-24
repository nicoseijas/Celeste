using System.Windows;
using Celeste.Wpf.Theming;

namespace Celeste.Gallery;

/// <summary>Entry point for the gallery.</summary>
public partial class App : Application
{
    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Follow Windows until the user picks a theme in the gallery's header.
        ThemeManager.Apply(ApplicationTheme.System);
    }
}
