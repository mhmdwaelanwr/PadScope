using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace PadScope.Desktop;

public partial class MainWindow
{
    static MainWindow()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnLoadedForSystemBrushPatch));
    }

    private static void OnLoadedForSystemBrushPatch(object sender, RoutedEventArgs e)
    {
        if (sender is MainWindow window)
        {
            window.Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    window.ApplyRuntimeUiFixes();
                    window.ApplySystemBrushPatch();
                    window.InitializeReportSelectionPersistence();
                    window.InstallModernLiveDashboard();
                    window.PolishLegacyInputButtonsSafe();
                    window.InstallScanListPanelControls();
                    window.ApplyTopNavigationPolish();
                }),
                DispatcherPriority.Background);
        }
    }

    private void ApplySystemBrushPatch()
    {
        Brush selectedBackground = (Brush)FindResource("B_PrimarySoft");
        Brush selectedStrong = (Brush)FindResource("B_PrimaryDim");
        Brush selectedText = (Brush)FindResource("B_Text");

        foreach (DependencyObject item in PatchWalk(this))
        {
            if (item is TabItem tab)
            {
                tab.Resources[SystemColors.ControlLightLightBrushKey] = selectedBackground;
                tab.Resources[SystemColors.ControlBrushKey] = selectedBackground;
                tab.Resources[SystemColors.ControlTextBrushKey] = selectedText;
                tab.Resources[SystemColors.HighlightBrushKey] = selectedStrong;
                tab.Resources[SystemColors.HighlightTextBrushKey] = selectedText;
            }
            else if (item is DataGrid dataGrid)
            {
                dataGrid.Resources[SystemColors.HighlightBrushKey] = selectedBackground;
                dataGrid.Resources[SystemColors.HighlightTextBrushKey] = selectedText;
                dataGrid.Resources[SystemColors.InactiveSelectionHighlightBrushKey] = selectedBackground;
                dataGrid.Resources[SystemColors.InactiveSelectionHighlightTextBrushKey] = selectedText;
                dataGrid.Resources[SystemColors.ControlBrushKey] = (Brush)FindResource("B_CardAlt");
                dataGrid.Resources[SystemColors.ControlTextBrushKey] = selectedText;
            }
        }
    }

    private static IEnumerable<DependencyObject> PatchWalk(DependencyObject root)
    {
        yield return root;

        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            foreach (DependencyObject nested in PatchWalk(child))
            {
                yield return nested;
            }
        }
    }
}
