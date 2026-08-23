using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using PadScope.Core.Models;

namespace PadScope.Desktop;

public partial class MainWindow
{
    private ModernLiveDashboard? _modernLiveDashboard;
    private ControllerDiagnosticsLab? _controllerDiagnosticsLab;
    private DispatcherTimer? _modernDashboardTimer;
    private string _modernDeviceFingerprint = string.Empty;

    private ContentControl? _liveWorkspaceContent;
    private Button? _overviewWorkspaceButton;
    private Button? _diagnosticsWorkspaceButton;
    private Button? _advancedWorkspaceButton;
    private UIElement? _overviewWorkspacePage;
    private UIElement? _diagnosticsWorkspacePage;
    private UIElement? _advancedWorkspacePage;
    private Button? _lightbarPickerButton;
    private Border? _lightbarPreview;

    private enum LiveWorkspacePage
    {
        Overview,
        Diagnostics,
        Advanced
    }

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

        _controllerDiagnosticsLab = new ControllerDiagnosticsLab
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            MinWidth = 0
        };

        ScrollViewer overviewScroll = new()
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            CanContentScroll = false,
            Content = _modernLiveDashboard
        };

        ScrollViewer diagnosticsScroll = new()
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            CanContentScroll = false,
            Content = _controllerDiagnosticsLab
        };

        Border advancedSurface = new()
        {
            Background = Brushes.Transparent,
            BorderBrush = (Brush)FindResource("B_Border"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(10),
            Margin = new Thickness(4, 0, 4, 12),
            Child = legacyContent
        };

        ScrollViewer advancedScroll = new()
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            CanContentScroll = false,
            Content = advancedSurface
        };

        _overviewWorkspacePage = overviewScroll;
        _diagnosticsWorkspacePage = diagnosticsScroll;
        _advancedWorkspacePage = advancedScroll;

        liveTab.Content = CreateLiveWorkspaceHost();
        SwitchLiveWorkspace(LiveWorkspacePage.Overview);
        RefreshModernDashboard(forceDeviceRefresh: true);

        _modernDashboardTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _modernDashboardTimer.Tick += (_, _) => RefreshModernDashboard(forceDeviceRefresh: false);
        _modernDashboardTimer.Start();
        Closed += (_, _) => _modernDashboardTimer?.Stop();
    }

    private Grid CreateLiveWorkspaceHost()
    {
        Grid host = new()
        {
            Margin = new Thickness(4, 8, 4, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        host.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        host.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        Border navigationRail = new()
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Padding = new Thickness(4),
            Margin = new Thickness(0, 0, 0, 14),
            CornerRadius = new CornerRadius(15),
            BorderThickness = new Thickness(1)
        };
        navigationRail.SetResourceReference(Border.BackgroundProperty, "B_CardAlt");
        navigationRail.SetResourceReference(Border.BorderBrushProperty, "B_Border");

        StackPanel navigationButtons = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };

        _overviewWorkspaceButton = CreateWorkspaceNavigationButton("Overview", 112, "Live controller overview and telemetry");
        _diagnosticsWorkspaceButton = CreateWorkspaceNavigationButton("Diagnostics Lab", 138, "Stick drift, range, polling and touchpad diagnostics");
        _advancedWorkspaceButton = CreateWorkspaceNavigationButton("Advanced HID tools", 168, "Capture/replay, raw HID, motion and controlled output tools");

        _overviewWorkspaceButton.Margin = new Thickness(0, 0, 6, 0);
        _diagnosticsWorkspaceButton.Margin = new Thickness(0, 0, 6, 0);
        _overviewWorkspaceButton.Click += (_, _) => SwitchLiveWorkspace(LiveWorkspacePage.Overview);
        _diagnosticsWorkspaceButton.Click += (_, _) => SwitchLiveWorkspace(LiveWorkspacePage.Diagnostics);
        _advancedWorkspaceButton.Click += (_, _) => SwitchLiveWorkspace(LiveWorkspacePage.Advanced);

        navigationButtons.Children.Add(_overviewWorkspaceButton);
        navigationButtons.Children.Add(_diagnosticsWorkspaceButton);
        navigationButtons.Children.Add(_advancedWorkspaceButton);
        navigationRail.Child = navigationButtons;
        host.Children.Add(navigationRail);

        _liveWorkspaceContent = new ContentControl
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            ClipToBounds = true
        };
        Grid.SetRow(_liveWorkspaceContent, 1);
        host.Children.Add(_liveWorkspaceContent);
        return host;
    }

    private Button CreateWorkspaceNavigationButton(string label, double minWidth, string toolTip) => new()
    {
        Content = label,
        Style = (Style)FindResource("Sec"),
        Height = 38,
        MinWidth = minWidth,
        Padding = new Thickness(16, 0, 16, 0),
        FontSize = 12.5,
        FontWeight = FontWeights.SemiBold,
        ToolTip = toolTip,
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Center
    };

    private void SwitchLiveWorkspace(LiveWorkspacePage page)
    {
        if (_liveWorkspaceContent is null || _overviewWorkspaceButton is null || _diagnosticsWorkspaceButton is null ||
            _advancedWorkspaceButton is null || _overviewWorkspacePage is null || _diagnosticsWorkspacePage is null || _advancedWorkspacePage is null)
            return;

        _liveWorkspaceContent.Content = page switch
        {
            LiveWorkspacePage.Diagnostics => _diagnosticsWorkspacePage,
            LiveWorkspacePage.Advanced => _advancedWorkspacePage,
            _ => _overviewWorkspacePage
        };

        SetWorkspaceNavigationState(_overviewWorkspaceButton, page == LiveWorkspacePage.Overview);
        SetWorkspaceNavigationState(_diagnosticsWorkspaceButton, page == LiveWorkspacePage.Diagnostics);
        SetWorkspaceNavigationState(_advancedWorkspaceButton, page == LiveWorkspacePage.Advanced);
    }

    private static void SetWorkspaceNavigationState(Button button, bool isSelected)
    {
        if (isSelected)
        {
            button.SetResourceReference(Control.BackgroundProperty, "B_PrimarySoft");
            button.SetResourceReference(Control.BorderBrushProperty, "B_Primary");
            button.SetResourceReference(Control.ForegroundProperty, "B_Text");
        }
        else
        {
            button.Background = Brushes.Transparent;
            button.BorderBrush = Brushes.Transparent;
            button.SetResourceReference(Control.ForegroundProperty, "B_TextDim");
        }
        button.BorderThickness = new Thickness(1);
    }

    private void PrepareLegacyLiveTools()
    {
        Style neutralButtonStyle = (Style)FindResource("Sec");
        Button[] stateButtons =
        {
            DpadUpButton, DpadDownButton, DpadLeftButton, DpadRightButton,
            SquareButton, CrossButton, CircleButton, TriangleButton,
            L1Button, R1Button, L2Button, R2Button,
            ShareButton, OptionsButton, L3Button, R3Button, PsButton, TouchpadButton
        };

        foreach (Button button in stateButtons) button.Style = neutralButtonStyle;
        FlipLegacyStickCanvas(LeftStickCanvas);
        FlipLegacyStickCanvas(RightStickCanvas);
        InstallLightbarPicker();
    }

    private void InstallLightbarPicker()
    {
        if (_lightbarPickerButton is not null || SetLightbarButton.Parent is not Panel actionsPanel) return;

        _lightbarPreview = new Border
        {
            Width = 34,
            Height = 34,
            CornerRadius = new CornerRadius(9),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 8, 0),
            ToolTip = "Selected lightbar preview"
        };
        _lightbarPreview.SetResourceReference(Border.BorderBrushProperty, "B_Border");

        _lightbarPickerButton = new Button
        {
            Content = "Pick color",
            Style = (Style)FindResource("Sec"),
            Height = 36,
            MinWidth = 92,
            Margin = new Thickness(0, 0, 8, 0),
            ToolTip = "Choose RGB/HEX lightbar color without sending output"
        };
        _lightbarPickerButton.Click += (_, _) => PickLightbarColor();

        int index = actionsPanel.Children.IndexOf(SetLightbarButton);
        if (index < 0) index = 0;
        actionsPanel.Children.Insert(index, _lightbarPreview);
        actionsPanel.Children.Insert(index + 1, _lightbarPickerButton);

        LightbarRedSlider.ValueChanged += (_, _) => RefreshLightbarPreview();
        LightbarGreenSlider.ValueChanged += (_, _) => RefreshLightbarPreview();
        LightbarBlueSlider.ValueChanged += (_, _) => RefreshLightbarPreview();
        RefreshLightbarPreview();
    }

    private void PickLightbarColor()
    {
        Color initial = Color.FromRgb(
            (byte)Math.Round(LightbarRedSlider.Value),
            (byte)Math.Round(LightbarGreenSlider.Value),
            (byte)Math.Round(LightbarBlueSlider.Value));
        LightbarColorPickerWindow picker = new(initial) { Owner = this };
        if (picker.ShowDialog() != true) return;

        LightbarRedSlider.Value = picker.SelectedColor.R;
        LightbarGreenSlider.Value = picker.SelectedColor.G;
        LightbarBlueSlider.Value = picker.SelectedColor.B;
        RefreshLightbarPreview();
    }

    private void RefreshLightbarPreview()
    {
        if (_lightbarPreview is null) return;
        Color color = Color.FromRgb(
            (byte)Math.Round(LightbarRedSlider.Value),
            (byte)Math.Round(LightbarGreenSlider.Value),
            (byte)Math.Round(LightbarBlueSlider.Value));
        _lightbarPreview.Background = new SolidColorBrush(color);
        _lightbarPreview.ToolTip = $"RGB({color.R}, {color.G}, {color.B}) · #{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private static void FlipLegacyStickCanvas(Canvas canvas)
    {
        canvas.RenderTransformOrigin = new Point(0.5, 0.5);
        canvas.RenderTransform = new ScaleTransform(1, -1);
    }

    private void RefreshModernDashboard(bool forceDeviceRefresh)
    {
        ModernLiveDashboard? dashboard = _modernLiveDashboard;
        ControllerDiagnosticsLab? diagnostics = _controllerDiagnosticsLab;
        if (dashboard is null) return;

        List<ControllerDevice> devices = _reports.Select(report => report.Device).Distinct().ToList();
        string fingerprint = string.Join("|", devices.Select(device => $"{device.DevicePath}\u001f{device.DisplayName}\u001f{device.VendorId}:{device.ProductId}"));

        if (forceDeviceRefresh || !string.Equals(fingerprint, _modernDeviceFingerprint, StringComparison.Ordinal))
        {
            _modernDeviceFingerprint = fingerprint;
            dashboard.SetDevices(devices);
        }

        bool running = _liveSession is { IsRunning: true };
        dashboard.SetSessionState(running, running ? LiveStatusText.Text : "Waiting for live input");
        dashboard.SetOutputEnabled(running && PulseRumbleButton.IsEnabled);
        diagnostics?.SetSessionState(running);
        diagnostics?.SetDevice(dashboard.SelectedDevice);

        var state = _latestState;
        if (state is not null)
        {
            dashboard.UpdateTelemetry(state, _latestTiming);
            diagnostics?.UpdateTelemetry(state, _latestTiming);
        }
    }

    private void ModernDashboard_StartRequested(object? sender, EventArgs e)
    {
        if (_modernLiveDashboard?.SelectedDevice is not ControllerDevice device)
        {
            MessageBox.Show(this, "Scan first, then select a controller.", "PadScope Live Input", MessageBoxButton.OK, MessageBoxImage.Information);
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
            MessageBox.Show(this, "Start a live hardware session before running a vibration test.", "PadScope Vibration", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        RumbleSmallSlider.Value = e.SmallMotor;
        RumbleLargeSlider.Value = e.LargeMotor;
        PulseRumbleButton_Click(this, new RoutedEventArgs());
    }

    private void ModernDashboard_ResetRumbleRequested(object? sender, EventArgs e)
    {
        if (_liveSession is null) return;
        ResetOutputButton_Click(this, new RoutedEventArgs());
    }
}
