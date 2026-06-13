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
            window.Dispatcher.BeginInvoke(window.ApplySystemBrushPatch, DispatcherPriority.Background);
        }
    }

    private void ApplySystemBrushPatch()
    {
        Brush selectedBackground = new SolidColorBrush(Color.FromRgb(3, 105, 161));
        Brush selectedText = Brushes.White;

        foreach (DependencyObject item in PatchWalk(this))
        {
            if (item is TabItem tab)
            {
                tab.Resources[SystemColors.ControlLightLightBrushKey] = selectedBackground;
                tab.Resources[SystemColors.ControlBrushKey] = selectedBackground;
                tab.Resources[SystemColors.ControlTextBrushKey] = selectedText;
                tab.Resources[SystemColors.HighlightBrushKey] = selectedBackground;
                tab.Resources[SystemColors.HighlightTextBrushKey] = selectedText;
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
