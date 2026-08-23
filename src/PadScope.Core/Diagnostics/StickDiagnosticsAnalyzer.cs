namespace PadScope.Core.Diagnostics;

public sealed record StickDiagnosticsSnapshot(
    int SampleCount,
    double CenterX,
    double CenterY,
    double DriftMagnitude,
    double Jitter,
    double MaximumMagnitude,
    double RecommendedDeadzone,
    double RangeCoveragePercent,
    double CircularityErrorPercent,
    IReadOnlyList<double> RangeBins);

/// <summary>
/// Thread-safe diagnostics accumulator for analog-stick center drift, jitter,
/// recommended deadzone, angular range coverage, and circularity error.
///
/// The implementation is intentionally independent and based only on normalized
/// stick coordinates supplied by PadScope's own HID parser.
/// </summary>
public sealed class StickDiagnosticsAnalyzer
{
    public const int DefaultRangeBinCount = 72;

    private readonly object _sync = new();
    private readonly double[] _rangeBins;

    private int _sampleCount;
    private double _sumX;
    private double _sumY;
    private double _sumX2;
    private double _sumY2;
    private double _maximumMagnitude;

    public StickDiagnosticsAnalyzer(int rangeBinCount = DefaultRangeBinCount)
    {
        if (rangeBinCount < 8)
        {
            throw new ArgumentOutOfRangeException(nameof(rangeBinCount), "At least 8 angular bins are required.");
        }

        _rangeBins = new double[rangeBinCount];
    }

    public void Add(float x, float y)
    {
        if (!float.IsFinite(x) || !float.IsFinite(y))
        {
            return;
        }

        double dx = Math.Clamp(x, -1.5f, 1.5f);
        double dy = Math.Clamp(y, -1.5f, 1.5f);
        double magnitude = Math.Sqrt(dx * dx + dy * dy);

        lock (_sync)
        {
            _sampleCount++;
            _sumX += dx;
            _sumY += dy;
            _sumX2 += dx * dx;
            _sumY2 += dy * dy;
            _maximumMagnitude = Math.Max(_maximumMagnitude, magnitude);

            // Ignore tiny center noise for range/circularity capture; otherwise a
            // centered stick would mark arbitrary angle bins as visited.
            if (magnitude >= 0.18)
            {
                double angle = Math.Atan2(-dy, dx);
                if (angle < 0)
                {
                    angle += Math.PI * 2;
                }

                int bin = Math.Min(
                    _rangeBins.Length - 1,
                    (int)Math.Floor(angle / (Math.PI * 2) * _rangeBins.Length));
                _rangeBins[bin] = Math.Max(_rangeBins[bin], magnitude);
            }
        }
    }

    public StickDiagnosticsSnapshot Snapshot()
    {
        lock (_sync)
        {
            if (_sampleCount == 0)
            {
                return new StickDiagnosticsSnapshot(
                    0, 0, 0, 0, 0, 0, 0, 0, 0, _rangeBins.ToArray());
            }

            double count = _sampleCount;
            double centerX = _sumX / count;
            double centerY = _sumY / count;
            double drift = Math.Sqrt(centerX * centerX + centerY * centerY);

            double varianceX = Math.Max(0, _sumX2 / count - centerX * centerX);
            double varianceY = Math.Max(0, _sumY2 / count - centerY * centerY);
            double jitter = Math.Sqrt(varianceX + varianceY);

            // Keep some safety margin above observed center noise without hiding
            // excessive drift. 35% is an intentionally conservative upper cap.
            double recommendedDeadzone = Math.Clamp(
                Math.Max(drift + 3 * jitter, _maximumMagnitude * 1.05),
                0,
                0.35);

            int populated = 0;
            int fullRange = 0;
            double circularityErrorSum = 0;
            foreach (double radius in _rangeBins)
            {
                if (radius < 0.18)
                {
                    continue;
                }

                populated++;
                if (radius >= 0.90)
                {
                    fullRange++;
                }

                circularityErrorSum += Math.Abs(Math.Clamp(radius, 0, 1.25) - 1.0);
            }

            double coverage = _rangeBins.Length == 0
                ? 0
                : fullRange * 100.0 / _rangeBins.Length;
            double circularityError = populated == 0
                ? 0
                : circularityErrorSum / populated * 100.0;

            return new StickDiagnosticsSnapshot(
                _sampleCount,
                centerX,
                centerY,
                drift,
                jitter,
                _maximumMagnitude,
                recommendedDeadzone,
                coverage,
                circularityError,
                _rangeBins.ToArray());
        }
    }

    public void Reset()
    {
        lock (_sync)
        {
            _sampleCount = 0;
            _sumX = 0;
            _sumY = 0;
            _sumX2 = 0;
            _sumY2 = 0;
            _maximumMagnitude = 0;
            Array.Clear(_rangeBins, 0, _rangeBins.Length);
        }
    }
}
