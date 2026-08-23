using System.Windows;

namespace PadScope.Desktop;

public partial class ControllerDiagnosticsLab
{
    private bool _externalOutputAvailable;
    private string _externalOutputStatus = "Start live input first";

    public void SetOutputAvailability(bool available, string? status)
    {
        _externalOutputAvailable = available;
        if (!string.IsNullOrWhiteSpace(status))
        {
            _externalOutputStatus = status;
        }

        ApplyExternalOutputState();
    }

    public void SetOutputStatus(string status)
    {
        if (!string.IsNullOrWhiteSpace(status))
        {
            _externalOutputStatus = status;
            VibrationStatusText.Text = status;
        }
    }

    internal void ApplyExternalOutputState()
    {
        bool enabled = _sessionRunning && _externalOutputAvailable;
        StartVibrationButton.IsEnabled = enabled;
        StopVibrationButton.IsEnabled = enabled;
        LargeMotorSlider.IsEnabled = enabled;
        SmallMotorSlider.IsEnabled = enabled;
        VibrationDurationSlider.IsEnabled = enabled;
        VibrationStatusText.Text = _externalOutputStatus;

        if (!enabled)
        {
            StartVibrationButton.ToolTip = _externalOutputStatus;
        }
        else
        {
            StartVibrationButton.ClearValue(FrameworkElement.ToolTipProperty);
        }
    }
}
