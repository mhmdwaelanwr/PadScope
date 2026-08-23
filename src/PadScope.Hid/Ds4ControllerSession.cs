using PadScope.Core.Diagnostics;
using PadScope.Core.Input;
using PadScope.Core.Models;

namespace PadScope.Hid;

public sealed class Ds4ControllerSession : IDisposable
{
    private const int OutputPrimeDelayMilliseconds = 12;

    private readonly IHidInputReader _reader;
    private readonly ControllerDevice _device;
    private readonly ReportTimingAnalyzer _timingAnalyzer = new();
    private readonly object _intervalSync = new();
    private readonly Queue<double> _reportIntervalsMs = new();
    private int _timingPublishCounter;
    private bool _disposed;
    private ConnectionType _effectiveConnectionType;
    private DateTimeOffset? _lastValidatedReportTimestamp;
    private byte _rumbleSmall;
    private byte _rumbleLarge;
    private byte _lightbarRed;
    private byte _lightbarGreen;
    private byte _lightbarBlue;
    private bool _outputPrimed;
    private string? _lastOutputWriteStatus;

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
    public ConnectionType EffectiveConnectionType => _effectiveConnectionType;
    public int MaxOutputReportLength => _reader.MaxOutputReportLength;
    public bool OutputPrimed => _outputPrimed;
    public string? LastOutputWriteStatus => _lastOutputWriteStatus ?? (_reader as HidSharpHidInputReader)?.LastOutputWriteStatus;

    public bool TryStart(out string? error)
    {
        _timingAnalyzer.Reset();
        _timingPublishCounter = 0;
        _effectiveConnectionType = _device.ConnectionType;
        _lastValidatedReportTimestamp = null;
        _rumbleSmall = _rumbleLarge = 0;
        _lightbarRed = _lightbarGreen = _lightbarBlue = 0;
        _outputPrimed = false;
        _lastOutputWriteStatus = null;
        lock (_intervalSync) _reportIntervalsMs.Clear();

        if (!_reader.TryOpen(_device, out error)) return false;
        _reader.Start();
        return true;
    }

    public void Stop() => _reader.Stop();

    public bool TrySendRumble(byte smallMotor, byte largeMotor, out string? error)
    {
        string? primeError = null;
        if ((smallMotor != 0 || largeMotor != 0) && !_outputPrimed)
            TryPrimeOutput(out primeError);

        byte[] report = Ds4OutputReportBuilder.BuildOutputReport(
            ResolveOutputConnectionType(),
            rumbleSmall: smallMotor,
            rumbleLarge: largeMotor,
            red: _lightbarRed,
            green: _lightbarGreen,
            blue: _lightbarBlue);

        if (!TryWriteOutput(report, out error))
        {
            if (!string.IsNullOrWhiteSpace(primeError))
                error = $"prime: {primeError} | requested rumble: {error}";
            return false;
        }

        _outputPrimed = true;
        _rumbleSmall = smallMotor;
        _rumbleLarge = largeMotor;
        return true;
    }

    public bool TryResetRumble(out string? error)
    {
        byte[] report = Ds4OutputReportBuilder.BuildOutputReport(
            ResolveOutputConnectionType(),
            rumbleSmall: 0,
            rumbleLarge: 0,
            red: _lightbarRed,
            green: _lightbarGreen,
            blue: _lightbarBlue);

        if (!TryWriteOutput(report, out error)) return false;
        _outputPrimed = true;
        _rumbleSmall = _rumbleLarge = 0;
        return true;
    }

    public bool TrySendLightbar(byte red, byte green, byte blue, out string? error)
    {
        string? primeError = null;
        if ((red != 0 || green != 0 || blue != 0) && !_outputPrimed)
            TryPrimeOutput(out primeError);

        byte[] report = Ds4OutputReportBuilder.BuildOutputReport(
            ResolveOutputConnectionType(),
            rumbleSmall: _rumbleSmall,
            rumbleLarge: _rumbleLarge,
            red: red,
            green: green,
            blue: blue);

        if (!TryWriteOutput(report, out error))
        {
            if (!string.IsNullOrWhiteSpace(primeError))
                error = $"prime: {primeError} | requested lightbar: {error}";
            return false;
        }

        _outputPrimed = true;
        _lightbarRed = red;
        _lightbarGreen = green;
        _lightbarBlue = blue;
        return true;
    }

    public bool TryResetOutput(out string? error)
    {
        byte[] report = Ds4OutputReportBuilder.BuildOutputReport(ResolveOutputConnectionType());
        if (!TryWriteOutput(report, out error)) return false;
        _outputPrimed = true;
        _rumbleSmall = _rumbleLarge = 0;
        _lightbarRed = _lightbarGreen = _lightbarBlue = 0;
        return true;
    }

    public IReadOnlyList<double> DrainReportIntervals(int maxSamples = 512)
    {
        maxSamples = Math.Clamp(maxSamples, 1, 4096);
        List<double> samples = new(Math.Min(maxSamples, 128));
        lock (_intervalSync)
        {
            while (_reportIntervalsMs.Count > 0 && samples.Count < maxSamples)
                samples.Add(_reportIntervalsMs.Dequeue());
        }
        return samples;
    }

    private bool TryPrimeOutput(out string? error)
    {
        if (_outputPrimed)
        {
            error = null;
            return true;
        }

        byte[] neutral = Ds4OutputReportBuilder.BuildOutputReport(
            ResolveOutputConnectionType(),
            rumbleSmall: 0,
            rumbleLarge: 0,
            red: _lightbarRed,
            green: _lightbarGreen,
            blue: _lightbarBlue);

        if (!TryWriteOutput(neutral, out error)) return false;

        _outputPrimed = true;
        Thread.Sleep(OutputPrimeDelayMilliseconds);
        return true;
    }

    private bool TryWriteOutput(byte[] report, out string? error)
    {
        if (HidSharpIndependentOutput.TryWrite(_device, report, out string? status, out string? independentError))
        {
            _lastOutputWriteStatus = status;
            error = null;
            return true;
        }
        if (_reader.TryWriteOutput(report, out string? readerError))
        {
            _lastOutputWriteStatus = (_reader as HidSharpHidInputReader)?.LastOutputWriteStatus ?? "Output OK · adaptive live HID path";
            error = null;
            return true;
        }
        error = $"independent stream: {independentError} | adaptive live path: {readerError}";
        _lastOutputWriteStatus = error;
        return false;
    }

    private ConnectionType ResolveOutputConnectionType() => _effectiveConnectionType switch
    {
        ConnectionType.Bluetooth => ConnectionType.Bluetooth,
        ConnectionType.Usb => ConnectionType.Usb,
        _ => _device.ConnectionType == ConnectionType.Bluetooth ? ConnectionType.Bluetooth : ConnectionType.Usb
    };

    private void OnReportReceived(HidInputReport report)
    {
        if (_disposed) return;
        ReportObserved?.Invoke(report);
        if (!Ds4ReportParser.LooksLikeDs4Report(report.Data)) return;
        if (report.ReportId == Ds4ReportParser.BluetoothReportId && !Ds4ReportParser.HasValidBluetoothCrc(report.Data)) return;

        if (report.ReportId == Ds4ReportParser.BluetoothReportId && report.Data.Length >= Ds4ReportParser.BluetoothReportLength)
            _effectiveConnectionType = ConnectionType.Bluetooth;
        else if (report.ReportId == Ds4ReportParser.UsbReportId && report.Data.Length >= Ds4ReportParser.UsbReportLength)
            _effectiveConnectionType = ConnectionType.Usb;

        DateTimeOffset? previous = _lastValidatedReportTimestamp;
        _lastValidatedReportTimestamp = report.Timestamp;
        if (previous.HasValue)
        {
            double intervalMs = (report.Timestamp - previous.Value).TotalMilliseconds;
            if (intervalMs is > 0 and < 1000)
            {
                lock (_intervalSync)
                {
                    _reportIntervalsMs.Enqueue(intervalMs);
                    while (_reportIntervalsMs.Count > 4096) _reportIntervalsMs.Dequeue();
                }
            }
        }

        _timingAnalyzer.Add(report.Timestamp);
        if (++_timingPublishCounter == 1 || _timingPublishCounter % 16 == 0)
            TimingUpdated?.Invoke(_timingAnalyzer.Snapshot());
        StateUpdated?.Invoke(Ds4ReportParser.Parse(report.Data));
    }

    private void OnError(string message)
    {
        if (!_disposed) Error?.Invoke(message);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _reader.ReportReceived -= OnReportReceived;
        _reader.ErrorOccurred -= OnError;
        _reader.Stop();
        _reader.Dispose();
    }
}
