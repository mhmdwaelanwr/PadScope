using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace PadScope.Desktop;

public partial class MainWindow
{
    private bool _uiFixesInstalled;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        if (!_uiFixesInstalled)
        {
            _uiFixesInstalled = true;
            HookUiFixes();
        }

        RefreshUiColors();
    }

    private void HookUiFixes()
    {
        foreach (TabControl tabControl in FindChildren<TabControl>(this))
        {
            tabControl.SelectionChanged += (_, _) =>
                Dispatcher.BeginInvoke(RefreshUiColors, DispatcherPriority.Background);
        }

        ThemeButton.Click += (_, _) =>
            Dispatcher.BeginInvoke(RefreshUiColors, DispatcherPriority.Background);

        foreach (Button button in FindChildren<Button>(this))
        {
            string text = button.Content?.ToString() ?? string.Empty;

            if (text.Equals("Clear", StringComparison.OrdinalIgnoreCase))
            {
                button.Click += (_, _) =>
                    Dispatcher.BeginInvoke(() =>
                    {
                        CurrentStageText.Text = "0/1 Ready";
                        RefreshUiColors();
                    }, DispatcherPriority.Background);
            }

            if (text.Contains("Scan", StringComparison.OrdinalIgnoreCase))
            {
                button.Click += (_, _) =>
                    Dispatcher.BeginInvoke(RefreshUiColors, DispatcherPriority.Background);
            }
        }
    }

    private void RefreshUiColors()
    {
        bool light = _isLightTheme;

        Brush background = MakeBrush(light ? "#F8FAFC" : "#07111F");
        Brush surface = MakeBrush(light ? "#FFFFFF" : "#0F172A");
        Brush surfaceAlt = MakeBrush(light ? "#E2E8F0" : "#162033");
        Brush border = MakeBrush(light ? "#94A3B8" : "#2B3A55");
        Brush primary = MakeBrush(light ? "#0284C7" : "#38BDF8");
        Brush primaryDark = MakeBrush("#0369A1");
        Brush text = MakeBrush(light ? "#0F172A" : "#E5E7EB");
        Brush muted = MakeBrush(light ? "#475569" : "#94A3B8");
        Brush warning = MakeBrush(light ? "#B45309" : "#F59E0B");
        Brush buttonText = MakeBrush("#FFFFFF");

        Application.Current.Resources["BrushBackground"] = background;
        Application.Current.Resources["BrushSurface"] = surface;
        Application.Current.Resources["BrushSurfaceAlt"] = surfaceAlt;
        Application.Current.Resources["BrushBorder"] = border;
        Application.Current.Resources["BrushPrimary"] = primary;
        Application.Current.Resources["BrushPrimaryDark"] = primaryDark;
        Application.Current.Resources["BrushText"] = text;
        Application.Current.Resources["BrushMuted"] = muted;
        Application.Current.Resources["BrushWarning"] = warning;
        Application.Current.Resources["BrushButtonText"] = buttonText;

        Background = background;
        CurrentStageText.Foreground = warning;
        ThemeButton.Content = light ? "Dark mode" : "Light mode";

        Style? cardStyle = TryFindResource("Card") as Style;
        Style? softCardStyle = TryFindResource("SoftCard") as Style;
        Style? pillStyle = TryFindResource("Pill") as Style;
        Style? mutedStyle = TryFindResource("MutedText") as Style;
        Style? accentStyle = TryFindResource("AccentText") as Style;

        foreach (DependencyObject item in Walk(this))
        {
            if (item is TextBlock block)
            {
                if (ReferenceEquals(block, CurrentStageText))
                {
                    block.Foreground = warning;
                }
                else if (ReferenceEquals(block.Style, mutedStyle))
                {
                    block.Foreground = muted;
                }
                else if (ReferenceEquals(block.Style, accentStyle))
                {
                    block.Foreground = primary;
                }
                else
                {
                    block.Foreground = text;
                }
            }
            else if (item is Border box)
            {
                box.BorderBrush = border;

                if (!double.IsNaN(box.Width) && !double.IsNaN(box.Height) && Math.Abs(box.Width - 58) < 1 && Math.Abs(box.Height - 58) < 1)
                {
                    box.Background = primaryDark;
                    box.BorderBrush = primary;
                }
                else if (ReferenceEquals(box.Style, cardStyle))
                {
                    box.Background = surface;
                }
                else if (ReferenceEquals(box.Style, softCardStyle) || ReferenceEquals(box.Style, pillStyle))
                {
                    box.Background = surfaceAlt;
                }
            }
            else if (item is Button button)
            {
                string content = button.Content?.ToString() ?? string.Empty;
                bool main = content.Contains("Scan", StringComparison.OrdinalIgnoreCase) || content.Contains("Open Audio", StringComparison.OrdinalIgnoreCase);
                button.Background = main ? primaryDark : surfaceAlt;
                button.BorderBrush = main ? primary : border;
                button.Foreground = main ? buttonText : text;
            }
            else if (item is TabItem tab)
            {
                tab.Background = tab.IsSelected ? primaryDark : surfaceAlt;
                tab.BorderBrush = tab.IsSelected ? primary : border;
                tab.Foreground = tab.IsSelected ? buttonText : text;
            }
            else if (item is DataGrid grid)
            {
                grid.Background = surface;
                grid.Foreground = text;
                grid.BorderBrush = border;
                grid.RowBackground = surface;
                grid.AlternatingRowBackground = surfaceAlt;
            }
            else if (item is DataGridColumnHeader header)
            {
                header.Background = surfaceAlt;
                header.Foreground = muted;
                header.BorderBrush = border;
            }
        }
    }

    private static Brush MakeBrush(string color)
    {
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)!);
    }

    private static IEnumerable<DependencyObject> Walk(DependencyObject root)
    {
        yield return root;
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            foreach (DependencyObject nested in Walk(child))
            {
                yield return nested;
            }
        }
    }

    private static IEnumerable<T> FindChildren<T>(DependencyObject root) where T : DependencyObject
    {
        foreach (DependencyObject item in Walk(root))
        {
            if (item is T found)
            {
                yield return found;
            }
        }
    }
}
