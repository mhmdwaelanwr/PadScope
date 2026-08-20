namespace PadScope.Core.Input;

public sealed record StickSettings
{
    public bool SwapSticks { get; init; }
    public bool InvertX { get; init; }
    public bool InvertY { get; init; }
    public byte Deadzone { get; init; }
}

public sealed record TriggerSettings
{
    public bool Invert { get; init; }
    public byte Deadzone { get; init; }
}

public sealed record ControllerProfile
{
    public required string Name { get; init; }
    public required string Version { get; init; }

    public Dictionary<Ds4Buttons, Ds4Buttons> ButtonRemap { get; init; } = new();

    public StickSettings? LeftStick { get; init; }
    public StickSettings? RightStick { get; init; }
    public TriggerSettings? LeftTrigger { get; init; }
    public TriggerSettings? RightTrigger { get; init; }

    public bool ApplyRumble { get; init; } = true;
    public bool ApplyLightbar { get; init; } = true;

    public IReadOnlyList<MacroDefinition> Macros { get; init; } = Array.Empty<MacroDefinition>();
    public IReadOnlyList<MacroSequence> Sequences { get; init; } = Array.Empty<MacroSequence>();
}