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
        Brush activeSurface = (Brush)FindResource("B_PrimarySoft");
        Brush hoverSurface = (Brush)FindResource("B_CardAlt");
        Brush accent = (Brush)FindResource("B_Primary");

        Style style = CreateTopNavigationTabStyle(text, textDim, activeSurface, hoverSurface, accent);

        mainTabs.Margin = new Thickness(8, 0, 8, 0);
        mainTabs.Padding = new Thickness(0, 0, 0, 8);
        mainTabs.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        mainTabs.VerticalContentAlignment = VerticalAlignment.Stretch;

        foreach (TabItem tab in mainTabs.Items.OfType<TabItem>())
        {
            if (tab.Header is string header)
            {
                tab.Header = header.Trim();
            }

            tab.Style = style;
            tab.Height = 46;
            tab.MinWidth = 0;
            tab.Margin = new Thickness(0, 0, 14, 8);
            tab.Padding = new Thickness(18, 9, 18, 9);
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
        Brush activeSurface,
        Brush hoverSurface,
        Brush accent)
    {
        Style style = new(typeof(TabItem));
        style.Setters.Add(new Setter(Control.ForegroundProperty, textDim));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(18, 9, 18, 9)));
        style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
        style.Setters.Add(new Setter(Control.FontSizeProperty, 13d));
        style.Setters.Add(new Setter(FrameworkElement.MinWidthProperty, 0d));
        style.Setters.Add(new Setter(FrameworkElement.HeightProperty, 46d));
        style.Setters.Add(new Setter(Control.FocusVisualStyleProperty, null));

        FrameworkElementFactory root = new(typeof(Grid));
        root.SetValue(FrameworkElement.SnapsToDevicePixelsProperty, true);

        FrameworkElementFactory surface = new(typeof(Border), "Surface");
        surface.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        surface.SetValue(Border.BorderBrushProperty, Brushes.Transparent);
        surface.SetValue(Border.BorderThicknessProperty, new Thickness(0));
        surface.SetValue(Border.CornerRadiusProperty, new CornerRadius(12));
        surface.SetValue(Border.PaddingProperty, new Thickness(18, 9, 18, 9));
        surface.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 4));

        FrameworkElementFactory presenter = new(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.ContentSourceProperty, "Header");
        presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        surface.AppendChild(presenter);
        root.AppendChild(surface);

        FrameworkElementFactory indicator = new(typeof(Border), "AccentLine");
        indicator.SetValue(FrameworkElement.WidthProperty, 36d);
        indicator.SetValue(FrameworkElement.HeightProperty, 3d);
        indicator.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        indicator.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Bottom);
        indicator.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 1));
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
        hover.Setters.Add(new Setter(Border.BackgroundProperty, hoverSurface, "Surface"));
        hover.Setters.Add(new Setter(Control.ForegroundProperty, text));
        template.Triggers.Add(hover);

        Trigger selected = new()
        {
            Property = TabItem.IsSelectedProperty,
            Value = true
        };
        selected.Setters.Add(new Setter(Border.BackgroundProperty, activeSurface, "Surface"));
        selected.Setters.Add(new Setter(Border.BackgroundProperty, accent, "AccentLine"));
        selected.Setters.Add(new Setter(Control.ForegroundProperty, text));
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
