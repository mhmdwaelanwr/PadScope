namespace PadScope.Core.Input;

public sealed record MacroDefinition
{
    public required string Name { get; init; }
    public required Ds4Buttons Trigger { get; init; }
    public Ds4Buttons Output { get; init; }
    public double ShotsPerSecond { get; init; }
    public bool IsToggle { get; init; }
}

public sealed record MacroSequenceStep
{
    public required Ds4Buttons Buttons { get; init; }
    public required double DurationSeconds { get; init; }
}

public sealed record MacroSequence
{
    public required string Name { get; init; }
    public required Ds4Buttons Trigger { get; init; }
    public required IReadOnlyList<MacroSequenceStep> Steps { get; init; }
}