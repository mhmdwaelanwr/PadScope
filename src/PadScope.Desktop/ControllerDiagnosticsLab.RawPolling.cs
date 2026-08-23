using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PadScope.Desktop;

public partial class ControllerDiagnosticsLab
{
    private readonly List<double> _rawPollingIntervals = new();
    private bool _rawPollingWasActive;
    private bool _rawPollingHasResult;

    public void AcceptRawPollingIntervals(IReadOnlyList<double> intervalsMs)
    {
        if (_pollingTestActive && !_rawPollingWasActive)
        {
            _rawPollingIntervals.Clear();
            _rawPollingHasResult = false;
        }
        _rawPollingWasActive = _pollingTestActive;

        if (_pollingTestActive)
        {
            foreach (double interval in intervalsMs)
            {
                if (interval is > 0.05 and < 1000)
                    _rawPollingIntervals.Add(interval);
            }

            if (_rawPollingIntervals.Count > 900)
                _rawPollingIntervals.RemoveRange(0, _rawPollingIntervals.Count - 900);

            if (_rawPollingIntervals.Count >= 2)
                _rawPollingHasResult = true;
        }

        if (_rawPollingHasResult)
            RenderRawPollingResult();
    }

    private void RenderRawPollingResult()
    {
        if (_rawPollingIntervals.Count < 2) return;

        double[] intervals = _rawPollingIntervals.ToArray();
        double[] rates = intervals.Select(interval => 1000.0 / interval).ToArray();
        double averageInterval = intervals.Average();
        double averageRate = 1000.0 / averageInterval;
        double currentRate = rates[^1];
        double peakRate = rates.Max();
        double variance = intervals.Select(value => Math.Pow(value - averageInterval, 2)).Average();
        double jitter = Math.Sqrt(variance);
        double[] sorted = intervals.OrderBy(value => value).ToArray();
        int p95Index = Math.Clamp((int)Math.Ceiling(sorted.Length * 0.95) - 1, 0, sorted.Length - 1);
        double p95 = sorted[p95Index];
        double spikeThreshold = Math.Max(averageInterval * 1.75, p95 * 1.15);
        int spikes = intervals.Count(value => value > spikeThreshold);

        PollingCurrentText.Text = $"{currentRate:F0} Hz";
        PollingPeakAverageText.Text = $"{peakRate:F0} / {averageRate:F0} Hz";
        PollingIntervalText.Text = $"{averageInterval:F2} ms";
        PollingJitterText.Text = $"{jitter:F2} ms";
        PollingP95Text.Text = $"{p95:F2} ms";
        PollingSpikesText.Text = spikes.ToString();

        RenderRawPollingGraph(rates);
    }

    private void RenderRawPollingGraph(IReadOnlyList<double> rates)
    {
        var canvas = PollingGraphCanvas;
        canvas.Children.Clear();
        double width = Math.Max(260, canvas.ActualWidth > 0 ? canvas.ActualWidth : 360);
        double height = Math.Max(150, canvas.ActualHeight > 0 ? canvas.ActualHeight : 190);
        Brush grid = ResolveBrush("B_Border");
        Brush accent = ResolveBrush("B_Primary");

        for (int row = 0; row <= 4; row++)
        {
            double y = 8 + (height - 24) * row / 4.0;
            canvas.Children.Add(new Line
            {
                X1 = 0, X2 = width, Y1 = y, Y2 = y,
                Stroke = grid, StrokeThickness = 1, Opacity = 0.42
            });
        }

        double[] visible = rates.TakeLast(Math.Min(180, rates.Count)).ToArray();
        if (visible.Length < 2) return;

        double median = visible.OrderBy(value => value).ElementAt(visible.Length / 2);
        double low = Math.Max(1, median * 0.65);
        double high = Math.Max(low + 1, median * 1.35);

        Polyline line = new()
        {
            Stroke = accent,
            StrokeThickness = 2.5,
            StrokeLineJoin = PenLineJoin.Round
        };

        for (int index = 0; index < visible.Length; index++)
        {
            double x = index * (width - 8) / (visible.Length - 1.0) + 4;
            double normalized = Math.Clamp((visible[index] - low) / (high - low), 0, 1);
            double y = height - 10 - normalized * (height - 24);
            line.Points.Add(new Point(x, y));
        }
        canvas.Children.Add(line);
    }
}
