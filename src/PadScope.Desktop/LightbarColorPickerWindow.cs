using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PadScope.Desktop;

internal sealed class LightbarColorPickerWindow : Window
{
    private readonly Slider _redSlider = CreateChannelSlider();
    private readonly Slider _greenSlider = CreateChannelSlider();
    private readonly Slider _blueSlider = CreateChannelSlider();
    private readonly TextBlock _redValue = CreateValueText();
    private readonly TextBlock _greenValue = CreateValueText();
    private readonly TextBlock _blueValue = CreateValueText();
    private readonly Border _preview = new();
    private readonly TextBlock _rgbText = new();
    private readonly TextBox _hexBox = new();
    private bool _syncing;

    public Color SelectedColor { get; private set; }

    public LightbarColorPickerWindow(Color initialColor)
    {
        Title = "Lightbar Color Picker";
        Width = 480;
        Height = 520;
        MinWidth = 440;
        MinHeight = 500;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = ResourceBrush("B_WindowBackdrop", Brushes.Black);
        Foreground = ResourceBrush("B_Text", Brushes.White);
        FontFamily = Application.Current.TryFindResource("PadScopeBodyFont") as FontFamily ?? new FontFamily("Segoe UI Variable Text");
        ShowInTaskbar = false;

        Content = BuildContent();

        _redSlider.ValueChanged += ChannelSlider_ValueChanged;
        _greenSlider.ValueChanged += ChannelSlider_ValueChanged;
        _blueSlider.ValueChanged += ChannelSlider_ValueChanged;
        _hexBox.KeyDown += HexBox_KeyDown;
        _hexBox.LostKeyboardFocus += (_, _) => TryApplyHex();

        SetColor(initialColor);
    }

    private UIElement BuildContent()
    {
        Grid root = new()
        {
            Margin = new Thickness(20)
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        StackPanel title = new();
        title.Children.Add(new TextBlock
        {
            Text = "Lightbar color",
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            Foreground = ResourceBrush("B_Text", Brushes.White)
        });
        title.Children.Add(new TextBlock
        {
            Text = "Choose a color visually, by RGB channels, or with a HEX value.",
            FontSize = 12,
            Foreground = ResourceBrush("B_TextDim", Brushes.Gray),
            Margin = new Thickness(0, 4, 0, 0)
        });
        root.Children.Add(title);

        Border previewCard = Card(new Thickness(0, 16, 0, 0));
        Grid previewGrid = new();
        previewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(92) });
        previewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _preview.Width = 72;
        _preview.Height = 72;
        _preview.CornerRadius = new CornerRadius(18);
        _preview.BorderThickness = new Thickness(1);
        _preview.BorderBrush = ResourceBrush("B_Border", Brushes.Gray);
        _preview.HorizontalAlignment = HorizontalAlignment.Left;
        previewGrid.Children.Add(_preview);

        StackPanel previewText = new()
        {
            Margin = new Thickness(10, 2, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(previewText, 1);
        previewText.Children.Add(new TextBlock
        {
            Text = "PREVIEW",
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = ResourceBrush("B_TextDim", Brushes.Gray)
        });
        _rgbText.FontSize = 13;
        _rgbText.FontWeight = FontWeights.SemiBold;
        _rgbText.Margin = new Thickness(0, 5, 0, 8);
        previewText.Children.Add(_rgbText);

        Grid hexRow = new();
        hexRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        hexRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _hexBox.Height = 34;
        _hexBox.Padding = new Thickness(9, 5, 9, 5);
        _hexBox.VerticalContentAlignment = VerticalAlignment.Center;
        _hexBox.FontFamily = new FontFamily("Consolas");
        hexRow.Children.Add(_hexBox);
        Button useHex = SecondaryButton("Use HEX");
        useHex.Margin = new Thickness(8, 0, 0, 0);
        useHex.MinWidth = 82;
        useHex.Click += (_, _) => TryApplyHex();
        Grid.SetColumn(useHex, 1);
        hexRow.Children.Add(useHex);
        previewText.Children.Add(hexRow);
        previewGrid.Children.Add(previewText);
        previewCard.Child = previewGrid;
        Grid.SetRow(previewCard, 1);
        root.Children.Add(previewCard);

        Border channelsCard = Card(new Thickness(0, 12, 0, 0));
        StackPanel channels = new();
        channels.Children.Add(ChannelRow("Red", _redSlider, _redValue, Color.FromRgb(248, 113, 113)));
        channels.Children.Add(ChannelRow("Green", _greenSlider, _greenValue, Color.FromRgb(52, 211, 153)));
        channels.Children.Add(ChannelRow("Blue", _blueSlider, _blueValue, Color.FromRgb(96, 165, 250)));
        channelsCard.Child = channels;
        Grid.SetRow(channelsCard, 2);
        root.Children.Add(channelsCard);

        StackPanel presetSection = new()
        {
            Margin = new Thickness(0, 14, 0, 0)
        };
        presetSection.Children.Add(new TextBlock
        {
            Text = "Quick colors",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = ResourceBrush("B_TextDim", Brushes.Gray),
            Margin = new Thickness(2, 0, 0, 7)
        });

        UniformGrid presets = new() { Columns = 8 };
        foreach ((string name, string value) in PresetColors)
        {
            Color color = (Color)ColorConverter.ConvertFromString(value);
            Button swatch = new()
            {
                Width = 42,
                Height = 34,
                MinWidth = 0,
                Margin = new Thickness(2),
                Padding = new Thickness(0),
                Background = new SolidColorBrush(color),
                BorderBrush = ResourceBrush("B_Border", Brushes.Gray),
                BorderThickness = new Thickness(1),
                ToolTip = name,
                Tag = color
            };
            swatch.Click += (_, _) => SetColor((Color)swatch.Tag);
            presets.Children.Add(swatch);
        }
        presetSection.Children.Add(presets);
        Grid.SetRow(presetSection, 3);
        root.Children.Add(presetSection);

        TextBlock hint = new()
        {
            Text = "Picking a color only changes the preview. PadScope sends it to the controller only after you press Set Lightbar in Advanced HID tools.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 11,
            Foreground = ResourceBrush("B_TextDim", Brushes.Gray),
            Margin = new Thickness(2, 14, 2, 0)
        };
        Grid.SetRow(hint, 4);
        root.Children.Add(hint);

        StackPanel actions = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };
        Button cancel = SecondaryButton("Cancel");
        cancel.IsCancel = true;
        cancel.MinWidth = 88;
        Button apply = new()
        {
            Content = "Use color",
            MinWidth = 108,
            Margin = new Thickness(8, 0, 0, 0),
            IsDefault = true
        };
        apply.Click += (_, _) =>
        {
            SelectedColor = ReadSliderColor();
            DialogResult = true;
        };
        actions.Children.Add(cancel);
        actions.Children.Add(apply);
        Grid.SetRow(actions, 5);
        root.Children.Add(actions);

        return root;
    }

    private Border Card(Thickness margin) => new()
    {
        Margin = margin,
        Padding = new Thickness(14),
        CornerRadius = new CornerRadius(16),
        Background = ResourceBrush("B_Card", Brushes.Black),
        BorderBrush = ResourceBrush("B_Border", Brushes.Gray),
        BorderThickness = new Thickness(1)
    };

    private Grid ChannelRow(string label, Slider slider, TextBlock valueText, Color accent)
    {
        Grid row = new()
        {
            Margin = new Thickness(0, 5, 0, 5)
        };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });

        StackPanel labelPanel = new()
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        labelPanel.Children.Add(new Ellipse
        {
            Width = 8,
            Height = 8,
            Fill = new SolidColorBrush(accent),
            Margin = new Thickness(0, 0, 7, 0)
        });
        labelPanel.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 11.5,
            Foreground = ResourceBrush("B_Text", Brushes.White),
            VerticalAlignment = VerticalAlignment.Center
        });
        row.Children.Add(labelPanel);

        slider.HorizontalAlignment = HorizontalAlignment.Stretch;
        slider.VerticalAlignment = VerticalAlignment.Center;
        slider.Margin = new Thickness(8, 0, 8, 0);
        Grid.SetColumn(slider, 1);
        row.Children.Add(slider);

        valueText.HorizontalAlignment = HorizontalAlignment.Right;
        valueText.VerticalAlignment = VerticalAlignment.Center;
        valueText.FontFamily = new FontFamily("Consolas");
        valueText.Foreground = ResourceBrush("B_TextDim", Brushes.Gray);
        Grid.SetColumn(valueText, 2);
        row.Children.Add(valueText);
        return row;
    }

    private static Slider CreateChannelSlider() => new()
    {
        Minimum = 0,
        Maximum = 255,
        TickFrequency = 1,
        IsSnapToTickEnabled = true
    };

    private static TextBlock CreateValueText() => new()
    {
        Text = "0",
        FontSize = 11
    };

    private static Button SecondaryButton(string text)
    {
        Button button = new()
        {
            Content = text
        };
        if (Application.Current.TryFindResource("Sec") is Style style)
        {
            button.Style = style;
        }
        return button;
    }

    private void ChannelSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_syncing)
        {
            RefreshPreview();
        }
    }

    private void HexBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            TryApplyHex();
            e.Handled = true;
        }
    }

    private void TryApplyHex()
    {
        string text = _hexBox.Text.Trim();
        if (!text.StartsWith('#'))
        {
            text = "#" + text;
        }

        try
        {
            if (ColorConverter.ConvertFromString(text) is Color color)
            {
                SetColor(Color.FromRgb(color.R, color.G, color.B));
            }
        }
        catch (FormatException)
        {
            _hexBox.SelectAll();
        }
    }

    private void SetColor(Color color)
    {
        _syncing = true;
        _redSlider.Value = color.R;
        _greenSlider.Value = color.G;
        _blueSlider.Value = color.B;
        _syncing = false;
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        Color color = ReadSliderColor();
        SelectedColor = color;
        _preview.Background = new SolidColorBrush(color);
        _redValue.Text = color.R.ToString(CultureInfo.InvariantCulture);
        _greenValue.Text = color.G.ToString(CultureInfo.InvariantCulture);
        _blueValue.Text = color.B.ToString(CultureInfo.InvariantCulture);
        _rgbText.Text = $"RGB {color.R}, {color.G}, {color.B}";
        _hexBox.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private Color ReadSliderColor() => Color.FromRgb(
        (byte)Math.Round(_redSlider.Value),
        (byte)Math.Round(_greenSlider.Value),
        (byte)Math.Round(_blueSlider.Value));

    private static Brush ResourceBrush(string key, Brush fallback) =>
        Application.Current.TryFindResource(key) as Brush ?? fallback;

    private static readonly (string Name, string Value)[] PresetColors =
    {
        ("White", "#FFFFFF"),
        ("Ice", "#67E8F9"),
        ("Blue", "#3B82F6"),
        ("Violet", "#8B5CF6"),
        ("Pink", "#F472B6"),
        ("Red", "#FB7185"),
        ("Amber", "#FBBF24"),
        ("Green", "#34D399")
    };
}
