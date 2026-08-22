namespace PadScope.Core.Diagnostics;

public sealed record ReportTimingSnapshot(
    int ReportCount,
    double AverageIntervalMs,
    double MedianIntervalMs,
    double P95IntervalMs,
    double MaximumIntervalMs,
    double JitterMs,
    double ReportRateHz,
    int SpikeCount);

public sealed class ReportTimingAnalyzer
{
    private const int DefaultCapacity = 4096;
    private readonly object _sync = new();
    private readonly Queue<double> _intervals;
    private readonly int _capacity;
    private DateTimeOffset? _previousTimestamp;
    private int _reportCount;

    public ReportTimingAnalyzer(int capacity = DefaultCapacity)
    {
        if (capacity < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be at least 2.");
        }

        _capacity = capacity;
        _intervals = new Queue<double>(capacity);
    }

    public void Add(DateTimeOffset timestamp)
    {
        lock (_sync)
        {
            _reportCount++;
            if (_previousTimestamp is DateTimeOffset previous)
            {
                double intervalMs = (timestamp - previous).TotalMilliseconds;
                if (intervalMs >= 0 && double.IsFinite(intervalMs))
                {
                    _intervals.Enqueue(intervalMs);
                    while (_intervals.Count > _capacity)
                    {
                        _intervals.Dequeue();
                    }
                }
            }

            _previousTimestamp = timestamp;
        }
    }

    public ReportTimingSnapshot Snapshot()
    {
        lock (_sync)
        {
            if (_intervals.Count == 0)
            {
                return new ReportTimingSnapshot(_reportCount, 0, 0, 0, 0, 0, 0, 0);
            }

            double[] sorted = _intervals.OrderBy(value => value).ToArray();
            double average = sorted.Average();
            double median = Percentile(sorted, 0.50);
            double p95 = Percentile(sorted, 0.95);
            double maximum = sorted[^1];
            double variance = sorted.Average(value => Math.Pow(value - average, 2));
            double jitter = Math.Sqrt(variance);
            double spikeThreshold = Math.Max(10, median * 2.5);
            int spikes = sorted.Count(value => value > spikeThreshold);
            double rate = average > 0 ? 1000 / average : 0;

            return new ReportTimingSnapshot(
                _reportCount,
                average,
                median,
                p95,
                maximum,
                jitter,
                rate,
                spikes);
        }
    }

    public void Reset()
    {
        lock (_sync)
        {
            _intervals.Clear();
            _previousTimestamp = null;
            _reportCount = 0;
        }
    }

    private static double Percentile(double[] sorted, double percentile)
    {
        int index = Math.Max(0, (int)Math.Ceiling(percentile * sorted.Length) - 1);
        return sorted[index];
    }
}
