using System.Buffers.Binary;
using PadScope.Core.Input;
using PadScope.Core.Models;
using Xunit;

namespace PadScope.Tests;

public class Ds4OutputReportBuilderTests
{
    [Fact]
    public void UsbReport_HasStandardProtocolHeaderAndPayload()
    {
        byte[] report = Ds4OutputReportBuilder.BuildOutputReport(
            ConnectionType.Usb,
            rumbleSmall: 0x12,
            rumbleLarge: 0x34,
            red: 0x56,
            green: 0x78,
            blue: 0x9A);

        Assert.Equal(32, report.Length);
        Assert.Equal(0x05, report[0]);
        Assert.Equal(0x07, report[1]);
        Assert.Equal(0x04, report[2]);
        Assert.Equal(0x12, report[4]);
        Assert.Equal(0x34, report[5]);
        Assert.Equal(0x56, report[6]);
        Assert.Equal(0x78, report[7]);
        Assert.Equal(0x9A, report[8]);
    }

    [Fact]
    public void BluetoothReport_HasStandardHeaderPayloadAndCrc()
    {
        byte[] report = Ds4OutputReportBuilder.BuildOutputReport(
            ConnectionType.Bluetooth,
            rumbleSmall: 0x12,
            rumbleLarge: 0x34,
            red: 0x56,
            green: 0x78,
            blue: 0x9A);

        Assert.Equal(78, report.Length);
        Assert.Equal(0x11, report[0]);
        Assert.Equal(0xC0, report[1]);
        Assert.Equal(0x00, report[2]);
        Assert.Equal(0x07, report[3]);
        Assert.Equal(0x04, report[4]);
        Assert.Equal(0x12, report[6]);
        Assert.Equal(0x34, report[7]);
        Assert.Equal(0x56, report[8]);
        Assert.Equal(0x78, report[9]);
        Assert.Equal(0x9A, report[10]);
        Assert.Equal(0x930184A9u, BinaryPrimitives.ReadUInt32LittleEndian(report.AsSpan(74)));
    }

    [Fact]
    public void StandardHeader_IsUsedForRumbleOnlyAndLightbarOnlyCalls()
    {
        byte[] rumble = Ds4OutputReportBuilder.BuildOutputReport(
            ConnectionType.Usb, setRumble: true, setLightbar: false);
        byte[] lightbar = Ds4OutputReportBuilder.BuildOutputReport(
            ConnectionType.Usb, setRumble: false, setLightbar: true);

        Assert.Equal(0x07, rumble[1]);
        Assert.Equal(0x04, rumble[2]);
        Assert.Equal(0x07, lightbar[1]);
        Assert.Equal(0x04, lightbar[2]);
    }
}
