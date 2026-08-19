namespace PadScope.Core.Input;

public sealed record Xbox360InputState
{
    public bool A { get; init; }
    public bool B { get; init; }
    public bool X { get; init; }
    public bool Y { get; init; }
    public bool LeftShoulder { get; init; }
    public bool RightShoulder { get; init; }
    public bool Back { get; init; }
    public bool Start { get; init; }
    public bool Guide { get; init; }
    public bool LeftThumb { get; init; }
    public bool RightThumb { get; init; }
    public byte LeftTrigger { get; init; }
    public byte RightTrigger { get; init; }
    public short LeftThumbX { get; init; }
    public short LeftThumbY { get; init; }
    public short RightThumbX { get; init; }
    public short RightThumbY { get; init; }
    public bool DpadUp { get; init; }
    public bool DpadDown { get; init; }
    public bool DpadLeft { get; init; }
    public bool DpadRight { get; init; }
}

public static class Ds4ToXboxMapper
{
    public static Xbox360InputState Map(Ds4InputState state)
    {
        return new Xbox360InputState
        {
            A = state.Buttons.HasFlag(Ds4Buttons.Cross),
            B = state.Buttons.HasFlag(Ds4Buttons.Circle),
            X = state.Buttons.HasFlag(Ds4Buttons.Square),
            Y = state.Buttons.HasFlag(Ds4Buttons.Triangle),
            LeftShoulder = state.Buttons.HasFlag(Ds4Buttons.L1),
            RightShoulder = state.Buttons.HasFlag(Ds4Buttons.R1),
            Back = state.Buttons.HasFlag(Ds4Buttons.Share),
            Start = state.Buttons.HasFlag(Ds4Buttons.Options),
            Guide = state.Buttons.HasFlag(Ds4Buttons.Ps),
            LeftThumb = state.Buttons.HasFlag(Ds4Buttons.L3),
            RightThumb = state.Buttons.HasFlag(Ds4Buttons.R3),
            LeftTrigger = state.LeftTrigger,
            RightTrigger = state.RightTrigger,
            LeftThumbX = MapAxis(state.LeftStickX),
            LeftThumbY = MapAxis(state.LeftStickY, invert: true),
            RightThumbX = MapAxis(state.RightStickX),
            RightThumbY = MapAxis(state.RightStickY, invert: true),
            DpadUp = state.Buttons.HasFlag(Ds4Buttons.DpadUp),
            DpadDown = state.Buttons.HasFlag(Ds4Buttons.DpadDown),
            DpadLeft = state.Buttons.HasFlag(Ds4Buttons.DpadLeft),
            DpadRight = state.Buttons.HasFlag(Ds4Buttons.DpadRight)
        };
    }

    public static short MapAxis(byte value)
    {
        return MapAxis(value, invert: false);
    }

    private static short MapAxis(byte value, bool invert)
    {
        int scaled = (value - 128) * 257;

        if (invert)
        {
            scaled = -scaled;
        }

        return (short)Math.Clamp(scaled, short.MinValue, short.MaxValue);
    }
}