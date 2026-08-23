using System.Windows.Controls;

namespace PadScope.Desktop;

public partial class MainWindow
{
    private bool _postLoadPolishInstalled;
    private bool _extendedDashboardTelemetryHooked;

    private void InstallPostLoadPolish()
    {
        if (_postLoadPolishInstalled)
        {
            return;
        }

        // This layer is presentation-only. A cosmetic enhancement must never be
        // able to terminate PadScope or block access to Live Input.
        try
        {
            PrepareLegacyOutputControlsForReparenting();
            InstallAdvancedLivePolish(this);
            _modernLiveDashboard?.EnsureControllerPolish();
            PolishWorkspaceNavigationRuntime();
            HookExtendedDashboardTelemetry();
            _postLoadPolishInstalled = true;
        }
        catch (Exception ex)
        {
            // Do not repeatedly retry a partially applied visual transformation.
            // The original/live controls remain functional even when polish falls back.
            _postLoadPolishInstalled = true;
            ReportNonCriticalPolishFailure(ex);
        }
    }

    private void HookExtendedDashboardTelemetry()
    {
        if (_extendedDashboardTelemetryHooked || _modernDashboardTimer is null)
        {
            return;
        }

        _extendedDashboardTelemetryHooked = true;
        _modernDashboardTimer.Tick += (_, _) =>
        {
            if (_latestState is { } state)
            {
                _modernLiveDashboard?.ApplyExtendedButtonTelemetry(state);
            }
        };
    }

    private void PolishWorkspaceNavigationRuntime()
    {
        TabItem? liveTab = WalkLogicalTree(this)
            .OfType<TabItem>()
            .FirstOrDefault(tab => tab.Header?.ToString()?.Contains("Live Input", StringComparison.OrdinalIgnoreCase) == true);
        if (liveTab is not null)
        {
            PolishMainNavigation(liveTab);
        }

        TabControl? workspaceTabs = WalkLogicalTree(this)
            .OfType<TabControl>()
            .FirstOrDefault(control =>
                control.Items.OfType<TabItem>().Any(tab => string.Equals(tab.Header?.ToString(), "Overview", StringComparison.Ordinal)) &&
                control.Items.OfType<TabItem>().Any(tab => string.Equals(tab.Header?.ToString(), "Advanced HID tools", StringComparison.Ordinal)));
        if (workspaceTabs is null)
        {
            return;
        }

        Style compactStyle = CreateWorkspaceTabStyle();
        workspaceTabs.Margin = new Thickness(4, 2, 4, 0);
        foreach (TabItem tab in workspaceTabs.Items.OfType<TabItem>())
        {
            tab.Style = compactStyle;
            tab.Margin = new Thickness(0, 0, 9, 0);
            if (tab.Content is ScrollViewer scroll)
            {
                scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            }
        }
    }
}
