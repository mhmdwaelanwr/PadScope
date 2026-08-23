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
        PrepareLegacyLiveTools();

        _modernLiveDashboard = new ModernLiveDashboard
        {
            Margin = new Thickness(4, 8, 4, 14),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            MinWidth = 0
        };
        _modernLiveDashboard.StartRequested += ModernDashboard_StartRequested;
        _modernLiveDashboard.StopRequested += ModernDashboard_StopRequested;
        _modernLiveDashboard.RumblePresetRequested += ModernDashboard_RumblePresetRequested;
        _modernLiveDashboard.ResetRumbleRequested += ModernDashboard_ResetRumbleRequested;

        ScrollViewer overviewScroll = new()
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            CanContentScroll = false,
            Content = _modernLiveDashboard
        };

        Border advancedSurface = new()
        {
            Background = Brushes.Transparent,
            BorderBrush = (Brush)FindResource("B_Border"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(10),
            Margin = new Thickness(4, 8, 4, 12),
            Child = legacyContent
        };

        ScrollViewer advancedScroll = new()
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            CanContentScroll = false,
            Content = advancedSurface
        };

        TabItem overviewTab = new()
        {
            Header = "Overview",
            Content = overviewScroll
        };
        TabItem advancedTab = new()
        {
            Header = "Advanced HID tools",
            ToolTip = "Capture/replay, raw HID, lightbar, detailed motion data and manual output controls",
            Content = advancedScroll
        };

        TabControl workspaceTabs = new()
        {
            Margin = new Thickness(0, 2, 0, 0),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch
        };
        workspaceTabs.Items.Add(overviewTab);
        workspaceTabs.Items.Add(advancedTab);
        workspaceTabs.SelectedIndex = 0;

        liveTab.Content = workspaceTabs;

        RefreshModernDashboard(forceDeviceRefresh: true);

        _modernDashboardTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _modernDashboardTimer.Tick += (_, _) => RefreshModernDashboard(forceDeviceRefresh: false);
        _modernDashboardTimer.Start();

        Closed += (_, _) => _modernDashboardTimer?.Stop();
    }

    private void PrepareLegacyLiveTools()
    {
        // The global primary Button style makes input-state buttons look pressed
        // even when idle. Pin those visualizer buttons to the neutral secondary
        // style so SetButtonState can temporarily override them only while pressed.
        Style neutralButtonStyle = (Style)FindResource("Sec");
        Button[] stateButtons =
        {
            DpadUpButton, DpadDownButton, DpadLeftButton, DpadRightButton,
            SquareButton, CrossButton, CircleButton, TriangleButton,
            L1Button, R1Button, L2Button, R2Button,
            ShareButton, OptionsButton, L3Button, R3Button, PsButton, TouchpadButton
        };

        foreach (Button button in stateButtons)
        {
            button.Style = neutralButtonStyle;
        }

        // DS4 raw Y is 0 at the top and 255 at the bottom. The legacy renderer
        // historically subtracts normalized Y from screen Y, so flip the two
        // symmetric stick canvases at presentation time. The modern dashboard
        // maps the coordinate correctly in code and does not need this transform.
        FlipLegacyStickCanvas(LeftStickCanvas);
        FlipLegacyStickCanvas(RightStickCanvas);
    }

    private static void FlipLegacyStickCanvas(Canvas canvas)
    {
        canvas.RenderTransformOrigin = new Point(0.5, 0.5);
        canvas.RenderTransform = new ScaleTransform(1, -1);
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
