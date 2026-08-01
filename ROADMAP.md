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
