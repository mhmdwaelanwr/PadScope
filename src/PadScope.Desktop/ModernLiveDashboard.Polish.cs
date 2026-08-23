using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using PadScope.Core.Input;

namespace PadScope.Desktop;

public partial class ModernLiveDashboard
{
    private readonly Dictionary<Ds4Buttons, FrameworkElement> _extendedButtonIndicators = new();
    private Border? _touchpadClickSurface;
    private Canvas? _controllerCanvas;

    private void InstallControllerPolish()
    {
        _controllerCanvas = ControllerLeftStickDot.Parent as Canvas;
        if (_controllerCanvas is null)
        {
            return;
        }

        PolishControllerBody(_controllerCanvas);
        ReplaceCombinedShoulderLabels(_controllerCanvas);
        ReplaceSystemLabels(_controllerCanvas);
        RegisterTouchpadSurface(_controllerCanvas);
        AddStickClickIndicators(_controllerCanvas);
        PolishExistingControllerButtons();
    }

    private void PolishControllerBody(Canvas canvas)
    {
        Path[] paths = canvas.Children.OfType<Path>().ToArray();
        if (paths.Length > 0)
        {
            paths[0].StrokeThickness = 7;
            paths[0].Opacity = 0.7;
            paths[0].Effect = new DropShadowEffect
            {
                BlurRadius = 12,
                ShadowDepth = 0,
                Opacity = 0.18
            };
        }
        if (paths.Length > 1)
        {
            paths[1].StrokeThickness = 3.2;
            paths[1].Effect = new DropShadowEffect
            {
                BlurRadius = 20,
                ShadowDepth = 4,
                Direction = 270,
                Opacity = 0.18
            };
        }
    }

    private void ReplaceCombinedShoulderLabels(Canvas canvas)
    {
        foreach (Border border in canvas.Children.OfType<Border>().ToArray())
        {
            string? text = DescendantText(border);
            if (text is "L1 / L2" or "R1 / R2")
            {
                border.Visibility = Visibility.Collapsed;
            }
        }

        AddButtonPill(canvas, Ds4Buttons.L2, "L2", 78, 56, 54, 27);
        AddButtonPill(canvas, Ds4Buttons.L1, "L1", 137, 56, 54, 27);
        AddButtonPill(canvas, Ds4Buttons.R1, "R1", 349, 56, 54, 27);
        AddButtonPill(canvas, Ds4Buttons.R2, "R2", 408, 56, 54, 27);
    }

    private void ReplaceSystemLabels(Canvas canvas)
    {
        foreach (TextBlock text in canvas.Children.OfType<TextBlock>().ToArray())
        {
            if (text.Text is "SHARE" or "OPTIONS")
            {
                text.Visibility = Visibility.Collapsed;
            }
        }

        AddButtonPill(canvas, Ds4Buttons.Share, "SHARE", 211, 164, 54, 24, system: true);
        AddButtonPill(canvas, Ds4Buttons.Options, "OPTIONS", 276, 164, 58, 24, system: true);
    }

    private void RegisterTouchpadSurface(Canvas canvas)
    {
        _touchpadClickSurface = canvas.Children
            .OfType<Border>()
            .FirstOrDefault(border => string.Equals(DescendantText(border), "TOUCHPAD", StringComparison.Ordinal));
        if (_touchpadClickSurface is not null)
        {
            _extendedButtonIndicators[Ds4Buttons.TouchpadClick] = _touchpadClickSurface;
            _touchpadClickSurface.ToolTip = "Touchpad click";
        }
    }

    private void AddStickClickIndicators(Canvas canvas)
    {
        Ellipse left = new()
        {
            Width = 58,
            Height = 58,
            StrokeThickness = 2.2,
            Fill = Brushes.Transparent,
            ToolTip = "L3 · left stick click",
            IsHitTestVisible = false
        };
        left.SetResourceReference(Shape.StrokeProperty, "B_Border");
        Canvas.SetLeft(left, 180);
        Canvas.SetTop(left, 213);
        canvas.Children.Add(left);
        _extendedButtonIndicators[Ds4Buttons.L3] = left;

        Ellipse right = new()
        {
            Width = 58,
            Height = 58,
            StrokeThickness = 2.2,
            Fill = Brushes.Transparent,
            ToolTip = "R3 · right stick click",
            IsHitTestVisible = false
        };
        right.SetResourceReference(Shape.StrokeProperty, "B_Border");
        Canvas.SetLeft(right, 302);
        Canvas.SetTop(right, 213);
        canvas.Children.Add(right);
        _extendedButtonIndicators[Ds4Buttons.R3] = right;
    }

    private void AddButtonPill(
        Canvas canvas,
        Ds4Buttons flag,
        string label,
        double left,
        double top,
        double width,
        double height,
        bool system = false)
    {
        Border pill = new()
        {
            Width = width,
            Height = height,
            CornerRadius = new CornerRadius(height / 2.2),
            BorderThickness = new Thickness(1),
            ToolTip = label,
            Child = new TextBlock
            {
                Text = label,
                FontSize = system ? 8.2 : 9.5,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            }
        };
        pill.SetResourceReference(Border.BackgroundProperty, "B_Card");
        pill.SetResourceReference(Border.BorderBrushProperty, "B_Border");
        if (pill.Child is TextBlock text)
        {
            text.SetResourceReference(TextBlock.ForegroundProperty, "B_TextDim");
        }
        Canvas.SetLeft(pill, left);
        Canvas.SetTop(pill, top);
        canvas.Children.Add(pill);
        _extendedButtonIndicators[flag] = pill;
    }

    private void PolishExistingControllerButtons()
    {
        foreach (Shape shape in new Shape[]
                 {
                     TriangleShape, CircleShape, CrossShape, SquareShape,
                     DpadUpShape, DpadDownShape, DpadLeftShape, DpadRightShape, PsShape
                 })
        {
            shape.StrokeThickness = Math.Max(shape.StrokeThickness, 1.7);
        }

        TriangleShape.ToolTip = "Triangle";
        CircleShape.ToolTip = "Circle";
        CrossShape.ToolTip = "Cross";
        SquareShape.ToolTip = "Square";
        DpadUpShape.ToolTip = "D-Pad Up";
        DpadDownShape.ToolTip = "D-Pad Down";
        DpadLeftShape.ToolTip = "D-Pad Left";
        DpadRightShape.ToolTip = "D-Pad Right";
        PsShape.ToolTip = "PlayStation / Home";
    }

    private void UpdateExtendedButtonTelemetry(Ds4InputState state)
    {
        foreach ((Ds4Buttons flag, FrameworkElement indicator) in _extendedButtonIndicators)
        {
            bool pressed = state.Buttons.HasFlag(flag);
            Brush accent = flag switch
            {
                Ds4Buttons.L2 or Ds4Buttons.R2 => ResolveBrush("B_Warning"),
                Ds4Buttons.L3 or Ds4Buttons.R3 => ResolveBrush("B_Success"),
                Ds4Buttons.Share or Ds4Buttons.Options or Ds4Buttons.TouchpadClick => ResolveBrush("B_PrimaryDim"),
                _ => ResolveBrush("B_Primary")
            };
            SetExtendedIndicatorState(indicator, pressed, accent);
        }
    }

    private void SetExtendedIndicatorState(FrameworkElement element, bool pressed, Brush accent)
    {
        switch (element)
        {
            case Border border:
                if (pressed)
                {
                    border.Background = AccentFill(accent, 0x48);
                    border.BorderBrush = accent;
                    border.BorderThickness = new Thickness(2);
                    if (border.Child is TextBlock text)
                    {
                        text.Foreground = ResolveBrush("B_Text");
                    }
                }
                else
                {
                    border.SetResourceReference(Border.BackgroundProperty, "B_Card");
                    border.SetResourceReference(Border.BorderBrushProperty, "B_Border");
                    border.BorderThickness = new Thickness(1);
                    if (border.Child is TextBlock text)
                    {
                        text.SetResourceReference(TextBlock.ForegroundProperty, "B_TextDim");
                    }
                }
                break;

            case Ellipse ellipse:
                ellipse.Stroke = pressed ? accent : ResolveBrush("B_Border");
                ellipse.StrokeThickness = pressed ? 3.2 : 2.2;
                ellipse.Fill = pressed ? AccentFill(accent, 0x2A) : Brushes.Transparent;
                break;
        }
    }

    private static Brush AccentFill(Brush accent, byte alpha)
    {
        if (accent is SolidColorBrush solid)
        {
            return new SolidColorBrush(Color.FromArgb(alpha, solid.Color.R, solid.Color.G, solid.Color.B));
        }
        return accent;
    }

    private static string? DescendantText(DependencyObject root)
    {
        if (root is TextBlock textBlock)
        {
            return textBlock.Text;
        }

        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int index = 0; index < count; index++)
        {
            string? text = DescendantText(VisualTreeHelper.GetChild(root, index));
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }
        return null;
    }
}
