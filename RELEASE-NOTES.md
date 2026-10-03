# EZoptimizer v0.2.1 preview

A complete visual refresh for the native Windows app.

- Charcoal and mint colors, clearer typography, quieter borders and consistent spacing.
- An icon sidebar that adapts to narrow windows, a custom title bar and a matching executable icon.
- A rebuilt dashboard with compact readings, current-session details and quick profile navigation.
- Visual profile cards with a separate review/staging action.
- Redesigned inputs, dropdowns, buttons, checkboxes, scrollbars and expandable sections.
- A recovery empty state and a review area that fits smaller windows.
- Dark, light, Windows-following and high-contrast themes.

The existing preview, journals and exact undo remain in place. Selecting a profile card does not apply any settings.

39 regression checks, the packaged read-only smoke check and 84 responsive theme/page/scaling layouts pass locally. Screenshots are actual-content WPF renders. Full Narrator/keyboard traversal, native Windows 11 integration and real OS DPI transitions remain pending. See the validation notes for the complete limits.

This portable x64 build includes .NET 10 and is unsigned. Preserve your existing recovery history when upgrading. GitHub Actions remains blocked by the account billing restriction; the build and checks ran locally.
