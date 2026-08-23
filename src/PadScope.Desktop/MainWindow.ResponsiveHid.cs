using System.Windows;
using System.Windows.Threading;
using PadScope.Core.Models;
using PadScope.Hid;

namespace PadScope.Desktop;

public partial class MainWindow
{
    private bool _responsiveHidInstalled;
    private bool _modernResponsiveHooksInstalled;
    private bool _isOpeningLiveSession;
    private bool _outputOperationBusy;
    private bool _outputUnavailableForSession;
    private string? _outputFailureReason;
    private DispatcherTimer? _responsiveHookTimer;
    private DispatcherTimer? _extendedControllerVisualTimer;

    internal void InstallResponsiveHidBehavior()
    {
        if (_responsiveHidInstalled)
        {
            TryInstallModernResponsiveHooks();
            return;
        }

        _responsiveHidInstalled = true;

        // Replace the synchronous WPF click handlers. HID open/output can block in
        // Windows drivers; those calls must never execute on the dispatcher thread.
        StartInputButton.Click -= StartInputButton_Click;
        StartInputButton.Click += ResponsiveStartInputButton_Click;
        StopInputButton.Click -= StopInputButton_Click;
        StopInputButton.Click += ResponsiveStopInputButton_Click;
        PulseRumbleButton.Click -= PulseRumbleButton_Click;
        PulseRumbleButton.Click += ResponsivePulseRumbleButton_Click;
        SetLightbarButton.Click -= SetLightbarButton_Click;
        SetLightbarButton.Click += ResponsiveSetLightbarButton_Click;
        ResetOutputButton.Click -= ResetOutputButton_Click;
        ResetOutputButton.Click += ResponsiveResetOutputButton_Click;

        _responsiveHookTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _responsiveHookTimer.Tick += (_, _) => TryInstallModernResponsiveHooks();
        _responsiveHookTimer.Start();
        TryInstallModernResponsiveHooks();

        Closed += (_, _) =>
        {
            _responsiveHookTimer?.Stop();
            _extendedControllerVisualTimer?.Stop();
        };
    }

    private void TryInstallModernResponsiveHooks()
    {
        if (_modernResponsiveHooksInstalled || _modernLiveDashboard is null || _controllerDiagnosticsLab is null)
        {
            return;
        }

        _modernResponsiveHooksInstalled = true;
        _responsiveHookTimer?.Stop();

        _modernLiveDashboard.StartRequested -= ModernDashboard_StartRequested;
        _modernLiveDashboard.StartRequested += ResponsiveDashboard_StartRequested;
        _modernLiveDashboard.StopRequested -= ModernDashboard_StopRequested;
        _modernLiveDashboard.StopRequested += ResponsiveDashboard_StopRequested;
        _modernLiveDashboard.RumblePresetRequested -= ModernDashboard_RumblePresetRequested;
        _modernLiveDashboard.RumblePresetRequested += ResponsiveDashboard_RumblePresetRequested;
        _modernLiveDashboard.ResetRumbleRequested -= ModernDashboard_ResetRumbleRequested;
        _modernLiveDashboard.ResetRumbleRequested += ResponsiveDashboard_ResetRumbleRequested;

        _controllerDiagnosticsLab.VibrationRequested -= DiagnosticsLab_VibrationRequested;
        _controllerDiagnosticsLab.VibrationRequested += ResponsiveDiagnosticsLab_VibrationRequested;
        _controllerDiagnosticsLab.StopVibrationRequested -= DiagnosticsLab_StopVibrationRequested;
        _controllerDiagnosticsLab.StopVibrationRequested += ResponsiveDiagnosticsLab_StopVibrationRequested;

        _modernLiveDashboard.ApplyControllerVisualPolish();
        ApplyOutputAvailabilityToUi();

        _extendedControllerVisualTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _extendedControllerVisualTimer.Tick += (_, _) =>
        {
            if (_latestState is { } state)
            {
                _modernLiveDashboard?.UpdateExtendedControllerVisuals(state);
            }
        };
        _extendedControllerVisualTimer.Start();
    }

    private async void ResponsiveStartInputButton_Click(object sender, RoutedEventArgs e)
    {
        if (DeviceComboBox.SelectedItem is not ControllerDevice device)
        {
            MessageBox.Show(this, "Scan first, then select a device from the list.", "PadScope Live Input",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        StopVirtualPassthrough();
        StopMouseEmulation();
        await StartLiveSessionResponsiveAsync(new HidSharpHidInputReader(), device, allowOutput: true);
    }

    private async void ResponsiveDashboard_StartRequested(object? sender, EventArgs e)
    {
        if (_modernLiveDashboard?.SelectedDevice is not ControllerDevice device)
        {
            MessageBox.Show(this, "Scan first, then select a controller.", "PadScope Live Input",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        DeviceComboBox.SelectedItem = device;
        StopVirtualPassthrough();
        StopMouseEmulation();
        await StartLiveSessionResponsiveAsync(new HidSharpHidInputReader(), device, allowOutput: true);
    }

    private async Task StartLiveSessionResponsiveAsync(IHidInputReader reader, ControllerDevice device, bool allowOutput)
    {
        if (_isOpeningLiveSession)
        {
            return;
        }

        _isOpeningLiveSession = true;
        _outputUnavailableForSession = false;
        _outputFailureReason = null;
        _latestState = null;
        _latestTiming = null;
        _liveTimer?.Stop();
        StartInputButton.IsEnabled = false;
        StopInputButton.IsEnabled = false;
        EnableOutputControls(false);
        _modernLiveDashboard?.SetSessionBusy(true, "Opening HID interface…");
        _controllerDiagnosticsLab?.SetOutputAvailability(false, "Waiting for live HID session");
        LiveStatusText.Text = "Opening controller HID interface…";

        Ds4ControllerSession? previous = _liveSession;
        _liveSession = null;
        if (previous is not null)
        {
            await Task.Run(previous.Dispose);
        }

        Ds4ControllerSession session = new(reader, device);
        session.Error += message => Dispatcher.BeginInvoke(() => LiveStatusText.Text = message);
        session.StateUpdated += state => _latestState = state;
        session.TimingUpdated += OnTimingUpdated;
        session.ReportObserved += OnReportObserved;

        (bool ok, string? error) = await Task.Run(() =>
        {
            bool started = session.TryStart(out string? startError);
            return (started, startError);
        });

        _isOpeningLiveSession = false;
        _modernLiveDashboard?.SetSessionBusy(false, null);

        if (!ok)
        {
            await Task.Run(session.Dispose);
            StartInputButton.IsEnabled = DeviceComboBox.Items.Count > 0;
            LiveStatusText.Text = error ?? "Could not start live input.";
            MessageBox.Show(this, error ?? "Could not start live input.", "PadScope Live Input",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _liveSession = session;
        _prevButtons = default;
        StartInputButton.IsEnabled = false;
        StopInputButton.IsEnabled = true;
        LiveStatusText.Text = $"Live: {session.DeviceDescription}";
        TimingText.Text = "Timing: waiting for reports...";
        EnableOutputControls(allowOutput);
        StartCaptureButton.IsEnabled = allowOutput && _captureRecorder is null;
        SaveCaptureButton.IsEnabled = _captureRecorder?.Count > 0;
        ApplyOutputAvailabilityToUi();

        _liveTimer?.Stop();
        _liveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _liveTimer.Tick += (_, _) => RenderLatestState();
        _liveTimer.Start();
        RefreshModernDashboard(forceDeviceRefresh: false);
    }

    private async void ResponsiveStopInputButton_Click(object sender, RoutedEventArgs e) =>
        await StopLiveSessionResponsiveAsync();

    private async void ResponsiveDashboard_StopRequested(object? sender, EventArgs e) =>
        await StopLiveSessionResponsiveAsync();

    private async Task StopLiveSessionResponsiveAsync()
    {
        _liveTimer?.Stop();
        Ds4ControllerSession? session = _liveSession;
        _liveSession = null;
        _isCapturing = false;
        _latestTiming = null;
        _latestState = null;
        _prevButtons = default;
        _outputUnavailableForSession = false;
        _outputFailureReason = null;

        StartInputButton.IsEnabled = false;
        StopInputButton.IsEnabled = false;
        EnableOutputControls(false);
        _modernLiveDashboard?.SetSessionBusy(true, "Closing HID session…");

        if (session is not null)
        {
            await Task.Run(session.Dispose);
        }

        StartInputButton.IsEnabled = DeviceComboBox.Items.Count > 0;
        StartCaptureButton.IsEnabled = false;
        SaveCaptureButton.IsEnabled = _captureRecorder?.Count > 0;
        LiveStatusText.Text = "Live input stopped.";
        TimingText.Text = "Timing: not running";
        _modernLiveDashboard?.SetSessionBusy(false, null);
        RefreshModernDashboard(forceDeviceRefresh: false);
    }

    private async void ResponsivePulseRumbleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_liveSession is not { IsRunning: true } session || !CanAttemptOutput())
        {
            return;
        }

        if (!ConfirmControlledAction("Send a rumble output report. Unsupported clones will be switched to input-only mode for this session.",
                DeviceComboBox.SelectedItem as ControllerDevice))
        {
            return;
        }

        byte small = (byte)RumbleSmallSlider.Value;
        byte large = (byte)RumbleLargeSlider.Value;
        await RunOutputAttemptAsync(
            () => session.TrySendRumble(small, large, out string? error) ? (true, (string?)null) : (false, error),
            $"Rumble sent (small {small}, large {large}).");
    }

    private async void ResponsiveSetLightbarButton_Click(object sender, RoutedEventArgs e)
    {
        if (_liveSession is not { IsRunning: true } session || !CanAttemptOutput())
        {
            return;
        }

        if (!ConfirmControlledAction("Send a lightbar output report. Unsupported clones will be switched to input-only mode for this session.",
                DeviceComboBox.SelectedItem as ControllerDevice))
        {
            return;
        }

        byte red = (byte)LightbarRedSlider.Value;
        byte green = (byte)LightbarGreenSlider.Value;
        byte blue = (byte)LightbarBlueSlider.Value;
        await RunOutputAttemptAsync(
            () => session.TrySendLightbar(red, green, blue, out string? error) ? (true, (string?)null) : (false, error),
            $"Lightbar sent: RGB({red}, {green}, {blue}).");
    }

    private async void ResponsiveResetOutputButton_Click(object sender, RoutedEventArgs e)
    {
        if (_liveSession is not { IsRunning: true } session || !CanAttemptOutput())
        {
            return;
        }

        await RunOutputAttemptAsync(
            () => session.TryResetOutput(out string? error) ? (true, (string?)null) : (false, error),
            "Output reset to neutral.");
    }

    private async void ResponsiveDashboard_RumblePresetRequested(object? sender, RumblePresetRequestedEventArgs e)
    {
        if (_liveSession is not { IsRunning: true } session || !CanAttemptOutput())
        {
            return;
        }

        if (!ConfirmControlledAction("Run a short vibration pulse. PadScope will reset the motors automatically.",
                DeviceComboBox.SelectedItem as ControllerDevice))
        {
            return;
        }

        bool success = await RunOutputAttemptAsync(
            () => session.TrySendRumble(e.SmallMotor, e.LargeMotor, out string? error) ? (true, (string?)null) : (false, error),
            "Vibration pulse started.",
            showSuccess: false);

        if (success)
        {
            await Task.Delay(280);
            await RunOutputAttemptAsync(
                () => session.TryResetRumble(out string? error) ? (true, (string?)null) : (false, error),
                "Vibration pulse completed.",
                showSuccess: false,
                tripCircuitOnFailure: false);
        }
    }

    private async void ResponsiveDashboard_ResetRumbleRequested(object? sender, EventArgs e)
    {
        if (_liveSession is not { IsRunning: true } session || !CanAttemptOutput())
        {
            return;
        }

        await RunOutputAttemptAsync(
            () => session.TryResetRumble(out string? error) ? (true, (string?)null) : (false, error),
            "Vibration stopped.");
    }

    private async void ResponsiveDiagnosticsLab_VibrationRequested(object? sender, VibrationRequestEventArgs e)
    {
        if (_liveSession is not { IsRunning: true } session || !CanAttemptOutput())
        {
            return;
        }

        if (!ConfirmControlledAction($"Run the {e.Pattern} vibration diagnostic for up to {e.DurationMs} ms. Output resets automatically.",
                DeviceComboBox.SelectedItem as ControllerDevice))
        {
            return;
        }

        _diagnosticsVibrationCts?.Cancel();
        _diagnosticsVibrationCts?.Dispose();
        CancellationTokenSource cts = new();
        _diagnosticsVibrationCts = cts;
        _outputOperationBusy = true;
        ApplyOutputAvailabilityToUi();

        try
        {
            await Task.Run(() => RunDiagnosticsVibrationPatternAsync(session, e, cts.Token), cts.Token);
            LiveStatusText.Text = $"Vibration diagnostic completed ({e.Pattern}).";
            _controllerDiagnosticsLab?.SetOutputStatus("Vibration completed · motors reset");
            await Task.Run(() => session.TryResetRumble(out _));
        }
        catch (OperationCanceledException)
        {
            LiveStatusText.Text = "Vibration diagnostic stopped.";
            await Task.Run(() => session.TryResetRumble(out _));
        }
        catch (Exception ex)
        {
            MarkOutputUnavailable(ex.Message);
        }
        finally
        {
            _outputOperationBusy = false;
            if (ReferenceEquals(_diagnosticsVibrationCts, cts))
            {
                _diagnosticsVibrationCts = null;
            }
            cts.Dispose();
            ApplyOutputAvailabilityToUi();
        }
    }

    private void ResponsiveDiagnosticsLab_StopVibrationRequested(object? sender, EventArgs e)
    {
        _diagnosticsVibrationCts?.Cancel();
        if (_liveSession is { IsRunning: true } session && !_outputUnavailableForSession)
        {
            _ = Task.Run(() => session.TryResetRumble(out _));
        }
    }

    private bool CanAttemptOutput()
    {
        if (_outputOperationBusy)
        {
            LiveStatusText.Text = "An output operation is already running.";
            return false;
        }

        if (_outputUnavailableForSession)
        {
            LiveStatusText.Text = "Input is live, but DS4 output is unavailable for this controller session.";
            _controllerDiagnosticsLab?.SetOutputStatus(_outputFailureReason ?? "DS4 output unavailable");
            return false;
        }

        return true;
    }

    private async Task<bool> RunOutputAttemptAsync(
        Func<(bool Success, string? Error)> operation,
        string successStatus,
        bool showSuccess = true,
        bool tripCircuitOnFailure = true)
    {
        if (_outputOperationBusy)
        {
            return false;
        }

        _outputOperationBusy = true;
        ApplyOutputAvailabilityToUi();
        try
        {
            (bool success, string? error) = await Task.Run(operation);
            if (!success)
            {
                if (tripCircuitOnFailure)
                {
                    MarkOutputUnavailable(error ?? "Controller rejected DS4 output.");
                }
                else
                {
                    LiveStatusText.Text = error ?? "Output write failed.";
                }
                return false;
            }

            if (showSuccess)
            {
                LiveStatusText.Text = successStatus;
            }
            _controllerDiagnosticsLab?.SetOutputStatus(successStatus);
            return true;
        }
        finally
        {
            _outputOperationBusy = false;
            ApplyOutputAvailabilityToUi();
        }
    }

    private void MarkOutputUnavailable(string error)
    {
        _outputUnavailableForSession = true;
        _outputFailureReason = error;
        EnableOutputControls(false);
        string friendly = "Input is working, but this HID interface rejected native DS4 output. Vibration/lightbar are disabled until you reconnect or start a new live session.";
        LiveStatusText.Text = friendly;
        _controllerDiagnosticsLab?.SetOutputAvailability(false, friendly);
        _modernLiveDashboard?.SetOutputEnabled(false);

        MessageBox.Show(this,
            friendly + "\n\nTechnical detail:\n" + error,
            "PadScope · Input-only controller session",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void ApplyOutputAvailabilityToUi()
    {
        bool running = _liveSession is { IsRunning: true };
        bool available = running && !_outputUnavailableForSession && !_outputOperationBusy;
        if (running)
        {
            EnableOutputControls(available);
        }
        _modernLiveDashboard?.SetOutputEnabled(available);
        _controllerDiagnosticsLab?.SetOutputAvailability(
            available,
            _outputUnavailableForSession
                ? "DS4 output unsupported on this HID path; input diagnostics remain available"
                : _outputOperationBusy
                    ? "Output operation in progress…"
                    : running ? "Ready · confirmation required" : "Start live input first");
    }
}
