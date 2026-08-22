using System.Windows;
using PadScope.Core.Input;
using PadScope.Core.Models;
using PadScope.Hid;
using PadScope.Hid.Mouse;

namespace PadScope.Desktop;

public partial class MainWindow
{
    private Ds4ControllerSession? _mouseSession;
    private MouseEmulationBridge? _mouseBridge;

    internal void RefreshMouseDeviceList()
    {
        var devices = _reports.Select(report => report.Device).Distinct().ToList();
        MouseDeviceComboBox.ItemsSource = devices;
        MouseDeviceComboBox.SelectedIndex = devices.Count > 0 ? 0 : -1;
        MouseStartButton.IsEnabled = devices.Count > 0;

        if (devices.Count == 0)
        {
            MouseStopButton.IsEnabled = false;
            MouseStatusText.Text = "Scan first to detect a controller.";
        }
    }

    internal void ClearMouseDeviceList()
    {
        StopMouseEmulation();
        MouseDeviceComboBox.ItemsSource = null;
        MouseStartButton.IsEnabled = false;
        MouseStopButton.IsEnabled = false;
        MouseStatusText.Text = "Scan first to detect a controller, then start mouse emulation.";
    }

    private void MouseStartButton_Click(object sender, RoutedEventArgs e)
    {
        if (MouseDeviceComboBox.SelectedItem is not ControllerDevice device)
        {
            MessageBox.Show(
                this,
                "Scan first, then select a device from the list.",
                "PadScope Mouse Lab",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            return;
        }

        if (!ConfirmControlledAction(
                "Allow this controller to move and click the Windows mouse until you press Stop.",
                device))
        {
            return;
        }

        StopMouseEmulation();

        bool touch = MouseTouchCheckBox.IsChecked == true;
        bool gyro = MouseGyroCheckBox.IsChecked == true;
        double sensitivity = MouseSensitivitySlider.Value;

        Ds4ControllerSession session = new(new HidSharpHidInputReader(), device);
        MouseEmulationBridge bridge = new(
            session,
            new WindowsMouseSink(),
            touch ? new TouchpadMouseSettings { Sensitivity = sensitivity } : null,
            gyro ? new GyroMouseSettings { Sensitivity = sensitivity } : null);

        if (!bridge.TryStart(out string? error))
        {
            MessageBox.Show(
                this,
                error ?? "Could not start mouse emulation.",
                "PadScope Mouse Lab",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            session.Dispose();
            bridge.Dispose();
            return;
        }

        _mouseSession = session;
        _mouseBridge = bridge;

        MouseStartButton.IsEnabled = false;
        MouseStopButton.IsEnabled = true;
        MouseStatusText.Text = $"Mouse emulation live: touchpad={touch} gyro={gyro} sensitivity={sensitivity:F2}. Move the touchpad or tilt the controller.";
    }

    private void MouseStopButton_Click(object sender, RoutedEventArgs e)
    {
        StopMouseEmulation();
    }

    private void MouseSensitivitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MouseSensitivityText is not null)
        {
            MouseSensitivityText.Text = MouseSensitivitySlider.Value.ToString("F2");
        }
    }

    private void StopMouseEmulation()
    {
        _mouseBridge?.Dispose();
        _mouseSession?.Dispose();
        _mouseBridge = null;
        _mouseSession = null;

        MouseStartButton.IsEnabled = MouseDeviceComboBox.Items.Count > 0;
        MouseStopButton.IsEnabled = false;

        if (IsLoaded)
        {
            MouseStatusText.Text = "Mouse emulation stopped.";
        }
    }
}
