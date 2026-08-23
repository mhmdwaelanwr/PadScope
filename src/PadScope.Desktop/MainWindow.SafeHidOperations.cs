using System.Threading;
using System.Windows;
using System.Windows.Threading;
using PadScope.Core.Models;
using PadScope.Hid;

namespace PadScope.Desktop;

public partial class MainWindow
{
    private readonly SemaphoreSlim _outputOperationGate = new(1, 1);
    private bool _nativeOutputRejected;
    private string? _nativeOutputFailure;

    private async Task<bool> StartLiveSessionResponsiveAsync(ControllerDevice device)
    {
        StopVirtualPassthrough();
        StopMouseEmulation();
        _nativeOutputRejected = false;
        _nativeOutputFailure = null;
        _liveTimer?.Stop();
        _latestState = null;
        _latestTiming = null;

        Ds4ControllerSession? previous = _liveSession;
        _liveSession = null;
        if (previous is not null) await Task.Run(previous.Dispose);

        HidSharpHidInputReader reader = new();
        Ds4ControllerSession session = new(reader, device);
        session.Error += message => Dispatcher.BeginInvoke(() => LiveStatusText.Text = message);
        session.StateUpdated += state => _latestState = state;
        session.TimingUpdated += OnTimingUpdated;
        session.ReportObserved += OnReportObserved;

        LiveStatusText.Text = "Opening HID device…";
        StartInputButton.IsEnabled = false;
        StopInputButton.IsEnabled = false;
        EnableOutputControls(false);

        (bool ok, string? error) = await Task.Run(() =>
        {
            bool success = session.TryStart(out string? startError);
            return (success, startError);
        });

        if (!ok)
        {
            await Task.Run(session.Dispose);
            StartInputButton.IsEnabled = DeviceComboBox.Items.Count > 0;
            MessageBox.Show(this, error ?? "Could not start live input.", "PadScope Live Input", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        _liveSession = session;
        _prevButtons = default;
        StartInputButton.IsEnabled = false;
        StopInputButton.IsEnabled = true;
        LiveStatusText.Text = $"Live: {session.DeviceDescription}";
        TimingText.Text = "Timing: waiting for reports...";
        EnableOutputControls(true);
        StartCaptureButton.IsEnabled = _captureRecorder is null;
        SaveCaptureButton.IsEnabled = _captureRecorder?.Count > 0;

        _liveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _liveTimer.Tick += (_, _) => RenderLatestState();
        _liveTimer.Start();
        return true;
    }

    private async Task StopLiveSessionResponsiveAsync()
    {
        _liveTimer?.Stop();
        _liveTimer = null;
        Ds4ControllerSession? session = _liveSession;
        _liveSession = null;
        if (session is not null) await Task.Run(session.Dispose);

        _isCapturing = false;
        _latestTiming = null;
        _latestState = null;
        _prevButtons = default;
        _nativeOutputRejected = false;
        _nativeOutputFailure = null;
        StartInputButton.IsEnabled = DeviceComboBox.Items.Count > 0;
        StopInputButton.IsEnabled = false;
        EnableOutputControls(false);
        StartCaptureButton.IsEnabled = false;
        SaveCaptureButton.IsEnabled = _captureRecorder?.Count > 0;
        if (_captureRecorder?.Count > 0)
            CaptureStatusText.Text = $"Capture paused with {_captureRecorder.Count:N0} reports. Save it before starting another capture.";
        LiveStatusText.Text = "Live input stopped.";
        TimingText.Text = "Timing: not running";
    }

    private Task<(bool Success, string? Error)> SendRumbleResponsiveAsync(byte small, byte large) =>
        RunOutputOperationAsync(session => session.TrySendRumble(small, large, out string? error) ? (true, (string?)null) : (false, error));

    private Task<(bool Success, string? Error)> ResetRumbleResponsiveAsync(bool allowAfterRejection = false) =>
        RunOutputOperationAsync(session => session.TryResetRumble(out string? error) ? (true, (string?)null) : (false, error), allowAfterRejection);

    private Task<(bool Success, string? Error)> SendLightbarResponsiveAsync(byte red, byte green, byte blue) =>
        RunOutputOperationAsync(session => session.TrySendLightbar(red, green, blue, out string? error) ? (true, (string?)null) : (false, error));

    private Task<(bool Success, string? Error)> ResetOutputResponsiveAsync(bool allowAfterRejection = false) =>
        RunOutputOperationAsync(session => session.TryResetOutput(out string? error) ? (true, (string?)null) : (false, error), allowAfterRejection);

    private async Task<(bool Success, string? Error)> RunOutputOperationAsync(
        Func<Ds4ControllerSession, (bool Success, string? Error)> operation,
        bool allowAfterRejection = false)
    {
        Ds4ControllerSession? session = _liveSession;
        if (session is null) return (false, "No live controller session.");
        if (_nativeOutputRejected && !allowAfterRejection)
            return (false, _nativeOutputFailure ?? "Native DS4 output is unavailable for this live session.");

        await _outputOperationGate.WaitAsync();
        try
        {
            if (!ReferenceEquals(session, _liveSession) || !session.IsRunning)
                return (false, "The live controller session changed before output could be sent.");

            (bool success, string? error) = await Task.Run(() => operation(session));
            if (!success && !allowAfterRejection)
            {
                _nativeOutputRejected = true;
                _nativeOutputFailure = error ?? "The controller rejected native DS4 output.";
                EnableOutputControls(false);
                LiveStatusText.Text = "Input remains active · native DS4 vibration/lightbar unavailable on this HID path.";
                LiveStatusText.ToolTip = _nativeOutputFailure;
            }
            return (success, error);
        }
        finally
        {
            _outputOperationGate.Release();
        }
    }
}
