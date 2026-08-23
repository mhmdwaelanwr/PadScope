using System.Windows.Threading;
using PadScope.Core.Diagnostics;
using PadScope.Core.Models;
using PadScope.Hid;

namespace PadScope.Desktop;

public partial class MainWindow
{
    private async Task<bool> StartLiveSessionResponsiveAsync(ControllerDevice device)
    {
        StopVirtualPassthrough();
        StopMouseEmulation();

        Ds4ControllerSession? previous = _liveSession;
        _liveSession = null;
        if (previous is not null)
        {
            await Task.Run(previous.Dispose);
        }

        HidSharpHidInputReader reader = new();
        Ds4ControllerSession session = new(reader, device);
        session.Error += message => Dispatcher.BeginInvoke(() => LiveStatusText.Text = message);
        session.StateUpdated += state => _latestState = state;
        session.TimingUpdated += OnTimingUpdated;
        session.ReportObserved += OnReportObserved;

        LiveStatusText.Text = "Opening HID device…";
        StartInputButton.IsEnabled = false;
        StopInputButton.IsEnabled = false;

        (bool ok, string? error) = await Task.Run(() =>
        {
            bool success = session.TryStart(out string? startError);
            return (success, startError);
        });

        if (!ok)
        {
            await Task.Run(session.Dispose);
            StartInputButton.IsEnabled = DeviceComboBox.Items.Count > 0;
            StopInputButton.IsEnabled = false;
            EnableOutputControls(false);
            MessageBox.Show(
                this,
                error ?? "Could not start live input.",
                "PadScope Live Input",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }

        _liveSession = session;
        _prevButtons = default;
        _latestTiming = null;
        StartInputButton.IsEnabled = false;
        StopInputButton.IsEnabled = true;
        LiveStatusText.Text = $"Live: {session.DeviceDescription}";
        TimingText.Text = "Timing: waiting for reports...";
        EnableOutputControls(true);
        StartCaptureButton.IsEnabled = _captureRecorder is null;
        SaveCaptureButton.IsEnabled = _captureRecorder?.Count > 0;

        _liveTimer?.Stop();
        _liveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _liveTimer.Tick += (_, _) => RenderLatestState();
        _liveTimer.Start();
        return true;
    }

    private async Task<(bool Success, string? Error)> SendRumbleResponsiveAsync(byte small, byte large)
    {
        Ds4ControllerSession? session = _liveSession;
        if (session is null) return (false, "No live controller session.");
        return await Task.Run(() =>
        {
            bool ok = session.TrySendRumble(small, large, out string? error);
            return (ok, error);
        });
    }

    private async Task<(bool Success, string? Error)> SendLightbarResponsiveAsync(byte red, byte green, byte blue)
    {
        Ds4ControllerSession? session = _liveSession;
        if (session is null) return (false, "No live controller session.");
        return await Task.Run(() =>
        {
            bool ok = session.TrySendLightbar(red, green, blue, out string? error);
            return (ok, error);
        });
    }

    private async Task<(bool Success, string? Error)> ResetOutputResponsiveAsync()
    {
        Ds4ControllerSession? session = _liveSession;
        if (session is null) return (false, "No live controller session.");
        return await Task.Run(() =>
        {
            bool ok = session.TryResetOutput(out string? error);
            return (ok, error);
        });
    }
}
