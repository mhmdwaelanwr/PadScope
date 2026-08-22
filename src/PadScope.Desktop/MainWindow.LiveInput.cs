using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
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
    private HidCaptureRecorder? _captureRecorder;
    private ReportTimingSnapshot? _latestTiming;
    private bool _isCapturing;
    private int _captureLimitNotified;

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

        StopVirtualPassthrough();
        StopMouseEmulation();
        StartLiveSession(new HidSharpHidInputReader(), device, allowOutput: true);
    }

    private void StartLiveSession(IHidInputReader reader, ControllerDevice device, bool allowOutput)
    {
        _liveSession?.Dispose();
        _liveSession = new Ds4ControllerSession(reader, device);
        _liveSession.Error += message => Dispatcher.BeginInvoke(() => LiveStatusText.Text = message);
        _liveSession.StateUpdated += state => _latestState = state;
        _liveSession.TimingUpdated += OnTimingUpdated;
        _liveSession.ReportObserved += OnReportObserved;

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

        _prevButtons = default;
        _latestTiming = null;
        StartInputButton.IsEnabled = false;
        StopInputButton.IsEnabled = true;
        LiveStatusText.Text = $"Live: {_liveSession.DeviceDescription}";
        TimingText.Text = "Timing: waiting for reports...";
        EnableOutputControls(allowOutput);
        StartCaptureButton.IsEnabled = allowOutput && _captureRecorder is null;
        SaveCaptureButton.IsEnabled = _captureRecorder?.Count > 0;

        _liveTimer?.Stop();
        _liveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _liveTimer.Tick += (_, _) => RenderLatestState();
        _liveTimer.Start();
    }

    private void StartCaptureButton_Click(object sender, RoutedEventArgs e)
    {
        if (_liveSession is null || DeviceComboBox.SelectedItem is not ControllerDevice device)
        {
            MessageBox.Show(this, "Start live input on a selected device first.", "PadScope Capture", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _captureRecorder = new HidCaptureRecorder(device);
        _isCapturing = true;
        _captureLimitNotified = 0;
        StartCaptureButton.IsEnabled = false;
        SaveCaptureButton.IsEnabled = true;
        CaptureStatusText.Text = $"Recording up to {HidCaptureRecorder.MaximumFrames:N0} raw reports...";
    }

    private void SaveCaptureButton_Click(object sender, RoutedEventArgs e)
    {
        HidCaptureRecorder? recorder = _captureRecorder;
        _isCapturing = false;
        StartCaptureButton.IsEnabled = false;
        SaveCaptureButton.IsEnabled = false;
        if (recorder is null || recorder.Count == 0)
        {
            CaptureStatusText.Text = "No reports were captured.";
            return;
        }

        SaveFileDialog dialog = new()
        {
            Title = "Save PadScope HID capture",
            Filter = "PadScope HID capture (*.padscope-hid.json)|*.padscope-hid.json|JSON file (*.json)|*.json",
            FileName = $"padscope-hid-{DateTime.Now:yyyyMMdd-HHmmss}.padscope-hid.json"
        };
        if (dialog.ShowDialog(this) != true)
        {
            CaptureStatusText.Text = $"Capture paused with {recorder.Count:N0} reports; not saved.";
            SaveCaptureButton.IsEnabled = true;
            return;
        }

        try
        {
            HidCaptureStore.Save(dialog.FileName, recorder.CreateDocument(_latestTiming));
            _captureRecorder = null;
            StartCaptureButton.IsEnabled = _liveSession is { IsRunning: true } &&
                                           !(_liveSession.DeviceDescription?.StartsWith("Replay:", StringComparison.OrdinalIgnoreCase) ?? false);
            CaptureStatusText.Text = $"Saved {recorder.Count:N0} reports: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            SaveCaptureButton.IsEnabled = true;
            MessageBox.Show(this, ex.Message, "Capture save failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ReplayCaptureButton_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new()
        {
            Title = "Open PadScope HID capture",
            Filter = "PadScope HID capture (*.padscope-hid.json)|*.padscope-hid.json|JSON file (*.json)|*.json"
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            HidCaptureDocument capture = HidCaptureStore.Load(dialog.FileName);
            StopLiveInput();
            StartLiveSession(new RecordedHidInputReader(capture), capture.Device, allowOutput: false);
            CaptureStatusText.Text = $"Replaying {capture.Frames.Count:N0} reports; output is disabled.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Capture replay failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnReportObserved(HidInputReport report)
    {
        HidCaptureRecorder? recorder = _captureRecorder;
        if (!_isCapturing || recorder is null)
        {
            return;
        }

        if (!recorder.TryAdd(report.Data, report.ReportId, report.Timestamp) &&
            recorder.IsFull &&
            Interlocked.Exchange(ref _captureLimitNotified, 1) == 0)
        {
            _isCapturing = false;
            Dispatcher.BeginInvoke(() =>
            {
                SaveCaptureButton.IsEnabled = true;
                CaptureStatusText.Text = $"Capture limit reached ({recorder.Count:N0} reports). Save the recording.";
            });
        }
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
            _latestTiming = timing;
            TimingText.Text = timing.ReportCount < 2
                ? "Timing: collecting samples..."
                : $"Timing: {timing.ReportRateHz:F0} Hz  |  avg {timing.AverageIntervalMs:F2} ms  |  " +
                  $"p95 {timing.P95IntervalMs:F2} ms  |  jitter {timing.JitterMs:F2} ms  |  " +
                  $"spikes {timing.SpikeCount}";

            if (DeviceComboBox.SelectedItem is ControllerDevice device)
            {
                int index = _reports.ToList().FindIndex(report => report.Device == device);
                if (index >= 0)
                {
                    _reports[index] = _reports[index] with { ReportTiming = timing };
                }
            }
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
        _isCapturing = false;
        _latestTiming = null;
        _latestState = null;
        _prevButtons = default;

        StartInputButton.IsEnabled = DeviceComboBox.Items.Count > 0;
        StopInputButton.IsEnabled = false;
        EnableOutputControls(false);
        StartCaptureButton.IsEnabled = false;
        SaveCaptureButton.IsEnabled = _captureRecorder?.Count > 0;
        if (_captureRecorder?.Count > 0)
        {
            CaptureStatusText.Text = $"Capture paused with {_captureRecorder.Count:N0} reports. Save it before starting another capture.";
        }
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
