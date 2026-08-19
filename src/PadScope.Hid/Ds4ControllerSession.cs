using PadScope.Core.Input;
using PadScope.Core.Models;

namespace PadScope.Hid;

public sealed class Ds4ControllerSession : IDisposable
{
    private readonly IHidInputReader _reader;
    private readonly ControllerDevice _device;
    private bool _disposed;

    public event Action<Ds4InputState>? StateUpdated;
    public event Action<string>? Error;

    public Ds4ControllerSession(IHidInputReader reader, ControllerDevice device)
    {
        _reader = reader;
        _device = device;

        _reader.ReportReceived += OnReportReceived;
        _reader.ErrorOccurred += OnError;
    }

    public bool IsRunning => _reader.IsRunning;

    public string? DeviceDescription => _reader.DeviceDescription;

    public bool TryStart(out string? error)
    {
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
            _device.ConnectionType,
            rumbleSmall: smallMotor,
            rumbleLarge: largeMotor,
            reportLength: GetOutputReportLength());

        return _reader.TryWriteOutput(report, out error);
    }

    public bool TrySendLightbar(byte red, byte green, byte blue, out string? error)
    {
        byte[] report = Ds4OutputReportBuilder.BuildOutputReport(
            _device.ConnectionType,
            red: red,
            green: green,
            blue: blue,
            reportLength: GetOutputReportLength());

        return _reader.TryWriteOutput(report, out error);
    }

    public bool TryResetOutput(out string? error)
    {
        byte[] report = Ds4OutputReportBuilder.BuildOutputReport(
            _device.ConnectionType,
            reportLength: GetOutputReportLength());

        return _reader.TryWriteOutput(report, out error);
    }

    private int GetOutputReportLength()
    {
        return _reader.MaxOutputReportLength > 0 ? _reader.MaxOutputReportLength : 64;
    }

    private void OnReportReceived(HidInputReport report)
    {
        if (_disposed)
        {
            return;
        }

        if (!Ds4ReportParser.LooksLikeDs4Report(report.Data))
        {
            return;
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