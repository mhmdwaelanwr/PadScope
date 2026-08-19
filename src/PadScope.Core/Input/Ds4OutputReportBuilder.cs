using PadScope.Core.Models;

namespace PadScope.Core.Input;

public static class Ds4OutputReportBuilder
{
    public const int UsbOutputReportId = 0x05;
    public const int BluetoothOutputReportId = 0x11;

    public static byte[] BuildOutputReport(
        ConnectionType connectionType,
        byte rumbleSmall = 0x00,
        byte rumbleLarge = 0x00,
        byte red = 0x00,
        byte green = 0x00,
        byte blue = 0x00,
        int reportLength = 64)
    {
        if (reportLength < 8)
        {
            reportLength = 8;
        }

        byte[] report = new byte[reportLength];

        report[0] = connectionType == ConnectionType.Bluetooth
            ? BluetoothOutputReportId
            : UsbOutputReportId;

        report[1] = rumbleSmall;
        report[2] = rumbleLarge;
        report[3] = red;
        report[4] = green;
        report[5] = blue;
        report[6] = 0x00;
        report[7] = 0x00;

        return report;
    }
}