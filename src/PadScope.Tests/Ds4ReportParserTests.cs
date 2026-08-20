using PadScope.Core.Input;
using Xunit;

namespace PadScope.Tests;

public class Ds4ReportParserTests
{
    private static byte[] BuildUsbReport(Action<byte[]>? mutate = null)
    {
        byte[] report = new byte[64];
        report[0] = Ds4ReportParser.UsbReportId;
        mutate?.Invoke(report);
        return report;
    }

    [Fact]
    public void NeutralReport_SticksCenter_TriggersZero_NoButtons()
    {
        byte[] report = BuildUsbReport(report =>
        {
            report[1] = 0x80;
            report[2] = 0x80;
            report[3] = 0x80;
            report[4] = 0x80;
            report[8] = 0x08;
        });

        Ds4InputState state = Ds4ReportParser.Parse(report);

        Assert.Equal(0x80, state.LeftStickX);
        Assert.Equal(0x80, state.LeftStickY);
        Assert.Equal(0x80, state.RightStickX);
        Assert.Equal(0x80, state.RightStickY);
        Assert.Equal(0, state.LeftTrigger);
        Assert.Equal(0, state.RightTrigger);
        Assert.Equal(Ds4Buttons.None, state.Buttons);
        Assert.Equal(0f, state.LeftStickXNorm, 2);
    }

    [Fact]
    public void FaceButtons_MappedFromButtons1Byte()
    {
        byte[] report = BuildUsbReport(report =>
        {
            report[7] = 0x01 | 0x08 | 0x20;
        });

        Ds4InputState state = Ds4ReportParser.Parse(report);

        Assert.True(state.Buttons.HasFlag(Ds4Buttons.Square));
        Assert.True(state.Buttons.HasFlag(Ds4Buttons.Triangle));
        Assert.True(state.Buttons.HasFlag(Ds4Buttons.R1));
        Assert.False(state.Buttons.HasFlag(Ds4Buttons.Cross));
        Assert.False(state.Buttons.HasFlag(Ds4Buttons.Circle));
    }

    [Fact]
    public void TriggersAndShoulderButtons_MappedFromButtons1HighBits()
    {
        byte[] report = BuildUsbReport(report =>
        {
            report[7] = 0xC0;
        });

        Ds4InputState state = Ds4ReportParser.Parse(report);

        Assert.True(state.Buttons.HasFlag(Ds4Buttons.L2));
        Assert.True(state.Buttons.HasFlag(Ds4Buttons.R2));
    }

    [Fact]
    public void Dpad_Up_FromLowNibble()
    {
        byte[] report = BuildUsbReport(report =>
        {
            report[8] = 0x00;
        });

        Ds4InputState state = Ds4ReportParser.Parse(report);
        Assert.True(state.Buttons.HasFlag(Ds4Buttons.DpadUp));
    }

    [Fact]
    public void Dpad_DownRight_FromLowNibble()
    {
        byte[] report = BuildUsbReport(report =>
        {
            report[8] = 0x03;
        });

        Ds4InputState state = Ds4ReportParser.Parse(report);
        Assert.True(state.Buttons.HasFlag(Ds4Buttons.DpadDown));
        Assert.True(state.Buttons.HasFlag(Ds4Buttons.DpadRight));
    }

    [Fact]
    public void Dpad_Released_HighNibbleStillReadsOtherButtons()
    {
        byte[] report = BuildUsbReport(report =>
        {
            report[8] = 0x38;
        });

        Ds4InputState state = Ds4ReportParser.Parse(report);
        Assert.False(state.Buttons.HasFlag(Ds4Buttons.DpadUp));
        Assert.True(state.Buttons.HasFlag(Ds4Buttons.Share));
        Assert.True(state.Buttons.HasFlag(Ds4Buttons.Options));
    }

    [Fact]
    public void PsAndTouchpadClick_FromButtons3Byte()
    {
        byte[] report = BuildUsbReport(report =>
        {
            report[9] = 0x03;
        });

        Ds4InputState state = Ds4ReportParser.Parse(report);
        Assert.True(state.Buttons.HasFlag(Ds4Buttons.Ps));
        Assert.True(state.Buttons.HasFlag(Ds4Buttons.TouchpadClick));
    }

    [Fact]
    public void GyroAndAccel_ReadAsLittleEndianInt16()
    {
        byte[] report = BuildUsbReport(report =>
        {
            short gyroX = 0x1234;
            short accelY = -5;

            report[19] = (byte)(gyroX & 0xFF);
            report[20] = (byte)((gyroX >> 8) & 0xFF);

            report[27] = (byte)(accelY & 0xFF);
            report[28] = (byte)((accelY >> 8) & 0xFF);
        });

        Ds4InputState state = Ds4ReportParser.Parse(report);

        Assert.Equal(0x1234, state.GyroX);
        Assert.Equal(-5, state.AccelY);
    }

    [Fact]
    public void Touchpad_ReadsPoint_WhenTouching()
    {
        byte[] report = BuildUsbReport(report =>
        {
            // Touch base offset for USB reports is 35.
            report[35] = 0x00;
            report[36] = 0x00;
            report[37] = 0x00;
            report[38] = 0x80 | 0x01;
            report[39] = 0x34; // X low byte
            report[40] = 0x12; // X high nibble = 1, Y high nibble = 2
            report[41] = 0x56; // Y low byte
        });

        Ds4InputState state = Ds4ReportParser.Parse(report);

        Assert.NotNull(state.Touch1);
        Assert.True(state.Touch1.Value.Touching);
        Assert.Equal(0x234, state.Touch1.Value.X);
        Assert.Equal(0x156, state.Touch1.Value.Y);
        Assert.Equal(1, state.Touch1.Value.FingerId);
    }

    [Fact]
    public void Battery_ParsedFromUsbReport()
    {
        byte[] report = BuildUsbReport(report =>
        {
            report[15] = 0x14 | 0x05; // charging bit set, level 5
        });

        Ds4InputState state = Ds4ReportParser.Parse(report);

        Assert.Equal((byte?)5, state.BatteryLevel);
        Assert.True(state.Charging);
    }

    [Fact]
    public void BluetoothReport_IdRecognized_BatteryNotGuessed()
    {
        byte[] report = new byte[78];
        report[0] = Ds4ReportParser.BluetoothReportId;

        Ds4InputState state = Ds4ReportParser.Parse(report);

        Assert.Equal(Ds4ReportParser.BluetoothReportId, state.ReportId);
        Assert.Null(state.BatteryLevel);
        Assert.False(state.Charging);
    }

    [Fact]
    public void ShortReport_DoesNotThrow()
    {
        byte[] report = new byte[] { 0x01 };

        Ds4InputState state = Ds4ReportParser.Parse(report);

        Assert.Equal(Ds4Buttons.None, state.Buttons);
        Assert.Equal(0, state.GyroX);
        Assert.Null(state.Touch1);
    }

    [Theory]
    [InlineData(0x01, true)]
    [InlineData(0x11, true)]
    [InlineData(0x31, false)]
    [InlineData(0x00, false)]
    public void LooksLikeDs4Report_ChecksReportId(byte reportId, bool expected)
    {
        byte[] report = new byte[16];
        report[0] = reportId;

        Assert.Equal(expected, Ds4ReportParser.LooksLikeDs4Report(report));
    }

    [Fact]
    public void LooksLikeDs4Report_RejectsTinyBuffer()
    {
        Assert.False(Ds4ReportParser.LooksLikeDs4Report(new byte[] { 0x01 }));
    }
}