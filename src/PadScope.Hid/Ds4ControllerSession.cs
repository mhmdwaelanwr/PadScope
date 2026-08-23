using PadScope.Core.Diagnostics;
using PadScope.Core.Input;
using PadScope.Core.Models;

namespace PadScope.Hid;

public sealed class Ds4ControllerSession : IDisposable
{
    private readonly IHidInputReader _reader;
    private readonly ControllerDevice _device;
    private readonly ReportTimingAnalyzer _timingAnalyzer = new();
    private int _timingPublishCounter;
    private bool _disposed;
    private ConnectionType _effectiveConnectionType;
    private int? _lastObservedReportId;

    public event Action<Ds4InputState>? StateUpdated;
    public event Action<ReportTimingSnapshot>? TimingUpdated;
    public event Action<HidInputReport>? ReportObserved;
    public event Action<string>? Error;

    public Ds4ControllerSession(IHidInputReader reader, ControllerDevice device)
    {
        _reader = reader;
        _device = device;
        _effectiveConnectionType = device.ConnectionType;

        _reader.ReportReceived += OnReportReceived;
        _reader.ErrorOccurred += OnError;
    }

    public bool IsRunning => _reader.IsRunning;

    public string? DeviceDescription => _reader.DeviceDescription;

    public ControllerDevice Device => _device;

    /// <summary>
    /// Transport inferred from live DS4 report shape when possible. This is more
    /// reliable than WMI for clone controllers whose PnP path can look like USB
    /// even while the gamepad HID interface is using the Bluetooth report format.
    /// </summary>
    public ConnectionType EffectiveConnectionType => _effectiveConnectionType;

    public int? LastObservedReportId => _lastObservedReportId;

    public int MaxOutputReportLength => _reader.MaxOutputReportLength;

    public bool TryStart(out string? error)
    {
        _timingAnalyzer.Reset();
        _timingPublishCounter = 0;
        _lastObservedReportId = null;
        _effectiveConnectionType = _device.ConnectionType;

        if (!_reader.TryOpen(_device, out error))
        {
            return false;
        }

        _reader.Start();
        return true;
    }

    public void Stop()
    {
        _reader.Stop();
    }

    public bool TrySendRumble(byte smallMotor, byte largeMotor, out string? error)
    {
        byte[] report = Ds4OutputReportBuilder.BuildOutputReport(
            ResolveOutputConnectionType(),
            rumbleSmall: smallMotor,
            rumbleLarge: largeMotor,
            setRumble: true,
            setLightbar: false);

        return _reader.TryWriteOutput(report, out error);
    }

    public bool TryResetRumble(out string? error) => TrySendRumble(0, 0, out error);

    public bool TrySendLightbar(byte red, byte green, byte blue, out string? error)
    {
        byte[] report = Ds4OutputReportBuilder.BuildOutputReport(
            ResolveOutputConnectionType(),
            red: red,
            green: green,
            blue: blue,
            setRumble: false,
            setLightbar: true);

        return _reader.TryWriteOutput(report, out error);
    }

    public bool TryResetOutput(out string? error)
    {
        byte[] report = Ds4OutputReportBuilder.BuildOutputReport(
            ResolveOutputConnectionType());

        return _reader.TryWriteOutput(report, out error);
    }

    private ConnectionType ResolveOutputConnectionType()
    {
        return _effectiveConnectionType switch
        {
            ConnectionType.Bluetooth => ConnectionType.Bluetooth,
            ConnectionType.Usb => ConnectionType.Usb,
            _ => _device.ConnectionType == ConnectionType.Bluetooth
                ? ConnectionType.Bluetooth
                : ConnectionType.Usb
        };
    }

    private void OnReportReceived(HidInputReport report)
    {
        if (_disposed)
        {
            return;
        }

        ReportObserved?.Invoke(report);
        _lastObservedReportId = report.ReportId;

        // Derive the transport from the actual packet on the wire. Full DS4
        // Bluetooth input is report 0x11 / 78 bytes; native USB input is
        // report 0x01 / 64 bytes. Do not treat the tiny Bluetooth minimal
        // report 0x01 as USB.
        if (report.ReportId == Ds4ReportParser.BluetoothReportId &&
            report.Data.Length >= Ds4ReportParser.BluetoothReportLength)
        {
            _effectiveConnectionType = ConnectionType.Bluetooth;
        }
        else if (report.ReportId == Ds4ReportParser.UsbReportId &&
                 report.Data.Length >= Ds4ReportParser.UsbReportLength)
        {
            _effectiveConnectionType = ConnectionType.Usb;
        }

        if (!Ds4ReportParser.LooksLikeDs4Report(report.Data))
        {
            return;
        }

        if (report.ReportId == Ds4ReportParser.BluetoothReportId &&
            !Ds4ReportParser.HasValidBluetoothCrc(report.Data))
        {
            return;
        }

        _timingAnalyzer.Add(report.Timestamp);
        if (++_timingPublishCounter == 1 || _timingPublishCounter % 16 == 0)
        {
            TimingUpdated?.Invoke(_timingAnalyzer.Snapshot());
        }

        StateUpdated?.Invoke(Ds4ReportParser.Parse(report.Data));
    }

    private void OnError(string message)
    {
        if (!_disposed)
        {
            Error?.Invoke(message);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _reader.ReportReceived -= OnReportReceived;
        _reader.ErrorOccurred -= OnError;
        _reader.Stop();
        _reader.Dispose();
    }
}
