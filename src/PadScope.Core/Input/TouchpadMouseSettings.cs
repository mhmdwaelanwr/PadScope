namespace PadScope.Core.Input;

public sealed record TouchpadMouseSettings
{
    public double Sensitivity { get; init; } = 1.0;
    public int TapThreshold { get; init; } = 12;
    public bool EnableTapClick { get; init; } = true;
}