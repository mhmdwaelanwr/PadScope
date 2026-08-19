namespace PadScope.Core.Input;

public readonly record struct Ds4TouchPoint(
    bool Touching,
    byte FingerId,
    ushort X,
    ushort Y
);

public sealed record Ds4InputState
{
    public required byte[] Raw { get; init; }

    public required int ReportId { get; init; }

    public Ds4Buttons Buttons { get; init; }

    public byte LeftStickX { get; init; }
    public byte LeftStickY { get; init; }
    public byte RightStickX { get; init; }
    public byte RightStickY { get; init; }

    public byte LeftTrigger { get; init; }
    public byte RightTrigger { get; init; }

    public short GyroX { get; init; }
    public short GyroY { get; init; }
    public short GyroZ { get; init; }

    public short AccelX { get; init; }
    public short AccelY { get; init; }
    public short AccelZ { get; init; }

    public Ds4TouchPoint? Touch1 { get; init; }
    public Ds4TouchPoint? Touch2 { get; init; }

    public byte? BatteryLevel { get; init; }
    public bool Charging { get; init; }

    public float LeftStickXNorm => Normalize(LeftStickX);
    public float LeftStickYNorm => Normalize(LeftStickY);
    public float RightStickXNorm => Normalize(RightStickX);
    public float RightStickYNorm => Normalize(RightStickY);

    public float LeftTriggerNorm => LeftTrigger / 255f;
    public float RightTriggerNorm => RightTrigger / 255f;

    private static float Normalize(byte value)
    {
        if (value == 128)
        {
            return 0f;
        }

        return value < 128
            ? (value - 128) / 128f
            : (value - 128) / 127f;
    }
}
