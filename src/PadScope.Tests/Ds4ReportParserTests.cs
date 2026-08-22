using PadScope.Core.Input;
using Xunit;

namespace PadScope.Tests;

public class Ds4ReportParserTests
{
    private static byte[] BuildReport(bool bluetooth, Action<byte[], int>? mutate = null)
    {
        byte[] report = new byte[bluetooth
            ? Ds4ReportParser.BluetoothReportLength
            : Ds4ReportParser.UsbReportLength];
        report[0] = bluetooth
            ? (byte)Ds4ReportParser.BluetoothReportId
            : (byte)Ds4ReportParser.UsbReportId;
        int commonOffset = bluetooth ? 3 : 1;
        report[commonOffset + 4] = 0x08; // d-pad released
        mutate?.Invoke(report, commonOffset);
        return report;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FullReport_ParsesCommonStateAtTransportOffset(bool bluetooth)
    {
        byte[] report = BuildReport(bluetooth, (bytes, common) =>
        {
            bytes[common] = 0x11;
            bytes[common + 1] = 0x22;
            bytes[common + 2] = 0x33;
            bytes[common + 3] = 0x44;
            bytes[common + 7] = 0x55;
            bytes[common + 8] = 0x66;
            bytes[common + 4] = 0x08 | 0x20 | 0x80; // Cross + Triangle
            bytes[common + 5] = 0x01 | 0x20; // L1 + Options
            bytes[common + 6] = 0x03; // PS + touch click
        });

        Ds4InputState state = Ds4ReportParser.Parse(report);

        Assert.Equal(0x11, state.LeftStickX);
        Assert.Equal(0x22, state.LeftStickY);
        Assert.Equal(0x33, state.RightStickX);
        Assert.Equal(0x44, state.RightStickY);
        Assert.Equal(0x55, state.LeftTrigger);
        Assert.Equal(0x66, state.RightTrigger);
        Assert.True(state.Buttons.HasFlag(Ds4Buttons.Cross));
        Assert.True(state.Buttons.HasFlag(Ds4Buttons.Triangle));
        Assert.True(state.Buttons.HasFlag(Ds4Buttons.L1));
        Assert.True(state.Buttons.HasFlag(Ds4Buttons.Options));
        Assert.True(state.Buttons.HasFlag(Ds4Buttons.Ps));
        Assert.True(state.Buttons.HasFlag(Ds4Buttons.TouchpadClick));
        Assert.False(state.Buttons.HasFlag(Ds4Buttons.DpadUp));
    }

    [Fact]
    public void DpadDiagonal_MapsFromFirstButtonNibble()
    {
        byte[] report = BuildReport(false, (bytes, common) => bytes[common + 4] = 0x03);
        Ds4InputState state = Ds4ReportParser.Parse(report);
        Assert.True(state.Buttons.HasFlag(Ds4Buttons.DpadDown));
        Assert.True(state.Buttons.HasFlag(Ds4Buttons.DpadRight));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ImuBatteryAndTouch_ParseFromFullReport(bool bluetooth)
    {
        byte[] report = BuildReport(bluetooth, (bytes, common) =>
        {
            WriteInt16(bytes, common + 12, 0x1234);
            WriteInt16(bytes, common + 20, -5);
            bytes[common + 29] = 0x15;

            int touch = common + 32 + 1;
            bytes[touch] = 0x77; // touch timestamp
            bytes[touch + 1] = 0x01; // active, id 1
            bytes[touch + 2] = 0x34;
            bytes[touch + 3] = 0x12;
            bytes[touch + 4] = 0x56;
            bytes[touch + 5] = 0x82; // inactive, id 2
        });

        Ds4InputState state = Ds4ReportParser.Parse(report);

        Assert.Equal(0x1234, state.GyroX);
        Assert.Equal(-5, state.AccelY);
        Assert.Equal((byte?)5, state.BatteryLevel);
        Assert.True(state.Charging);
        Assert.True(state.Touch1?.Touching);
        Assert.Equal((byte)1, state.Touch1?.FingerId);
        Assert.Equal((ushort)0x234, state.Touch1?.X);
        Assert.Equal((ushort)0x561, state.Touch1?.Y);
        Assert.False(state.Touch2?.Touching);
    }

    [Fact]
    public void BluetoothMinimalReport_ParsesBasicStateWithoutInventingFullState()
    {
        byte[] report = new byte[Ds4ReportParser.BluetoothMinimalReportLength];
        report[0] = Ds4ReportParser.UsbReportId;
        report[1] = 0x80;
        report[5] = 0x28; // Cross + released d-pad

        Ds4InputState state = Ds4ReportParser.Parse(report);

        Assert.Equal(0x80, state.LeftStickX);
        Assert.True(state.Buttons.HasFlag(Ds4Buttons.Cross));
        Assert.Equal(0, state.GyroX);
        Assert.Null(state.BatteryLevel);
        Assert.Null(state.Touch1);
    }

    [Fact]
    public void ShortReport_DoesNotThrow()
    {
        Ds4InputState state = Ds4ReportParser.Parse(new byte[] { 0x01 });
        Assert.Equal(Ds4Buttons.None, state.Buttons);
        Assert.Null(state.Touch1);
    }

    [Theory]
    [InlineData(0x01, 10, true)]
    [InlineData(0x11, 78, true)]
    [InlineData(0x11, 77, false)]
    [InlineData(0x31, 78, false)]
    [InlineData(0x01, 9, false)]
    public void LooksLikeDs4Report_ValidatesIdAndTransportLength(byte reportId, int length, bool expected)
    {
        byte[] report = new byte[length];
        report[0] = reportId;
        Assert.Equal(expected, Ds4ReportParser.LooksLikeDs4Report(report));
    }

    private static void WriteInt16(byte[] report, int offset, short value)
    {
        report[offset] = (byte)value;
        report[offset + 1] = (byte)(value >> 8);
    }
}
