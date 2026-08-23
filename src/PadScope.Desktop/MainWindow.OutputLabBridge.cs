using System.Windows;
using System.Windows.Controls;
using PadScope.Core.Models;

namespace PadScope.Desktop;

public partial class MainWindow
{
    private ControllerOutputLab? _controllerOutputLab;
    private Button? _outputWorkspaceButton;
    private UIElement? _outputWorkspacePage;
    private CancellationTokenSource? _outputPulseCancellation;

    private void InstallControllerOutputLab()
    {
        if (_controllerOutputLab is not null ||
            _liveWorkspaceContent is null ||
            _advancedWorkspaceButton?.Parent is not StackPanel navigationButtons)
            return;

        _controllerOutputLab = new ControllerOutputLab
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            MinWidth = 0
        };
        _controllerOutputLab.RumbleRequested += OutputLab_RumbleRequested;
        _controllerOutputLab.StopRumbleRequested += OutputLab_StopRumbleRequested;
        _controllerOutputLab.LightbarRequested += OutputLab_LightbarRequested;
        _controllerOutputLab.ResetOutputRequested += OutputLab_ResetOutputRequested;

        _outputWorkspacePage = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            CanContentScroll = false,
            Content = _controllerOutputLab
        };

        _outputWorkspaceButton = CreateWorkspaceNavigationButton(
            "Output Lab", 116, "Vibration, lightbar and controlled hardware output tests");
        _outputWorkspaceButton.Margin = new Thickness(0, 0, 6, 0);
        _outputWorkspaceButton.Click += (_, _) => ShowOutputWorkspace();

        int advancedIndex = navigationButtons.Children.IndexOf(_advancedWorkspaceButton);
        if (advancedIndex < 0) advancedIndex = navigationButtons.Children.Count;
        navigationButtons.Children.Insert(advancedIndex, _outputWorkspaceButton);

        _overviewWorkspaceButton?.AddHandler(Button.ClickEvent, new RoutedEventHandler((_, _) => SetOutputNavSelected(false)));
        _diagnosticsWorkspaceButton?.AddHandler(Button.ClickEvent, new RoutedEventHandler((_, _) => SetOutputNavSelected(false)));
        _advancedWorkspaceButton?.AddHandler(Button.ClickEvent, new RoutedEventHandler((_, _) => SetOutputNavSelected(false)));

        if (_modernDashboardTimer is not null)
        {
            _modernDashboardTimer.Tick += (_, _) =>
            {
                RefreshOutputLabAvailability();
                DrainRawPollingIntervals();
            };
        }

        Closed += (_, _) =>
        {
            _outputPulseCancellation?.Cancel();
            _outputPulseCancellation?.Dispose();
            _outputPulseCancellation = null;
        };

        RefreshOutputLabAvailability();
    }

    private void DrainRawPollingIntervals()
    {
        if (_liveSession is null || _controllerDiagnosticsLab is null) return;
        IReadOnlyList<double> intervals = _liveSession.DrainReportIntervals(512);
        _controllerDiagnosticsLab.AcceptRawPollingIntervals(intervals);
    }

    private void ShowOutputWorkspace()
    {
        if (_liveWorkspaceContent is null || _outputWorkspacePage is null || _outputWorkspaceButton is null) return;
        _liveWorkspaceContent.Content = _outputWorkspacePage;
        if (_overviewWorkspaceButton is not null) SetWorkspaceNavigationState(_overviewWorkspaceButton, false);
        if (_diagnosticsWorkspaceButton is not null) SetWorkspaceNavigationState(_diagnosticsWorkspaceButton, false);
        if (_advancedWorkspaceButton is not null) SetWorkspaceNavigationState(_advancedWorkspaceButton, false);
        SetWorkspaceNavigationState(_outputWorkspaceButton, true);
        RefreshOutputLabAvailability();
    }

    private void SetOutputNavSelected(bool selected)
    {
        if (_outputWorkspaceButton is not null) SetWorkspaceNavigationState(_outputWorkspaceButton, selected);
    }

    private void RefreshOutputLabAvailability()
    {
        if (_controllerOutputLab is null) return;
        bool running = _liveSession is { IsRunning: true };
        bool outputAvailable = running && !_nativeOutputRejected;
        _controllerOutputLab.SetAvailability(running, outputAvailable, _nativeOutputFailure);
    }

    private async void OutputLab_RumbleRequested(object? sender, OutputRumbleRequestedEventArgs e)
    {
        if (_controllerOutputLab is null || _liveSession is null) return;

        ControllerDevice? device = DeviceComboBox.SelectedItem as ControllerDevice;
        if (!ConfirmControlledAction(
                $"Run vibration for {e.DurationMs} ms (low-frequency {e.LowMotor}, high-frequency {e.HighMotor}).",
                device))
            return;

        _outputPulseCancellation?.Cancel();
        _outputPulseCancellation?.Dispose();
        CancellationTokenSource pulse = new();
        _outputPulseCancellation = pulse;

        _controllerOutputLab.SetBusy(true, "Sending vibration…");
        // DS4 protocol: small motor = high-frequency, large motor = low-frequency.
        (bool success, string? error) = await SendRumbleResponsiveAsync(e.HighMotor, e.LowMotor);
        if (!success)
        {
            _controllerOutputLab.SetBusy(false);
            _controllerOutputLab.SetStatus("Vibration unavailable on this HID path; live input remains active.", error);
            RefreshOutputLabAvailability();
            return;
        }

        _controllerOutputLab.SetStatus($"Vibration active · {e.DurationMs} ms · {_liveSession.LastOutputWriteStatus}");
        try { await Task.Delay(e.DurationMs, pulse.Token); }
        catch (OperationCanceledException) { }

        if (!pulse.IsCancellationRequested)
        {
            (bool stopped, string? stopError) = await ResetRumbleResponsiveAsync(allowAfterRejection: true);
            _controllerOutputLab.SetStatus(
                stopped ? "Vibration test complete." : "Could not stop vibration cleanly.",
                stopped ? null : stopError);
        }

        if (ReferenceEquals(_outputPulseCancellation, pulse))
        {
            _outputPulseCancellation.Dispose();
            _outputPulseCancellation = null;
        }
        _controllerOutputLab.SetBusy(false);
        RefreshOutputLabAvailability();
    }

    private async void OutputLab_StopRumbleRequested(object? sender, EventArgs e)
    {
        if (_controllerOutputLab is null || _liveSession is null) return;
        _outputPulseCancellation?.Cancel();
        _controllerOutputLab.SetBusy(true, "Stopping vibration…");
        (bool success, string? error) = await ResetRumbleResponsiveAsync(allowAfterRejection: true);
        _controllerOutputLab.SetBusy(false);
        _controllerOutputLab.SetStatus(
            success ? "Vibration stopped." : "Vibration stop was rejected; live input remains active.",
            success ? null : error);
        RefreshOutputLabAvailability();
    }

    private async void OutputLab_LightbarRequested(object? sender, OutputLightbarRequestedEventArgs e)
    {
        if (_controllerOutputLab is null || _liveSession is null) return;
        ControllerDevice? device = DeviceComboBox.SelectedItem as ControllerDevice;
        if (!ConfirmControlledAction($"Set controller lightbar to RGB({e.Red}, {e.Green}, {e.Blue}).", device)) return;

        _controllerOutputLab.SetBusy(true, "Sending lightbar color…");
        (bool success, string? error) = await SendLightbarResponsiveAsync(e.Red, e.Green, e.Blue);
        _controllerOutputLab.SetBusy(false);
        _controllerOutputLab.SetStatus(
            success ? $"Lightbar sent · #{e.Red:X2}{e.Green:X2}{e.Blue:X2} · {_liveSession.LastOutputWriteStatus}"
                    : "Lightbar unavailable on this HID path; live input remains active.",
            success ? null : error);
        RefreshOutputLabAvailability();
    }

    private async void OutputLab_ResetOutputRequested(object? sender, EventArgs e)
    {
        if (_controllerOutputLab is null || _liveSession is null) return;
        _outputPulseCancellation?.Cancel();
        _controllerOutputLab.SetBusy(true, "Resetting output…");
        (bool success, string? error) = await ResetOutputResponsiveAsync(allowAfterRejection: true);
        _controllerOutputLab.SetBusy(false);
        _controllerOutputLab.SetStatus(
            success ? "Controller output reset to neutral." : "Output reset failed; live input remains active.",
            success ? null : error);
        RefreshOutputLabAvailability();
    }
}
