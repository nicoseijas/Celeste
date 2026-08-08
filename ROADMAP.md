# Roadmap

What Celeste plans to add, in the order it plans to add it. Items move down the list when they get in the way of something above them, not on a schedule — there are no release dates here.

Version numbers are `0.x` throughout, so every entry below may break the public API. See [README.md](README.md#status) for what already exists.

## Legend

| Mark | Meaning |
| --- | --- |
| ✅ | Shipped. Kept here so the milestone still reads as a whole. |
| 🔜 | Next up. Design settled enough to start. |
| 🧭 | Planned. Shape still open. |
| 💭 | Considered. May never happen. |

---

## 0.2 — Chrome and shell

The pieces an application needs before it can look like one window instead of a page of controls.

### 🔜 Replacing the system title bar

The default Windows caption is the one part of a Celeste window the library does not control: it keeps the OS colors, the OS font, and the OS height, directly above a themed client area. A window style that turns it off and draws its own is the first item on this list.

**`Celeste.Window.Chrome` — a themed title bar.** A window style built on `WindowChrome` that removes the system caption and draws a Celeste one in its place.

- Themed caption with title, icon, and a right-aligned area the application can fill with its own content (search box, account button).
- Minimize / maximize / close buttons as Celeste styles. The snap-layouts flyout that Windows 11 shows on hover over the maximize button has to keep working, which means answering `WM_NCHITTEST` with `HTMAXBUTTON` rather than handling a plain `Click`.
- Correct maximized metrics. A `WindowChrome` window sized to the monitor bounds hides its own caption and part of its border under the screen edge unless the frame is compensated per monitor DPI on `WM_GETMINMAXINFO`.
- Drag, double-click-to-maximize, `Alt+Space` for the system menu, and the resize borders all keep behaving as they do on a system caption. A custom title bar that cannot be dragged from an empty area of itself is the usual failure here.
- Off by default. `Celeste.Window` keeps the system caption, so existing consumers are unaffected and applications that want the OS title bar are not forced off it.

**`Celeste.Window.Borderless` — no title bar at all.** The same style with the caption region collapsed, for splash screens, overlays, tool windows, and kiosk-style full-screen views.

- The window still resizes and still snaps; only the caption is gone. This is the difference from setting `WindowStyle="None"` by hand, which also drops the resize borders and the drop shadow.
- A `WindowHelper.TitleBarDragArea` attached property, so any element the application chooses acts as the drag handle — otherwise a window with no caption is a window that cannot be moved.
- Escape hatch guidance: a borderless full-screen window with no visible close affordance needs a keyboard path out, and the sample provides one.

### 🧭 Window backdrops

Optional acrylic and Mica-like backdrops on Windows 11, off by default. Celeste is not a Fluent library and will not adopt Fluent metrics, but a translucent window background is a look, not a design language.

---

## 0.3 — Navigation

### ✅ `Sidebar`

A vertical navigation control — the thing every one of these applications builds by hand out of a `ListBox` and a `Border`.

- `SidebarItem` with icon, label, and an optional trailing badge; `SidebarGroupHeader` and `SidebarSeparator` for grouping; a `Footer` region pinned to the bottom while the items scroll.
- Selection is the source of truth and is bindable, so the sidebar works with any navigation stack the application already has. Celeste supplies no router.
- **Collapsed (rail) state**: icons only at `CollapsedWidth`, with the label surfacing as a tooltip and group headers surviving as rules. `CollapsedHeader` puts a logo mark where the header name was, and the band is dropped rather than left blank when there is no mark and no chevron. `IsCollapsed` binds two ways, so the built-in chevron and the application's own layout rules drive the same property.
- Keyboard navigation across items, inherited from `ListBox`, stepping over headers and separators; a focus ring that survives the rail.
- `SidebarAutomationPeer` keeps the list and selection semantics a screen reader needs to announce "3 of 7", and renames the control type so the user hears which list they landed in.

Still open, and deliberately not blocking the milestone: an icon-less item is an empty row in the rail, so a collapsible sidebar has to supply icons. The width is animated but nothing else is.

### ✅ Off-canvas `Sidebar`

`SidebarHost` pairs a pane with the content beside it and owns the presentation: docked, rail, or off-canvas as an overlay that slides in over the content instead of taking layout space.

- Overlay presentation with a scrim behind it; clicking the scrim or pressing `Esc` dismisses.
- Focus is trapped while open and returned to the invoking element on close — an overlay that leaves `Tab` walking through the content underneath is worse than no overlay.
- Slide and scrim fade use `Celeste.Duration.Fast`, and both collapse to an instant transition when the user has disabled animations in Windows.
- **Responsive switching**: `DisplayMode="Auto"` picks the presentation from the host's own width, with `RailBreakpoint` and `OffCanvasBreakpoint` defaulted from tokens. Setting an explicit mode stops the breakpoints being consulted, so an application that drives the mode itself is not fighting them. `ActualDisplayMode` is the resolved value, bindable.
- `TogglePaneCommand` is the way in: Celeste never opens the pane itself, because it cannot know where the application's menu button lives.

Still open: the host has no automation peer of its own, and an open overlay is not announced as modal. Focus is trapped and returned, which is the behavioral half of that.

### ✅ `NavigationHost`

A content host that pairs with `Sidebar`: holds the current page, keeps a back stack, and preserves scroll position per page. Deliberately after `Sidebar`, because a navigation control that assumes its own host is a control that cannot be dropped into an existing application.

- It does not navigate. It watches its own `Content`, so binding that to a selection is the entire integration and Celeste never becomes the router. Bind two ways: `GoBack` writes the previous page back, and the selection has to follow.
- Back is `NavigationCommands.BrowseBack` rather than a Celeste command, so it arrives with the gestures Windows already assigns to it. `CanGoBack` and `BackStackDepth` are bindable.
- Scroll position is restored per page on the way back only; forward is a fresh look at a page and starts at the top.
- `MaxBackStackDepth` caps the stack, and the scroll position of a page that falls off it is dropped too. An unbounded stack is an unbounded reference to every page an application has ever shown.

Still open: there is no forward stack, no transition between pages, and no automation peer of its own. Pages that scroll themselves need `IsScrollEnabled="False"`, and then nothing is restored, because the host is no longer what scrolls.

### 🧭 Breadcrumbs and `TabStrip`

Secondary navigation for applications that are wider than they are deep.

---

## 0.4 — The controls that still look like 2006

Named in the README as the most jarring gap. Each is a restyle of a built-in control, so it is theme-token work rather than new API.

- 🔜 `Menu` and `ContextMenu`, including submenu popups, separators, icons, and input-gesture text.
- 🔜 `Expander`.
- 🧭 `TreeView`.
- 🧭 `Calendar` and `DatePicker`.
- 🧭 `DataGrid` — restyled only. Celeste is not writing a grid; it is making the one in the box stop clashing.
- 🧭 `ToolBar`, `StatusBar`, `GroupBox`.

---

## 0.5 — Feedback and overlays

- 🧭 `Dialog` / `ContentDialog`: modal over the window rather than a separate OS window, with focus trapping and a result-returning `await` API.
- 🧭 `Flyout` and themed `Popup` primitives, shared with `ComboBox` and `ContextMenu` so popup shadow and radius are defined once.
- 🧭 `Toast` / `InfoBar`: transient and inline notifications built on the existing badge palette.
- 🧭 `Skeleton` and a busy overlay, driven by the existing `ProgressRing`.

---

## Cross-cutting, ongoing

These are not milestones. They apply to everything above.

### Accessibility

- Automation peers and names on every custom control.
- Respect the Windows "show animations" and "reduce transparency" settings. A motion token set that resolves to zero duration is the mechanism.
- High-contrast theme support. Today a high-contrast user gets Celeste's colors, which is the wrong answer; the fix is a third theme dictionary selected from the system setting, not per-control triggers.
- Contrast audit of both theme dictionaries against WCAG AA, enforced by a test over the token pairs the styles actually use together.

### Theming

- 💭 A documented token contract, so "override these N brushes and everything follows" is a promise instead of an observation.
- 💭 Brand palette generation: derive the full ramp from one accent color rather than requiring every hover and pressed variant by hand.
- 🧭 Density: a compact token set that swaps control heights and paddings without touching styles.

### Testing and tooling

- 🧭 Extend `ControlTemplateTests` coverage as controls land; every new style needs its row.
- 🧭 Visual regression snapshots of the gallery in both themes. Template-application tests prove a style loads, not that it looks right.
- 🧭 CI on Windows for `net8.0-windows` and `net10.0-windows`, with the package built and validated on every push.

### Documentation

- 🧭 A published token reference generated from `Light.xaml`, so the README stops pointing at a source file.
- 🧭 A per-control page with the states each style covers.

---

## Explicitly out of scope

Saying no here is cheaper than saying it in an issue later.

- **A control suite.** Docking, ribbons, charts, report designers, and a from-scratch data grid are not coming.
- **Fluent or Material parity.** [WPF-UI](https://github.com/lepoco/wpfui) and [MaterialDesignInXamlToolkit](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit) already do those properly.
- **.NET Framework.** The package targets `net8.0-windows` and `net10.0-windows`.
- **An MVVM framework.** Celeste ships controls and tokens. Navigation state, DI, and messaging belong to the application.
- **An icon set.** Icons are `IconContent`, and any vector source works. Bundling one would double the package size to make one design decision for everyone.

---

## Influencing this list

Open an issue describing the application that is blocked, not the control that is missing — the second one is usually a consequence of the first, and knowing the first sometimes produces a smaller change. See [CONTRIBUTING.md](CONTRIBUTING.md).
