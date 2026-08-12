# Changelog

Consumer-visible changes to `Celeste.Wpf`: what changes for an application that upgrades, rather than what happened in the repository. `0.x` releases may break the API, which is what the version number is telling you.

## Unreleased

### Added

- **`Sidebar`**, a navigation list that collapses from a labelled pane to a 56px icon rail. Items carry an icon and an optional badge, with group headers and separators between them; arrow keys step over both. In the rail the label becomes the tooltip, so give every item an `Icon` if the sidebar can collapse.
- **`SidebarHost`**, which reads its own width and presents the pane docked, as a rail, or off-canvas over the content. Off-canvas draws a scrim, traps Tab inside the pane, and gives focus back when Esc or the scrim closes it. The host never opens the pane itself — route `SidebarHost.TogglePaneCommand` to a button of your own.
- **`NavigationHost`**, which holds the current page, keeps a capped back stack, and puts each page back at the scroll position it was left at on `BrowseBack`. Forward navigation starts at the top.
- **`ImageView`, `Avatar`, and `MasonryPanel`.** `ImageView` reads and decodes a URI on a background thread, at the width the control was laid out at rather than at full resolution, and shares the result with every other view showing the same picture at the same size. `Avatar` falls back to initials. `MasonryPanel` puts each child in the column that is shortest at the time.
- **An automation peer on every custom control**, each reporting a control type and, where it has one to give, a name — a card's header, a badge's content, an avatar's initials. `ProgressRing` reports a progress bar and `ToggleSwitch` reports a switch rather than a toggle button. A picture is left unnamed rather than named after its file, so missing alternative text stays visible as missing.
- **`Celeste.ToggleButton.Segment`**, a `RadioButton` carrying the toggle-button appearance, for a row of buttons that is a single choice: one option checked per `GroupName`, and clicking the checked one keeps it checked instead of leaving the choice empty.
- **Debugging symbols** in the published package.

### Changed

- **A bare `ToggleButton` is now styled.** Celeste declared the style by key only, so a plain one kept its Windows appearance. Every `<ToggleButton>` in an application that merges `ControlsDictionary` changes appearance on upgrade; set `Style="{x:Null}"` on one to keep the Windows look.
- **Animations stop when the user has turned animations off in Windows.** The progress ring, the indeterminate progress bar, the toggle switch's travel, and the image fade now check `SystemParameters.ClientAreaAnimation`. Each control still shows its state — it stops moving, rather than disappearing.
- **Three light-theme tokens darkened to meet WCAG AA for text.** `Success` and `Warning` move to their darker step and `MutedForeground` darkens; against their backgrounds the pairs measured 3.30:1, 3.19:1, and 4.28:1, below the 4.5:1 minimum for text under 18.66px bold or 24px regular. An application painting with those brushes sees the new colours.

### Fixed

- `Label` painted black on every surface instead of taking the theme's foreground, which left it unreadable on the dark theme. Windows styles `Label` with a foreground of its own, and a theme style beats the value a control would otherwise inherit.
- `ThemeManager.CurrentTheme` could report a theme the application was not painted in. `ThemesDictionary.Theme` is public and writable, so the manager's own copy could disagree with it, and `Apply` would then decide there was nothing to do.
- A `Slider` gave no visible sign of holding keyboard focus.
- `Celeste.Button.Small` and `Celeste.Button.Large` recoloured the button instead of changing only its metrics, so a button that asked to be a different size also stopped being the variant it was.
- `Badge` used a corner radius that did not match the rest of the library.
- The documented way to install the package did not work. The package named a repository that does not exist, and the README ships as the package README, so its relative links resolved against nuget.org and returned 404.

## 0.1.0-alpha.1

The first published package. This changelog starts after it.
