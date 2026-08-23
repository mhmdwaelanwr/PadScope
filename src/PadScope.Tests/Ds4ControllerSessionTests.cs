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
    public void Output_UsesTransportSpecificPacket(ConnectionType connection, int length, int reportId)
    {
        FakeHidInputReader reader = new();
        using Ds4ControllerSession session = new(reader, Device(connection));
        session.TryStart(out _);

        Assert.True(session.TrySendRumble(7, 11, out string? error));

        Assert.Null(error);
        byte[] packet = Assert.Single(reader.Writes);
        Assert.Equal(length, packet.Length);
        Assert.Equal(reportId, packet[0]);
        int common = connection == ConnectionType.Bluetooth ? 3 : 1;
        Assert.Equal(0x07, packet[common]);
        Assert.Equal(0x04, packet[common + 1]);
        Assert.Equal(7, packet[common + 3]);
        Assert.Equal(11, packet[common + 4]);
    }

    [Fact]
    public void ValidBluetoothInput_OverridesMisclassifiedUsbForOutput()
    {
        FakeHidInputReader reader = new();
        using Ds4ControllerSession session = new(reader, Device(ConnectionType.Usb));
        session.TryStart(out _);

        byte[] report = BluetoothReport();
        WriteBluetoothInputCrc(report);
        reader.Inject(report, DateTimeOffset.UnixEpoch);

        Assert.Equal(ConnectionType.Bluetooth, session.EffectiveConnectionType);
        Assert.True(session.TrySendRumble(12, 34, out _));
        byte[] packet = Assert.Single(reader.Writes);
        Assert.Equal(Ds4OutputReportBuilder.BluetoothOutputReportId, packet[0]);
        Assert.Equal(Ds4OutputReportBuilder.BluetoothOutputReportLength, packet.Length);
    }

    [Fact]
    public void ValidUsbInput_OverridesMisclassifiedBluetoothForOutput()
    {
        FakeHidInputReader reader = new();
        using Ds4ControllerSession session = new(reader, Device(ConnectionType.Bluetooth));
        session.TryStart(out _);

        reader.Inject(UsbReport(), DateTimeOffset.UnixEpoch);

        Assert.Equal(ConnectionType.Usb, session.EffectiveConnectionType);
        Assert.True(session.TrySendRumble(12, 34, out _));
        byte[] packet = Assert.Single(reader.Writes);
        Assert.Equal(Ds4OutputReportBuilder.UsbOutputReportId, packet[0]);
        Assert.Equal(Ds4OutputReportBuilder.UsbOutputReportLength, packet.Length);
    }

    [Fact]
    public void InvalidBluetoothCrc_DoesNotChangeEffectiveTransport()
    {
        FakeHidInputReader reader = new();
        using Ds4ControllerSession session = new(reader, Device(ConnectionType.Usb));
        session.TryStart(out _);

        reader.Inject(BluetoothReport(), DateTimeOffset.UnixEpoch);

        Assert.Equal(ConnectionType.Usb, session.EffectiveConnectionType);
    }

    [Fact]
    public void ResetRumble_UsesStandardHeaderAndNeutralMotors()
    {
        FakeHidInputReader reader = new();
        using Ds4ControllerSession session = new(reader, Device(ConnectionType.Usb));
        session.TryStart(out _);

        Assert.True(session.TryResetRumble(out string? error));

        Assert.Null(error);
        byte[] packet = Assert.Single(reader.Writes);
        Assert.Equal(0x07, packet[1]);
        Assert.Equal(0x04, packet[2]);
        Assert.Equal(0, packet[4]);
        Assert.Equal(0, packet[5]);
    }

    [Fact]
    public void ResetRumble_PreservesPreviouslySetLightbar()
    {
        FakeHidInputReader reader = new();
        using Ds4ControllerSession session = new(reader, Device(ConnectionType.Usb));
        session.TryStart(out _);

        Assert.True(session.TrySendLightbar(0x11, 0x22, 0x33, out _));
        Assert.True(session.TryResetRumble(out _));

        Assert.Equal(2, reader.Writes.Count);
        byte[] reset = reader.Writes[1];
        Assert.Equal(0, reset[4]);
        Assert.Equal(0, reset[5]);
        Assert.Equal(0x11, reset[6]);
        Assert.Equal(0x22, reset[7]);
        Assert.Equal(0x33, reset[8]);
    }

    [Fact]
    public void ValidatedReports_ExposeRawIntervalsForPollingDiagnostics()
    {
        FakeHidInputReader reader = new();
        using Ds4ControllerSession session = new(reader, Device(ConnectionType.Usb));
        session.TryStart(out _);

        reader.Inject(UsbReport(), DateTimeOffset.UnixEpoch);
        reader.Inject(UsbReport(), DateTimeOffset.UnixEpoch.AddMilliseconds(10));
        reader.Inject(UsbReport(), DateTimeOffset.UnixEpoch.AddMilliseconds(20.5));

        IReadOnlyList<double> intervals = session.DrainReportIntervals();
        Assert.Equal(2, intervals.Count);
        Assert.Equal(10.0, intervals[0], 3);
        Assert.Equal(10.5, intervals[1], 3);
        Assert.Empty(session.DrainReportIntervals());
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
