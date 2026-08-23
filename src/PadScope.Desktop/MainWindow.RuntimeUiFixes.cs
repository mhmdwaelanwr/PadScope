using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PadScope.Desktop;

public partial class MainWindow
{
    private bool _runtimeUiFixesInitialized;
    private ResourceDictionary? _polishedControlResources;

    /// <summary>
    /// Applies layout and visual corrections at runtime so the existing XAML
    /// names/handlers stay untouched while the desktop shell remains responsive
    /// and consistently themed.
    /// </summary>
    private void ApplyRuntimeUiFixes()
    {
        if (!_runtimeUiFixesInitialized)
        {
            ThemeButton.Click += ThemeButton_PostClick;
            ApplyPolishedControlStyles();
            _runtimeUiFixesInitialized = true;
        }

        FixScanPaneLayout();
        RebuildThemeBrushResources();
    }

    private void ThemeButton_PostClick(object sender, RoutedEventArgs e)
    {
        // The original handler flips _isLightTheme and updates the color entries.
        // Rebuild the dependent brush/gradient resources afterwards so every
        // DynamicResource consumer receives a fresh object immediately.
        RebuildThemeBrushResources();
        ApplySystemBrushPatch();
        InvalidateVisual();
        UpdateLayout();
    }

    private void ApplyPolishedControlStyles()
    {
        _polishedControlResources ??= new ResourceDictionary
        {
            Source = new Uri("Themes/PolishedControls.xaml", UriKind.Relative)
        };

        Style tabControlStyle = (Style)_polishedControlResources["PolishedTabControlStyle"];
        Style tabItemStyle = (Style)_polishedControlResources["PolishedTabItemStyle"];
        Style comboBoxStyle = (Style)_polishedControlResources["PolishedComboBoxStyle"];
        FontFamily bodyFont = (FontFamily)_polishedControlResources["PadScopeBodyFont"];

        // MainWindow.xaml historically pinned Segoe UI as a local value. Replace
        // it after InitializeComponent so Windows 11 can use Segoe UI Variable,
        // while Windows 10 automatically falls back to Segoe UI.
        FontFamily = bodyFont;

        // Publish the polished styles as the current implicit application styles
        // as well. This covers controls created later (for example ComboBoxItem
        // containers generated when a drop-down first opens).
        ResourceDictionary appResources = Application.Current.Resources;
        appResources[typeof(TabControl)] = tabControlStyle;
        appResources[typeof(TabItem)] = tabItemStyle;
        appResources[typeof(ComboBox)] = comboBoxStyle;

        // Existing XAML elements may already have resolved their previous
        // implicit styles, so update the logical tree explicitly once at startup.
        foreach (DependencyObject item in WalkLogicalTree(this))
        {
            switch (item)
            {
                case TabControl tabControl:
                    tabControl.Style = tabControlStyle;
                    break;
                case TabItem tabItem:
                    tabItem.Style = tabItemStyle;
                    break;
                case ComboBox comboBox:
                    comboBox.Style = comboBoxStyle;
                    break;
            }
        }
    }

    private static IEnumerable<DependencyObject> WalkLogicalTree(DependencyObject root)
    {
        yield return root;

        foreach (object child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is not DependencyObject dependencyObject)
            {
                continue;
            }

            foreach (DependencyObject nested in WalkLogicalTree(dependencyObject))
            {
                yield return nested;
            }
        }
    }

    private void FixScanPaneLayout()
    {
        // The Test Stages / Feature Tests pane was authored in Grid.Column=1,
        // which is the 12 px spacer between the two real content columns.
        if (StagesGrid.Parent is Border stagesCard && stagesCard.Parent is Grid rightPane)
        {
            Grid.SetColumn(rightPane, 2);
            rightPane.MinWidth = 0;
        }

        // The scan table is evidence-heavy. Give status values enough room and
        // let the columns share any additional width instead of overlapping.
        if (ReportsGrid.Columns.Count >= 8)
        {
            SetColumn(0, 1.55, 170); // Device
            SetColumn(1, 1.55, 180); // Profile
            SetColumn(2, 1.15, 115); // Input
            SetColumn(3, 0.90, 90);  // Rumble
            SetColumn(4, 0.95, 95);  // Lightbar
            SetColumn(5, 0.75, 75);  // Gyro
            SetColumn(6, 0.95, 95);  // Touchpad
            SetColumn(7, 1.05, 105); // Audio
        }

        ReportsGrid.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        ReportsGrid.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        ReportsGrid.ClipToBounds = true;

        // The original 190 px report card wastes vertical room when only one or
        // two devices are present. A slightly shorter card leaves the lower
        // diagnostics panes visible while the DataGrid still scrolls as needed.
        if (ReportsGrid.Parent is Grid reportsHost && reportsHost.Parent is Border reportsCard)
        {
            reportsCard.Height = 160;
        }

        void SetColumn(int index, double starWeight, double minWidth)
        {
            DataGridColumn column = ReportsGrid.Columns[index];
            column.MinWidth = minWidth;
            column.Width = new DataGridLength(starWeight, DataGridLengthUnitType.Star);
        }
    }

    private void RebuildThemeBrushResources()
    {
        ResourceDictionary resources = Application.Current.Resources;

        Color Resolve(string key)
        {
            var entry = ThemeColors.First(item => item.Key == key);
            string value = _isLightTheme ? entry.Light : entry.Dark;
            return (Color)ColorConverter.ConvertFromString(value);
        }

        void SetSolid(string brushKey, string colorKey)
        {
            SolidColorBrush brush = new(Resolve(colorKey));
            brush.Freeze();
            resources[brushKey] = brush;
        }

        SetSolid("B_Background", "C_Background");
        SetSolid("B_Card", "C_Card");
        SetSolid("B_CardAlt", "C_CardAlt");
        SetSolid("B_Border", "C_Border");
        SetSolid("B_Primary", "C_Primary");
        SetSolid("B_PrimaryDim", "C_PrimaryDim");
        SetSolid("B_Text", "C_Text");
        SetSolid("B_TextDim", "C_TextDim");
        SetSolid("B_Success", "C_Success");
        SetSolid("B_Warning", "C_Warning");
        SetSolid("B_Danger", "C_Danger");
        SetSolid("B_PrimarySoft", "C_PrimarySoft");
        SetSolid("B_SurfaceHover", "C_SurfaceHover");

        LinearGradientBrush backdrop = new()
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops =
            {
                new GradientStop(Resolve("C_Background"), 0),
                new GradientStop(Resolve("C_BackdropMid"), 0.55),
                new GradientStop(Resolve("C_BackdropEnd"), 1)
            }
        };
        backdrop.Freeze();
        resources["B_WindowBackdrop"] = backdrop;

        LinearGradientBrush brand = new()
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops =
            {
                new GradientStop((Color)ColorConverter.ConvertFromString(_isLightTheme ? "#7C3AED" : "#8B5CF6"), 0),
                new GradientStop((Color)ColorConverter.ConvertFromString(_isLightTheme ? "#2563EB" : "#3B82F6"), 0.52),
                new GradientStop((Color)ColorConverter.ConvertFromString(_isLightTheme ? "#0891B2" : "#22D3EE"), 1)
            }
        };
        brand.Freeze();
        resources["B_BrandGradient"] = brand;

        Background = backdrop;
    }
}
