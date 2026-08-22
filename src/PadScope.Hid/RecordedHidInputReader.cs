using System.Diagnostics;
using PadScope.Core.Diagnostics;
using PadScope.Core.Models;

namespace PadScope.Hid;

public sealed class RecordedHidInputReader : IHidInputReader
{
    private readonly HidCaptureDocument _capture;
    private Thread? _replayThread;
    private volatile bool _keepRunning;
    private bool _disposed;

    public RecordedHidInputReader(HidCaptureDocument capture)
    {
        HidCaptureStore.Validate(capture);
        _capture = capture;
    }

    public event Action<HidInputReport>? ReportReceived;
    public event Action<string>? ErrorOccurred;

    public bool IsRunning => _keepRunning;
    public string DeviceDescription => $"Replay: {_capture.Device.DisplayName}";
    public int MaxOutputReportLength => 0;

    public bool TryOpen(ControllerDevice device, out string? error)
    {
        if (_disposed)
        {
            error = "The replay reader has been disposed.";
            return false;
        }

        error = null;
        return true;
    }

    public void Start()
    {
        if (_disposed || _keepRunning || _capture.Frames.Count == 0)
        {
            return;
        }

        _keepRunning = true;
        _replayThread = new Thread(ReplayLoop)
        {
            IsBackground = true,
            Name = "PadScope.Hid.ReplayLoop"
        };
        _replayThread.Start();
    }

    public void Stop()
    {
        _keepRunning = false;
        _replayThread?.Join(TimeSpan.FromSeconds(1));
        _replayThread = null;
    }

    public bool TryWriteOutput(byte[] report, out string? error)
    {
        error = "Output is disabled during capture replay.";
        return false;
    }

    private void ReplayLoop()
    {
        Stopwatch clock = Stopwatch.StartNew();
        try
        {
            foreach (HidCaptureFrame frame in _capture.Frames)
            {
                if (!_keepRunning)
                {
                    return;
                }

                TimeSpan due = TimeSpan.FromTicks(frame.OffsetMicroseconds * 10);
                while (_keepRunning && clock.Elapsed < due)
                {
                    TimeSpan remaining = due - clock.Elapsed;
                    Thread.Sleep(remaining > TimeSpan.FromMilliseconds(5) ? 5 : 1);
                }

                if (!_keepRunning)
                {
                    return;
                }

                byte[] data = HidCaptureStore.DecodeFrame(frame);
                ReportReceived?.Invoke(new HidInputReport(
                    data,
                    frame.ReportId,
                    DateTimeOffset.UtcNow));
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke($"HID replay failed: {ex.Message}");
        }
        finally
        {
            _keepRunning = false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _disposed = true;
    }
}
