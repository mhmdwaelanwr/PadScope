using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using PadScope.Core.Diagnostics;
using PadScope.Core.Input;
using PadScope.Core.Models;

namespace PadScope.Desktop;

public partial class ControllerDiagnosticsLab : UserControl
{
    private readonly StickDiagnosticsAnalyzer _rangeLeft = new();
    private readonly StickDiagnosticsAnalyzer _rangeRight = new();
    private readonly StickDiagnosticsAnalyzer _staticLeft = new();
    private readonly StickDiagnosticsAnalyzer _staticRight = new();
    private readonly List<double> _pollingRates = new();
    private readonly DispatcherTimer _staticTestTimer;

    private StickDiagnosticsSnapshot _lastStaticLeft = EmptyStickSnapshot();
    private StickDiagnosticsSnapshot _lastStaticRight = EmptyStickSnapshot();
    private ControllerDevice? _device;
    private bool _sessionRunning;
    private bool _staticTestActive;
    private bool _rangeCaptureActive;
    private bool _pollingTestActive;
    private DateTimeOffset _staticTestEndsAt;
    private bool _previousTouchpadPressed;
    private int _touchpadPressCount;
    private VibrationPattern _selectedPattern = VibrationPattern.Custom;

    public event EventHandler<VibrationRequestEventArgs>? VibrationRequested;
    public event EventHandler? StopVibrationRequested;

    public ControllerDiagnosticsLab()
    {
        InitializeComponent();
        _staticTestTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _staticTestTimer.Tick += StaticTestTimer_Tick;
        DrawStickDiagnostics();
        RenderPollingGraph();
        RefreshVibrationValues();
        SetSessionState(false);
    }

    public void SetDevice(ControllerDevice? device)
    {
        _device = device;
        UpdateConnectionPill();
    }

    public void SetSessionState(bool running)
    {
        _sessionRunning = running;
        StaticTestButton.IsEnabled = running && !_staticTestActive;
        RangeCaptureButton.IsEnabled = running;
        PollingTestButton.IsEnabled = running;
        StartVibrationButton.IsEnabled = running;
        StopVibrationButton.IsEnabled = running;
        VibrationStatusText.Text = running ? "Ready · confirmation required" : "Start live input first";
        UpdateConnectionPill();
    }

    public void UpdateTelemetry(Ds4InputState state, ReportTimingSnapshot? timing)
    {
        if (_rangeCaptureActive)
        {
            _rangeLeft.Add(state.LeftStickXNorm, state.LeftStickYNorm);
            _rangeRight.Add(state.RightStickXNorm, state.RightStickYNorm);
        }

        if (_staticTestActive)
        {
            _staticLeft.Add(state.LeftStickXNorm, state.LeftStickYNorm);
            _staticRight.Add(state.RightStickXNorm, state.RightStickYNorm);
        }

        if (_pollingTestActive && timing is { ReportCount: >= 2, ReportRateHz: > 0 })
        {
            _pollingRates.Add(timing.ReportRateHz);
            if (_pollingRates.Count > 160)
            {
                _pollingRates.RemoveAt(0);
            }
        }

        UpdateStickReadouts();
        UpdatePollingReadouts(timing);
        UpdateTouchpad(state);

        string buttonState = state.Buttons == Ds4Buttons.None ? "no buttons" : state.Buttons.ToString();
        InputHealthText.Text = timing is { ReportCount: >= 2 }
            ? $"Report 0x{state.ReportId:X2} · {timing.ReportRateHz:F0} Hz · jitter {timing.JitterMs:F2} ms · {buttonState}"
            : $"Report 0x{state.ReportId:X2} · collecting timing · {buttonState}";
    }

    private void StaticTestButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_sessionRunning)
        {
            return;
        }

        _staticLeft.Reset();
        _staticRight.Reset();
        _staticTestActive = true;
        _staticTestEndsAt = DateTimeOffset.UtcNow.AddSeconds(3);
        StaticTestButton.IsEnabled = false;
        StaticTestButton.Content = "Testing… 3.0s";
        DriftInstructionText.Text = "Hands off both sticks. PadScope is sampling center drift and jitter for 3 seconds…";
        _staticTestTimer.Start();
    }

    private void StaticTestTimer_Tick(object? sender, EventArgs e)
    {
        if (!_staticTestActive)
        {
            _staticTestTimer.Stop();
            return;
        }

        TimeSpan remaining = _staticTestEndsAt - DateTimeOffset.UtcNow;
        if (remaining > TimeSpan.Zero)
        {
            StaticTestButton.Content = $"Testing… {remaining.TotalSeconds:F1}s";
            return;
        }

        _staticTestActive = false;
        _staticTestTimer.Stop();
        _lastStaticLeft = _staticLeft.Snapshot();
        _lastStaticRight = _staticRight.Snapshot();
        StaticTestButton.Content = "Run 3s drift test";
        StaticTestButton.IsEnabled = _sessionRunning;

        double worst = Math.Max(_lastStaticLeft.DriftMagnitude, _lastStaticRight.DriftMagnitude);
        DriftInstructionText.Text = worst switch
        {
            < 0.03 => "Static result: center looks healthy. Rotate both sticks around their full edge to measure range and circularity.",
            < 0.08 => "Static result: mild center offset detected. The recommended deadzone values below can mask it.",
            _ => "Static result: notable drift detected. Consider cleaning/calibration or a larger deadzone before competitive use."
        };
        UpdateStickReadouts();
    }

    private void RangeCaptureButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_sessionRunning)
        {
            return;
        }

        _rangeCaptureActive = !_rangeCaptureActive;
        RangeCaptureButton.Content = _rangeCaptureActive ? "Stop range capture" : "Start range capture";
        if (_rangeCaptureActive && _rangeLeft.Snapshot().SampleCount == 0)
        {
            DriftInstructionText.Text = "Rotate each stick slowly around its full outer edge. Coverage fills as PadScope sees each angle.";
        }
    }

    private void ResetStickDiagnosticsButton_Click(object sender, RoutedEventArgs e)
    {
        _rangeLeft.Reset();
        _rangeRight.Reset();
        _staticLeft.Reset();
        _staticRight.Reset();
        _lastStaticLeft = EmptyStickSnapshot();
        _lastStaticRight = EmptyStickSnapshot();
        _rangeCaptureActive = false;
        _staticTestActive = false;
        _staticTestTimer.Stop();
        RangeCaptureButton.Content = "Start range capture";
        StaticTestButton.Content = "Run 3s drift test";
        StaticTestButton.IsEnabled = _sessionRunning;
        DriftInstructionText.Text = "Leave both sticks untouched and run the static test, then rotate each stick around its full outer edge for range coverage.";
        UpdateStickReadouts();
    }

    private void PollingTestButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_sessionRunning)
        {
            return;
        }

        _pollingTestActive = !_pollingTestActive;
        if (_pollingTestActive)
        {
            _pollingRates.Clear();
            PollingTestButton.Content = "Stop test";
        }
        else
        {
            PollingTestButton.Content = "Start test";
        }
        RenderPollingGraph();
    }

    private void UpdatePollingReadouts(ReportTimingSnapshot? timing)
    {
        if (timing is not { ReportCount: >= 2 })
        {
            PollingCurrentText.Text = "-- Hz";
            PollingIntervalText.Text = "-- ms";
            PollingJitterText.Text = "-- ms";
            PollingP95Text.Text = "-- ms";
            PollingSpikesText.Text = "--";
            PollingPeakAverageText.Text = "-- / -- Hz";
            return;
        }

        PollingCurrentText.Text = $"{timing.ReportRateHz:F0} Hz";
        PollingIntervalText.Text = $"{timing.AverageIntervalMs:F2} ms";
        PollingJitterText.Text = $"{timing.JitterMs:F2} ms";
        PollingP95Text.Text = $"{timing.P95IntervalMs:F2} ms";
        PollingSpikesText.Text = timing.SpikeCount.ToString();

        if (_pollingRates.Count > 0)
        {
            PollingPeakAverageText.Text = $"{_pollingRates.Max():F0} / {_pollingRates.Average():F0} Hz";
        }
        else
        {
            PollingPeakAverageText.Text = $"{timing.ReportRateHz:F0} / {timing.ReportRateHz:F0} Hz";
        }

        RenderPollingGraph();
    }

    private void PollingGraphCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => RenderPollingGraph();

    private void RenderPollingGraph()
    {
        Canvas canvas = PollingGraphCanvas;
        canvas.Children.Clear();
        double width = Math.Max(260, canvas.ActualWidth > 0 ? canvas.ActualWidth : 360);
        double height = Math.Max(150, canvas.ActualHeight > 0 ? canvas.ActualHeight : 190);
        Brush grid = ResolveBrush("B_Border");
        Brush accent = ResolveBrush("B_Primary");
        Brush dim = ResolveBrush("B_TextDim");

        for (int row = 0; row <= 4; row++)
        {
            double y = 8 + (height - 24) * row / 4.0;
            canvas.Children.Add(new Line
            {
                X1 = 0,
                X2 = width,
                Y1 = y,
                Y2 = y,
                Stroke = grid,
                StrokeThickness = 1,
                Opacity = 0.45
            });
        }

        if (_pollingRates.Count < 2)
        {
            TextBlock waiting = new()
            {
                Text = _pollingTestActive ? "Collecting native HID timing…" : "Start the test to record a rate history",
                Foreground = dim,
                FontSize = 11
            };
            Canvas.SetLeft(waiting, 12);
            Canvas.SetTop(waiting, height / 2 - 8);
            canvas.Children.Add(waiting);
            return;
        }

        double ceiling = Math.Max(125, _pollingRates.Max() * 1.15);
        Polyline line = new()
        {
            Stroke = accent,
            StrokeThickness = 2.5,
            StrokeLineJoin = PenLineJoin.Round
        };

        int count = _pollingRates.Count;
        for (int index = 0; index < count; index++)
        {
            double x = count == 1 ? 0 : index * (width - 8) / (count - 1.0) + 4;
            double normalized = Math.Clamp(_pollingRates[index] / ceiling, 0, 1);
            double y = height - 10 - normalized * (height - 24);
            line.Points.Add(new Point(x, y));
        }
        canvas.Children.Add(line);
    }

    private void UpdateStickReadouts()
    {
        StickDiagnosticsSnapshot rangeLeft = _rangeLeft.Snapshot();
        StickDiagnosticsSnapshot rangeRight = _rangeRight.Snapshot();

        LeftDriftText.Text = _lastStaticLeft.SampleCount > 0 ? $"{_lastStaticLeft.DriftMagnitude:P2}" : "--";
        RightDriftText.Text = _lastStaticRight.SampleCount > 0 ? $"{_lastStaticRight.DriftMagnitude:P2}" : "--";
        LeftDeadzoneText.Text = _lastStaticLeft.SampleCount > 0 ? $"{_lastStaticLeft.RecommendedDeadzone:P1}" : "--";
        RightDeadzoneText.Text = _lastStaticRight.SampleCount > 0 ? $"{_lastStaticRight.RecommendedDeadzone:P1}" : "--";
        LeftCoverageText.Text = rangeLeft.SampleCount > 0 ? $"{rangeLeft.RangeCoveragePercent:F0}%" : "--";
        RightCoverageText.Text = rangeRight.SampleCount > 0 ? $"{rangeRight.RangeCoveragePercent:F0}%" : "--";
        LeftCircularityText.Text = rangeLeft.SampleCount > 0 ? $"{rangeLeft.CircularityErrorPercent:F1}%" : "--";
        RightCircularityText.Text = rangeRight.SampleCount > 0 ? $"{rangeRight.CircularityErrorPercent:F1}%" : "--";

        RenderStickCanvas(LeftRangeCanvas, rangeLeft, _lastStaticLeft, ResolveBrush("B_Success"));
        RenderStickCanvas(RightRangeCanvas, rangeRight, _lastStaticRight, ResolveBrush("B_Primary"));
    }

    private void DrawStickDiagnostics() => UpdateStickReadouts();

    private void RenderStickCanvas(
        Canvas canvas,
        StickDiagnosticsSnapshot range,
        StickDiagnosticsSnapshot center,
        Brush accent)
    {
        canvas.Children.Clear();
        const double size = 230;
        const double c = size / 2;
        const double radius = 94;
        Brush border = ResolveBrush("B_Border");
        Brush background = ResolveBrush("B_Background");

        canvas.Children.Add(new Ellipse
        {
            Width = radius * 2,
            Height = radius * 2,
            Stroke = border,
            StrokeThickness = 1.5,
            Fill = background,
            Opacity = 0.9
        });
        Canvas.SetLeft(canvas.Children[^1], c - radius);
        Canvas.SetTop(canvas.Children[^1], c - radius);

        foreach (double ringScale in new[] { 0.25, 0.5, 0.75 })
        {
            Ellipse ring = new()
            {
                Width = radius * 2 * ringScale,
                Height = radius * 2 * ringScale,
                Stroke = border,
                StrokeThickness = 1,
                Opacity = 0.45
            };
            Canvas.SetLeft(ring, c - radius * ringScale);
            Canvas.SetTop(ring, c - radius * ringScale);
            canvas.Children.Add(ring);
        }

        canvas.Children.Add(new Line { X1 = c - radius, X2 = c + radius, Y1 = c, Y2 = c, Stroke = border, Opacity = 0.45 });
        canvas.Children.Add(new Line { X1 = c, X2 = c, Y1 = c - radius, Y2 = c + radius, Stroke = border, Opacity = 0.45 });

        if (range.RangeBins.Count > 0)
        {
            for (int index = 0; index < range.RangeBins.Count; index++)
            {
                double magnitude = range.RangeBins[index];
                if (magnitude < 0.18)
                {
                    continue;
                }

                double angle = index * Math.PI * 2 / range.RangeBins.Count;
                double shownMagnitude = Math.Clamp(magnitude, 0, 1.15) / 1.15;
                double x2 = c + Math.Cos(angle) * radius * shownMagnitude;
                double y2 = c - Math.Sin(angle) * radius * shownMagnitude;
                canvas.Children.Add(new Line
                {
                    X1 = c,
                    Y1 = c,
                    X2 = x2,
                    Y2 = y2,
                    Stroke = accent,
                    StrokeThickness = 2.2,
                    Opacity = 0.58
                });
            }
        }

        if (center.SampleCount > 0)
        {
            double dz = Math.Clamp(center.RecommendedDeadzone, 0, 0.35);
            if (dz > 0.001)
            {
                Ellipse deadzone = new()
                {
                    Width = radius * 2 * dz,
                    Height = radius * 2 * dz,
                    Stroke = ResolveBrush("B_Warning"),
                    StrokeThickness = 1.5,
                    StrokeDashArray = new DoubleCollection { 3, 3 },
                    Opacity = 0.8
                };
                Canvas.SetLeft(deadzone, c - radius * dz);
                Canvas.SetTop(deadzone, c - radius * dz);
                canvas.Children.Add(deadzone);
            }

            Ellipse centerDot = new() { Width = 10, Height = 10, Fill = accent };
            Canvas.SetLeft(centerDot, c + center.CenterX * radius - 5);
            Canvas.SetTop(centerDot, c + center.CenterY * radius - 5);
            canvas.Children.Add(centerDot);
        }
        else
        {
            Ellipse centerDot = new() { Width = 8, Height = 8, Fill = accent, Opacity = 0.8 };
            Canvas.SetLeft(centerDot, c - 4);
            Canvas.SetTop(centerDot, c - 4);
            canvas.Children.Add(centerDot);
        }
    }

    private void UpdateTouchpad(Ds4InputState state)
    {
        bool pressed = state.Buttons.HasFlag(Ds4Buttons.TouchpadClick);
        if (pressed && !_previousTouchpadPressed)
        {
            _touchpadPressCount++;
            TouchpadPressCountText.Text = _touchpadPressCount.ToString();
        }
        _previousTouchpadPressed = pressed;

        Ds4TouchPoint? point = state.Touch1 is { Touching: true } ? state.Touch1 : state.Touch2 is { Touching: true } ? state.Touch2 : null;
        if (point is Ds4TouchPoint touch)
        {
            TouchpadStateText.Text = pressed ? $"Pressed · touch {touch.X},{touch.Y}" : $"Touch {touch.X},{touch.Y}";
            TouchPointDot.Visibility = Visibility.Visible;
            double width = Math.Max(40, TouchpadCanvas.ActualWidth);
            double height = Math.Max(40, TouchpadCanvas.ActualHeight);
            double x = Math.Clamp(touch.X / 1919.0 * width - 6, 0, Math.Max(0, width - 12));
            double y = Math.Clamp(touch.Y / 941.0 * height - 6, 0, Math.Max(0, height - 12));
            Canvas.SetLeft(TouchPointDot, x);
            Canvas.SetTop(TouchPointDot, y);
        }
        else
        {
            TouchpadStateText.Text = pressed ? "Touchpad pressed" : "Touchpad idle";
            TouchPointDot.Visibility = Visibility.Collapsed;
        }
    }

    private void ResetTouchCountButton_Click(object sender, RoutedEventArgs e)
    {
        _touchpadPressCount = 0;
        TouchpadPressCountText.Text = "0";
    }

    private void VibrationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _selectedPattern = VibrationPattern.Custom;
        RefreshVibrationValues();
    }

    private void RefreshVibrationValues()
    {
        if (LargeMotorValueText is null || SmallMotorValueText is null || VibrationDurationValueText is null)
        {
            return;
        }

        LargeMotorValueText.Text = ((int)LargeMotorSlider.Value).ToString();
        SmallMotorValueText.Text = ((int)SmallMotorSlider.Value).ToString();
        VibrationDurationValueText.Text = $"{(int)VibrationDurationSlider.Value} ms";
    }

    private void HeartbeatPreset_Click(object sender, RoutedEventArgs e) => SetVibrationPreset(VibrationPattern.Heartbeat, 190, 45, 900);
    private void ExplosionPreset_Click(object sender, RoutedEventArgs e) => SetVibrationPreset(VibrationPattern.Explosion, 245, 135, 1200);
    private void ClickPreset_Click(object sender, RoutedEventArgs e) => SetVibrationPreset(VibrationPattern.Click, 45, 230, 120);
    private void BalancedPreset_Click(object sender, RoutedEventArgs e) => SetVibrationPreset(VibrationPattern.Balanced, 150, 150, 700);

    private void SetVibrationPreset(VibrationPattern pattern, byte large, byte small, int durationMs)
    {
        LargeMotorSlider.Value = large;
        SmallMotorSlider.Value = small;
        VibrationDurationSlider.Value = durationMs;
        _selectedPattern = pattern;
        RefreshVibrationValues();
        VibrationStatusText.Text = $"Preset: {pattern}";
    }

    private void StartVibrationButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_sessionRunning)
        {
            return;
        }

        VibrationRequested?.Invoke(
            this,
            new VibrationRequestEventArgs(
                (byte)Math.Round(SmallMotorSlider.Value),
                (byte)Math.Round(LargeMotorSlider.Value),
                (int)Math.Round(VibrationDurationSlider.Value),
                _selectedPattern));
        VibrationStatusText.Text = $"Running {_selectedPattern.ToString().ToLowerInvariant()} test…";
    }

    private void StopVibrationButton_Click(object sender, RoutedEventArgs e)
    {
        StopVibrationRequested?.Invoke(this, EventArgs.Empty);
        VibrationStatusText.Text = "Stopped · output reset";
    }

    private void ExportReportButton_Click(object sender, RoutedEventArgs e)
    {
        SaveFileDialog dialog = new()
        {
            Title = "Export PadScope diagnostics report",
            Filter = "JSON report (*.json)|*.json",
            FileName = $"padscope-diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.json"
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true)
        {
            return;
        }

        StickDiagnosticsSnapshot leftRange = _rangeLeft.Snapshot();
        StickDiagnosticsSnapshot rightRange = _rangeRight.Snapshot();
        object report = new
        {
            generatedAt = DateTimeOffset.Now,
            device = _device is null ? null : new
            {
                _device.DisplayName,
                _device.VendorId,
                _device.ProductId,
                _device.ConnectionType,
                _device.DevicePath
            },
            stickDiagnostics = new
            {
                left = new { center = _lastStaticLeft, range = leftRange },
                right = new { center = _lastStaticRight, range = rightRange }
            },
            polling = new
            {
                sampleCount = _pollingRates.Count,
                peakHz = _pollingRates.Count == 0 ? 0 : _pollingRates.Max(),
                averageHz = _pollingRates.Count == 0 ? 0 : _pollingRates.Average(),
                historyHz = _pollingRates.ToArray()
            },
            touchpadPressCount = _touchpadPressCount
        };

        string json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(dialog.FileName, json);
    }

    private void UpdateConnectionPill()
    {
        if (!_sessionRunning)
        {
            ConnectionPillText.Text = _device is null ? "No controller selected" : "Controller selected · start live";
            ConnectionPillText.SetResourceReference(TextBlock.ForegroundProperty, "B_TextDim");
            return;
        }

        ConnectionPillText.Text = _device is null ? "Live HID connected" : $"Live · {_device.ConnectionType} · {_device.VendorId}:{_device.ProductId}";
        ConnectionPillText.SetResourceReference(TextBlock.ForegroundProperty, "B_Success");
    }

    private Brush ResolveBrush(string key) => TryFindResource(key) as Brush ?? Brushes.Gray;

    private static StickDiagnosticsSnapshot EmptyStickSnapshot() =>
        new(0, 0, 0, 0, 0, 0, 0, 0, 0, new double[StickDiagnosticsAnalyzer.DefaultRangeBinCount]);
}

public enum VibrationPattern
{
    Custom,
    Balanced,
    Heartbeat,
    Explosion,
    Click
}

public sealed class VibrationRequestEventArgs(
    byte smallMotor,
    byte largeMotor,
    int durationMs,
    VibrationPattern pattern) : EventArgs
{
    public byte SmallMotor { get; } = smallMotor;
    public byte LargeMotor { get; } = largeMotor;
    public int DurationMs { get; } = durationMs;
    public VibrationPattern Pattern { get; } = pattern;
}
