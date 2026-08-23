using PadScope.Core.Input;
using PadScope.Core.Models;
using Xunit;

namespace PadScope.Tests;

public class Ds4ControllerOutputStateTests
{
    [Fact]
    public void StandardUsbHeader_UsesProtocolFeatureAndSecondaryBytes()
    {
        byte[] report = Ds4OutputReportBuilder.BuildOutputReport(
            ConnectionType.Usb,
            rumbleSmall: 10,
            rumbleLarge: 20,
            red: 30,
            green: 40,
            blue: 50);

        Assert.Equal(0x05, report[0]);
        Assert.Equal(0x07, report[1]);
        Assert.Equal(0x04, report[2]);
        Assert.Equal(10, report[4]);
        Assert.Equal(20, report[5]);
        Assert.Equal(30, report[6]);
        Assert.Equal(40, report[7]);
        Assert.Equal(50, report[8]);
    }
}
