using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using PadScope.Core.Models;

namespace PadScope.Desktop;

public partial class MainWindow
{
    private ModernLiveDashboard? _modernLiveDashboard;
    private DispatcherTimer? _modernDashboardTimer;
    private string _modernDeviceFingerprint = string.Empty;

    private void InstallModernLiveDashboard()
    {
        if (_modernLiveDashboard is not null)
        {
            return;
        }

        TabItem? liveTab = WalkLogicalTree(this)
            .OfType<TabItem>()
            .FirstOrDefault(tab => tab.Header?.ToString()?.Contains("Live Input", StringComparison.OrdinalIgnoreCase) == true);

        if (liveTab?.Content is not UIElement legacyContent)
        {
            return;
        }

        liveTab.Content = null;

        _modernLiveDashboard = new ModernLiveDashboard
        {
            Height = 555,
            Margin = new Thickness(4, 8, 4, 0)
        };
        _modernLiveDashboard.StartRequested += ModernDashboard_StartRequested;
        _modernLiveDashboard.StopRequested += ModernDashboard_StopRequested;
        _modernLiveDashboard.RumblePresetRequested += ModernDashboard_RumblePresetRequested;
        _modernLiveDashboard.ResetRumbleRequested += ModernDashboard_ResetRumbleRequested;

        Expander advanced = new()
        {
            Header = "Advanced HID tools",
            IsExpanded = false,
            Margin = new Thickness(4, 12, 4, 10),
            Content = legacyContent,
            Foreground = (Brush)FindResource("B_Text"),
            Background = Brushes.Transparent,
            BorderBrush = (Brush)FindResource("B_Border"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10)
        };

        TextBlock advancedHint = new()
        {
            Text = "Capture/replay, raw HID, lightbar, detailed motion data and manual output controls",
            Foreground = (Brush)FindResource("B_TextDim"),
            FontSize = 11,
            Margin = new Thickness(4, 2, 4, 0)
        };

        StackPanel stack = new();
        stack.Children.Add(_modernLiveDashboard);
        stack.Children.Add(advancedHint);
        stack.Children.Add(advanced);

        liveTab.Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = stack
        };

        RefreshModernDashboard(forceDeviceRefresh: true);

        _modernDashboardTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _modernDashboardTimer.Tick += (_, _) => RefreshModernDashboard(forceDeviceRefresh: false);
        _modernDashboardTimer.Start();

        Closed += (_, _) => _modernDashboardTimer?.Stop();
    }

    private void RefreshModernDashboard(bool forceDeviceRefresh)
    {
        ModernLiveDashboard? dashboard = _modernLiveDashboard;
        if (dashboard is null)
        {
            return;
        }

        List<ControllerDevice> devices = _reports
            .Select(report => report.Device)
            .Distinct()
            .ToList();

        string fingerprint = string.Join(
            "|",
            devices.Select(device => $"{device.DevicePath}\u001f{device.DisplayName}\u001f{device.VendorId}:{device.ProductId}"));

        if (forceDeviceRefresh || !string.Equals(fingerprint, _modernDeviceFingerprint, StringComparison.Ordinal))
        {
            _modernDeviceFingerprint = fingerprint;
            dashboard.SetDevices(devices);
        }

        bool running = _liveSession is { IsRunning: true };
        dashboard.SetSessionState(running, running ? LiveStatusText.Text : "Waiting for live input");
        dashboard.SetOutputEnabled(running && PulseRumbleButton.IsEnabled);

        var state = _latestState;
        if (state is not null)
        {
            dashboard.UpdateTelemetry(state, _latestTiming);
        }
    }

    private void ModernDashboard_StartRequested(object? sender, EventArgs e)
    {
        if (_modernLiveDashboard?.SelectedDevice is not ControllerDevice device)
        {
            MessageBox.Show(
                this,
                "Scan first, then select a controller.",
                "PadScope Live Input",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        DeviceComboBox.SelectedItem = device;
        StartInputButton_Click(this, new RoutedEventArgs());
        RefreshModernDashboard(forceDeviceRefresh: false);
    }

    private void ModernDashboard_StopRequested(object? sender, EventArgs e)
    {
        StopInputButton_Click(this, new RoutedEventArgs());
        RefreshModernDashboard(forceDeviceRefresh: false);
    }

    private void ModernDashboard_RumblePresetRequested(object? sender, RumblePresetRequestedEventArgs e)
    {
        if (_liveSession is null || !PulseRumbleButton.IsEnabled)
        {
            MessageBox.Show(
                this,
                "Start a live hardware session before running a vibration test.",
                "PadScope Vibration",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        RumbleSmallSlider.Value = e.SmallMotor;
        RumbleLargeSlider.Value = e.LargeMotor;
        PulseRumbleButton_Click(this, new RoutedEventArgs());
    }

    private void ModernDashboard_ResetRumbleRequested(object? sender, EventArgs e)
    {
        if (_liveSession is null)
        {
            return;
        }

        ResetOutputButton_Click(this, new RoutedEventArgs());
    }
}
