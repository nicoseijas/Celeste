using System.Windows;
using System.Windows.Markup;

// Custom controls resolve their default style from Themes/Generic.xaml in this assembly.
[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]

// Single XAML namespace for the whole library, so consumers write one xmlns.
[assembly: XmlnsDefinition(Celeste.Wpf.CelesteUi.XmlNamespace, "Celeste.Wpf")]
[assembly: XmlnsDefinition(Celeste.Wpf.CelesteUi.XmlNamespace, "Celeste.Wpf.Controls")]
[assembly: XmlnsDefinition(Celeste.Wpf.CelesteUi.XmlNamespace, "Celeste.Wpf.Theming")]
[assembly: XmlnsPrefix(Celeste.Wpf.CelesteUi.XmlNamespace, "celeste")]
