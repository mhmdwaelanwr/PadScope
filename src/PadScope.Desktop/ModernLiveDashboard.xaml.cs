using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using PadScope.Core.Diagnostics;
using PadScope.Core.Input;
using PadScope.Core.Models;

namespace PadScope.Desktop;

public partial class ModernLiveDashboard : UserControl
{
    private static readonly Brush TriangleAccent = FrozenBrush("#34D399");
    private static readonly Brush CircleAccent = FrozenBrush("#FB7185");
    private static readonly Brush CrossAccent = FrozenBrush("#60A5FA");
    private static readonly Brush SquareAccent = FrozenBrush("#F472B6");

    public event EventHandler? StartRequested;
    public event EventHandler? StopRequested;
    public event EventHandler<RumblePresetRequestedEventArgs>? RumblePresetRequested;
    public event EventHandler? ResetRumbleRequested;

    public ControllerDevice? SelectedDevice => DevicePicker.SelectedItem as ControllerDevice;

    public ModernLiveDashboard()
    {
        InitializeComponent();
        StopButton.IsEnabled = false;
        SetOutputEnabled(false);
    }

    public void SetDevices(IReadOnlyList<ControllerDevice> devices)
    {
        ControllerDevice? previous = SelectedDevice;
        DevicePicker.ItemsSource = devices;

        if (devices.Count == 0)
        {
            DevicePicker.SelectedIndex = -1;
            DeviceIdentityText.Text = "Scan and select a controller";
            StartButton.IsEnabled = false;
            return;
        }

        int index = 0;
        if (previous is not null)
        {
            int matched = devices.ToList().FindIndex(device => SameDevice(device, previous));
            if (matched >= 0)
            {
                index = matched;
            }
        }

        DevicePicker.SelectedIndex = index;
        StartButton.IsEnabled = true;
        UpdateIdentity();
    }

    public void SetSessionState(bool running, string? status = null)
    {
        StartButton.IsEnabled = !running && DevicePicker.Items.Count > 0;
        StopButton.IsEnabled = running;
        SessionStateText.Text = status ?? (running ? "Live HID stream active" : "Waiting for live input");
    }

    public void SetOutputEnabled(bool enabled)
    {
        HeavyRumbleButton.IsEnabled = enabled;
        LightRumbleButton.IsEnabled = enabled;
        BalancedRumbleButton.IsEnabled = enabled;
        ResetRumbleButton.IsEnabled = enabled;
    }

    public void UpdateTelemetry(Ds4InputState state, ReportTimingSnapshot? timing)
    {
        UpdateIdentity();
        ReportIdText.Text = $"Report 0x{state.ReportId:X2}";
        BatteryPillText.Text = state.BatteryLevel.HasValue
            ? $"Battery {state.BatteryLevel.Value * 10}%{(state.Charging ? " · charging" : string.Empty)}"
            : "Battery --";

        UpdateStick(LeftStickCanvas, LeftStickDot, LeftStickText, LeftStickOffsetText, state.LeftStickXNorm, state.LeftStickYNorm);
        UpdateStick(RightStickCanvas, RightStickDot, RightStickText, RightStickOffsetText, state.RightStickXNorm, state.RightStickYNorm);

        MoveControllerStick(ControllerLeftStickDot, state.LeftStickXNorm, state.LeftStickYNorm, 205, 238);
        MoveControllerStick(ControllerRightStickDot, state.RightStickXNorm, state.RightStickYNorm, 327, 238);

        LeftTriggerBar.Value = state.LeftTrigger;
        RightTriggerBar.Value = state.RightTrigger;
        LeftTriggerText.Text = $"L2  {state.LeftTriggerNorm:P0}";
        RightTriggerText.Text = $"R2  {state.RightTriggerNorm:P0}";

        SetShapeState(TriangleShape, state.Buttons.HasFlag(Ds4Buttons.Triangle), TriangleAccent);
        SetShapeState(CircleShape, state.Buttons.HasFlag(Ds4Buttons.Circle), CircleAccent);
        SetShapeState(CrossShape, state.Buttons.HasFlag(Ds4Buttons.Cross), CrossAccent);
        SetShapeState(SquareShape, state.Buttons.HasFlag(Ds4Buttons.Square), SquareAccent);

        Brush primary = ResolveBrush("B_Primary");
        SetShapeState(DpadUpShape, state.Buttons.HasFlag(Ds4Buttons.DpadUp), primary);
        SetShapeState(DpadDownShape, state.Buttons.HasFlag(Ds4Buttons.DpadDown), primary);
        SetShapeState(DpadLeftShape, state.Buttons.HasFlag(Ds4Buttons.DpadLeft), primary);
        SetShapeState(DpadRightShape, state.Buttons.HasFlag(Ds4Buttons.DpadRight), primary);
        SetShapeState(PsShape, state.Buttons.HasFlag(Ds4Buttons.Ps), ResolveBrush("B_PrimaryDim"));

        Ds4Buttons[] pressed = Enum.GetValues<Ds4Buttons>()
            .Where(button => button != Ds4Buttons.None && state.Buttons.HasFlag(button))
            .ToArray();
        PressedButtonsText.Text = pressed.Length == 0
            ? "No buttons pressed"
            : string.Join("  ·  ", pressed.Take(6));

        string touch = state.Touch1?.Touching == true
            ? $"Touch {state.Touch1.Value.X},{state.Touch1.Value.Y}"
            : "Touch idle";
        MotionSummaryText.Text = $"Gyro {state.GyroX},{state.GyroY},{state.GyroZ}  ·  {touch}";

        RawPreviewText.Text = state.Raw.Length == 0
            ? "RAW  --"
            : "RAW  " + string.Join(" ", state.Raw.Take(18).Select(value => value.ToString("X2"))) +
              (state.Raw.Length > 18 ? " …" : string.Empty);

        if (timing is not null && timing.ReportCount >= 2)
        {
            PollingRateText.Text = $"{timing.ReportRateHz:F0} Hz";
            AverageIntervalText.Text = $"{timing.AverageIntervalMs:F2} ms";
            JitterText.Text = $"Jitter {timing.JitterMs:F2} ms";
            SpikeText.Text = $"Spikes {timing.SpikeCount}";
        }
        else
        {
            PollingRateText.Text = "-- Hz";
            AverageIntervalText.Text = "-- ms";
            JitterText.Text = "Jitter -- ms";
            SpikeText.Text = "Spikes --";
        }
    }

    private void DevicePicker_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateIdentity();
    private void StartButton_Click(object sender, RoutedEventArgs e) => StartRequested?.Invoke(this, EventArgs.Empty);
    private void StopButton_Click(object sender, RoutedEventArgs e) => StopRequested?.Invoke(this, EventArgs.Empty);

    private void HeavyRumble_Click(object sender, RoutedEventArgs e) =>
        RumblePresetRequested?.Invoke(this, new RumblePresetRequestedEventArgs(40, 235));

    private void LightRumble_Click(object sender, RoutedEventArgs e) =>
        RumblePresetRequested?.Invoke(this, new RumblePresetRequestedEventArgs(225, 35));

    private void BalancedRumble_Click(object sender, RoutedEventArgs e) =>
        RumblePresetRequested?.Invoke(this, new RumblePresetRequestedEventArgs(150, 150));

    private void ResetRumble_Click(object sender, RoutedEventArgs e) => ResetRumbleRequested?.Invoke(this, EventArgs.Empty);

    private void UpdateIdentity()
    {
        if (SelectedDevice is not ControllerDevice device)
        {
            DeviceIdentityText.Text = "Scan and select a controller";
            return;
        }

        string ids = $"{device.VendorId ?? "?"}:{device.ProductId ?? "?"}";
        DeviceIdentityText.Text = $"{device.ConnectionType}  ·  {ids}";
    }

    private static void UpdateStick(
        Canvas canvas,
        FrameworkElement dot,
        TextBlock valueText,
        TextBlock offsetText,
        float x,
        float y)
    {
        const double center = 45;
        const double radius = 36;
        Canvas.SetLeft(dot, Math.Clamp(center + x * radius - 4.5, 4, 77));
        Canvas.SetTop(dot, Math.Clamp(center - y * radius - 4.5, 4, 77));

        double offset = Math.Min(1.5, Math.Sqrt(x * x + y * y));
        valueText.Text = $"{x:+0.00;-0.00;+0.00}, {y:+0.00;-0.00;+0.00}";
        offsetText.Text = $"offset {offset:P1}";
    }

    private static void MoveControllerStick(FrameworkElement dot, float x, float y, double baseLeft, double baseTop)
    {
        Canvas.SetLeft(dot, baseLeft + (x * 13));
        Canvas.SetTop(dot, baseTop - (y * 13));
    }

    private void SetShapeState(Shape shape, bool pressed, Brush accent)
    {
        shape.Fill = pressed ? accent : ResolveBrush("B_Background");
        shape.Opacity = pressed ? 1 : 0.92;
    }

    private Brush ResolveBrush(string key) => TryFindResource(key) as Brush ?? Brushes.Gray;

    private static bool SameDevice(ControllerDevice a, ControllerDevice b)
    {
        if (!string.IsNullOrWhiteSpace(a.DevicePath) || !string.IsNullOrWhiteSpace(b.DevicePath))
        {
            return string.Equals(a.DevicePath, b.DevicePath, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(a.VendorId, b.VendorId, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(a.ProductId, b.ProductId, StringComparison.OrdinalIgnoreCase);
    }

    private static Brush FrozenBrush(string value)
    {
        SolidColorBrush brush = new((Color)ColorConverter.ConvertFromString(value));
        brush.Freeze();
        return brush;
    }
}

public sealed class RumblePresetRequestedEventArgs(byte smallMotor, byte largeMotor) : EventArgs
{
    public byte SmallMotor { get; } = smallMotor;
    public byte LargeMotor { get; } = largeMotor;
}
