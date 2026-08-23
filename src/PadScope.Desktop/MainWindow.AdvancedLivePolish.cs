using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace PadScope.Desktop;

public partial class MainWindow
{
    private bool _advancedLivePolishInstalled;
    private Border? _lightbarPreviewSwatch;
    private TextBlock? _lightbarPreviewHex;
    private TextBlock? _lightbarPreviewStatus;
    private TextBlock? _rumbleSmallValue;
    private TextBlock? _rumbleLargeValue;
    private Button? _pickLightbarColorButton;
    private Color _lastAppliedLightbarColor = Colors.Black;
    private bool _hasAppliedLightbarColor;

    private void InstallAdvancedLivePolish(UIElement legacyRoot)
    {
        if (_advancedLivePolishInstalled)
        {
            return;
        }

        _advancedLivePolishInstalled = true;
        PolishLegacyInputButtons();
        RebuildOutputLab();
        PolishLegacyMiniController(legacyRoot);
    }

    private void PolishLegacyInputButtons()
    {
        Style stateStyle = CreateStateButtonStyle();

        Button[] dpadButtons = { DpadUpButton, DpadDownButton, DpadLeftButton, DpadRightButton };
        foreach (Button button in dpadButtons)
        {
            button.Style = stateStyle;
            button.Width = 34;
            button.Height = 34;
            button.MinWidth = 0;
            button.Margin = new Thickness(1);
            button.FontSize = 15;
            button.FontWeight = FontWeights.SemiBold;
        }

        DpadUpButton.Content = "↑";
        DpadDownButton.Content = "↓";
        DpadLeftButton.Content = "←";
        DpadRightButton.Content = "→";
        DpadUpButton.ToolTip = "D-Pad Up";
        DpadDownButton.ToolTip = "D-Pad Down";
        DpadLeftButton.ToolTip = "D-Pad Left";
        DpadRightButton.ToolTip = "D-Pad Right";

        Button[] faceButtons = { TriangleButton, CrossButton, SquareButton, CircleButton };
        foreach (Button button in faceButtons)
        {
            button.Style = stateStyle;
            button.Width = 34;
            button.Height = 34;
            button.MinWidth = 0;
            button.Margin = new Thickness(1);
            button.FontSize = 15;
            button.FontWeight = FontWeights.SemiBold;
        }

        TriangleButton.Content = "△";
        CrossButton.Content = "×";
        SquareButton.Content = "□";
        CircleButton.Content = "○";
        TriangleButton.ToolTip = "Triangle";
        CrossButton.ToolTip = "Cross";
        SquareButton.ToolTip = "Square";
        CircleButton.ToolTip = "Circle";

        Button[] shoulderButtons =
        {
            L1Button, R1Button, L2Button, R2Button, L3Button, R3Button,
            ShareButton, OptionsButton, PsButton, TouchpadButton
        };
        foreach (Button button in shoulderButtons)
        {
            button.Style = stateStyle;
            button.MinWidth = 0;
            button.Width = 48;
            button.Height = 34;
            button.Margin = new Thickness(0, 0, 5, 5);
            button.Padding = new Thickness(4, 0, 4, 0);
            button.FontSize = 10.5;
        }

        ShareButton.Content = "Share";
        ShareButton.Width = 58;
        OptionsButton.Content = "Options";
        OptionsButton.Width = 64;
        TouchpadButton.Content = "Touch";
        TouchpadButton.Width = 56;
        PsButton.Content = "PS";

        L1Button.ToolTip = "Left shoulder (L1)";
        R1Button.ToolTip = "Right shoulder (R1)";
        L2Button.ToolTip = "Left trigger digital state (L2)";
        R2Button.ToolTip = "Right trigger digital state (R2)";
        L3Button.ToolTip = "Left stick click (L3)";
        R3Button.ToolTip = "Right stick click (R3)";
        ShareButton.ToolTip = "Share / Create";
        OptionsButton.ToolTip = "Options";
        PsButton.ToolTip = "PlayStation / Home";
        TouchpadButton.ToolTip = "Touchpad click";
    }

    private Style CreateStateButtonStyle()
    {
        Style baseStyle = (Style)FindResource("Sec");
        Style style = new(typeof(Button), baseStyle);
        style.Setters.Add(new Setter(Control.MinWidthProperty, 0d));
        style.Setters.Add(new Setter(Control.HeightProperty, 34d));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        style.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
        style.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        return style;
    }

    private void RebuildOutputLab()
    {
        if (ResetOutputButton.Parent is not StackPanel outputPanel)
        {
            return;
        }

        outputPanel.Children.Clear();

        Grid heading = new();
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        TextBlock title = new()
        {
            Text = "Output Lab",
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold
        };
        heading.Children.Add(title);
        TextBlock safety = new()
        {
            Text = "CONFIRMATION REQUIRED",
            FontSize = 9.5,
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(8, 4, 8, 4),
            VerticalAlignment = VerticalAlignment.Center
        };
        safety.SetResourceReference(TextBlock.ForegroundProperty, "B_Warning");
        Grid.SetColumn(safety, 1);
        heading.Children.Add(safety);
        outputPanel.Children.Add(heading);

        TextBlock subtitle = new()
        {
            Text = "Preview settings safely, then send them through PadScope's existing controlled-output gate.",
            FontSize = 10.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 10)
        };
        subtitle.SetResourceReference(TextBlock.ForegroundProperty, "B_TextDim");
        outputPanel.Children.Add(subtitle);

        Border rumbleCard = CreateSubCard();
        StackPanel rumblePanel = new();
        rumblePanel.Children.Add(SectionLabel("RUMBLE MOTORS"));
        _rumbleSmallValue = MonoValue("0");
        _rumbleLargeValue = MonoValue("0");
        PrepareOutputSlider(RumbleSmallSlider);
        PrepareOutputSlider(RumbleLargeSlider);
        rumblePanel.Children.Add(CreateSliderRow("Small", RumbleSmallSlider, _rumbleSmallValue));
        rumblePanel.Children.Add(CreateSliderRow("Large", RumbleLargeSlider, _rumbleLargeValue));
        RumbleSmallSlider.ValueChanged += (_, _) => _rumbleSmallValue.Text = ((int)RumbleSmallSlider.Value).ToString();
        RumbleLargeSlider.ValueChanged += (_, _) => _rumbleLargeValue.Text = ((int)RumbleLargeSlider.Value).ToString();

        PulseRumbleButton.MinWidth = 112;
        PulseRumbleButton.Margin = new Thickness(0, 8, 0, 0);
        PulseRumbleButton.Content = "Pulse rumble";
        rumblePanel.Children.Add(PulseRumbleButton);
        rumbleCard.Child = rumblePanel;
        outputPanel.Children.Add(rumbleCard);

        Border lightCard = CreateSubCard();
        lightCard.Margin = new Thickness(0, 8, 0, 0);
        StackPanel lightPanel = new();
        lightPanel.Children.Add(SectionLabel("LIGHTBAR COLOR"));

        Border previewCard = new()
        {
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 7, 0, 8)
        };
        previewCard.SetResourceReference(Border.BackgroundProperty, "B_Background");
        previewCard.SetResourceReference(Border.BorderBrushProperty, "B_Border");
        Grid previewGrid = new();
        previewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(54) });
        previewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _lightbarPreviewSwatch = new Border
        {
            Width = 42,
            Height = 42,
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        _lightbarPreviewSwatch.SetResourceReference(Border.BorderBrushProperty, "B_Border");
        previewGrid.Children.Add(_lightbarPreviewSwatch);

        StackPanel previewText = new()
        {
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(previewText, 1);
        _lightbarPreviewHex = new TextBlock
        {
            Text = "#000000",
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold
        };
        _lightbarPreviewStatus = new TextBlock
        {
            Text = "Preview only",
            FontSize = 10.5,
            Margin = new Thickness(0, 2, 0, 0)
        };
        _lightbarPreviewStatus.SetResourceReference(TextBlock.ForegroundProperty, "B_TextDim");
        previewText.Children.Add(_lightbarPreviewHex);
        previewText.Children.Add(_lightbarPreviewStatus);
        previewGrid.Children.Add(previewText);
        previewCard.Child = previewGrid;
        lightPanel.Children.Add(previewCard);

        PrepareOutputSlider(LightbarRedSlider);
        PrepareOutputSlider(LightbarGreenSlider);
        PrepareOutputSlider(LightbarBlueSlider);
        TextBlock redValue = MonoValue("0");
        TextBlock greenValue = MonoValue("0");
        TextBlock blueValue = MonoValue("0");
        lightPanel.Children.Add(CreateSliderRow("Red", LightbarRedSlider, redValue));
        lightPanel.Children.Add(CreateSliderRow("Green", LightbarGreenSlider, greenValue));
        lightPanel.Children.Add(CreateSliderRow("Blue", LightbarBlueSlider, blueValue));

        void OnLightSliderChanged()
        {
            redValue.Text = ((int)LightbarRedSlider.Value).ToString();
            greenValue.Text = ((int)LightbarGreenSlider.Value).ToString();
            blueValue.Text = ((int)LightbarBlueSlider.Value).ToString();
            UpdateLightbarPreview(markAsPreview: true);
        }
        LightbarRedSlider.ValueChanged += (_, _) => OnLightSliderChanged();
        LightbarGreenSlider.ValueChanged += (_, _) => OnLightSliderChanged();
        LightbarBlueSlider.ValueChanged += (_, _) => OnLightSliderChanged();

        WrapPanel swatches = new() { Margin = new Thickness(0, 7, 0, 1) };
        foreach ((string name, string hex) in QuickLightbarColors)
        {
            Color color = (Color)ColorConverter.ConvertFromString(hex);
            Button swatch = new()
            {
                Width = 30,
                Height = 28,
                MinWidth = 0,
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 5, 5),
                Background = new SolidColorBrush(color),
                BorderThickness = new Thickness(1),
                Tag = color,
                ToolTip = name
            };
            swatch.SetResourceReference(Control.BorderBrushProperty, "B_Border");
            swatch.Click += (_, _) => SetLightbarSliders((Color)swatch.Tag);
            swatches.Children.Add(swatch);
        }
        lightPanel.Children.Add(swatches);

        WrapPanel lightActions = new() { Margin = new Thickness(0, 6, 0, 0) };
        _pickLightbarColorButton = new Button
        {
            Content = "Pick color",
            MinWidth = 96,
            Margin = new Thickness(0, 0, 6, 6)
        };
        _pickLightbarColorButton.Style = (Style)FindResource("Sec");
        _pickLightbarColorButton.Click += PickLightbarColorButton_Click;
        SetLightbarButton.Content = "Set lightbar";
        SetLightbarButton.MinWidth = 104;
        SetLightbarButton.Margin = new Thickness(0, 0, 6, 6);
        SetLightbarButton.Style = (Style)FindResource("Sec");
        lightActions.Children.Add(_pickLightbarColorButton);
        lightActions.Children.Add(SetLightbarButton);
        lightPanel.Children.Add(lightActions);

        SetLightbarButton.Click += (_, _) =>
        {
            if (LiveStatusText.Text.StartsWith("Lightbar set to RGB", StringComparison.OrdinalIgnoreCase))
            {
                MarkLightbarApplied(
                    (byte)LightbarRedSlider.Value,
                    (byte)LightbarGreenSlider.Value,
                    (byte)LightbarBlueSlider.Value);
            }
        };

        lightCard.Child = lightPanel;
        outputPanel.Children.Add(lightCard);

        ResetOutputButton.Content = "Reset all output";
        ResetOutputButton.Style = (Style)FindResource("Sec");
        ResetOutputButton.MinWidth = 118;
        ResetOutputButton.Margin = new Thickness(0, 9, 0, 0);
        ResetOutputButton.HorizontalAlignment = HorizontalAlignment.Left;
        ResetOutputButton.Click += (_, _) =>
        {
            if (string.Equals(LiveStatusText.Text, "Output reset to neutral.", StringComparison.OrdinalIgnoreCase))
            {
                MarkLightbarReset();
            }
        };
        outputPanel.Children.Add(ResetOutputButton);

        UpdateLightbarPreview(markAsPreview: true);
    }

    private Border CreateSubCard()
    {
        Border card = new()
        {
            Padding = new Thickness(11),
            CornerRadius = new CornerRadius(13),
            BorderThickness = new Thickness(1)
        };
        card.SetResourceReference(Border.BackgroundProperty, "B_CardAlt");
        card.SetResourceReference(Border.BorderBrushProperty, "B_Border");
        return card;
    }

    private TextBlock SectionLabel(string text)
    {
        TextBlock label = new()
        {
            Text = text,
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 4)
        };
        label.SetResourceReference(TextBlock.ForegroundProperty, "B_TextDim");
        return label;
    }

    private TextBlock MonoValue(string text)
    {
        TextBlock value = new()
        {
            Text = text,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 10.5,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        value.SetResourceReference(TextBlock.ForegroundProperty, "B_TextDim");
        return value;
    }

    private Grid CreateSliderRow(string label, Slider slider, TextBlock valueText)
    {
        Grid row = new() { Margin = new Thickness(0, 3, 0, 3) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });
        TextBlock name = new()
        {
            Text = label,
            FontSize = 10.5,
            VerticalAlignment = VerticalAlignment.Center
        };
        name.SetResourceReference(TextBlock.ForegroundProperty, "B_TextDim");
        row.Children.Add(name);
        slider.Margin = new Thickness(6, 0, 7, 0);
        Grid.SetColumn(slider, 1);
        row.Children.Add(slider);
        Grid.SetColumn(valueText, 2);
        row.Children.Add(valueText);
        return row;
    }

    private static void PrepareOutputSlider(Slider slider)
    {
        slider.Width = double.NaN;
        slider.MinWidth = 60;
        slider.HorizontalAlignment = HorizontalAlignment.Stretch;
    }

    private void PickLightbarColorButton_Click(object sender, RoutedEventArgs e)
    {
        Color current = ReadLightbarSliderColor();
        LightbarColorPickerWindow picker = new(current)
        {
            Owner = this
        };
        if (picker.ShowDialog() == true)
        {
            SetLightbarSliders(picker.SelectedColor);
        }
    }

    private void SetLightbarSliders(Color color)
    {
        LightbarRedSlider.Value = color.R;
        LightbarGreenSlider.Value = color.G;
        LightbarBlueSlider.Value = color.B;
        UpdateLightbarPreview(markAsPreview: true);
    }

    private Color ReadLightbarSliderColor() => Color.FromRgb(
        (byte)Math.Round(LightbarRedSlider.Value),
        (byte)Math.Round(LightbarGreenSlider.Value),
        (byte)Math.Round(LightbarBlueSlider.Value));

    private void UpdateLightbarPreview(bool markAsPreview)
    {
        if (_lightbarPreviewSwatch is null || _lightbarPreviewHex is null || _lightbarPreviewStatus is null)
        {
            return;
        }

        Color color = ReadLightbarSliderColor();
        _lightbarPreviewSwatch.Background = new SolidColorBrush(color);
        _lightbarPreviewHex.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        if (markAsPreview)
        {
            bool matchesApplied = _hasAppliedLightbarColor && color == _lastAppliedLightbarColor;
            _lightbarPreviewStatus.Text = matchesApplied ? "Applied to controller" : "Preview only — press Set lightbar to send";
            _lightbarPreviewStatus.SetResourceReference(
                TextBlock.ForegroundProperty,
                matchesApplied ? "B_Success" : "B_TextDim");
        }
    }

    private void MarkLightbarApplied(byte red, byte green, byte blue)
    {
        _lastAppliedLightbarColor = Color.FromRgb(red, green, blue);
        _hasAppliedLightbarColor = true;
        UpdateLightbarPreview(markAsPreview: true);
    }

    private void MarkLightbarReset()
    {
        _hasAppliedLightbarColor = false;
        if (_lightbarPreviewStatus is not null)
        {
            _lightbarPreviewStatus.Text = "Controller output reset to neutral";
            _lightbarPreviewStatus.SetResourceReference(TextBlock.ForegroundProperty, "B_TextDim");
        }
    }

    private void PolishLegacyMiniController(UIElement legacyRoot)
    {
        TextBlock? primaryLabel = Descendants<TextBlock>(legacyRoot)
            .FirstOrDefault(text => string.Equals(text.Text, "PRIMARY DEVICE", StringComparison.Ordinal));
        if (primaryLabel is null)
        {
            return;
        }

        Border? heroCard = Ancestor<Border>(primaryLabel);
        Viewbox? viewbox = heroCard is null ? null : Descendants<Viewbox>(heroCard).FirstOrDefault();
        if (viewbox is null)
        {
            return;
        }

        viewbox.Child = CreateMiniControllerCanvas();
        viewbox.Margin = new Thickness(8, 0, 4, 0);
    }

    private Canvas CreateMiniControllerCanvas()
    {
        Canvas canvas = new() { Width = 520, Height = 220 };

        Path glow = new()
        {
            Data = Geometry.Parse("M 104,62 C 150,30 370,30 416,62"),
            StrokeThickness = 8,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Opacity = 0.35
        };
        glow.SetResourceReference(Shape.StrokeProperty, "B_PrimaryDim");
        canvas.Children.Add(glow);

        Path body = new()
        {
            Data = Geometry.Parse("M 92,56 C 62,58 45,82 46,121 L 54,167 C 60,198 76,211 94,204 C 111,197 126,174 141,151 L 167,119 C 180,103 198,96 222,96 L 298,96 C 322,96 340,103 353,119 L 379,151 C 394,174 409,197 426,204 C 444,211 460,198 466,167 L 474,121 C 475,82 458,58 428,56 C 397,55 377,66 350,75 C 322,84 298,88 260,88 C 222,88 198,84 170,75 C 143,66 123,55 92,56 Z"),
            StrokeThickness = 3,
            Effect = new DropShadowEffect { BlurRadius = 14, ShadowDepth = 0, Opacity = 0.22 }
        };
        body.SetResourceReference(Shape.FillProperty, "B_CardAlt");
        body.SetResourceReference(Shape.StrokeProperty, "B_Border");
        canvas.Children.Add(body);

        Border touch = new()
        {
            Width = 142,
            Height = 54,
            CornerRadius = new CornerRadius(14),
            BorderThickness = new Thickness(1.5)
        };
        touch.SetResourceReference(Border.BackgroundProperty, "B_Background");
        touch.SetResourceReference(Border.BorderBrushProperty, "B_PrimaryDim");
        Canvas.SetLeft(touch, 189);
        Canvas.SetTop(touch, 53);
        canvas.Children.Add(touch);

        Border light = new()
        {
            Width = 96,
            Height = 4,
            CornerRadius = new CornerRadius(2),
            Background = new LinearGradientBrush(
                Color.FromRgb(139, 92, 246),
                Color.FromRgb(34, 211, 238),
                0)
        };
        Canvas.SetLeft(light, 212);
        Canvas.SetTop(light, 62);
        canvas.Children.Add(light);

        AddMiniDpad(canvas, 119, 109);
        AddMiniFaceButtons(canvas, 379, 108);
        AddMiniStick(canvas, 183, 139, "B_PrimaryDim");
        AddMiniStick(canvas, 287, 139, "B_Primary");

        Border ps = CirclePill(30, "B_PrimaryDim");
        ps.Child = new TextBlock
        {
            Text = "P",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Canvas.SetLeft(ps, 245);
        Canvas.SetTop(ps, 143);
        canvas.Children.Add(ps);

        AddMiniShoulder(canvas, "L1", 104, 40);
        AddMiniShoulder(canvas, "L2", 154, 47);
        AddMiniShoulder(canvas, "R2", 326, 47);
        AddMiniShoulder(canvas, "R1", 376, 40);

        return canvas;
    }

    private void AddMiniDpad(Canvas canvas, double left, double top)
    {
        double s = 24;
        foreach ((double x, double y) in new[] { (s, 0d), (s, s * 2), (0d, s), (s * 2, s) })
        {
            Border key = new()
            {
                Width = s,
                Height = s,
                CornerRadius = new CornerRadius(5),
                BorderThickness = new Thickness(1)
            };
            key.SetResourceReference(Border.BackgroundProperty, "B_Background");
            key.SetResourceReference(Border.BorderBrushProperty, "B_Border");
            Canvas.SetLeft(key, left + x);
            Canvas.SetTop(key, top + y);
            canvas.Children.Add(key);
        }
    }

    private void AddMiniFaceButtons(Canvas canvas, double left, double top)
    {
        AddMiniFace(canvas, "△", left + 24, top, "#34D399");
        AddMiniFace(canvas, "○", left + 48, top + 24, "#FB7185");
        AddMiniFace(canvas, "×", left + 24, top + 48, "#60A5FA");
        AddMiniFace(canvas, "□", left, top + 24, "#F472B6");
    }

    private static void AddMiniFace(Canvas canvas, string text, double left, double top, string color)
    {
        Color accent = (Color)ColorConverter.ConvertFromString(color);
        Border circle = new()
        {
            Width = 26,
            Height = 26,
            CornerRadius = new CornerRadius(13),
            BorderThickness = new Thickness(1.5),
            BorderBrush = new SolidColorBrush(accent),
            Background = Brushes.Transparent,
            Child = new TextBlock
            {
                Text = text,
                FontSize = 12,
                Foreground = new SolidColorBrush(accent),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        Canvas.SetLeft(circle, left);
        Canvas.SetTop(circle, top);
        canvas.Children.Add(circle);
    }

    private void AddMiniStick(Canvas canvas, double left, double top, string accentKey)
    {
        Ellipse outer = new() { Width = 58, Height = 58, StrokeThickness = 3 };
        outer.SetResourceReference(Shape.FillProperty, "B_Background");
        outer.SetResourceReference(Shape.StrokeProperty, accentKey);
        Canvas.SetLeft(outer, left);
        Canvas.SetTop(outer, top);
        canvas.Children.Add(outer);
        Ellipse dot = new() { Width = 9, Height = 9 };
        dot.SetResourceReference(Shape.FillProperty, "B_Primary");
        Canvas.SetLeft(dot, left + 24.5);
        Canvas.SetTop(dot, top + 24.5);
        canvas.Children.Add(dot);
    }

    private void AddMiniShoulder(Canvas canvas, string text, double left, double top)
    {
        Border shoulder = new()
        {
            Width = 42,
            Height = 24,
            CornerRadius = new CornerRadius(9),
            BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                Text = text,
                FontSize = 9.5,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        shoulder.SetResourceReference(Border.BackgroundProperty, "B_Card");
        shoulder.SetResourceReference(Border.BorderBrushProperty, "B_Border");
        Canvas.SetLeft(shoulder, left);
        Canvas.SetTop(shoulder, top);
        canvas.Children.Add(shoulder);
    }

    private Border CirclePill(double size, string borderKey)
    {
        Border border = new()
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(size / 2),
            BorderThickness = new Thickness(2)
        };
        border.SetResourceReference(Border.BackgroundProperty, "B_Background");
        border.SetResourceReference(Border.BorderBrushProperty, borderKey);
        return border;
    }

    private Style CreateWorkspaceTabStyle()
    {
        Style? baseStyle = TryFindResource(typeof(TabItem)) as Style;
        Style style = new(typeof(TabItem), baseStyle);
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(16, 8, 16, 8)));
        style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0, 0, 9, 0)));
        style.Setters.Add(new Setter(FrameworkElement.MinWidthProperty, 112d));
        style.Setters.Add(new Setter(Control.FontSizeProperty, 12.5d));
        return style;
    }

    private void PolishMainNavigation(TabItem liveTab)
    {
        if (ItemsControl.ItemsControlFromItemContainer(liveTab) is not TabControl mainTabs)
        {
            return;
        }

        foreach (TabItem tab in mainTabs.Items.OfType<TabItem>())
        {
            tab.MinWidth = 104;
            tab.Padding = new Thickness(19, 10, 19, 10);
            tab.Margin = new Thickness(0, 0, 10, 0);
        }
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int index = 0; index < count; index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, index);
            if (child is T match)
            {
                yield return match;
            }
            foreach (T nested in Descendants<T>(child))
            {
                yield return nested;
            }
        }
    }

    private static T? Ancestor<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject? current = VisualTreeHelper.GetParent(child);
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private static readonly (string Name, string Hex)[] QuickLightbarColors =
    {
        ("Ice", "#67E8F9"),
        ("Blue", "#3B82F6"),
        ("Violet", "#8B5CF6"),
        ("Pink", "#F472B6"),
        ("Red", "#FB7185"),
        ("Amber", "#FBBF24"),
        ("Green", "#34D399")
    };
}
