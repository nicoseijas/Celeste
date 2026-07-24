namespace Celeste.Wpf;

/// <summary>
/// Library-wide constants.
/// </summary>
public static class CelesteUi
{
    /// <summary>
    /// The XAML namespace that exposes every public Celeste type under a single <c>xmlns</c>.
    /// </summary>
    public const string XmlNamespace = "https://celeste-ui.dev/wpf";

    internal const string AssemblyName = "Celeste.Wpf";

    internal static Uri PackUri(string relativePath) =>
        new($"pack://application:,,,/{AssemblyName};component/{relativePath}", UriKind.Absolute);
}
