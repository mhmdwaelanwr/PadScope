namespace PadScope.Core.Input;

public static class Ds4Remapper
{
    public static Ds4InputState Apply(ControllerProfile profile, Ds4InputState state)
    {
        Ds4Buttons buttons = RemapButtons(profile, state.Buttons);

        byte leftX = state.LeftStickX;
        byte leftY = state.LeftStickY;
        byte rightX = state.RightStickX;
        byte rightY = state.RightStickY;

        if ((profile.LeftStick?.SwapSticks ?? false) || (profile.RightStick?.SwapSticks ?? false))
        {
            (leftX, rightX) = (rightX, leftX);
            (leftY, rightY) = (rightY, leftY);
        }

        (leftX, leftY) = ApplyStick(profile.LeftStick, leftX, leftY);
        (rightX, rightY) = ApplyStick(profile.RightStick, rightX, rightY);

        byte leftTrigger = ApplyTrigger(profile.LeftTrigger, state.LeftTrigger);
        byte rightTrigger = ApplyTrigger(profile.RightTrigger, state.RightTrigger);

        return state with
        {
            Buttons = buttons,
            LeftStickX = leftX,
            LeftStickY = leftY,
            RightStickX = rightX,
            RightStickY = rightY,
            LeftTrigger = leftTrigger,
            RightTrigger = rightTrigger
        };
    }

    private static Ds4Buttons RemapButtons(ControllerProfile profile, Ds4Buttons buttons)
    {
        if (profile.ButtonRemap.Count == 0)
        {
            return buttons;
        }

        Ds4Buttons result = Ds4Buttons.None;

        foreach (Ds4Buttons bit in Enum.GetValues<Ds4Buttons>())
        {
            if (bit == Ds4Buttons.None || !buttons.HasFlag(bit))
            {
                continue;
            }

            result |= profile.ButtonRemap.TryGetValue(bit, out Ds4Buttons mapped) ? mapped : bit;
        }

        return result;
    }

    private static (byte X, byte Y) ApplyStick(StickSettings? settings, byte x, byte y)
    {
        if (settings is null)
        {
            return (x, y);
        }

        byte resultX = settings.InvertX ? (byte)(255 - x) : x;
        byte resultY = settings.InvertY ? (byte)(255 - y) : y;

        if (settings.Deadzone > 0 && IsInDeadzone(resultX, resultY, settings.Deadzone))
        {
            return (128, 128);
        }

        return (resultX, resultY);
    }

    private static bool IsInDeadzone(byte x, byte y, byte deadzone)
    {
        int dx = x - 128;
        int dy = y - 128;
        return (dx * dx + dy * dy) <= deadzone * deadzone;
    }

    private static byte ApplyTrigger(TriggerSettings? settings, byte value)
    {
        if (settings is null)
        {
            return value;
        }

        byte result = settings.Invert ? (byte)(255 - value) : value;
        return settings.Deadzone > 0 && result <= settings.Deadzone ? (byte)0 : result;
    }
}