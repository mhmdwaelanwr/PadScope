using System.Buffers.Binary;
using PadScope.Core.Models;

namespace PadScope.Core.Input;

public static class Ds4OutputReportBuilder
{
    public const int UsbOutputReportId = 0x05;
    public const int BluetoothOutputReportId = 0x11;
    public const int UsbOutputReportLength = 32;
    public const int BluetoothOutputReportLength = 78;

    private const byte ValidMotor = 0x01;
    private const byte ValidLightbar = 0x02;
    private const byte BluetoothHwControl = 0xC4;
    private const byte OutputCrcSeed = 0xA2;

    public static byte[] BuildOutputReport(
        ConnectionType connectionType,
        byte rumbleSmall = 0,
        byte rumbleLarge = 0,
        byte red = 0,
        byte green = 0,
        byte blue = 0,
        bool setRumble = true,
        bool setLightbar = true)
    {
        bool bluetooth = connectionType == ConnectionType.Bluetooth;
        byte[] report = new byte[bluetooth ? BluetoothOutputReportLength : UsbOutputReportLength];
        report[0] = bluetooth ? (byte)BluetoothOutputReportId : (byte)UsbOutputReportId;

        int commonOffset;
        if (bluetooth)
        {
            report[1] = BluetoothHwControl;
            report[2] = 0;
            commonOffset = 3;
        }
        else
        {
            commonOffset = 1;
        }

        byte validFlags = 0;
        if (setRumble) validFlags |= ValidMotor;
        if (setLightbar) validFlags |= ValidLightbar;

        report[commonOffset] = validFlags;
        report[commonOffset + 3] = rumbleSmall;
        report[commonOffset + 4] = rumbleLarge;
        report[commonOffset + 5] = red;
        report[commonOffset + 6] = green;
        report[commonOffset + 7] = blue;

        if (bluetooth)
        {
            uint crc = ComputeCrc32(OutputCrcSeed, report.AsSpan(0, report.Length - sizeof(uint)));
            BinaryPrimitives.WriteUInt32LittleEndian(report.AsSpan(report.Length - sizeof(uint)), crc);
        }

        return report;
    }

    public static uint ComputeCrc32(byte seed, ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFF;
        crc = UpdateCrc32(crc, seed);
        foreach (byte value in data)
        {
            crc = UpdateCrc32(crc, value);
        }

        return ~crc;
    }

    private static uint UpdateCrc32(uint crc, byte value)
    {
        crc ^= value;
        for (int bit = 0; bit < 8; bit++)
        {
            crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
        }

        return crc;
    }
}
