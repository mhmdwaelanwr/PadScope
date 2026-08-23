using System.Threading;
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
    private CancellationTokenSource? _diagnosticsVibrationCts;

    private ContentControl? _liveWorkspaceContent;
    private Button? _overviewWorkspaceButton;
    private Button? _diagnosticsWorkspaceButton;
    private Button? _advancedWorkspaceButton;
    private UIElement? _overviewWorkspacePage;
    private UIElement? _diagnosticsWorkspacePage;
    private UIElement? _advancedWorkspacePage;

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
        _controllerDiagnosticsLab.VibrationRequested += DiagnosticsLab_VibrationRequested;
        _controllerDiagnosticsLab.StopVibrationRequested += DiagnosticsLab_StopVibrationRequested;

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

        Grid workspaceHost = CreateLiveWorkspaceHost();
        liveTab.Content = workspaceHost;
        SwitchLiveWorkspace(LiveWorkspacePage.Overview);

        RefreshModernDashboard(forceDeviceRefresh: true);

        _modernDashboardTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _modernDashboardTimer.Tick += (_, _) => RefreshModernDashboard(forceDeviceRefresh: false);
        _modernDashboardTimer.Start();

        Closed += (_, _) =>
        {
            _modernDashboardTimer?.Stop();
            _diagnosticsVibrationCts?.Cancel();
            _diagnosticsVibrationCts?.Dispose();
        };
    }

    /// <summary>
    /// Builds a dedicated two-row workspace instead of nesting a TabControl inside
    /// the main TabControl. WPF's nested TabPanel was the source of the clipped
    /// border/underline and content overlap seen at different DPI/window sizes.
    /// </summary>
    private Grid CreateLiveWorkspaceHost()
    {
        Grid host = new()
        {
            Margin = new Thickness(4, 8, 4, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            ClipToBounds = false
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

        _overviewWorkspaceButton = CreateWorkspaceNavigationButton(
            "Overview",
            minWidth: 112,
            "Live controller overview and telemetry");
        _diagnosticsWorkspaceButton = CreateWorkspaceNavigationButton(
            "Diagnostics Lab",
            minWidth: 138,
            "Stick drift, range, polling rate, touchpad, raw HID and vibration diagnostics");
        _advancedWorkspaceButton = CreateWorkspaceNavigationButton(
            "Advanced HID tools",
            minWidth: 168,
            "Capture/replay, raw HID, lightbar, detailed motion data and manual output controls");

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
            Margin = new Thickness(0),
            Padding = new Thickness(0),
            ClipToBounds = true
        };
        Grid.SetRow(_liveWorkspaceContent, 1);
        host.Children.Add(_liveWorkspaceContent);

        return host;
    }

    private Button CreateWorkspaceNavigationButton(string label, double minWidth, string toolTip)
    {
        Button button = new()
        {
            Content = label,
            Style = (Style)FindResource("Sec"),
            Height = 38,
            MinWidth = minWidth,
            Padding = new Thickness(16, 0, 16, 0),
            Margin = new Thickness(0),
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold,
            ToolTip = toolTip,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        return button;
    }

    private void SwitchLiveWorkspace(bool showAdvanced) =>
        SwitchLiveWorkspace(showAdvanced ? LiveWorkspacePage.Advanced : LiveWorkspacePage.Overview);

    private void SwitchLiveWorkspace(LiveWorkspacePage page)
    {
        if (_liveWorkspaceContent is null ||
            _overviewWorkspaceButton is null ||
            _diagnosticsWorkspaceButton is null ||
            _advancedWorkspaceButton is null ||
            _overviewWorkspacePage is null ||
            _diagnosticsWorkspacePage is null ||
            _advancedWorkspacePage is null)
        {
            return;
        }

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
            button.BorderThickness = new Thickness(1);
        }
        else
        {
            button.Background = Brushes.Transparent;
            button.BorderBrush = Brushes.Transparent;
            button.SetResourceReference(Control.ForegroundProperty, "B_TextDim");
            button.BorderThickness = new Thickness(1);
        }
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
        ControllerDiagnosticsLab? diagnostics = _controllerDiagnosticsLab;
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
        diagnostics?.SetSessionState(running);
        diagnostics?.SetDevice(running && _liveSession is not null ? _liveSession.Device : dashboard.SelectedDevice);

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
        if (_liveSession is not { IsRunning: true } session)
        {
            return;
        }

        if (!session.TryResetRumble(out string? error))
        {
            _controllerDiagnosticsLab?.SetOutputStatus(error, success: false);
            LiveStatusText.Text = error ?? "Rumble reset failed.";
            return;
        }

        _controllerDiagnosticsLab?.SetOutputStatus(session.LastOutputWriteStatus, success: true);
        LiveStatusText.Text = "Rumble reset to neutral; lightbar state preserved.";
    }

    private async void DiagnosticsLab_VibrationRequested(object? sender, VibrationRequestEventArgs e)
    {
        if (_liveSession is not { IsRunning: true } session || !PulseRumbleButton.IsEnabled)
        {
            MessageBox.Show(
                this,
                "Start a live hardware session before running a vibration diagnostic.",
                "PadScope Vibration Lab",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (!ConfirmControlledAction(
                $"Run the {e.Pattern} vibration diagnostic for up to {e.DurationMs} ms. PadScope will reset rumble automatically without changing the lightbar.",
                session.Device))
        {
            return;
        }

        _diagnosticsVibrationCts?.Cancel();
        _diagnosticsVibrationCts?.Dispose();
        CancellationTokenSource cts = new();
        _diagnosticsVibrationCts = cts;
        bool completed = false;
        bool cancelled = false;
        string? failure = null;

        try
        {
            await RunDiagnosticsVibrationPatternAsync(session, e, cts.Token);
            completed = true;
            LiveStatusText.Text = $"Vibration diagnostic completed ({e.Pattern}).";
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
            LiveStatusText.Text = "Vibration diagnostic stopped.";
        }
        catch (Exception ex)
        {
            failure = session.LastOutputWriteStatus ?? ex.Message;
            _controllerDiagnosticsLab?.SetOutputStatus(failure, success: false);
            LiveStatusText.Text = failure;
            MessageBox.Show(
                this,
                failure,
                "PadScope Vibration Lab",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            bool resetOk = session.TryResetRumble(out string? resetError);
            if (failure is null)
            {
                if (resetOk)
                {
                    string prefix = completed
                        ? "Vibration completed"
                        : cancelled
                            ? "Vibration stopped"
                            : "Vibration ended";
                    string detail = session.LastOutputWriteStatus ?? "rumble reset succeeded";
                    _controllerDiagnosticsLab?.SetOutputStatus($"{prefix} · {detail}", success: true);
                    LiveStatusText.Text += " Rumble reset; lightbar preserved.";
                }
                else
                {
                    _controllerDiagnosticsLab?.SetOutputStatus(resetError, success: false);
                    LiveStatusText.Text += $" Rumble reset failed: {resetError}";
                }
            }

            if (ReferenceEquals(_diagnosticsVibrationCts, cts))
            {
                _diagnosticsVibrationCts = null;
            }
            cts.Dispose();
        }
    }

    private void DiagnosticsLab_StopVibrationRequested(object? sender, EventArgs e)
    {
        _diagnosticsVibrationCts?.Cancel();
        if (_liveSession is not { IsRunning: true } session)
        {
            return;
        }

        if (session.TryResetRumble(out string? error))
        {
            _controllerDiagnosticsLab?.SetOutputStatus(session.LastOutputWriteStatus, success: true);
        }
        else
        {
            _controllerDiagnosticsLab?.SetOutputStatus(error, success: false);
        }
    }

    private static async Task RunDiagnosticsVibrationPatternAsync(
        Ds4ControllerSession session,
        VibrationRequestEventArgs request,
        CancellationToken cancellationToken)
    {
        static byte Scale(byte value, double factor) =>
            (byte)Math.Clamp((int)Math.Round(value * factor), 0, 255);

        async Task Segment(byte small, byte large, int durationMs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!session.TrySendRumble(small, large, out string? error))
            {
                throw new InvalidOperationException(error ?? "Rumble write failed.");
            }
            await Task.Delay(Math.Max(20, durationMs), cancellationToken);
        }

        async Task Pause(int durationMs)
        {
            if (!session.TryResetRumble(out string? error))
            {
                throw new InvalidOperationException(error ?? "Rumble pause/reset failed.");
            }
            await Task.Delay(Math.Max(20, durationMs), cancellationToken);
        }

        int duration = Math.Clamp(request.DurationMs, 80, 3000);
        switch (request.Pattern)
        {
            case VibrationPattern.Heartbeat:
                await Segment(request.SmallMotor, request.LargeMotor, Math.Min(180, duration / 3));
                await Pause(90);
                await Segment(Scale(request.SmallMotor, 0.85), Scale(request.LargeMotor, 0.90), Math.Min(240, duration / 2));
                break;

            case VibrationPattern.Explosion:
                int third = Math.Max(70, duration / 3);
                await Segment(request.SmallMotor, request.LargeMotor, third);
                await Segment(Scale(request.SmallMotor, 0.65), Scale(request.LargeMotor, 0.72), third);
                await Segment(Scale(request.SmallMotor, 0.30), Scale(request.LargeMotor, 0.38), third);
                break;

            case VibrationPattern.Click:
                await Segment(request.SmallMotor, request.LargeMotor, Math.Min(duration, 140));
                break;

            default:
                await Segment(request.SmallMotor, request.LargeMotor, duration);
                break;
        }
    }
}
