namespace PadScope.Core.Input;

public static class Ds4ReportParser
{
    public const int UsbReportId = 0x01;
    public const int BluetoothReportId = 0x11;

    private const int TouchUsbBaseOffset = 35;
    private const int TouchBluetoothBaseOffset = 33;

    public static Ds4InputState Parse(byte[] report)
    {
        ArgumentNullException.ThrowIfNull(report);

        int reportId = report.Length > 0 ? report[0] : 0;
        bool bluetooth = reportId == BluetoothReportId;

        Ds4Buttons buttons = ReadButtons(report);

        byte lx = ByteAt(report, 1);
        byte ly = ByteAt(report, 2);
        byte rx = ByteAt(report, 3);
        byte ry = ByteAt(report, 4);
        byte l2 = ByteAt(report, 5);
        byte r2 = ByteAt(report, 6);

        short gyroX = Int16LeAt(report, 19);
        short gyroY = Int16LeAt(report, 21);
        short gyroZ = Int16LeAt(report, 23);
        short accelX = Int16LeAt(report, 25);
        short accelY = Int16LeAt(report, 27);
        short accelZ = Int16LeAt(report, 29);

        int touchBase = bluetooth ? TouchBluetoothBaseOffset : TouchUsbBaseOffset;
        Ds4TouchPoint? touch1 = ReadTouchPoint(report, touchBase);
        Ds4TouchPoint? touch2 = ReadTouchPoint(report, touchBase + 7);

        (byte? batteryLevel, bool charging) = ReadBattery(report, bluetooth);

        return new Ds4InputState
        {
            Raw = report,
            ReportId = reportId,
            Buttons = buttons,
            LeftStickX = lx,
            LeftStickY = ly,
            RightStickX = rx,
            RightStickY = ry,
            LeftTrigger = l2,
            RightTrigger = r2,
            GyroX = gyroX,
            GyroY = gyroY,
            GyroZ = gyroZ,
            AccelX = accelX,
            AccelY = accelY,
            AccelZ = accelZ,
            Touch1 = touch1,
            Touch2 = touch2,
            BatteryLevel = batteryLevel,
            Charging = charging
        };
    }

    public static bool LooksLikeDs4Report(byte[] report)
    {
        if (report is null || report.Length < 10)
        {
            return false;
        }

        int reportId = report[0];
        return reportId is UsbReportId or BluetoothReportId;
    }

    private static Ds4Buttons ReadButtons(byte[] report)
    {
        byte buttons1 = ByteAt(report, 7);
        byte buttons2 = ByteAt(report, 8);
        byte buttons3 = ByteAt(report, 9);

        Ds4Buttons result = Ds4Buttons.None;

        if ((buttons1 & 0x01) != 0) result |= Ds4Buttons.Square;
        if ((buttons1 & 0x02) != 0) result |= Ds4Buttons.Cross;
        if ((buttons1 & 0x04) != 0) result |= Ds4Buttons.Circle;
        if ((buttons1 & 0x08) != 0) result |= Ds4Buttons.Triangle;
        if ((buttons1 & 0x10) != 0) result |= Ds4Buttons.L1;
        if ((buttons1 & 0x20) != 0) result |= Ds4Buttons.R1;
        if ((buttons1 & 0x40) != 0) result |= Ds4Buttons.L2;
        if ((buttons1 & 0x80) != 0) result |= Ds4Buttons.R2;

        if ((buttons2 & 0x10) != 0) result |= Ds4Buttons.Share;
        if ((buttons2 & 0x20) != 0) result |= Ds4Buttons.Options;
        if ((buttons2 & 0x40) != 0) result |= Ds4Buttons.L3;
        if ((buttons2 & 0x80) != 0) result |= Ds4Buttons.R3;

        if ((buttons3 & 0x01) != 0) result |= Ds4Buttons.Ps;
        if ((buttons3 & 0x02) != 0) result |= Ds4Buttons.TouchpadClick;

        ApplyDpad(result, buttons2 & 0x0F);

        return result;
    }

    private static void ApplyDpad(Ds4Buttons result, byte dpadValue)
    {
        switch (dpadValue)
        {
            case 0:
                result |= Ds4Buttons.DpadUp;
                break;
            case 1:
                result |= Ds4Buttons.DpadUp | Ds4Buttons.DpadRight;
                break;
            case 2:
                result |= Ds4Buttons.DpadRight;
                break;
            case 3:
                result |= Ds4Buttons.DpadDown | Ds4Buttons.DpadRight;
                break;
            case 4:
                result |= Ds4Buttons.DpadDown;
                break;
            case 5:
                result |= Ds4Buttons.DpadDown | Ds4Buttons.DpadLeft;
                break;
            case 6:
                result |= Ds4Buttons.DpadLeft;
                break;
            case 7:
                result |= Ds4Buttons.DpadUp | Ds4Buttons.DpadLeft;
                break;
            default:
                break;
        }
    }

    private static Ds4TouchPoint? ReadTouchPoint(byte[] report, int baseOffset)
    {
        if (baseOffset + 6 >= report.Length)
        {
            return null;
        }

        byte status = report[baseOffset + 3];
        byte fingerId = (byte)(status & 0x7F);
        bool touching = (status & 0x80) != 0;

        ushort x = (ushort)(report[baseOffset + 4] | ((report[baseOffset + 5] & 0x0F) << 8));
        ushort y = (ushort)(report[baseOffset + 6] | ((report[baseOffset + 5] & 0xF0) << 4));

        return new Ds4TouchPoint(touching, fingerId, x, y);
    }

    private static (byte? Level, bool Charging) ReadBattery(byte[] report, bool bluetooth)
    {
        if (bluetooth)
        {
            // Bluetooth report battery offset differs between revisions and is not
            // parsed until verified on real hardware. This stays honest instead of guessed.
            return (null, false);
        }

        if (report.Length <= 15)
        {
            return (null, false);
        }

        byte battery = report[15];
        return ((byte)(battery & 0x0F), (battery & 0x10) != 0);
    }

    private static byte ByteAt(byte[] report, int index)
    {
        return index >= 0 && index < report.Length ? report[index] : (byte)0;
    }

    private static short Int16LeAt(byte[] report, int index)
    {
        if (index < 0 || index + 1 >= report.Length)
        {
            return 0;
        }

        return (short)(report[index] | (report[index + 1] << 8));
    }
}
