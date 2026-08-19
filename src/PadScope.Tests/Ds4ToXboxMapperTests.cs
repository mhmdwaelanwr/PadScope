using PadScope.Core.Input;
using Xunit;

namespace PadScope.Tests;

public class Ds4ToXboxMapperTests
{
    private static byte[] BuildUsbReport()
    {
        byte[] report = new byte[64];
        report[0] = Ds4ReportParser.UsbReportId;
        return report;
    }

    [Fact]
    public void NeutralState_MapsToNeutralXbox()
    {
        Xbox360InputState x = Ds4ToXboxMapper.Map(Ds4ReportParser.Parse(BuildUsbReport()));

        Assert.Equal((short)0, x.LeftThumbX);
        Assert.Equal((short)0, x.LeftThumbY);
        Assert.Equal(0, x.LeftTrigger);
        Assert.False(x.A);
        Assert.False(x.DpadUp);
        Assert.False(x.Guide);
    }

    [Fact]
    public void FaceButtons_MapToXInput()
    {
        byte[] report = BuildUsbReport();
        report[7] = 0x0F; // Square, Cross, Circle, Triangle

        Xbox360InputState x = Ds4ToXboxMapper.Map(Ds4ReportParser.Parse(report));

        Assert.True(x.X); // Square
        Assert.True(x.A); // Cross
        Assert.True(x.B); // Circle
        Assert.True(x.Y); // Triangle
    }

    [Fact]
    public void SystemAndMenuButtons_MapToXInput()
    {
        byte[] report = BuildUsbReport();
        report[8] = 0x30; // Share, Options
        report[9] = 0x01; // PS

        Xbox360InputState x = Ds4ToXboxMapper.Map(Ds4ReportParser.Parse(report));

        Assert.True(x.Back);
        Assert.True(x.Start);
        Assert.True(x.Guide);
    }

    [Fact]
    public void DpadDirection_IsCarried()
    {
        byte[] report = BuildUsbReport();
        report[8] = 0x03; // Down + Right

        Xbox360InputState x = Ds4ToXboxMapper.Map(Ds4ReportParser.Parse(report));

        Assert.True(x.DpadDown);
        Assert.True(x.DpadRight);
        Assert.False(x.DpadUp);
        Assert.False(x.DpadLeft);
    }

    [Fact]
    public void StickCenter_MapsToZero()
    {
        Xbox360InputState x = Ds4ToXboxMapper.Map(Ds4ReportParser.Parse(BuildUsbReport()));

        Assert.Equal((short)0, x.LeftThumbX);
        Assert.Equal((short)0, x.RightThumbY);
    }

    [Fact]
    public void StickMax_MapsToPositiveShort()
    {
        byte[] report = BuildUsbReport();
        report[1] = 0xFF; // LeftStickX max

        Xbox360InputState x = Ds4ToXboxMapper.Map(Ds4ReportParser.Parse(report));

        Assert.Equal((short)32639, x.LeftThumbX);
    }

    [Fact]
    public void StickMin_MapsToShortMin()
    {
        byte[] report = BuildUsbReport();
        report[1] = 0x00; // LeftStickX min

        Xbox360InputState x = Ds4ToXboxMapper.Map(Ds4ReportParser.Parse(report));

        Assert.Equal(short.MinValue, x.LeftThumbX);
    }

    [Fact]
    public void StickY_IsInverted_ForXInputConvention()
    {
        byte[] report = BuildUsbReport();
        report[2] = 0x00; // LeftStickY pushed up on DS4 (byte 0)

        Xbox360InputState x = Ds4ToXboxMapper.Map(Ds4ReportParser.Parse(report));

        Assert.Equal(short.MaxValue, x.LeftThumbY);

        report[2] = 0xFF; // LeftStickY pushed down on DS4 (byte 255)
        x = Ds4ToXboxMapper.Map(Ds4ReportParser.Parse(report));

        Assert.Equal((short)(-32639), x.LeftThumbY);
    }

    [Fact]
    public void Triggers_MapDirectly()
    {
        byte[] report = BuildUsbReport();
        report[5] = 200;
        report[6] = 50;

        Xbox360InputState x = Ds4ToXboxMapper.Map(Ds4ReportParser.Parse(report));

        Assert.Equal(200, x.LeftTrigger);
        Assert.Equal(50, x.RightTrigger);
    }
}