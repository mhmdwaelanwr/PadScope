using PadScope.Core.Diagnostics;
using Xunit;

namespace PadScope.Tests;

public class StickDiagnosticsAnalyzerTests
{
    [Fact]
    public void CenteredStick_ProducesSmallDriftAndDeadzone()
    {
        StickDiagnosticsAnalyzer analyzer = new();

        for (int index = 0; index < 200; index++)
        {
            float x = index % 2 == 0 ? 0.004f : -0.004f;
            float y = index % 3 == 0 ? 0.003f : -0.003f;
            analyzer.Add(x, y);
        }

        StickDiagnosticsSnapshot snapshot = analyzer.Snapshot();

        Assert.Equal(200, snapshot.SampleCount);
        Assert.InRange(snapshot.DriftMagnitude, 0, 0.01);
        Assert.InRange(snapshot.Jitter, 0, 0.02);
        Assert.InRange(snapshot.RecommendedDeadzone, 0, 0.08);
    }

    [Fact]
    public void PersistentOffset_IsDetectedAsDrift()
    {
        StickDiagnosticsAnalyzer analyzer = new();

        for (int index = 0; index < 120; index++)
        {
            analyzer.Add(0.11f, -0.07f);
        }

        StickDiagnosticsSnapshot snapshot = analyzer.Snapshot();

        Assert.InRange(snapshot.CenterX, 0.109, 0.111);
        Assert.InRange(snapshot.CenterY, -0.071, -0.069);
        Assert.True(snapshot.DriftMagnitude > 0.12);
        Assert.True(snapshot.RecommendedDeadzone > snapshot.DriftMagnitude);
    }

    [Fact]
    public void FullRotation_PopulatesRangeCoverage()
    {
        StickDiagnosticsAnalyzer analyzer = new(rangeBinCount: 36);

        for (int degree = 0; degree < 360; degree += 5)
        {
            double radians = degree * Math.PI / 180.0;
            analyzer.Add((float)Math.Cos(radians), (float)-Math.Sin(radians));
        }

        StickDiagnosticsSnapshot snapshot = analyzer.Snapshot();

        Assert.True(snapshot.RangeCoveragePercent >= 95);
        Assert.True(snapshot.CircularityErrorPercent < 2);
    }

    [Fact]
    public void Reset_ClearsCenterAndRangeSamples()
    {
        StickDiagnosticsAnalyzer analyzer = new();
        analyzer.Add(1, 0);
        analyzer.Add(0.1f, 0.1f);

        analyzer.Reset();
        StickDiagnosticsSnapshot snapshot = analyzer.Snapshot();

        Assert.Equal(0, snapshot.SampleCount);
        Assert.Equal(0, snapshot.DriftMagnitude);
        Assert.All(snapshot.RangeBins, value => Assert.Equal(0, value));
    }
}
