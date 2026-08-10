# Contributing to Celeste

## Getting set up

You need Windows and a .NET 10 SDK, 10.0.100 or newer. `global.json` asks for `10.0.100` with `rollForward: latestFeature`, which accepts any release SDK in that band or a later one and picks the highest installed — it does not pin you to a feature band, and prereleases are excluded. If a newer band ever starts failing the build, pin it then by moving to `latestPatch` with a base version you have installed; requiring one exact band up front only turns contributors away.

```powershell
dotnet build
dotnet test
dotnet run --project samples/Celeste.Gallery
```

The gallery is the fastest way to see a change. Run it, switch the theme selector between System, Light, and Dark, and look at the control you touched in both themes before opening a pull request.

`dotnet test` runs the suite twice, once for each framework the library ships. The gallery targets `net10.0-windows` only, so the tests that realize it are compiled only there; everything else runs on both.

CI also verifies formatting, coverage, and that the package is usable, all of which you can run yourself:

```powershell
dotnet format --verify-no-changes
dotnet test /p:CollectCoverage=true
dotnet pack src/Celeste.Wpf/Celeste.Wpf.csproj -c Release -o artifacts
./eng/validate-package.ps1
```

The coverage floors live in the test project rather than in a CI command line, and are a ratchet: set just under what the suite reaches, so they catch coverage falling out. Raise them when it climbs. Two areas sit below the rest on purpose — `SystemThemeWatcher`'s registry-failure path and `ThemeManager`'s reaction to a Windows theme change need either `InternalsVisibleTo` or a test that edits the developer's registry, and neither is worth it.

The last command builds a throwaway WPF app outside the repository against the packed `.nupkg`, on both target frameworks. Run it after anything that changes what ships: a new `Themes/` file, a change to the `.csproj`, a new public type.

## Where things live

| Path | Contents |
| --- | --- |
| `src/Celeste.Wpf/Themes/Light.xaml`, `Dark.xaml` | Color tokens. Swapped wholesale at runtime. |
| `src/Celeste.Wpf/Themes/Tokens/` | Theme-independent tokens: spacing, radii, type scale, motion. |
| `src/Celeste.Wpf/Themes/Controls/` | One dictionary per control family. |
| `src/Celeste.Wpf/Themes/Controls.xaml` | Merges every control dictionary. New files must be listed here. |
| `src/Celeste.Wpf/Themes/Generic.xaml` | Default styles for custom controls only. |
| `src/Celeste.Wpf/Controls/` | Custom control classes. |
| `src/Celeste.Wpf/Media/` | Services behind a control rather than controls: `ImageLoader`. |
| `src/Celeste.Wpf/Theming/` | `ThemeManager` and the dictionary types consumers merge. |

## Rules that are not negotiable

**Every key in `Light.xaml` exists in `Dark.xaml`, and the reverse.** The theme dictionaries are replaced as a unit, so a key defined in only one theme becomes a missing brush the moment the user toggles. `ThemeDictionaryTests.LightAndDarkDefineTheSameKeys` fails the build if this drifts.

**Color references inside control styles use `DynamicResource`.** `StaticResource` captures the brush at load time, and the control stops responding to theme changes.

**Non-color values use `DynamicResource` too, or a literal.** Spacing, radii, and font sizes live outside the theme dictionaries and do not change with the theme, but keeping them dynamic lets consumers override them from application resources.

**Do not add an implicit style for a type WPF uses internally.** An implicit `TextBlock` style overrides the font a `Button` passes down to its own content; an implicit `ScrollViewer` style retemplates the internal `PART_ContentHost` of every `TextBox`. Both are keyed-only in this repository, with a comment explaining why. If you find another case, key it and write down the reason.

## Adding a control style

1. Put it in the right `Themes/Controls/*.xaml`, or add a new file and list it in `Themes/Controls.xaml`.
2. Give it a keyed style named `Celeste.<ControlName>`, then add the implicit `<Style BasedOn="..." TargetType="..." />` on the next line if the style should apply automatically.
3. Cover the states: `IsMouseOver`, pressed or checked where it applies, `IsKeyboardFocused` (the focus ring), and `IsEnabled="False"`.
4. Add the control to `ControlTemplateTests.ControlNames`. That test applies the template and runs a layout pass in both themes, which catches a trigger pointing at a name the template does not define.
5. Add it to the gallery. `GalleryTests` shows the gallery window off-screen and selects every tab in both themes, so anything you put there is exercised on a real WPF layout pass. If you add a tab, extend `GalleryTests.TabIndexes`.

## Globalization

Do not set `InvariantGlobalization`. WPF's font fallback builds `CultureInfo` objects by name, and in globalization-invariant mode that throws `CultureNotFoundException` from inside `TextBlock.MeasureOverride`. It only fails on text that reaches the fallback path, so the symptom is a crash on one screen rather than at startup.

## Adding a custom control

Custom controls live in `src/Celeste.Wpf/Controls/` as lookless `Control` subclasses with `DefaultStyleKeyProperty.OverrideMetadata` in a static constructor. The default style goes in its own dictionary under `Themes/Controls/`, listed in **both** `Controls.xaml` and `Generic.xaml`.

Public members need XML documentation. `GenerateDocumentationFile` is on and warnings are errors, so a missing `<summary>` fails the build.

## Cross-file resource references

A `StaticResource` in one dictionary cannot see a key in a sibling dictionary that happens to be merged alongside it. If two styles share a `ControlTemplate`, keep them in the same file — this is why `Celeste.ToggleButton` lives in `Button.xaml` rather than `Selection.xaml`.

## Code style

`.editorconfig` is the authority; warnings are errors across the solution. Analyzer suppressions belong in `.editorconfig` with a comment explaining the reason, not scattered as `#pragma` in source.

## Commits and pull requests

Commit subjects use `<type>: <description>` — `feat`, `fix`, `refactor`, `docs`, `test`, `chore`, `perf`, `ci`.

A pull request should say what changed, which controls or themes it affects, and how you verified it. A before/after screenshot of the gallery in both themes is worth more than a paragraph for anything visual.
