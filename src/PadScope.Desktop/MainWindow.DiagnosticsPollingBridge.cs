using System.Windows.Threading;

namespace PadScope.Desktop;

public partial class MainWindow
{
    private bool _diagnosticsPollingBridgeInstalled;
    private DispatcherTimer? _diagnosticsPollingHookTimer;

    internal void InstallDiagnosticsPollingBridge()
    {
        if (_diagnosticsPollingBridgeInstalled)
        {
            return;
        }

        if (_controllerDiagnosticsLab is null || _modernDashboardTimer is null)
        {
            _diagnosticsPollingHookTimer ??= new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _diagnosticsPollingHookTimer.Tick -= DiagnosticsPollingHookTimer_Tick;
            _diagnosticsPollingHookTimer.Tick += DiagnosticsPollingHookTimer_Tick;
            _diagnosticsPollingHookTimer.Start();
            return;
        }

        _diagnosticsPollingBridgeInstalled = true;
        _diagnosticsPollingHookTimer?.Stop();
        _controllerDiagnosticsLab.InstallRawPollingPresentation();

        // Registered after the normal dashboard refresh so the raw polling
        // presentation wins over the smoothed snapshot rendering every frame.
        _modernDashboardTimer.Tick += (_, _) =>
        {
            ControllerDiagnosticsLab? lab = _controllerDiagnosticsLab;
            if (lab is null)
            {
                return;
            }

            if (_liveSession is { IsRunning: true } session)
            {
                IReadOnlyList<double> rawIntervals = session.DrainReportIntervals();
                if (rawIntervals.Count > 0)
                {
                    lab.AcceptRawPollingIntervals(rawIntervals);
                }
            }

            lab.RenderRawPollingPresentation();
        };

        Closed += (_, _) => _diagnosticsPollingHookTimer?.Stop();
    }

    private void DiagnosticsPollingHookTimer_Tick(object? sender, EventArgs e)
    {
        if (_controllerDiagnosticsLab is not null && _modernDashboardTimer is not null)
        {
            InstallDiagnosticsPollingBridge();
        }
    }
}
