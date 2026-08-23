using System.Text;
using HidSharp;
using PadScope.Core.Input;
using PadScope.Core.Models;

namespace PadScope.Hid;

public sealed class HidSharpHidInputReader : IHidInputReader
{
    private const int HidReadTimeoutMilliseconds = 250;
    private const int HidWriteTimeoutMilliseconds = 450;
    private const int StopJoinTimeoutMilliseconds = 1000;

    private readonly object _outputSync = new();
    private HidStream? _stream;
    private HidDevice? _device;
    private Thread? _readThread;
    private volatile bool _keepReading;
    private bool _disposed;
    private string? _preferredControlPath;
    private int _preferredControlOutputLength;

    public event Action<HidInputReport>? ReportReceived;
    public event Action<string>? ErrorOccurred;

    public bool IsRunning => _keepReading;

    public string? DeviceDescription { get; private set; }

    public int MaxOutputReportLength => SafeGetReportLength(_device, input: false);

    /// <summary>
    /// Human-readable description of the most recent output transport decision.
    /// Useful for diagnosing clone controllers and Windows Bluetooth stacks.
    /// </summary>
    public string? LastOutputWriteStatus { get; private set; }

    public bool TryOpen(ControllerDevice device, out string? error)
    {
        if (_disposed)
        {
            error = "The reader has been disposed.";
            return false;
        }

        Stop();
        _stream?.Dispose();
        _stream = null;
        _preferredControlPath = null;
        _preferredControlOutputLength = 0;
        LastOutputWriteStatus = null;

        try
        {
            _device = SelectBestDevice(device);
        }
        catch (Exception ex)
        {
            error = $"HID enumeration failed: {ex.Message}";
            return false;
        }

        if (_device is null)
        {
            error = BuildNoDeviceMessage(device);
            return false;
        }

        try
        {
            _stream = _device.Open();
            _stream.ReadTimeout = HidReadTimeoutMilliseconds;
            _stream.WriteTimeout = HidWriteTimeoutMilliseconds;
        }
        catch (Exception ex)
        {
            error = $"Could not open the HID device: {ex.Message}";
            return false;
        }

        int inputLength = SafeGetReportLength(_device, input: true);
        int outputLength = SafeGetReportLength(_device, input: false);
        DeviceDescription = $"{SafeGetProductName(_device)} (VID {_device.VendorID:X4}/PID {_device.ProductID:X4}, in {inputLength} B, out {outputLength} B)";
        error = null;
        return true;
    }

    public void Start()
    {
        if (_stream is null || _keepReading)
        {
            return;
        }

        _keepReading = true;
        _readThread = new Thread(ReadLoop)
        {
            IsBackground = true,
            Name = "PadScope.Hid.ReadLoop"
        };
        _readThread.Start();
    }

    public void Stop()
    {
        _keepReading = false;
        Thread? thread = _readThread;
        if (thread is null)
        {
            return;
        }

        if (!thread.Join(TimeSpan.FromMilliseconds(StopJoinTimeoutMilliseconds)))
        {
            // Some Windows HID drivers do not honor read cancellation promptly.
            // Closing the stream is the final bounded escape hatch.
            _stream?.Dispose();
            _stream = null;
            if (!thread.Join(TimeSpan.FromMilliseconds(HidReadTimeoutMilliseconds)))
            {
                ErrorOccurred?.Invoke("HID read loop did not stop after the stream was closed.");
                return;
            }
        }

        _readThread = null;
    }

    public bool TryWriteOutput(byte[] report, out string? error)
    {
        if (report is null || report.Length == 0)
        {
            error = "HID output report is empty.";
            LastOutputWriteStatus = error;
            return false;
        }

        lock (_outputSync)
        {
            HidStream? stream = _stream;
            HidDevice? primary = _device;
            if (stream is null || primary is null)
            {
                error = "No HID device is open.";
                LastOutputWriteStatus = error;
                return false;
            }

            List<string> attempts = new();
            bool bluetoothReport = report[0] == Ds4OutputReportBuilder.BluetoothOutputReportId;

            // Once a control path succeeds, reuse it first. This matters for
            // multi-segment vibration patterns: a controller that rejects the
            // interrupt path should not pay a timeout on every segment.
            if (!string.IsNullOrWhiteSpace(_preferredControlPath))
            {
                if (TryControlWritePath(
                        _preferredControlPath,
                        report,
                        _preferredControlOutputLength,
                        out string? preferredError))
                {
                    LastOutputWriteStatus = BuildSuccessStatus(
                        "control (cached)",
                        report,
                        _preferredControlOutputLength);
                    error = null;
                    return true;
                }

                attempts.Add($"cached control: {preferredError}");
                _preferredControlPath = null;
                _preferredControlOutputLength = 0;
            }

            int primaryOutputLength = SafeGetReportLength(primary, input: false);

            // On Windows, DS4 Bluetooth output is commonly delivered through a
            // HID control transfer, while USB normally uses the interrupt output
            // endpoint. Try the transport-appropriate path first, then fall back.
            if (bluetoothReport)
            {
                if (TryControlWriteDevice(primary, report, out string? controlError))
                {
                    CacheControlPath(primary.DevicePath, primaryOutputLength);
                    LastOutputWriteStatus = BuildSuccessStatus("control", report, primaryOutputLength);
                    error = null;
                    return true;
                }
                attempts.Add($"primary control: {controlError}");

                if (TryInterruptWrite(stream, primary, report, out string? interruptError))
                {
                    LastOutputWriteStatus = BuildSuccessStatus("interrupt", report, primaryOutputLength);
                    error = null;
                    return true;
                }
                attempts.Add($"interrupt: {interruptError}");
            }
            else
            {
                if (TryInterruptWrite(stream, primary, report, out string? interruptError))
                {
                    LastOutputWriteStatus = BuildSuccessStatus("interrupt", report, primaryOutputLength);
                    error = null;
                    return true;
                }
                attempts.Add($"interrupt: {interruptError}");

                if (TryControlWriteDevice(primary, report, out string? controlError))
                {
                    CacheControlPath(primary.DevicePath, primaryOutputLength);
                    LastOutputWriteStatus = BuildSuccessStatus("control", report, primaryOutputLength);
                    error = null;
                    return true;
                }
                attempts.Add($"primary control: {controlError}");
            }

            // Composite/clone controllers can expose input on one HID interface
            // and writable output on another interface with the same VID/PID.
            if (TrySiblingControlWrite(
                    primary,
                    report,
                    out string? siblingPath,
                    out int siblingOutputLength,
                    out string? siblingError))
            {
                CacheControlPath(siblingPath, siblingOutputLength);
                LastOutputWriteStatus = BuildSuccessStatus(
                    "sibling control",
                    report,
                    siblingOutputLength);
                error = null;
                return true;
            }

            if (!string.IsNullOrWhiteSpace(siblingError))
            {
                attempts.Add($"sibling control: {siblingError}");
            }

            error = $"HID output report 0x{report[0]:X2} ({report.Length} B) failed. " + string.Join(" | ", attempts);
            LastOutputWriteStatus = error;
            return false;
        }
    }

    private void CacheControlPath(string? path, int outputLength)
    {
        _preferredControlPath = path;
        _preferredControlOutputLength = outputLength;
    }

    private static bool TryInterruptWrite(HidStream stream, HidDevice device, byte[] report, out string? error)
    {
        int outputLength = SafeGetReportLength(device, input: false);
        byte[]? prepared = PrepareReportForLength(report, outputLength, out error);
        if (prepared is null)
        {
            return false;
        }

        try
        {
            stream.Write(prepared);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex is TimeoutException ? "operation timed out" : ex.Message;
            return false;
        }
    }

    private static bool TryControlWriteDevice(HidDevice device, byte[] report, out string? error)
    {
        return TryControlWritePath(
            device.DevicePath,
            report,
            SafeGetReportLength(device, input: false),
            out error);
    }

    private static bool TryControlWritePath(string? path, byte[] report, int outputLength, out string? error)
    {
        byte[]? prepared = PrepareReportForLength(report, outputLength, out error);
        if (prepared is null)
        {
            return false;
        }

        return WindowsHidControlOutput.TryWrite(path, prepared, out error);
    }

    private static bool TrySiblingControlWrite(
        HidDevice primary,
        byte[] report,
        out string? successfulPath,
        out int successfulOutputLength,
        out string? error)
    {
        successfulPath = null;
        successfulOutputLength = 0;
        List<string> failures = new();

        try
        {
            IEnumerable<HidDevice> candidates = DeviceList.Local
                .GetHidDevices(primary.VendorID, primary.ProductID)
                .Where(candidate => !string.Equals(candidate.DevicePath, primary.DevicePath, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(candidate => OutputCompatibilityScore(candidate, report.Length))
                .ThenByDescending(candidate => SafeGetReportLength(candidate, input: true));

            foreach (HidDevice candidate in candidates)
            {
                int outputLength = SafeGetReportLength(candidate, input: false);
                if (outputLength <= 0)
                {
                    continue;
                }

                if (TryControlWriteDevice(candidate, report, out string? candidateError))
                {
                    successfulPath = candidate.DevicePath;
                    successfulOutputLength = outputLength;
                    error = null;
                    return true;
                }

                failures.Add($"{SafeGetProductName(candidate)} out {outputLength} B: {candidateError}");
                if (failures.Count >= 3)
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            failures.Add($"enumeration failed: {ex.Message}");
        }

        error = failures.Count == 0
            ? "no compatible writable sibling HID interface was found"
            : string.Join("; ", failures);
        return false;
    }

    private static byte[]? PrepareReportForLength(byte[] report, int outputLength, out string? error)
    {
        if (outputLength <= 0 || outputLength == report.Length)
        {
            error = null;
            return report;
        }

        if (outputLength < report.Length)
        {
            error = $"interface output length is {outputLength} B but report needs {report.Length} B";
            return null;
        }

        byte[] padded = new byte[outputLength];
        Buffer.BlockCopy(report, 0, padded, 0, report.Length);
        error = null;
        return padded;
    }

    private static string BuildSuccessStatus(string path, byte[] report, int maxOutputLength)
    {
        return $"Output OK · {path} · report 0x{report[0]:X2} · {report.Length} B · HID max {maxOutputLength} B";
    }

    private void ReadLoop()
    {
        HidStream? stream = _stream;
        if (stream is null)
        {
            return;
        }

        int bufferLength = SafeGetReportLength(_device, input: true);
        if (bufferLength <= 0)
        {
            bufferLength = 64;
        }

        byte[] buffer = new byte[bufferLength];

        while (_keepReading)
        {
            try
            {
                int read = stream.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                {
                    continue;
                }

                byte[] copy = new byte[read];
                Buffer.BlockCopy(buffer, 0, copy, 0, read);

                ReportReceived?.Invoke(new HidInputReport(
                    copy,
                    ReportId: copy.Length > 0 ? copy[0] : 0,
                    Timestamp: DateTimeOffset.UtcNow
                ));
            }
            catch (TimeoutException)
            {
                // Expected while idle. The short timeout makes Stop deterministic.
            }
            catch (Exception ex)
            {
                if (!_keepReading)
                {
                    return;
                }

                ErrorOccurred?.Invoke($"HID read failed: {ex.Message}");
                Thread.Sleep(200);
            }
        }
    }

    private static HidDevice? SelectBestDevice(ControllerDevice device)
    {
        int vendorId = ParseHexId(device.VendorId);
        int productId = ParseHexId(device.ProductId);

        // Never fall back to every HID interface: that can open a keyboard,
        // mouse, or an unrelated controller when WMI did not provide IDs.
        if (vendorId <= 0 || productId <= 0)
        {
            return null;
        }

        List<HidDevice> candidates = DeviceList.Local
            .GetHidDevices(vendorId, productId)
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates
            .Select(candidate => new
            {
                Device = candidate,
                Score = ScoreDevice(candidate, device)
            })
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => SafeGetReportLength(item.Device, input: true))
            .Select(item => item.Device)
            .FirstOrDefault();
    }

    private static int ScoreDevice(HidDevice candidate, ControllerDevice selected)
    {
        int score = 0;
        string name = SafeGetProductName(candidate);
        string lowered = name.ToLowerInvariant();
        int inputLength = SafeGetReportLength(candidate, input: true);
        int outputLength = SafeGetReportLength(candidate, input: false);

        if (!string.IsNullOrWhiteSpace(selected.DevicePath) &&
            PathsReferToSameInstance(candidate.DevicePath, selected.DevicePath))
        {
            score += 20;
        }

        if (lowered.Contains("game controller") || lowered.Contains("gamepad") || lowered.Contains("wireless controller"))
        {
            score += 4;
        }

        if (inputLength >= 64)
        {
            score += 4;
        }

        int expectedOutput = selected.ConnectionType == ConnectionType.Bluetooth
            ? Ds4OutputReportBuilder.BluetoothOutputReportLength
            : Ds4OutputReportBuilder.UsbOutputReportLength;

        if (outputLength == expectedOutput)
        {
            score += 10;
        }
        else if (outputLength >= expectedOutput)
        {
            score += 6;
        }
        else if (outputLength == 0)
        {
            score -= 8;
        }
        else
        {
            score -= 3;
        }

        if (lowered.Contains("audio") || lowered.Contains("headset") || lowered.Contains("speaker") || lowered.Contains("microphone"))
        {
            score -= 6;
        }

        return score;
    }

    private static int OutputCompatibilityScore(HidDevice candidate, int reportLength)
    {
        int outputLength = SafeGetReportLength(candidate, input: false);
        if (outputLength == reportLength)
        {
            return 100;
        }
        if (outputLength > reportLength)
        {
            return 70 - Math.Min(50, outputLength - reportLength);
        }
        return outputLength <= 0 ? -100 : -50;
    }

    private static int SafeGetReportLength(HidDevice? device, bool input)
    {
        if (device is null)
        {
            return 0;
        }

        try
        {
            return input ? device.GetMaxInputReportLength() : device.GetMaxOutputReportLength();
        }
        catch
        {
            return 0;
        }
    }

    private static string SafeGetProductName(HidDevice device)
    {
        try
        {
            return device.GetProductName() ?? "Unnamed HID device";
        }
        catch
        {
            return "Unnamed HID device";
        }
    }

    private static bool PathsReferToSameInstance(string? hidPath, string? pnpPath)
    {
        if (string.IsNullOrWhiteSpace(hidPath) || string.IsNullOrWhiteSpace(pnpPath))
        {
            return false;
        }

        static string Normalize(string value) => value
            .Replace('#', '\\')
            .TrimStart('\\', '?')
            .ToUpperInvariant();

        string normalizedHid = Normalize(hidPath);
        string normalizedPnp = Normalize(pnpPath);
        return normalizedHid.Contains(normalizedPnp, StringComparison.Ordinal) ||
               normalizedPnp.Contains(normalizedHid, StringComparison.Ordinal);
    }

    private static int ParseHexId(string? value)
    {
        return value is not null && int.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out int parsed)
            ? parsed
            : -1;
    }

    private static string BuildNoDeviceMessage(ControllerDevice device)
    {
        StringBuilder message = new();
        message.Append("No HID interface was found for the selected device.");

        if (device.VendorId is not null || device.ProductId is not null)
        {
            message.Append($" Tried VID {device.VendorId ?? "?"}/PID {device.ProductId ?? "?"}.");
        }

        message.Append(" The controller may be asleep, unplugged, or its driver may expose only non-HID interfaces.");
        return message.ToString();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
        _stream?.Dispose();
        _stream = null;
    }
}
