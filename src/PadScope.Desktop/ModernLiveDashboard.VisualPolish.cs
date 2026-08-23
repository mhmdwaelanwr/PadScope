using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using PadScope.Core.Input;

namespace PadScope.Desktop;

public partial class ModernLiveDashboard
{
    private bool _controllerVisualPolishApplied;
    private Border? _l1Visual;
    private Border? _l2Visual;
    private Border? _r1Visual;
    private Border? _r2Visual;
    private Border? _shareVisual;
    private Border? _optionsVisual;
    private Border? _touchpadPressVisual;
    private Ellipse? _l3Visual;
    private Ellipse? _r3Visual;

    public void SetSessionBusy(bool busy, string? status)
    {
        DevicePicker.IsEnabled = !busy;
        StartButton.IsEnabled = !busy && DevicePicker.Items.Count > 0;
        if (busy)
        {
            StopButton.IsEnabled = false;
        }
        StartButton.Content = busy ? "Opening…" : "Start live";
        if (!string.IsNullOrWhiteSpace(status))
        {
            SessionStateText.Text = status;
        }
    }

    public void ApplyControllerVisualPolish()
    {
        if (_controllerVisualPolishApplied)
        {
            return;
        }

        if (VisualTreeHelper.GetParent(ControllerLeftStickDot) is not Canvas canvas)
        {
            return;
        }

        _controllerVisualPolishApplied = true;

        foreach (Border border in FindVisualChildren<Border>(canvas))
        {
            string text = FindVisualChildren<TextBlock>(border)
                .Select(block => block.Text)
                .FirstOrDefault(value => value is "L1 / L2" or "R1 / R2") ?? string.Empty;
            if (text.Length > 0)
            {
                border.Visibility = Visibility.Collapsed;
            }
        }

        foreach (TextBlock text in FindVisualChildren<TextBlock>(canvas))
        {
            if (text.Text is "SHARE" or "OPTIONS")
            {
                text.Visibility = Visibility.Collapsed;
            }
        }

        Path leftGrip = new()
        {
            Data = Geometry.Parse("M 86,104 C 72,145 72,229 94,272 C 103,289 112,292 123,279"),
            Stroke = ResolveBrush("B_PrimaryDim"),
            StrokeThickness = 2,
            Opacity = 0.38,
            IsHitTestVisible = false
        };
        Path rightGrip = new()
        {
            Data = Geometry.Parse("M 454,104 C 468,145 468,229 446,272 C 437,289 428,292 417,279"),
            Stroke = ResolveBrush("B_Primary"),
            StrokeThickness = 2,
            Opacity = 0.32,
            IsHitTestVisible = false
        };
        canvas.Children.Add(leftGrip);
        canvas.Children.Add(rightGrip);

        _l2Visual = AddControllerPill(canvas, "L2", 96, 55, 62, 27);
        _l1Visual = AddControllerPill(canvas, "L1", 164, 61, 62, 27);
        _r1Visual = AddControllerPill(canvas, "R1", 314, 61, 62, 27);
        _r2Visual = AddControllerPill(canvas, "R2", 382, 55, 62, 27);

        _shareVisual = AddControllerPill(canvas, "SHARE", 218, 162, 60, 24, 8.5);
        _optionsVisual = AddControllerPill(canvas, "OPTIONS", 284, 162, 66, 24, 8.5);

        _touchpadPressVisual = new Border
        {
            Width = 164,
            Height = 76,
            CornerRadius = new CornerRadius(13),
            Background = Brushes.Transparent,
            BorderBrush = ResolveBrush("B_PrimaryDim"),
            BorderThickness = new Thickness(1.5),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(_touchpadPressVisual, 188);
        Canvas.SetTop(_touchpadPressVisual, 82);
        canvas.Children.Add(_touchpadPressVisual);

        _l3Visual = AddStickClickRing(canvas, 174, 207, ResolveBrush("B_PrimaryDim"));
        _r3Visual = AddStickClickRing(canvas, 296, 207, ResolveBrush("B_Primary"));

        foreach (Ellipse face in new[] { TriangleShape, CircleShape, CrossShape, SquareShape })
        {
            face.StrokeThickness = 2.6;
        }
        foreach (Rectangle dpad in new[] { DpadUpShape, DpadDownShape, DpadLeftShape, DpadRightShape })
        {
            dpad.StrokeThickness = 1.7;
            dpad.Opacity = 0.98;
        }
        PsShape.StrokeThickness = 2.6;
    }

    public void UpdateExtendedControllerVisuals(Ds4InputState state)
    {
        if (!_controllerVisualPolishApplied)
        {
            ApplyControllerVisualPolish();
        }

        Brush primary = ResolveBrush("B_Primary");
        Brush success = ResolveBrush("B_Success");
        Brush secondary = ResolveBrush("B_PrimaryDim");

        SetBorderState(_l1Visual, state.Buttons.HasFlag(Ds4Buttons.L1), secondary);
        SetBorderState(_l2Visual, state.Buttons.HasFlag(Ds4Buttons.L2) || state.LeftTrigger > 8, secondary);
        SetBorderState(_r1Visual, state.Buttons.HasFlag(Ds4Buttons.R1), primary);
        SetBorderState(_r2Visual, state.Buttons.HasFlag(Ds4Buttons.R2) || state.RightTrigger > 8, primary);
        SetBorderState(_shareVisual, state.Buttons.HasFlag(Ds4Buttons.Share), primary);
        SetBorderState(_optionsVisual, state.Buttons.HasFlag(Ds4Buttons.Options), primary);
        SetBorderState(_touchpadPressVisual, state.Buttons.HasFlag(Ds4Buttons.TouchpadClick), success);
        SetRingState(_l3Visual, state.Buttons.HasFlag(Ds4Buttons.L3), secondary);
        SetRingState(_r3Visual, state.Buttons.HasFlag(Ds4Buttons.R3), primary);
    }

    private Border AddControllerPill(
        Canvas canvas,
        string label,
        double left,
        double top,
        double width,
        double height,
        double fontSize = 10)
    {
        Border pill = new()
        {
            Width = width,
            Height = height,
            CornerRadius = new CornerRadius(height / 2),
            Background = ResolveBrush("B_Card"),
            BorderBrush = ResolveBrush("B_Border"),
            BorderThickness = new Thickness(1.2),
            IsHitTestVisible = false,
            Child = new TextBlock
            {
                Text = label,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = ResolveBrush("B_TextDim"),
                FontWeight = FontWeights.SemiBold,
                FontSize = fontSize
            }
        };
        Canvas.SetLeft(pill, left);
        Canvas.SetTop(pill, top);
        canvas.Children.Add(pill);
        return pill;
    }

    private static Ellipse AddStickClickRing(Canvas canvas, double left, double top, Brush accent)
    {
        Ellipse ring = new()
        {
            Width = 70,
            Height = 70,
            Fill = Brushes.Transparent,
            Stroke = accent,
            StrokeThickness = 2,
            Opacity = 0.20,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(ring, left);
        Canvas.SetTop(ring, top);
        canvas.Children.Add(ring);
        return ring;
    }

    private void SetBorderState(Border? border, bool pressed, Brush accent)
    {
        if (border is null)
        {
            return;
        }

        border.Background = pressed ? accent : ResolveBrush("B_Card");
        border.BorderBrush = pressed ? accent : ResolveBrush("B_Border");
        border.Opacity = pressed ? 0.96 : 0.86;
        if (border.Child is TextBlock label)
        {
            label.Foreground = pressed ? Brushes.White : ResolveBrush("B_TextDim");
        }
    }

    private void SetRingState(Ellipse? ring, bool pressed, Brush accent)
    {
        if (ring is null)
        {
            return;
        }
        ring.Stroke = accent;
        ring.StrokeThickness = pressed ? 5 : 2;
        ring.Opacity = pressed ? 0.95 : 0.20;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int index = 0; index < count; index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, index);
            if (child is T match)
            {
                yield return match;
            }

            foreach (T descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }
}
