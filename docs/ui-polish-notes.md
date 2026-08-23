# UI polish notes

This pass focuses on three Windows desktop issues found during real-device testing:

- Tab headers must keep clear spacing and a contained active indicator without overlap or clipping.
- Combo boxes must use PadScope surfaces and text colors in both dark and light themes, including disabled controls and drop-down items.
- The desktop shell should prefer Segoe UI Variable on supported Windows versions and fall back to Segoe UI elsewhere.

The styles live in `src/PadScope.Desktop/Themes/PolishedControls.xaml` and are applied by `MainWindow.RuntimeUiFixes.cs` so the existing XAML control names and event handlers remain unchanged.
