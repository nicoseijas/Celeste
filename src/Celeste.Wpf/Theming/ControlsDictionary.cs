using System.Windows;

namespace Celeste.Wpf.Theming;

/// <summary>
/// A <see cref="ResourceDictionary"/> that holds every Celeste control style.
/// Merge one of these into your <c>App.xaml</c>, after the <see cref="ThemesDictionary"/>:
/// <code language="xml">
/// &lt;celeste:ControlsDictionary /&gt;
/// </code>
/// </summary>
public sealed class ControlsDictionary : ResourceDictionary
{
    /// <summary>Initializes a new instance of the <see cref="ControlsDictionary"/> class.</summary>
    public ControlsDictionary() => Source = CelesteUi.PackUri("Themes/Controls.xaml");
}
