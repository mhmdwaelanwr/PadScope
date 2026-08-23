using System.Windows;
using System.Windows.Controls;

namespace PadScope.Desktop;

public partial class ControllerDiagnosticsLab
{
    private bool _externalOutputAvailable;
    private string _externalOutputStatus = "Start live input first";
    private bool _externalOutputGuardInstalled;
    private bool _applyingExternalOutputState;

    public void SetOutputAvailability(bool available, string? status)
    {
        _externalOutputAvailable = available;
        if (!string.IsNullOrWhiteSpace(status))
        {
            _externalOutputStatus = status;
        }

        EnsureExternalOutputGuard();
        ApplyExternalOutputState();
    }

    public void SetOutputStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return;
        }

        _externalOutputStatus = status;
        VibrationStatusText.Text = status;
    }

    private void EnsureExternalOutputGuard()
    {
        if (_externalOutputGuardInstalled)
        {
            return;
        }

        _externalOutputGuardInstalled = true;
        foreach (UIElement element in OutputElements())
        {
            element.IsEnabledChanged += OutputElement_IsEnabledChanged;
        }
    }

    private void OutputElement_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_applyingExternalOutputState)
        {
            return;
        }

        // RefreshModernDashboard calls SetSessionState every 50 ms. That method is
        // allowed to enable general diagnostics, but it must not be able to reopen
        // the vibration controls after the HID output path has been rejected.
        bool shouldBeEnabled = _sessionRunning && _externalOutputAvailable;
        if (sender is UIElement element && element.IsEnabled != shouldBeEnabled)
        {
            ApplyExternalOutputState();
        }
    }

    internal void ApplyExternalOutputState()
    {
        if (_applyingExternalOutputState)
        {
            return;
        }

        _applyingExternalOutputState = true;
        try
        {
            bool enabled = _sessionRunning && _externalOutputAvailable;
            foreach (UIElement element in OutputElements())
            {
                element.IsEnabled = enabled;
                element.IsHitTestVisible = enabled;
                element.Opacity = enabled ? 1.0 : 0.55;
            }

            VibrationStatusText.Text = _externalOutputStatus;
            VibrationStatusText.SetResourceReference(
                TextBlock.ForegroundProperty,
                _sessionRunning && !_externalOutputAvailable ? "B_Warning" : "B_TextDim");

            if (!enabled)
            {
                StartVibrationButton.ToolTip = _externalOutputStatus;
                StartVibrationButton.Focusable = false;
            }
            else
            {
                StartVibrationButton.ClearValue(FrameworkElement.ToolTipProperty);
                StartVibrationButton.Focusable = true;
            }
        }
        finally
        {
            _applyingExternalOutputState = false;
        }
    }

    private UIElement[] OutputElements() =>
    [
        StartVibrationButton,
        StopVibrationButton,
        LargeMotorSlider,
        SmallMotorSlider,
        VibrationDurationSlider
    ];
}
