using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PadScope.Core.Models;

namespace PadScope.Core.Diagnostics;

public sealed record HidCaptureFrame(
    long OffsetMicroseconds,
    int ReportId,
    string DataBase64,
    string Sha256);

public sealed record HidCaptureDocument(
    int FormatVersion,
    DateTimeOffset CapturedAt,
    ControllerDevice Device,
    IReadOnlyList<HidCaptureFrame> Frames,
    ReportTimingSnapshot? Timing);

public sealed class HidCaptureRecorder
{
    public const int MaximumFrames = 50_000;
    public const int MaximumReportBytes = 1_024;

    private readonly object _sync = new();
    private readonly ControllerDevice _device;
    private readonly DateTimeOffset _capturedAt;
    private readonly List<HidCaptureFrame> _frames = new();
    private DateTimeOffset? _firstReportAt;

    public HidCaptureRecorder(ControllerDevice device, DateTimeOffset? capturedAt = null)
    {
        _device = device;
        _capturedAt = capturedAt ?? DateTimeOffset.UtcNow;
    }

    public int Count
    {
        get { lock (_sync) return _frames.Count; }
    }

    public bool IsFull
    {
        get { lock (_sync) return _frames.Count >= MaximumFrames; }
    }

    public bool TryAdd(ReadOnlySpan<byte> data, int reportId, DateTimeOffset timestamp)
    {
        if (data.Length is <= 0 or > MaximumReportBytes ||
            reportId is < 0 or > 255 ||
            reportId != data[0])
        {
            return false;
        }

        byte[] copy = data.ToArray();
        lock (_sync)
        {
            if (_frames.Count >= MaximumFrames)
            {
                return false;
            }

            _firstReportAt ??= timestamp;
            long offset = Math.Max(0, (timestamp - _firstReportAt.Value).Ticks / 10);
            if (_frames.Count > 0)
            {
                offset = Math.Max(offset, _frames[^1].OffsetMicroseconds);
            }

            _frames.Add(new HidCaptureFrame(
                offset,
                reportId,
                Convert.ToBase64String(copy),
                Convert.ToHexString(SHA256.HashData(copy))));
            return true;
        }
    }

    public HidCaptureDocument CreateDocument(ReportTimingSnapshot? timing = null)
    {
        lock (_sync)
        {
            return new HidCaptureDocument(1, _capturedAt, _device, _frames.ToArray(), timing);
        }
    }
}

public static class HidCaptureStore
{
    public const long MaximumFileBytes = 64 * 1024 * 1024;
    public const long MaximumDurationMicroseconds = 24L * 60 * 60 * 1_000_000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static void Save(string path, HidCaptureDocument capture)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Validate(capture);
        string json = JsonSerializer.Serialize(capture, JsonOptions);
        if (Encoding.UTF8.GetByteCount(json) > MaximumFileBytes)
        {
            throw new InvalidDataException("HID capture exceeds the 64 MiB safety limit.");
        }

        File.WriteAllText(path, json);
    }

    public static HidCaptureDocument Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        FileInfo file = new(path);
        if (!file.Exists)
        {
            throw new FileNotFoundException("HID capture file was not found.", path);
        }

        if (file.Length > MaximumFileBytes)
        {
            throw new InvalidDataException("HID capture exceeds the 64 MiB safety limit.");
        }

        HidCaptureDocument capture = JsonSerializer.Deserialize<HidCaptureDocument>(
            File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidDataException("HID capture is empty or invalid.");
        Validate(capture);
        return capture;
    }

    public static byte[] DecodeFrame(HidCaptureFrame frame)
    {
        if (string.IsNullOrWhiteSpace(frame.DataBase64) || string.IsNullOrWhiteSpace(frame.Sha256))
        {
            throw new InvalidDataException("A capture frame is missing data or its integrity hash.");
        }

        byte[] data;
        try
        {
            data = Convert.FromBase64String(frame.DataBase64);
        }
        catch (FormatException ex)
        {
            throw new InvalidDataException("A capture frame contains invalid Base64 data.", ex);
        }

        if (data.Length is <= 0 or > HidCaptureRecorder.MaximumReportBytes)
        {
            throw new InvalidDataException("A capture frame has an invalid report length.");
        }

        string actualHash = Convert.ToHexString(SHA256.HashData(data));
        if (!actualHash.Equals(frame.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("A capture frame failed its SHA-256 integrity check.");
        }

        return data;
    }

    public static void Validate(HidCaptureDocument capture)
    {
        ArgumentNullException.ThrowIfNull(capture);
        if (capture.FormatVersion != 1)
        {
            throw new InvalidDataException($"Unsupported HID capture format version {capture.FormatVersion}.");
        }

        if (capture.Device is null || capture.Frames is null)
        {
            throw new InvalidDataException("HID capture metadata is incomplete.");
        }

        if (capture.Frames.Count > HidCaptureRecorder.MaximumFrames)
        {
            throw new InvalidDataException("HID capture contains too many frames.");
        }

        long previousOffset = 0;
        foreach (HidCaptureFrame frame in capture.Frames)
        {
            if (frame.OffsetMicroseconds < previousOffset ||
                frame.OffsetMicroseconds > MaximumDurationMicroseconds)
            {
                throw new InvalidDataException("Capture timestamps must be monotonic and within 24 hours.");
            }

            byte[] data = DecodeFrame(frame);
            if (frame.ReportId is < 0 or > 255 || frame.ReportId != data[0])
            {
                throw new InvalidDataException("A capture frame has an inconsistent report ID.");
            }

            previousOffset = frame.OffsetMicroseconds;
        }
    }
}
