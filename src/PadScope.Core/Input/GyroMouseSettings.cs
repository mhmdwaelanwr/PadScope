namespace PadScope.Core.Input;

public sealed record GyroMouseSettings
{
    public double Sensitivity { get; init; } = 1.0;
    public bool InvertX { get; init; }
    public bool InvertY { get; init; }
    public bool SwapAxes { get; init; }
    public double Smoothing { get; init; } = 0.3;
    public double Deadzone { get; init; } = 12.0;
}