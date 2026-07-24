using System.Windows;
using System.Windows.Controls;
using Celeste.Wpf.Theming;

namespace Celeste.Gallery;

/// <summary>The gallery's only window.</summary>
public partial class MainWindow : Window
{
    /// <summary>Initializes a new instance of the <see cref="MainWindow"/> class.</summary>
    public MainWindow()
    {
        InitializeComponent();
        ThemeSelector.SelectedIndex = (int)ThemeManager.RequestedTheme;
    }

    private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized)
        {
            return;
        }

        ThemeManager.Apply((ApplicationTheme)ThemeSelector.SelectedIndex);
    }
}
