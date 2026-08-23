using System.Buffers.Binary;
using PadScope.Core.Input;
using PadScope.Core.Models;
using PadScope.Hid;
using Xunit;

namespace PadScope.Tests;

public sealed class Ds4ControllerSessionTests
{
    [Fact]
    public void TryStart_OpensAndStartsReader()
    {
        FakeHidInputReader reader = new();
        using Ds4ControllerSession session = new(reader, Device(ConnectionType.Usb));

        Assert.True(session.TryStart(out string? error));
        Assert.Null(error);
        Assert.Equal(1, reader.OpenCount);
        Assert.True(reader.IsRunning);
        Assert.False(session.OutputPrimed);
    }

    [Fact]
    public void UsbInput_PublishesParsedStateAndTiming()
    {
        FakeHidInputReader reader = new();
        using Ds4ControllerSession session = new(reader, Device(ConnectionType.Usb));
        List<Ds4InputState> states = new();
        List<int> timingCounts = new();
        session.StateUpdated += states.Add;
        session.TimingUpdated += timing => timingCounts.Add(timing.ReportCount);
        session.TryStart(out _);

        byte[] report = UsbReport();
        report[1] = 0x42;
        reader.Inject(report, DateTimeOffset.UnixEpoch);

        Assert.Single(states);
        Assert.Equal(0x42, states[0].LeftStickX);
        Assert.Equal(new[] { 1 }, timingCounts);
    }

    [Fact]
    public void ValidatedInput_CollectsRawPacketIntervals()
    {
        FakeHidInputReader reader = new();
        using Ds4ControllerSession session = new(reader, Device(ConnectionType.Usb));
        session.TryStart(out _);

        byte[] report = UsbReport();
        reader.Inject(report, DateTimeOffset.UnixEpoch);
        reader.Inject(report, DateTimeOffset.UnixEpoch.AddMilliseconds(10));
        reader.Inject(report, DateTimeOffset.UnixEpoch.AddMilliseconds(21));

        Assert.Equal(new[] { 10d, 11d }, session.DrainReportIntervals());
        Assert.Empty(session.DrainReportIntervals());
    }

    [Fact]
    public void BluetoothInput_RejectsBadCrcAndAcceptsValidCrc()
    {
        FakeHidInputReader reader = new();
        using Ds4ControllerSession session = new(reader, Device(ConnectionType.Bluetooth));
        List<Ds4InputState> states = new();
        session.StateUpdated += states.Add;
        session.TryStart(out _);

        byte[] report = BluetoothReport();
        report[3] = 0x33;
        reader.Inject(report, DateTimeOffset.UnixEpoch);
        Assert.Empty(states);

        WriteBluetoothInputCrc(report);
        reader.Inject(report, DateTimeOffset.UnixEpoch.AddMilliseconds(4));
        Assert.Single(states);
        Assert.Equal(0x33, states[0].LeftStickX);
    }

    [Theory]
    [InlineData(ConnectionType.Usb, Ds4OutputReportBuilder.UsbOutputReportLength, Ds4OutputReportBuilder.UsbOutputReportId)]
    [InlineData(ConnectionType.Bluetooth, Ds4OutputReportBuilder.BluetoothOutputReportLength, Ds4OutputReportBuilder.BluetoothOutputReportId)]
    public void FirstStatefulOutput_PrimesThenSendsRequestedPacket(ConnectionType connection, int length, int reportId)
    {
        FakeHidInputReader reader = new();
        using Ds4ControllerSession session = new(reader, Device(connection));
        session.TryStart(out _);

        Assert.True(session.TrySendRumble(7, 11, out string? error));

        Assert.Null(error);
        Assert.True(session.OutputPrimed);
        Assert.Equal(2, reader.Writes.Count);

        int common = connection == ConnectionType.Bluetooth ? 3 : 1;
        byte[] prime = reader.Writes[0];
        Assert.Equal(length, prime.Length);
        Assert.Equal(reportId, prime[0]);
        Assert.Equal(0, prime[common + 3]);
        Assert.Equal(0, prime[common + 4]);

        byte[] requested = reader.Writes[1];
        Assert.Equal(length, requested.Length);
        Assert.Equal(reportId, requested[0]);
        Assert.Equal(7, requested[common + 3]);
        Assert.Equal(11, requested[common + 4]);
    }

    [Fact]
    public void ResetRumble_PrimesSessionSoNextRumbleNeedsOneWrite()
    {
        FakeHidInputReader reader = new();
        using Ds4ControllerSession session = new(reader, Device(ConnectionType.Usb));
        session.TryStart(out _);

        Assert.True(session.TryResetRumble(out _));
        Assert.True(session.OutputPrimed);
        Assert.Single(reader.Writes);

        Assert.True(session.TrySendRumble(40, 220, out _));
        Assert.Equal(2, reader.Writes.Count);
        Assert.Equal(40, reader.Writes[1][4]);
        Assert.Equal(220, reader.Writes[1][5]);
    }

    [Fact]
    public void FirstLightbarWrite_PrimesThenAppliesRequestedColor()
    {
        FakeHidInputReader reader = new();
        using Ds4ControllerSession session = new(reader, Device(ConnectionType.Usb));
        session.TryStart(out _);

        Assert.True(session.TrySendLightbar(0x22, 0x44, 0x66, out _));

        Assert.Equal(2, reader.Writes.Count);
        Assert.Equal(0, reader.Writes[0][6]);
        Assert.Equal(0, reader.Writes[0][7]);
        Assert.Equal(0, reader.Writes[0][8]);
        Assert.Equal(0x22, reader.Writes[1][6]);
        Assert.Equal(0x44, reader.Writes[1][7]);
        Assert.Equal(0x66, reader.Writes[1][8]);
    }

    [Fact]
    public void Rumble_PreservesCurrentLightbarState()
    {
        FakeHidInputReader reader = new();
        using Ds4ControllerSession session = new(reader, Device(ConnectionType.Usb));
        session.TryStart(out _);

        Assert.True(session.TrySendLightbar(0x22, 0x44, 0x66, out _));
        Assert.True(session.TrySendRumble(0x11, 0x77, out _));

        Assert.Equal(3, reader.Writes.Count);
        byte[] rumblePacket = reader.Writes[^1];
        Assert.Equal(0x11, rumblePacket[4]);
        Assert.Equal(0x77, rumblePacket[5]);
        Assert.Equal(0x22, rumblePacket[6]);
        Assert.Equal(0x44, rumblePacket[7]);
        Assert.Equal(0x66, rumblePacket[8]);
    }

    [Fact]
    public void ResetRumble_StopsMotorsWithoutClearingLightbar()
    {
        FakeHidInputReader reader = new();
        using Ds4ControllerSession session = new(reader, Device(ConnectionType.Usb));
        session.TryStart(out _);

        Assert.True(session.TrySendLightbar(10, 20, 30, out _));
        Assert.True(session.TrySendRumble(90, 180, out _));
        Assert.True(session.TryResetRumble(out _));

        byte[] resetPacket = reader.Writes[^1];
        Assert.Equal(0, resetPacket[4]);
        Assert.Equal(0, resetPacket[5]);
        Assert.Equal(10, resetPacket[6]);
        Assert.Equal(20, resetPacket[7]);
        Assert.Equal(30, resetPacket[8]);
    }

    [Fact]
    public void Dispose_UnsubscribesAndStopsReader()
    {
        FakeHidInputReader reader = new();
        Ds4ControllerSession session = new(reader, Device(ConnectionType.Usb));
        int updates = 0;
        session.StateUpdated += _ => updates++;
        session.TryStart(out _);

        session.Dispose();
        reader.Inject(UsbReport(), DateTimeOffset.UnixEpoch);

        Assert.False(reader.IsRunning);
        Assert.True(reader.Disposed);
        Assert.Equal(0, updates);
    }

    private static ControllerDevice Device(ConnectionType connection) => new(
        "Test controller", "PadScope", "054C", "09CC", "fake://controller", connection, "Test");

    private static byte[] UsbReport()
    {
        byte[] report = new byte[Ds4ReportParser.UsbReportLength];
        report[0] = Ds4ReportParser.UsbReportId;
        report[5] = 0x08;
        return report;
    }

    private static byte[] BluetoothReport()
    {
        byte[] report = new byte[Ds4ReportParser.BluetoothReportLength];
        report[0] = Ds4ReportParser.BluetoothReportId;
        report[7] = 0x08;
        return report;
    }

    private static void WriteBluetoothInputCrc(byte[] report)
    {
        uint crc = Ds4OutputReportBuilder.ComputeCrc32(0xA1, report.AsSpan(0, report.Length - sizeof(uint)));
        BinaryPrimitives.WriteUInt32LittleEndian(report.AsSpan(report.Length - sizeof(uint)), crc);
    }

    private sealed class FakeHidInputReader : IHidInputReader
    {
        public event Action<HidInputReport>? ReportReceived;
        public event Action<string>? ErrorOccurred
        {
            add { }
            remove { }
        }

        public bool IsRunning { get; private set; }
        public string? DeviceDescription => "Fake HID";
        public int MaxOutputReportLength => Ds4OutputReportBuilder.BluetoothOutputReportLength;
        public int OpenCount { get; private set; }
        public bool Disposed { get; private set; }
        public List<byte[]> Writes { get; } = new();

        public bool TryOpen(ControllerDevice device, out string? error)
        {
            OpenCount++;
            error = null;
            return true;
        }

        public void Start() => IsRunning = true;
        public void Stop() => IsRunning = false;

        public bool TryWriteOutput(byte[] report, out string? error)
        {
            Writes.Add(report.ToArray());
            error = null;
            return true;
        }

        public void Inject(byte[] data, DateTimeOffset timestamp) =>
            ReportReceived?.Invoke(new HidInputReport(data, data[0], timestamp));

        public void Dispose()
        {
            IsRunning = false;
            Disposed = true;
            ReportReceived = null;
        }
    }
}
