# Celeste

Celeste is a UI library for WPF applications that need to look current without adopting the Fluent or Material design language. It restyles the controls WPF already ships, adds the few it never did, and drives everything from one set of semantic color tokens that can be swapped at runtime.

## What it gives you

- **Light and dark themes** built from the same token names, switchable while the app runs. `ThemeManager.Apply(ApplicationTheme.System)` follows the Windows app theme and keeps following it.
- **Restyled built-in controls**: `Button`, `TextBox`, `PasswordBox`, `CheckBox`, `RadioButton`, `ComboBox`, `ListBox`, `TabControl`, `Slider`, `ProgressBar`, `ScrollBar`, `ToolTip`, `Label`, `Separator`.
- **Controls WPF does not have**: `Card`, `Badge`, `ToggleSwitch`, `ProgressRing`.
- **Button variants** — primary, secondary, destructive, outline, ghost, link — that all share one `ControlTemplate`. Defining a new variant means setting three brushes, not copying a template.
- **A type scale** and layout tokens (spacing, radii, control heights) exposed as XAML resources, so your own controls can sit on the same grid as the library's.

## Installation

```powershell
dotnet add package Celeste.Wpf
```

Then merge the two dictionaries into `App.xaml`. Order matters only for readability — control styles reference color tokens through `DynamicResource`.

```xml
<Application x:Class="MyApp.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:celeste="https://celeste-ui.dev/wpf"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <celeste:ThemesDictionary Theme="System" />
                <celeste:ControlsDictionary />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

Apply `Celeste.Window` to your window. Every control below it inherits the theme's font, foreground, and background:

```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:celeste="https://celeste-ui.dev/wpf"
        Style="{StaticResource Celeste.Window}"
        Title="My app" Width="600" Height="400">

    <celeste:Card Header="Workspace" Description="Everyone with the link can view this."
                  Margin="24">
        <StackPanel>
            <TextBox celeste:ControlHelper.PlaceholderText="Workspace name" />
            <celeste:ToggleSwitch Content="Public" Margin="0,12,0,0" />
        </StackPanel>
        <celeste:Card.Footer>
            <Button Content="Save" Style="{StaticResource Celeste.Button.Primary}"
                    HorizontalAlignment="Right" />
        </celeste:Card.Footer>
    </celeste:Card>
</Window>
```

## Switching themes at runtime

```csharp
using Celeste.Wpf.Theming;

ThemeManager.Apply(ApplicationTheme.Dark);    // pin dark
ThemeManager.Apply(ApplicationTheme.System);  // follow Windows, and keep following it
```

`ThemeManager` rewrites the `ThemesDictionary` you merged in `App.xaml`. Controls repaint immediately because every built-in style references color tokens through `DynamicResource`. If your own XAML uses `StaticResource` for a Celeste brush, it will keep the color it was given at load time.

## Theming it

Colors are semantic, not literal. `Celeste.Brush.Primary` is "the brand action color", and the light and dark dictionaries define it differently. To recolor the library, override the tokens after merging Celeste:

```xml
<ResourceDictionary.MergedDictionaries>
    <celeste:ThemesDictionary Theme="System" />
    <celeste:ControlsDictionary />
</ResourceDictionary.MergedDictionaries>

<SolidColorBrush x:Key="Celeste.Brush.Primary" Color="#FF7C3AED" />
<SolidColorBrush x:Key="Celeste.Brush.PrimaryHover" Color="#FF6D28D9" />
```

The full token list lives in [`src/Celeste.Wpf/Themes/Light.xaml`](src/Celeste.Wpf/Themes/Light.xaml). Every key defined there also exists in `Dark.xaml`; a test enforces that, because a key present in only one theme becomes a missing brush the moment a user toggles.

For per-control tweaks without retemplating, `Celeste.Wpf.Controls.ControlHelper` exposes attached properties: `CornerRadius`, `PlaceholderText`, and `IconContent`.

## When Celeste is the wrong choice

- **You want Windows 11 Fluent.** [WPF-UI](https://github.com/lepoco/wpfui) implements that design language properly, including Mica and the Fluent icon set. Celeste has its own look and will never match Windows chrome.
- **You want Material Design.** Use [MaterialDesignInXamlToolkit](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit).
- **You need a data grid, docking, ribbons, or charts.** Celeste has none of these and is not trying to become a control suite.
- **You are on .NET Framework.** The package targets `net8.0-windows` and `net10.0-windows` only.

## Status

Alpha. The token names and the public control API are still open to change; treat `0.x` releases as breaking. What exists is covered by tests that apply every control's template in both themes on a real WPF dispatcher, so the styles load and measure — but they have not been through a wide range of real applications yet.

Not covered yet: `Menu` / `ContextMenu`, `DataGrid`, `TreeView`, `Expander`, `Calendar` / `DatePicker`, dialogs, and navigation. Those controls keep their default WPF appearance in a Celeste application, which is visible and jarring. Contributions in that direction are the most useful ones right now.

[ROADMAP.md](ROADMAP.md) has the order those gaps are being closed in, plus what is deliberately out of scope.

## Repository layout

| Path | Contents |
| --- | --- |
| `src/Celeste.Wpf` | The library. Custom controls in `Controls/`, styles and tokens in `Themes/`. |
| `samples/Celeste.Gallery` | A WPF app showing every control in both themes. `dotnet run --project samples/Celeste.Gallery` |
| `tests/Celeste.Wpf.Tests` | Theme parity and template-application tests. |

## Building

```powershell
dotnet build
dotnet test
dotnet run --project samples/Celeste.Gallery
```

Requires the .NET 10 SDK (pinned in `global.json`) and Windows. The library itself also builds for `net8.0-windows`.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). New control styles need an entry in the gallery and a row in the template-application test; that is what keeps a style from silently breaking in one of the two themes.

## License

MIT. See [LICENSE](LICENSE).
