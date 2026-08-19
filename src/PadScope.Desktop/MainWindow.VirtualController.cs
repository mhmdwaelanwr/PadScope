using System.Windows;
using System.Windows.Controls;
using PadScope.Core.Diagnostics;
using PadScope.Core.Models;
using PadScope.Hid;
using PadScope.Hid.Virtual;

namespace PadScope.Desktop;

public partial class MainWindow
{
    private Ds4ControllerSession? _virtualSession;
    private IVirtualControllerTarget? _virtualTarget;
    private Ds4PassThrough? _virtualBridge;

    internal void RefreshVirtualDeviceList()
    {
        var devices = _reports.Select(report => report.Device).Distinct().ToList();
        VirtualDeviceComboBox.ItemsSource = devices;
        VirtualDeviceComboBox.SelectedIndex = devices.Count > 0 ? 0 : -1;
        VirtualStartButton.IsEnabled = devices.Count > 0;

        if (devices.Count == 0)
        {
            VirtualStopButton.IsEnabled = false;
            VirtualStatusText.Text = ViGEmBusDetector.DescribeStatus();
        }
    }

    internal void ClearVirtualDeviceList()
    {
        StopVirtualPassthrough();
        VirtualDeviceComboBox.ItemsSource = null;
        VirtualStartButton.IsEnabled = false;
        VirtualStopButton.IsEnabled = false;
        VirtualFeedbackText.Text = "No feedback received yet.";
        VirtualStatusText.Text = "Scan first to detect a controller, then start the passthrough.";
    }

    private void VirtualStartButton_Click(object sender, RoutedEventArgs e)
    {
        if (VirtualDeviceComboBox.SelectedItem is not ControllerDevice device)
        {
            MessageBox.Show(
                this,
                "Scan first, then select a device from the list.",
                "PadScope Virtual Controller",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            return;
        }

        StopVirtualPassthrough();

        IVirtualControllerTarget target = VirtualTargetComboBox.SelectedIndex == 1
            ? new ViGEmXbox360Target()
            : new ViGEmDualShock4Target();

        Ds4ControllerSession session = new(new HidSharpHidInputReader(), device);
        Ds4PassThrough bridge = new(session, target);

        if (!bridge.TryStart(out string? error))
        {
            MessageBox.Show(
                this,
                error ?? "Could not start the virtual controller.",
                "PadScope Virtual Controller",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            session.Dispose();
            bridge.Dispose();
            return;
        }

        _virtualSession = session;
        _virtualTarget = target;
        _virtualBridge = bridge;

        target.FeedbackReceived += OnVirtualFeedback;

        VirtualStartButton.IsEnabled = false;
        VirtualStopButton.IsEnabled = true;
        VirtualStatusText.Text = $"Passthrough live: {device.DisplayName} -> {target.GetType().Name}. Games can now use the virtual pad.";
    }

    private void VirtualStopButton_Click(object sender, RoutedEventArgs e)
    {
        StopVirtualPassthrough();
    }

    private void OnVirtualFeedback(VirtualControllerFeedback feedback)
    {
        Dispatcher.BeginInvoke(() =>
        {
            string line = $"Last game request: rumble small={feedback.SmallMotor} large={feedback.LargeMotor}";

            if (feedback.LedNumber > 0)
            {
                line += $", LED {feedback.LedNumber}";
            }

            if (feedback.Red != 0 || feedback.Green != 0 || feedback.Blue != 0)
            {
                line += $", lightbar #{feedback.Red:X2}{feedback.Green:X2}{feedback.Blue:X2}";
            }

            VirtualFeedbackText.Text = line;
        });
    }

    private void StopVirtualPassthrough()
    {
        if (_virtualTarget is not null)
        {
            _virtualTarget.FeedbackReceived -= OnVirtualFeedback;
        }

        _virtualBridge?.Dispose();
        _virtualSession?.Dispose();
        _virtualBridge = null;
        _virtualSession = null;
        _virtualTarget = null;

        VirtualStartButton.IsEnabled = VirtualDeviceComboBox.Items.Count > 0;
        VirtualStopButton.IsEnabled = false;

        if (IsLoaded)
        {
            VirtualStatusText.Text = "Passthrough stopped.";
        }
    }
}