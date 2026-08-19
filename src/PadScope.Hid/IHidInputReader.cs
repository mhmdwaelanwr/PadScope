using PadScope.Core.Models;

namespace PadScope.Hid;

public interface IHidInputReader : IDisposable
{
    event Action<HidInputReport>? ReportReceived;
    event Action<string>? ErrorOccurred;

    bool IsRunning { get; }

    string? DeviceDescription { get; }

    int MaxOutputReportLength { get; }

    bool TryOpen(ControllerDevice device, out string? error);

    void Start();

    void Stop();

    bool TryWriteOutput(byte[] report, out string? error);
}
