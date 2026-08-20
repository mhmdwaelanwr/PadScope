using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using PadScope.Core.Diagnostics;
using PadScope.Core.Input;
using PadScope.Core.Models;
using PadScope.Hid;
using PadScope.Hid.Virtual;

namespace PadScope.Desktop;

public partial class MainWindow
{
    private Ds4ControllerSession? _virtualSession;
    private IVirtualControllerTarget? _virtualTarget;
    private Ds4PassThrough? _virtualBridge;
    private ControllerProfile? _virtualProfile;

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
        _virtualProfile = null;
        VirtualDeviceComboBox.ItemsSource = null;
        VirtualStartButton.IsEnabled = false;
        VirtualStopButton.IsEnabled = false;
        VirtualFeedbackText.Text = "No feedback received yet.";
        VirtualProfileStatusText.Text = "No profile applied. Remapping is off.";
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
        Ds4PassThrough bridge = new(session, target, _virtualProfile);

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

    private void VirtualProfileApplyButton_Click(object sender, RoutedEventArgs e)
    {
        string path = VirtualProfileTextBox.Text.Trim();
        if (path.Length == 0)
        {
            MessageBox.Show(
                this,
                "Enter a profile file path or click Create Default first.",
                "PadScope Profile",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            return;
        }

        try
        {
            ControllerProfile profile = ProfileStore.Load(path);
            _virtualProfile = profile;
            VirtualProfileStatusText.Text = $"Profile applied: {profile.Name} v{profile.Version}. Remapping is on.";
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException or ArgumentException)
        {
            MessageBox.Show(
                this,
                $"Could not load profile '{path}': {ex.Message}",
                "PadScope Profile",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    private void VirtualProfileDefaultButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string path = ProfileStore.SaveDefaultProfile();
            VirtualProfileTextBox.Text = path;
            _virtualProfile = ProfileStore.CreateDefault();
            VirtualProfileStatusText.Text = $"Default profile saved and applied ({path}). Remapping is on.";
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException or ArgumentException)
        {
            MessageBox.Show(
                this,
                $"Could not create the default profile: {ex.Message}",
                "PadScope Profile",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }
}