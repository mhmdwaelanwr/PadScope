using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using PadScope.Core.Diagnostics;
using PadScope.Core.Input;
using PadScope.Core.Models;
using PadScope.Hid;

namespace PadScope.Desktop;

public partial class MainWindow
{
    private Ds4ControllerSession? _liveSession;
    private volatile Ds4InputState? _latestState;
    private DispatcherTimer? _liveTimer;
    private Ds4Buttons _prevButtons;

    private static readonly Brush PressedBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94));

    internal void RefreshLiveDeviceList()
    {
        var devices = _reports.Select(report => report.Device).Distinct().ToList();
        DeviceComboBox.ItemsSource = devices;
        DeviceComboBox.SelectedIndex = devices.Count > 0 ? 0 : -1;
        StartInputButton.IsEnabled = devices.Count > 0;

        if (devices.Count == 0)
        {
            StopInputButton.IsEnabled = false;
            EnableOutputControls(false);
            LiveStatusText.Text = "Scan first to detect a controller.";
        }
    }

    internal void ClearLiveDeviceList()
    {
        DeviceComboBox.ItemsSource = null;
        StartInputButton.IsEnabled = false;
        StopInputButton.IsEnabled = false;
        EnableOutputControls(false);
        LiveStatusText.Text = "Pick a detected device, then press Start.";
    }

    private void StartInputButton_Click(object sender, RoutedEventArgs e)
    {
        if (DeviceComboBox.SelectedItem is not ControllerDevice device)
        {
            MessageBox.Show(
                this,
                "Scan first, then select a device from the list.",
                "PadScope Live Input",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            return;
        }

        _liveSession?.Dispose();
        _liveSession = new Ds4ControllerSession(new HidSharpHidInputReader(), device);

        if (!_liveSession.TryStart(out string? error))
        {
            MessageBox.Show(
                this,
                error ?? "Could not start live input.",
                "PadScope Live Input",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            _liveSession.Dispose();
            _liveSession = null;
            return;
        }

        _liveSession.Error += message => Dispatcher.BeginInvoke(() => LiveStatusText.Text = message);
        _liveSession.StateUpdated += state => _latestState = state;
        _liveSession.TimingUpdated += OnTimingUpdated;

        _prevButtons = default;
        StartInputButton.IsEnabled = false;
        StopInputButton.IsEnabled = true;
        LiveStatusText.Text = $"Live: {_liveSession.DeviceDescription}";
        TimingText.Text = "Timing: waiting for reports...";
        EnableOutputControls(true);

        _liveTimer?.Stop();
        _liveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _liveTimer.Tick += (_, _) => RenderLatestState();
        _liveTimer.Start();
    }

    private void StopInputButton_Click(object sender, RoutedEventArgs e)
    {
        StopLiveInput();
    }

    private void PulseRumbleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_liveSession is null)
        {
            return;
        }

        if (!ConfirmControlledAction(
                "Send a rumble output report. The controller may ignore an unsupported DS4 report.",
                DeviceComboBox.SelectedItem as ControllerDevice))
        {
            return;
        }

        byte small = (byte)RumbleSmallSlider.Value;
        byte large = (byte)RumbleLargeSlider.Value;

        if (!_liveSession.TrySendRumble(small, large, out string? error))
        {
            MessageBox.Show(
                this,
                error ?? "Rumble write failed.",
                "PadScope",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            return;
        }

        LiveStatusText.Text = $"Rumble sent (small {small}, large {large}). Did the controller vibrate?";
    }

    private void SetLightbarButton_Click(object sender, RoutedEventArgs e)
    {
        if (_liveSession is null)
        {
            return;
        }

        if (!ConfirmControlledAction(
                "Send a lightbar output report. The controller may ignore an unsupported DS4 report.",
                DeviceComboBox.SelectedItem as ControllerDevice))
        {
            return;
        }

        byte red = (byte)LightbarRedSlider.Value;
        byte green = (byte)LightbarGreenSlider.Value;
        byte blue = (byte)LightbarBlueSlider.Value;

        if (!_liveSession.TrySendLightbar(red, green, blue, out string? error))
        {
            MessageBox.Show(
                this,
                error ?? "Lightbar write failed.",
                "PadScope",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            return;
        }

        LiveStatusText.Text = $"Lightbar set to RGB({red}, {green}, {blue}). Did the color change?";
    }

    private void ResetOutputButton_Click(object sender, RoutedEventArgs e)
    {
        if (_liveSession is null)
        {
            MessageBox.Show(this, "No active session.", "PadScope", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (!_liveSession.TryResetOutput(out string? error))
        {
            MessageBox.Show(
                this,
                error ?? "Output reset failed.",
                "PadScope",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            return;
        }

        LiveStatusText.Text = "Output reset to neutral.";
    }

    private void RenderLatestState()
    {
        Ds4InputState? state = _latestState;
        if (state is null)
        {
            return;
        }

        try
        {
            LeftStickText.Text = $"X {state.LeftStickX,3}  Y {state.LeftStickY,3}";
            RightStickText.Text = $"X {state.RightStickX,3}  Y {state.RightStickY,3}";

            PlaceStickDot(LeftStickCanvas, LeftStickDot, state.LeftStickXNorm, state.LeftStickYNorm);
            PlaceStickDot(RightStickCanvas, RightStickDot, state.RightStickXNorm, state.RightStickYNorm);

            L2ValueText.Text = $"L2 {state.LeftTrigger}  ({state.LeftTriggerNorm:P0})";
            R2ValueText.Text = $"R2 {state.RightTrigger}  ({state.RightTriggerNorm:P0})";
            L2Bar.Value = state.LeftTrigger;
            R2Bar.Value = state.RightTrigger;

            if (state.Buttons != _prevButtons)
            {
                UpdateButtonIfNeeded(DpadUpButton, state.Buttons, Ds4Buttons.DpadUp);
                UpdateButtonIfNeeded(DpadDownButton, state.Buttons, Ds4Buttons.DpadDown);
                UpdateButtonIfNeeded(DpadLeftButton, state.Buttons, Ds4Buttons.DpadLeft);
                UpdateButtonIfNeeded(DpadRightButton, state.Buttons, Ds4Buttons.DpadRight);
                UpdateButtonIfNeeded(SquareButton, state.Buttons, Ds4Buttons.Square);
                UpdateButtonIfNeeded(CrossButton, state.Buttons, Ds4Buttons.Cross);
                UpdateButtonIfNeeded(CircleButton, state.Buttons, Ds4Buttons.Circle);
                UpdateButtonIfNeeded(TriangleButton, state.Buttons, Ds4Buttons.Triangle);
                UpdateButtonIfNeeded(L1Button, state.Buttons, Ds4Buttons.L1);
                UpdateButtonIfNeeded(R1Button, state.Buttons, Ds4Buttons.R1);
                UpdateButtonIfNeeded(L2Button, state.Buttons, Ds4Buttons.L2);
                UpdateButtonIfNeeded(R2Button, state.Buttons, Ds4Buttons.R2);
                UpdateButtonIfNeeded(ShareButton, state.Buttons, Ds4Buttons.Share);
                UpdateButtonIfNeeded(OptionsButton, state.Buttons, Ds4Buttons.Options);
                UpdateButtonIfNeeded(L3Button, state.Buttons, Ds4Buttons.L3);
                UpdateButtonIfNeeded(R3Button, state.Buttons, Ds4Buttons.R3);
                UpdateButtonIfNeeded(PsButton, state.Buttons, Ds4Buttons.Ps);
                UpdateButtonIfNeeded(TouchpadButton, state.Buttons, Ds4Buttons.TouchpadClick);
                _prevButtons = state.Buttons;
            }

            GyroText.Text = $"Gyro  X {state.GyroX,6}  Y {state.GyroY,6}  Z {state.GyroZ,6}";
            AccelText.Text = $"Accel X {state.AccelX,6}  Y {state.AccelY,6}  Z {state.AccelZ,6}";

            BatteryText.Text = state.BatteryLevel.HasValue
                ? $"Level {state.BatteryLevel.Value}/10  {(state.Charging ? "Charging" : "Not charging")}"
                : "Battery level not reported (common over Bluetooth)";

            string touch = state.Touch1?.Touching == true
                ? $"\nTouch 1  X {state.Touch1.Value.X,4}  Y {state.Touch1.Value.Y,4}"
                : string.Empty;

            if (state.Touch2?.Touching == true)
            {
                touch += $"\nTouch 2  X {state.Touch2.Value.X,4}  Y {state.Touch2.Value.Y,4}";
            }

            if (touch.Length > 0)
            {
                BatteryText.Text += touch;
            }

            RawHexText.Text = FormatHex(state.Raw);
        }
        catch (Exception)
        {
            // UI element may be null after theme swap — ignore
        }
    }

    private void OnTimingUpdated(ReportTimingSnapshot timing)
    {
        Dispatcher.BeginInvoke(() =>
        {
            TimingText.Text = timing.ReportCount < 2
                ? "Timing: collecting samples..."
                : $"Timing: {timing.ReportRateHz:F0} Hz  |  avg {timing.AverageIntervalMs:F2} ms  |  " +
                  $"p95 {timing.P95IntervalMs:F2} ms  |  jitter {timing.JitterMs:F2} ms  |  " +
                  $"spikes {timing.SpikeCount}";
        });
    }

    private void UpdateButtonIfNeeded(Button button, Ds4Buttons current, Ds4Buttons flag)
    {
        bool pressed = current.HasFlag(flag);
        bool wasPressed = _prevButtons.HasFlag(flag);
        if (pressed != wasPressed)
        {
            SetButtonState(button, pressed);
        }
    }

    private void PlaceStickDot(Canvas canvas, FrameworkElement dot, float xNorm, float yNorm)
    {
        const double center = 90.0;
        const double radius = 81.0;

        double x = center + xNorm * radius - 5;
        double y = center - yNorm * radius - 5;

        Canvas.SetLeft(dot, Math.Clamp(x, 0, 170));
        Canvas.SetTop(dot, Math.Clamp(y, 0, 170));
    }

    private void SetButtonState(Button button, bool pressed)
    {
        try
        {
            if (pressed)
            {
                button.Background = PressedBrush;
                button.BorderBrush = PressedBrush;
                button.Foreground = Brushes.White;
            }
            else
            {
                button.ClearValue(Control.BackgroundProperty);
                button.ClearValue(Control.BorderBrushProperty);
                button.ClearValue(Control.ForegroundProperty);
            }
        }
        catch (Exception)
        {
            // ignore during theme transition
        }
    }

    private void EnableOutputControls(bool enabled)
    {
        PulseRumbleButton.IsEnabled = enabled;
        SetLightbarButton.IsEnabled = enabled;
        ResetOutputButton.IsEnabled = enabled;
    }

    private void StopLiveInput()
    {
        _liveTimer?.Stop();
        _liveSession?.Stop();
        _liveSession?.Dispose();
        _liveSession = null;
        _latestState = null;
        _prevButtons = default;

        StartInputButton.IsEnabled = DeviceComboBox.Items.Count > 0;
        StopInputButton.IsEnabled = false;
        EnableOutputControls(false);
        LiveStatusText.Text = "Live input stopped.";
        TimingText.Text = "Timing: not running";
    }

    private static string FormatHex(byte[] data)
    {
        StringBuilder builder = new(data.Length * 3);
        for (int i = 0; i < data.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(' ');
            }

            builder.Append(data[i].ToString("X2"));
        }

        return builder.ToString();
    }
}
