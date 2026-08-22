using PadScope.Core.Diagnostics;
using Xunit;

namespace PadScope.Tests;

public class ReportTimingAnalyzerTests
{
    [Fact]
    public void Snapshot_ComputesRatePercentileJitterAndSpikes()
    {
        ReportTimingAnalyzer analyzer = new();
        DateTimeOffset timestamp = DateTimeOffset.UnixEpoch;
        analyzer.Add(timestamp);

        foreach (double interval in new[] { 4d, 4d, 4d, 5d, 30d })
        {
            timestamp = timestamp.AddMilliseconds(interval);
            analyzer.Add(timestamp);
        }

        ReportTimingSnapshot snapshot = analyzer.Snapshot();

        Assert.Equal(6, snapshot.ReportCount);
        Assert.Equal(9.4, snapshot.AverageIntervalMs, 3);
        Assert.Equal(4, snapshot.MedianIntervalMs, 3);
        Assert.Equal(30, snapshot.P95IntervalMs, 3);
        Assert.Equal(30, snapshot.MaximumIntervalMs, 3);
        Assert.Equal(1, snapshot.SpikeCount);
        Assert.InRange(snapshot.ReportRateHz, 106.3, 106.4);
        Assert.True(snapshot.JitterMs > 10);
    }

    [Fact]
    public void Capacity_BoundsTheObservationWindow()
    {
        ReportTimingAnalyzer analyzer = new(capacity: 2);
        DateTimeOffset timestamp = DateTimeOffset.UnixEpoch;
        analyzer.Add(timestamp);

        foreach (int interval in new[] { 100, 4, 6 })
        {
            timestamp = timestamp.AddMilliseconds(interval);
            analyzer.Add(timestamp);
        }

        ReportTimingSnapshot snapshot = analyzer.Snapshot();

        Assert.Equal(5, snapshot.AverageIntervalMs, 3);
        Assert.Equal(6, snapshot.P95IntervalMs, 3);
    }

    [Fact]
    public void Reset_ClearsSamplesAndCount()
    {
        ReportTimingAnalyzer analyzer = new();
        analyzer.Add(DateTimeOffset.UnixEpoch);
        analyzer.Add(DateTimeOffset.UnixEpoch.AddMilliseconds(4));

        analyzer.Reset();

        Assert.Equal(0, analyzer.Snapshot().ReportCount);
        Assert.Equal(0, analyzer.Snapshot().AverageIntervalMs);
    }

    [Fact]
    public void BackwardsClockSample_IsIgnoredWithoutCorruptingStatistics()
    {
        ReportTimingAnalyzer analyzer = new();
        analyzer.Add(DateTimeOffset.UnixEpoch.AddMilliseconds(10));
        analyzer.Add(DateTimeOffset.UnixEpoch);

        ReportTimingSnapshot snapshot = analyzer.Snapshot();

        Assert.Equal(2, snapshot.ReportCount);
        Assert.Equal(0, snapshot.AverageIntervalMs);
    }
}
