using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PadScope.Desktop;

internal sealed class LightbarColorPickerWindow : Window
{
    private readonly Slider _red = ChannelSlider();
    private readonly Slider _green = ChannelSlider();
    private readonly Slider _blue = ChannelSlider();
    private readonly Border _preview = new();
    private readonly TextBox _hex = new();
    private readonly TextBlock _rgb = new();
    private bool _syncing;

    public Color SelectedColor { get; private set; }

    public LightbarColorPickerWindow(Color initialColor)
    {
        Title = "PadScope · Lightbar Color";
        Width = 470;
        Height = 500;
        MinWidth = 440;
        MinHeight = 470;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        Background = ResourceBrush("B_WindowBackdrop", Brushes.Black);
        Foreground = ResourceBrush("B_Text", Brushes.White);
        FontFamily = Application.Current.TryFindResource("PadScopeBodyFont") as FontFamily ?? new FontFamily("Segoe UI");
        Content = BuildContent();

        _red.ValueChanged += ChannelChanged;
        _green.ValueChanged += ChannelChanged;
        _blue.ValueChanged += ChannelChanged;
        _hex.KeyDown += HexKeyDown;
        SetColor(initialColor);
    }

    private UIElement BuildContent()
    {
        StackPanel root = new() { Margin = new Thickness(20) };
        root.Children.Add(new TextBlock { Text = "Lightbar color", FontSize = 22, FontWeight = FontWeights.SemiBold });
        root.Children.Add(new TextBlock
        {
            Text = "Preview a color locally, then apply it with Set Lightbar.",
            Foreground = ResourceBrush("B_TextDim", Brushes.Gray), FontSize = 12, Margin = new Thickness(0, 4, 0, 14)
        });

        Border previewCard = Card();
        Grid previewGrid = new();
        previewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        previewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _preview.Width = 72; _preview.Height = 72; _preview.CornerRadius = new CornerRadius(18);
        _preview.BorderBrush = ResourceBrush("B_Border", Brushes.Gray); _preview.BorderThickness = new Thickness(1);
        previewGrid.Children.Add(_preview);
        StackPanel previewText = new() { Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(previewText, 1);
        _rgb.FontWeight = FontWeights.SemiBold; _rgb.FontSize = 13; _rgb.Margin = new Thickness(0, 0, 0, 8);
        previewText.Children.Add(_rgb);
        Grid hexRow = new();
        hexRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        hexRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _hex.Height = 34; _hex.Padding = new Thickness(8, 5, 8, 5); _hex.FontFamily = new FontFamily("Consolas");
        hexRow.Children.Add(_hex);
        Button useHex = Secondary("Use HEX"); useHex.Margin = new Thickness(8, 0, 0, 0); useHex.Click += (_, _) => TryApplyHex();
        Grid.SetColumn(useHex, 1); hexRow.Children.Add(useHex);
        previewText.Children.Add(hexRow); previewGrid.Children.Add(previewText); previewCard.Child = previewGrid;
        root.Children.Add(previewCard);

        Border channelCard = Card(); channelCard.Margin = new Thickness(0, 12, 0, 0);
        StackPanel channels = new();
        channels.Children.Add(ChannelRow("Red", _red, "#F87171"));
        channels.Children.Add(ChannelRow("Green", _green, "#34D399"));
        channels.Children.Add(ChannelRow("Blue", _blue, "#60A5FA"));
        channelCard.Child = channels; root.Children.Add(channelCard);

        TextBlock quickTitle = new() { Text = "Quick colors", Foreground = ResourceBrush("B_TextDim", Brushes.Gray), FontSize = 11, FontWeight = FontWeights.SemiBold, Margin = new Thickness(2, 14, 0, 6) };
        root.Children.Add(quickTitle);
        UniformGrid presets = new() { Columns = 8 };
        foreach ((string name, string value) in Presets)
        {
            Color color = (Color)ColorConverter.ConvertFromString(value);
            Button swatch = new()
            {
                Width = 42, Height = 34, MinWidth = 0, Margin = new Thickness(2), Padding = new Thickness(0),
                Background = new SolidColorBrush(color), BorderBrush = ResourceBrush("B_Border", Brushes.Gray), BorderThickness = new Thickness(1),
                ToolTip = name, Tag = color
            };
            swatch.Click += (_, _) => SetColor((Color)swatch.Tag);
            presets.Children.Add(swatch);
        }
        root.Children.Add(presets);

        StackPanel actions = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 18, 0, 0) };
        Button cancel = Secondary("Cancel"); cancel.IsCancel = true; cancel.MinWidth = 88;
        Button apply = new() { Content = "Use color", MinWidth = 110, Margin = new Thickness(8, 0, 0, 0), IsDefault = true };
        apply.Click += (_, _) => { SelectedColor = ReadColor(); DialogResult = true; };
        actions.Children.Add(cancel); actions.Children.Add(apply); root.Children.Add(actions);
        return root;
    }

    private Grid ChannelRow(string label, Slider slider, string accentHex)
    {
        Grid row = new() { Margin = new Thickness(0, 6, 0, 6) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
        Color accent = (Color)ColorConverter.ConvertFromString(accentHex);
        StackPanel left = new() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        left.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(accent), Margin = new Thickness(0, 0, 7, 0) });
        left.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
        row.Children.Add(left);
        slider.Margin = new Thickness(8, 0, 8, 0); Grid.SetColumn(slider, 1); row.Children.Add(slider);
        TextBlock value = new() { Tag = slider, FontFamily = new FontFamily("Consolas"), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        slider.ValueChanged += (_, _) => value.Text = ((int)Math.Round(slider.Value)).ToString(CultureInfo.InvariantCulture);
        value.Text = "0"; Grid.SetColumn(value, 2); row.Children.Add(value);
        return row;
    }

    private static Slider ChannelSlider() => new() { Minimum = 0, Maximum = 255, TickFrequency = 1, IsSnapToTickEnabled = true };
    private Border Card() => new() { Padding = new Thickness(14), CornerRadius = new CornerRadius(16), Background = ResourceBrush("B_Card", Brushes.Black), BorderBrush = ResourceBrush("B_Border", Brushes.Gray), BorderThickness = new Thickness(1) };
    private static Button Secondary(string text)
    {
        Button button = new() { Content = text };
        if (Application.Current.TryFindResource("Sec") is Style style) button.Style = style;
        return button;
    }

    private void ChannelChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_syncing) RefreshPreview();
    }

    private void HexKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        TryApplyHex(); e.Handled = true;
    }

    private void TryApplyHex()
    {
        string text = _hex.Text.Trim();
        if (!text.StartsWith('#')) text = "#" + text;
        try
        {
            if (ColorConverter.ConvertFromString(text) is Color color) SetColor(Color.FromRgb(color.R, color.G, color.B));
        }
        catch (FormatException) { _hex.SelectAll(); }
    }

    private void SetColor(Color color)
    {
        _syncing = true;
        _red.Value = color.R; _green.Value = color.G; _blue.Value = color.B;
        _syncing = false;
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        Color color = ReadColor();
        SelectedColor = color;
        _preview.Background = new SolidColorBrush(color);
        _rgb.Text = $"RGB {color.R}, {color.G}, {color.B}";
        _hex.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private Color ReadColor() => Color.FromRgb((byte)Math.Round(_red.Value), (byte)Math.Round(_green.Value), (byte)Math.Round(_blue.Value));
    private static Brush ResourceBrush(string key, Brush fallback) => Application.Current.TryFindResource(key) as Brush ?? fallback;

    private static readonly (string Name, string Value)[] Presets =
    {
        ("White", "#FFFFFF"), ("Ice", "#67E8F9"), ("Blue", "#3B82F6"), ("Violet", "#8B5CF6"),
        ("Pink", "#F472B6"), ("Red", "#FB7185"), ("Amber", "#FBBF24"), ("Green", "#34D399")
    };
}
