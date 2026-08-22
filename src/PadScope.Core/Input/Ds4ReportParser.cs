namespace PadScope.Core.Input;

public static class Ds4ReportParser
{
    public const int UsbReportId = 0x01;
    public const int BluetoothReportId = 0x11;
    public const int UsbReportLength = 64;
    public const int BluetoothReportLength = 78;
    public const int BluetoothMinimalReportLength = 10;

    private const int UsbCommonOffset = 1;
    private const int BluetoothCommonOffset = 3;
    private const int CommonDataLength = 32;
    private const int TouchReportLength = 9;

    public static Ds4InputState Parse(byte[] report)
    {
        ArgumentNullException.ThrowIfNull(report);
        int reportId = report.Length > 0 ? report[0] : 0;
        bool fullBluetooth = reportId == BluetoothReportId && report.Length >= BluetoothReportLength;
        int commonOffset = fullBluetooth ? BluetoothCommonOffset : UsbCommonOffset;
        bool hasFullState = report.Length >= commonOffset + CommonDataLength;
        Ds4Buttons buttons = ReadButtons(report, commonOffset);

        Ds4TouchPoint? touch1 = null;
        Ds4TouchPoint? touch2 = null;
        if (hasFullState)
        {
            int touchReportOffset = commonOffset + CommonDataLength + 1;
            if (touchReportOffset + TouchReportLength <= report.Length)
            {
                touch1 = ReadTouchPoint(report, touchReportOffset + 1);
                touch2 = ReadTouchPoint(report, touchReportOffset + 5);
            }
        }

        (byte? batteryLevel, bool charging) = hasFullState
            ? ReadBattery(report, commonOffset)
            : (null, false);

        return new Ds4InputState
        {
            Raw = report,
            ReportId = reportId,
            Buttons = buttons,
            LeftStickX = ByteAt(report, commonOffset),
            LeftStickY = ByteAt(report, commonOffset + 1),
            RightStickX = ByteAt(report, commonOffset + 2),
            RightStickY = ByteAt(report, commonOffset + 3),
            LeftTrigger = ByteAt(report, commonOffset + 7),
            RightTrigger = ByteAt(report, commonOffset + 8),
            GyroX = hasFullState ? Int16LeAt(report, commonOffset + 12) : (short)0,
            GyroY = hasFullState ? Int16LeAt(report, commonOffset + 14) : (short)0,
            GyroZ = hasFullState ? Int16LeAt(report, commonOffset + 16) : (short)0,
            AccelX = hasFullState ? Int16LeAt(report, commonOffset + 18) : (short)0,
            AccelY = hasFullState ? Int16LeAt(report, commonOffset + 20) : (short)0,
            AccelZ = hasFullState ? Int16LeAt(report, commonOffset + 22) : (short)0,
            Touch1 = touch1,
            Touch2 = touch2,
            BatteryLevel = batteryLevel,
            Charging = charging
        };
    }

    public static bool LooksLikeDs4Report(byte[] report)
    {
        if (report is null) return false;
        return (report.Length >= BluetoothMinimalReportLength && report[0] == UsbReportId) ||
               (report.Length >= BluetoothReportLength && report[0] == BluetoothReportId);
    }

    private static Ds4Buttons ReadButtons(byte[] report, int commonOffset)
    {
        if (report.Length < commonOffset + 7) return Ds4Buttons.None;
        byte buttons1 = report[commonOffset + 4];
        byte buttons2 = report[commonOffset + 5];
        byte buttons3 = report[commonOffset + 6];
        Ds4Buttons result = Ds4Buttons.None;

        if ((buttons1 & 0x10) != 0) result |= Ds4Buttons.Square;
        if ((buttons1 & 0x20) != 0) result |= Ds4Buttons.Cross;
        if ((buttons1 & 0x40) != 0) result |= Ds4Buttons.Circle;
        if ((buttons1 & 0x80) != 0) result |= Ds4Buttons.Triangle;
        if ((buttons2 & 0x01) != 0) result |= Ds4Buttons.L1;
        if ((buttons2 & 0x02) != 0) result |= Ds4Buttons.R1;
        if ((buttons2 & 0x04) != 0) result |= Ds4Buttons.L2;
        if ((buttons2 & 0x08) != 0) result |= Ds4Buttons.R2;
        if ((buttons2 & 0x10) != 0) result |= Ds4Buttons.Share;
        if ((buttons2 & 0x20) != 0) result |= Ds4Buttons.Options;
        if ((buttons2 & 0x40) != 0) result |= Ds4Buttons.L3;
        if ((buttons2 & 0x80) != 0) result |= Ds4Buttons.R3;
        if ((buttons3 & 0x01) != 0) result |= Ds4Buttons.Ps;
        if ((buttons3 & 0x02) != 0) result |= Ds4Buttons.TouchpadClick;
        ApplyDpad(ref result, (byte)(buttons1 & 0x0F));
        return result;
    }

    private static void ApplyDpad(ref Ds4Buttons result, byte value)
    {
        switch (value)
        {
            case 0: result |= Ds4Buttons.DpadUp; break;
            case 1: result |= Ds4Buttons.DpadUp | Ds4Buttons.DpadRight; break;
            case 2: result |= Ds4Buttons.DpadRight; break;
            case 3: result |= Ds4Buttons.DpadDown | Ds4Buttons.DpadRight; break;
            case 4: result |= Ds4Buttons.DpadDown; break;
            case 5: result |= Ds4Buttons.DpadDown | Ds4Buttons.DpadLeft; break;
            case 6: result |= Ds4Buttons.DpadLeft; break;
            case 7: result |= Ds4Buttons.DpadUp | Ds4Buttons.DpadLeft; break;
        }
    }

    private static Ds4TouchPoint? ReadTouchPoint(byte[] report, int offset)
    {
        if (offset < 0 || offset + 3 >= report.Length) return null;
        byte status = report[offset];
        bool touching = (status & 0x80) == 0;
        byte fingerId = (byte)(status & 0x7F);
        ushort x = (ushort)(report[offset + 1] | ((report[offset + 2] & 0x0F) << 8));
        ushort y = (ushort)((report[offset + 2] >> 4) | (report[offset + 3] << 4));
        return new Ds4TouchPoint(touching, fingerId, x, y);
    }

    private static (byte? Level, bool Charging) ReadBattery(byte[] report, int commonOffset)
    {
        byte status = ByteAt(report, commonOffset + 29);
        return ((byte)(status & 0x0F), (status & 0x10) != 0);
    }

    private static byte ByteAt(byte[] report, int index) =>
        index >= 0 && index < report.Length ? report[index] : (byte)0;

    private static short Int16LeAt(byte[] report, int index) =>
        index >= 0 && index + 1 < report.Length
            ? (short)(report[index] | (report[index + 1] << 8))
            : (short)0;
}
