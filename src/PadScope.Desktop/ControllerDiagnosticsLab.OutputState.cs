using System.Windows;
using System.Windows.Threading;

namespace PadScope.Desktop;

public partial class ControllerDiagnosticsLab
{
    private bool _externalOutputAvailable;
    private string _externalOutputStatus = "Start live input first";
    private DispatcherTimer? _externalOutputStateTimer;

    public void SetOutputAvailability(bool available, string? status)
    {
        _externalOutputAvailable = available;
        if (!string.IsNullOrWhiteSpace(status))
        {
            _externalOutputStatus = status;
        }

        EnsureExternalOutputStateTimer();
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

    private void EnsureExternalOutputStateTimer()
    {
        if (_externalOutputStateTimer is not null)
        {
            return;
        }

        _externalOutputStateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _externalOutputStateTimer.Tick += (_, _) => ApplyExternalOutputState();
        _externalOutputStateTimer.Start();
        Unloaded += (_, _) => _externalOutputStateTimer?.Stop();
        Loaded += (_, _) => _externalOutputStateTimer?.Start();
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
