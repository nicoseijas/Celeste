# Roadmap

What Celeste plans to add, in the order it plans to add it. Items move down the list when they get in the way of something above them, not on a schedule — there are no release dates here.

Version numbers are `0.x` throughout, so every entry below may break the public API. See [README.md](README.md#status) for what already exists.

## Legend

| Mark | Meaning |
| --- | --- |
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

Sidebars, the host they navigate, and the secondary navigation around both.

---

## 0.4 — The controls that still look like 2006

Named in the README as the most jarring gap. Each is a restyle of a built-in control, so it is theme-token work rather than new API.

---

## 0.5 — Feedback and overlays

Dialogs, popups, and transient notifications.

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
