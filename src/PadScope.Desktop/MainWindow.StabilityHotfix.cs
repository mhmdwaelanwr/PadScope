using System.Windows;
using System.Windows.Threading;
using PadScope.Core.Models;

namespace PadScope.Desktop;

public partial class MainWindow
{
    private bool _stabilityHotfixInstalled;
    private bool _stabilityModernHooksInstalled;
    private DispatcherTimer? _stabilityHookTimer;

    internal void InstallStabilityHotfix()
    {
        if (_stabilityHotfixInstalled)
        {
            TryInstallStabilityModernHooks();
            return;
        }

        _stabilityHotfixInstalled = true;

        // Keep exactly one event-handler layer on the legacy controls. The previous
        // responsive patch added a second visual/runtime layer on top of the existing
        // dashboard polish; this hotfix deliberately reuses only the async HID start/
        // stop path and owns output actions itself.
        StartInputButton.Click -= StartInputButton_Click;
        StartInputButton.Click -= ResponsiveStartInputButton_Click;
        StartInputButton.Click += ResponsiveStartInputButton_Click;

        StopInputButton.Click -= StopInputButton_Click;
        StopInputButton.Click -= ResponsiveStopInputButton_Click;
        StopInputButton.Click += ResponsiveStopInputButton_Click;

        PulseRumbleButton.Click -= PulseRumbleButton_Click;
        PulseRumbleButton.Click -= ResponsivePulseRumbleButton_Click;
        PulseRumbleButton.Click -= StabilityPulseRumbleButton_Click;
        PulseRumbleButton.Click += StabilityPulseRumbleButton_Click;

        SetLightbarButton.Click -= SetLightbarButton_Click;
        SetLightbarButton.Click -= ResponsiveSetLightbarButton_Click;
        SetLightbarButton.Click -= StabilitySetLightbarButton_Click;
        SetLightbarButton.Click += StabilitySetLightbarButton_Click;

        ResetOutputButton.Click -= ResetOutputButton_Click;
        ResetOutputButton.Click -= ResponsiveResetOutputButton_Click;
        ResetOutputButton.Click -= StabilityResetOutputButton_Click;
        ResetOutputButton.Click += StabilityResetOutputButton_Click;

        _stabilityHookTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _stabilityHookTimer.Tick += (_, _) => TryInstallStabilityModernHooks();
        _stabilityHookTimer.Start();
        TryInstallStabilityModernHooks();

        Closed += (_, _) => _stabilityHookTimer?.Stop();
    }

    private void TryInstallStabilityModernHooks()
    {
        if (_modernLiveDashboard is null || _controllerDiagnosticsLab is null)
        {
            return;
        }

        if (!_stabilityModernHooksInstalled)
        {
            _stabilityModernHooksInstalled = true;

            _modernLiveDashboard.StartRequested -= ModernDashboard_StartRequested;
            _modernLiveDashboard.StartRequested -= ResponsiveDashboard_StartRequested;
            _modernLiveDashboard.StartRequested += ResponsiveDashboard_StartRequested;

            _modernLiveDashboard.StopRequested -= ModernDashboard_StopRequested;
            _modernLiveDashboard.StopRequested -= ResponsiveDashboard_StopRequested;
            _modernLiveDashboard.StopRequested += ResponsiveDashboard_StopRequested;

            _modernLiveDashboard.RumblePresetRequested -= ModernDashboard_RumblePresetRequested;
            _modernLiveDashboard.RumblePresetRequested -= ResponsiveDashboard_RumblePresetRequested;
            _modernLiveDashboard.RumblePresetRequested += StabilityDashboard_RumblePresetRequested;

            _modernLiveDashboard.ResetRumbleRequested -= ModernDashboard_ResetRumbleRequested;
            _modernLiveDashboard.ResetRumbleRequested -= ResponsiveDashboard_ResetRumbleRequested;
            _modernLiveDashboard.ResetRumbleRequested += StabilityDashboard_ResetRumbleRequested;

            _controllerDiagnosticsLab.VibrationRequested -= DiagnosticsLab_VibrationRequested;
            _controllerDiagnosticsLab.VibrationRequested -= ResponsiveDiagnosticsLab_VibrationRequested;
            _controllerDiagnosticsLab.VibrationRequested += StabilityDiagnosticsLab_VibrationRequested;

            _controllerDiagnosticsLab.StopVibrationRequested -= DiagnosticsLab_StopVibrationRequested;
            _controllerDiagnosticsLab.StopVibrationRequested -= ResponsiveDiagnosticsLab_StopVibrationRequested;
            _controllerDiagnosticsLab.StopVibrationRequested += StabilityDiagnosticsLab_StopVibrationRequested;

            // Use the original controller polish only. The second VisualPolish layer
            // was the source of duplicated shoulder/system controls.
            _modernLiveDashboard.ApplyStabilityVisualCleanup();
            _stabilityHookTimer?.Stop();
        }

        ApplyOutputAvailabilityToUi();
    }

    private async void StabilityPulseRumbleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_liveSession is not { IsRunning: true } session || !CanAttemptStabilityOutput())
        {
            return;
        }

        if (!ConfirmControlledAction(
                "Send a rumble output report. If this controller does not expose a writable DS4 output path, PadScope will keep input working and disable output for this session.",
                DeviceComboBox.SelectedItem as ControllerDevice))
        {
            return;
        }

        byte small = (byte)RumbleSmallSlider.Value;
        byte large = (byte)RumbleLargeSlider.Value;
        await RunStabilityOutputAttemptAsync(
            () => session.TrySendRumble(small, large, out string? error) ? (true, (string?)null) : (false, error),
            $"Rumble sent (small {small}, large {large}).");
    }

    private async void StabilitySetLightbarButton_Click(object sender, RoutedEventArgs e)
    {
        if (_liveSession is not { IsRunning: true } session || !CanAttemptStabilityOutput())
        {
            return;
        }

        if (!ConfirmControlledAction(
                "Send a lightbar output report. Unsupported output paths are disabled for the rest of this live session instead of being retried repeatedly.",
                DeviceComboBox.SelectedItem as ControllerDevice))
        {
            return;
        }

        byte red = (byte)LightbarRedSlider.Value;
        byte green = (byte)LightbarGreenSlider.Value;
        byte blue = (byte)LightbarBlueSlider.Value;
        await RunStabilityOutputAttemptAsync(
            () => session.TrySendLightbar(red, green, blue, out string? error) ? (true, (string?)null) : (false, error),
            $"Lightbar sent: RGB({red}, {green}, {blue}).");
    }

    private async void StabilityResetOutputButton_Click(object sender, RoutedEventArgs e)
    {
        if (_liveSession is not { IsRunning: true } session || !CanAttemptStabilityOutput())
        {
            return;
        }

        await RunStabilityOutputAttemptAsync(
            () => session.TryResetOutput(out string? error) ? (true, (string?)null) : (false, error),
            "Controller output reset to neutral.");
    }

    private async void StabilityDashboard_RumblePresetRequested(object? sender, RumblePresetRequestedEventArgs e)
    {
        if (_liveSession is not { IsRunning: true } session || !CanAttemptStabilityOutput())
        {
            return;
        }

        if (!ConfirmControlledAction(
                "Run a short vibration pulse. PadScope will stop retrying output if this HID path rejects DS4 writes.",
                DeviceComboBox.SelectedItem as ControllerDevice))
        {
            return;
        }

        bool success = await RunStabilityOutputAttemptAsync(
            () => session.TrySendRumble(e.SmallMotor, e.LargeMotor, out string? error) ? (true, (string?)null) : (false, error),
            "Vibration pulse started.",
            showSuccess: false);

        if (!success)
        {
            return;
        }

        await Task.Delay(280);
        await RunStabilityOutputAttemptAsync(
            () => session.TryResetRumble(out string? error) ? (true, (string?)null) : (false, error),
            "Vibration pulse completed.",
            showSuccess: false);
    }

    private async void StabilityDashboard_ResetRumbleRequested(object? sender, EventArgs e)
    {
        if (_liveSession is not { IsRunning: true } session || !CanAttemptStabilityOutput())
        {
            return;
        }

        await RunStabilityOutputAttemptAsync(
            () => session.TryResetRumble(out string? error) ? (true, (string?)null) : (false, error),
            "Vibration stopped.");
    }

    private async void StabilityDiagnosticsLab_VibrationRequested(object? sender, VibrationRequestEventArgs e)
    {
        if (_liveSession is not { IsRunning: true } session || !CanAttemptStabilityOutput())
        {
            return;
        }

        if (!ConfirmControlledAction(
                $"Run the {e.Pattern} vibration diagnostic for up to {e.DurationMs} ms. PadScope will reset the motors when the output path is writable.",
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

            (bool resetOk, string? resetError) = await Task.Run(() =>
            {
                bool ok = session.TryResetRumble(out string? error);
                return (ok, error);
            });

            if (!resetOk)
            {
                MarkStabilityOutputUnavailable(resetError ?? session.LastOutputWriteStatus ?? "Rumble reset failed.");
                return;
            }

            LiveStatusText.Text = $"Vibration diagnostic completed ({e.Pattern}).";
            _controllerDiagnosticsLab?.SetOutputStatus("Vibration completed · motors reset", success: true);
        }
        catch (OperationCanceledException)
        {
            LiveStatusText.Text = "Vibration diagnostic stopped.";
            if (!_outputUnavailableForSession)
            {
                _ = Task.Run(() => session.TryResetRumble(out _));
            }
        }
        catch (Exception ex)
        {
            MarkStabilityOutputUnavailable(session.LastOutputWriteStatus ?? ex.Message);
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

    private void StabilityDiagnosticsLab_StopVibrationRequested(object? sender, EventArgs e)
    {
        _diagnosticsVibrationCts?.Cancel();
        if (_liveSession is { IsRunning: true } session && !_outputUnavailableForSession)
        {
            _ = Task.Run(() => session.TryResetRumble(out _));
        }
    }

    private bool CanAttemptStabilityOutput()
    {
        if (_outputOperationBusy)
        {
            LiveStatusText.Text = "Controller output is already busy.";
            return false;
        }

        if (_outputUnavailableForSession)
        {
            LiveStatusText.Text = "Input is live. Vibration/lightbar are unavailable on this controller connection.";
            _controllerDiagnosticsLab?.SetOutputAvailability(
                false,
                "Vibration unavailable · input diagnostics remain active");
            return false;
        }

        return true;
    }

    private async Task<bool> RunStabilityOutputAttemptAsync(
        Func<(bool Success, string? Error)> operation,
        string successStatus,
        bool showSuccess = true)
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
                MarkStabilityOutputUnavailable(error ?? "Controller rejected the DS4 output report.");
                return false;
            }

            if (showSuccess)
            {
                LiveStatusText.Text = successStatus;
            }
            _controllerDiagnosticsLab?.SetOutputStatus(successStatus, success: true);
            return true;
        }
        catch (Exception ex)
        {
            MarkStabilityOutputUnavailable(ex.Message);
            return false;
        }
        finally
        {
            _outputOperationBusy = false;
            ApplyOutputAvailabilityToUi();
        }
    }

    private void MarkStabilityOutputUnavailable(string error)
    {
        _outputUnavailableForSession = true;
        _outputFailureReason = error;
        EnableOutputControls(false);

        const string friendly =
            "Input is working normally. This controller/driver path does not accept PadScope's native DS4 output, so vibration and lightbar are disabled for this live session.";

        LiveStatusText.Text = friendly;
        LiveStatusText.ToolTip = error;
        _modernLiveDashboard?.SetOutputEnabled(false);
        _controllerDiagnosticsLab?.SetOutputStatus(
            "Native DS4 output is unavailable on this controller connection.",
            success: false);
        _controllerDiagnosticsLab?.SetOutputAvailability(
            false,
            "Vibration unavailable · input diagnostics remain active");

        // Deliberately no modal MessageBox here. Unsupported output is a capability
        // result, not a fatal application error; input/diagnostics stay usable.
    }
}
