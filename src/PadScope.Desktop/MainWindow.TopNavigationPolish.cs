using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace PadScope.Desktop;

public partial class MainWindow
{
    private bool _topNavigationThemeHooked;

    private void ApplyTopNavigationPolish()
    {
        TabControl? mainTabs = WalkLogicalTree(this)
            .OfType<TabControl>()
            .FirstOrDefault(control =>
            {
                string[] headers = control.Items
                    .OfType<TabItem>()
                    .Select(tab => tab.Header?.ToString()?.Trim() ?? string.Empty)
                    .ToArray();

                return headers.Contains("Scan", StringComparer.OrdinalIgnoreCase) &&
                       headers.Contains("Live Input", StringComparer.OrdinalIgnoreCase) &&
                       headers.Contains("Virtual Controller", StringComparer.OrdinalIgnoreCase) &&
                       headers.Contains("Mouse Lab", StringComparer.OrdinalIgnoreCase) &&
                       headers.Contains("Audio", StringComparer.OrdinalIgnoreCase);
            });

        if (mainTabs is null)
        {
            return;
        }

        Brush text = (Brush)FindResource("B_Text");
        Brush textDim = (Brush)FindResource("B_TextDim");
        Brush accent = (Brush)FindResource("B_Primary");
        Brush accentDim = (Brush)FindResource("B_PrimaryDim");

        Style style = CreateTopNavigationTabStyle(text, textDim, accent, accentDim);

        mainTabs.Background = Brushes.Transparent;
        mainTabs.BorderThickness = new Thickness(0);
        mainTabs.Margin = new Thickness(8, 0, 8, 0);
        mainTabs.Padding = new Thickness(0, 0, 0, 10);
        mainTabs.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        mainTabs.VerticalContentAlignment = VerticalAlignment.Stretch;

        foreach (TabItem tab in mainTabs.Items.OfType<TabItem>())
        {
            if (tab.Header is string header)
            {
                tab.Header = header.Trim();
            }

            tab.Style = style;
            tab.Background = Brushes.Transparent;
            tab.BorderBrush = Brushes.Transparent;
            tab.BorderThickness = new Thickness(0);
            tab.Height = 44;
            tab.MinWidth = 0;
            tab.Margin = new Thickness(0, 0, 22, 0);
            tab.Padding = new Thickness(12, 8, 12, 8);
        }

        if (!_topNavigationThemeHooked)
        {
            _topNavigationThemeHooked = true;
            ThemeButton.Click += (_, _) => Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(ApplyTopNavigationPolish));
        }
    }

    private static Style CreateTopNavigationTabStyle(
        Brush text,
        Brush textDim,
        Brush accent,
        Brush accentDim)
    {
        Style style = new(typeof(TabItem));
        style.Setters.Add(new Setter(Control.ForegroundProperty, textDim));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 8, 12, 8)));
        style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
        style.Setters.Add(new Setter(Control.FontSizeProperty, 13d));
        style.Setters.Add(new Setter(FrameworkElement.MinWidthProperty, 0d));
        style.Setters.Add(new Setter(FrameworkElement.HeightProperty, 44d));
        style.Setters.Add(new Setter(Control.FocusVisualStyleProperty, null));

        FrameworkElementFactory root = new(typeof(Grid));
        root.SetValue(FrameworkElement.SnapsToDevicePixelsProperty, true);
        root.SetValue(Panel.BackgroundProperty, Brushes.Transparent);

        FrameworkElementFactory presenter = new(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.ContentSourceProperty, "Header");
        presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        presenter.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 5));
        root.AppendChild(presenter);

        FrameworkElementFactory indicator = new(typeof(Border), "AccentLine");
        indicator.SetValue(FrameworkElement.WidthProperty, 0d);
        indicator.SetValue(FrameworkElement.HeightProperty, 3d);
        indicator.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        indicator.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Bottom);
        indicator.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 2));
        indicator.SetValue(Border.CornerRadiusProperty, new CornerRadius(2));
        indicator.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        indicator.SetValue(UIElement.IsHitTestVisibleProperty, false);
        root.AppendChild(indicator);

        ControlTemplate template = new(typeof(TabItem))
        {
            VisualTree = root
        };

        Trigger hover = new()
        {
            Property = UIElement.IsMouseOverProperty,
            Value = true
        };
        hover.Setters.Add(new Setter(Control.ForegroundProperty, text));
        hover.Setters.Add(new Setter(FrameworkElement.WidthProperty, 22d, "AccentLine"));
        hover.Setters.Add(new Setter(Border.BackgroundProperty, accentDim, "AccentLine"));
        template.Triggers.Add(hover);

        Trigger selected = new()
        {
            Property = TabItem.IsSelectedProperty,
            Value = true
        };
        selected.Setters.Add(new Setter(Control.ForegroundProperty, text));
        selected.Setters.Add(new Setter(FrameworkElement.WidthProperty, 42d, "AccentLine"));
        selected.Setters.Add(new Setter(Border.BackgroundProperty, accent, "AccentLine"));
        template.Triggers.Add(selected);

        Trigger disabled = new()
        {
            Property = UIElement.IsEnabledProperty,
            Value = false
        };
        disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.45d));
        template.Triggers.Add(disabled);

        style.Setters.Add(new Setter(Control.TemplateProperty, template));
        return style;
    }
}
