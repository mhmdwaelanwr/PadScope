using System.Security.Cryptography;
using PadScope.Core.Diagnostics;
using PadScope.Core.Models;
using PadScope.Hid;
using Xunit;

namespace PadScope.Tests;

public sealed class HidCaptureTests
{
    [Fact]
    public void Recorder_PreservesReportsOffsetsAndIntegrity()
    {
        HidCaptureRecorder recorder = new(Device(), DateTimeOffset.UnixEpoch);
        DateTimeOffset first = DateTimeOffset.UnixEpoch.AddSeconds(1);

        Assert.True(recorder.TryAdd(new byte[] { 0x01, 0x10 }, 0x01, first));
        Assert.True(recorder.TryAdd(new byte[] { 0x01, 0x20 }, 0x01, first.AddMilliseconds(4)));
        HidCaptureDocument document = recorder.CreateDocument();

        Assert.Equal(2, document.Frames.Count);
        Assert.Equal(0, document.Frames[0].OffsetMicroseconds);
        Assert.Equal(4_000, document.Frames[1].OffsetMicroseconds);
        Assert.Equal(new byte[] { 0x01, 0x20 }, HidCaptureStore.DecodeFrame(document.Frames[1]));
    }

    [Fact]
    public void Validate_RejectsTamperedFrame()
    {
        byte[] original = { 0x01, 0x10 };
        HidCaptureFrame frame = new(
            0,
            1,
            Convert.ToBase64String(new byte[] { 0x01, 0x11 }),
            Convert.ToHexString(SHA256.HashData(original)));
        HidCaptureDocument document = new(1, DateTimeOffset.UnixEpoch, Device(), new[] { frame }, null);

        Assert.Throws<InvalidDataException>(() => HidCaptureStore.Validate(document));
    }

    [Fact]
    public void Validate_RejectsNonMonotonicOffsets()
    {
        HidCaptureRecorder recorder = new(Device());
        recorder.TryAdd(new byte[] { 0x01 }, 1, DateTimeOffset.UnixEpoch);
        HidCaptureFrame valid = recorder.CreateDocument().Frames[0];
        HidCaptureDocument document = new(
            1,
            DateTimeOffset.UnixEpoch,
            Device(),
            new[] { valid with { OffsetMicroseconds = 10 }, valid with { OffsetMicroseconds = 9 } },
            null);

        Assert.Throws<InvalidDataException>(() => HidCaptureStore.Validate(document));
    }

    [Fact]
    public void Store_RoundTripsCapture()
    {
        string path = Path.Combine(Path.GetTempPath(), $"padscope-{Guid.NewGuid():N}.json");
        try
        {
            HidCaptureRecorder recorder = new(Device());
            recorder.TryAdd(new byte[] { 0x01, 0x08 }, 1, DateTimeOffset.UnixEpoch);
            HidCaptureDocument expected = recorder.CreateDocument();

            HidCaptureStore.Save(path, expected);
            HidCaptureDocument actual = HidCaptureStore.Load(path);

            Assert.Equal(expected.Device, actual.Device);
            Assert.Equal(expected.Frames, actual.Frames);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Replay_EmitsFramesAndRefusesOutput()
    {
        HidCaptureRecorder recorder = new(Device());
        DateTimeOffset first = DateTimeOffset.UnixEpoch;
        recorder.TryAdd(new byte[] { 0x01, 0x08 }, 1, first);
        recorder.TryAdd(new byte[] { 0x01, 0x18 }, 1, first.AddMilliseconds(1));
        using RecordedHidInputReader reader = new(recorder.CreateDocument());
        List<byte[]> reports = new();
        using ManualResetEventSlim complete = new();
        reader.ReportReceived += report =>
        {
            reports.Add(report.Data);
            if (reports.Count == 2) complete.Set();
        };

        Assert.True(reader.TryOpen(Device(), out _));
        reader.Start();

        Assert.True(complete.Wait(TimeSpan.FromSeconds(2)));
        Assert.Equal(2, reports.Count);
        Assert.False(reader.TryWriteOutput(new byte[] { 0x05 }, out string? error));
        Assert.Contains("disabled", error, StringComparison.OrdinalIgnoreCase);
    }

    private static ControllerDevice Device() => new(
        "Captured controller", "PadScope", "054C", "09CC", "capture://test", ConnectionType.Usb, "Capture");
}
