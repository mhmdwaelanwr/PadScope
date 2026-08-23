using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PadScope.Desktop;

public partial class ControllerOutputLab : UserControl
{
    private bool _sessionRunning;
    private bool _outputAvailable;
    private bool _busy;

    public event EventHandler<OutputRumbleRequestedEventArgs>? RumbleRequested;
    public event EventHandler? StopRumbleRequested;
    public event EventHandler<OutputLightbarRequestedEventArgs>? LightbarRequested;
    public event EventHandler? ResetOutputRequested;

    public ControllerOutputLab()
    {
        InitializeComponent();
        LowMotorSlider.ValueChanged += (_, _) => LowMotorValue.Text = ((int)LowMotorSlider.Value).ToString();
        HighMotorSlider.ValueChanged += (_, _) => HighMotorValue.Text = ((int)HighMotorSlider.Value).ToString();
        DurationSlider.ValueChanged += (_, _) => DurationValue.Text = $"{(int)DurationSlider.Value} ms";
        RedSlider.ValueChanged += (_, _) => RefreshLightbarPreview();
        GreenSlider.ValueChanged += (_, _) => RefreshLightbarPreview();
        BlueSlider.ValueChanged += (_, _) => RefreshLightbarPreview();
        RefreshLightbarPreview();
        ApplyAvailability();
    }

    public void SetAvailability(bool sessionRunning, bool outputAvailable, string? detail = null)
    {
        _sessionRunning = sessionRunning;
        _outputAvailable = outputAvailable;
        CapabilityText.Text = !sessionRunning
            ? "Start live input"
            : outputAvailable
                ? "Output ready"
                : "Input-only session";
        CapabilityText.SetResourceReference(TextBlock.ForegroundProperty,
            outputAvailable ? "B_Success" : sessionRunning ? "B_Warning" : "B_TextDim");

        if (!string.IsNullOrWhiteSpace(detail) && !outputAvailable)
        {
            StatusText.Text = "Native DS4 output is unavailable for this controller path; input diagnostics remain active.";
            TechnicalDetailText.Text = detail;
        }
        else if (!sessionRunning)
        {
            StatusText.Text = "Start a live session to use controlled output tests.";
            TechnicalDetailText.Text = string.Empty;
        }
        else if (outputAvailable && !_busy)
        {
            StatusText.Text = "Output path is available. Every write still goes through PadScope confirmation.";
            TechnicalDetailText.Text = string.Empty;
        }

        ApplyAvailability();
    }

    public void SetBusy(bool busy, string? status = null)
    {
        _busy = busy;
        if (!string.IsNullOrWhiteSpace(status)) StatusText.Text = status;
        ApplyAvailability();
    }

    public void SetStatus(string message, string? technicalDetail = null)
    {
        StatusText.Text = message;
        TechnicalDetailText.Text = technicalDetail ?? string.Empty;
    }

    private void ApplyAvailability()
    {
        bool enabled = _sessionRunning && _outputAvailable && !_busy;
        bool canStop = _sessionRunning && _outputAvailable;
        StartVibrationButton.IsEnabled = enabled;
        StopVibrationButton.IsEnabled = canStop;
        SetLightbarButton.IsEnabled = enabled;
        ResetOutputButton.IsEnabled = enabled;
        LowMotorSlider.IsEnabled = enabled;
        HighMotorSlider.IsEnabled = enabled;
        DurationSlider.IsEnabled = enabled;
        RedSlider.IsEnabled = enabled;
        GreenSlider.IsEnabled = enabled;
        BlueSlider.IsEnabled = enabled;
    }

    private void Heartbeat_Click(object sender, RoutedEventArgs e) => SetVibrationPreset(80, 185, 420);
    private void Explosion_Click(object sender, RoutedEventArgs e) => SetVibrationPreset(225, 245, 800);
    private void ClickPreset_Click(object sender, RoutedEventArgs e) => SetVibrationPreset(220, 50, 110);
    private void Balanced_Click(object sender, RoutedEventArgs e) => SetVibrationPreset(150, 150, 500);

    private void SetVibrationPreset(byte low, byte high, int durationMs)
    {
        LowMotorSlider.Value = low;
        HighMotorSlider.Value = high;
        DurationSlider.Value = durationMs;
    }

    private void StartVibration_Click(object sender, RoutedEventArgs e)
    {
        if (!_sessionRunning || !_outputAvailable || _busy) return;
        RumbleRequested?.Invoke(this, new OutputRumbleRequestedEventArgs(
            (byte)Math.Round(LowMotorSlider.Value),
            (byte)Math.Round(HighMotorSlider.Value),
            (int)Math.Round(DurationSlider.Value)));
    }

    private void StopVibration_Click(object sender, RoutedEventArgs e)
    {
        if (!_sessionRunning || !_outputAvailable) return;
        StopRumbleRequested?.Invoke(this, EventArgs.Empty);
    }

    private void PickColor_Click(object sender, RoutedEventArgs e)
    {
        Color current = CurrentColor();
        LightbarColorPickerWindow picker = new(current)
        {
            Owner = Window.GetWindow(this)
        };
        if (picker.ShowDialog() != true) return;

        RedSlider.Value = picker.SelectedColor.R;
        GreenSlider.Value = picker.SelectedColor.G;
        BlueSlider.Value = picker.SelectedColor.B;
        RefreshLightbarPreview();
    }

    private void SetLightbar_Click(object sender, RoutedEventArgs e)
    {
        if (!_sessionRunning || !_outputAvailable || _busy) return;
        Color color = CurrentColor();
        LightbarRequested?.Invoke(this, new OutputLightbarRequestedEventArgs(color.R, color.G, color.B));
    }

    private void ResetOutput_Click(object sender, RoutedEventArgs e)
    {
        if (!_sessionRunning || !_outputAvailable || _busy) return;
        ResetOutputRequested?.Invoke(this, EventArgs.Empty);
    }

    private Color CurrentColor() => Color.FromRgb(
        (byte)Math.Round(RedSlider.Value),
        (byte)Math.Round(GreenSlider.Value),
        (byte)Math.Round(BlueSlider.Value));

    private void RefreshLightbarPreview()
    {
        Color color = CurrentColor();
        LightbarPreview.Background = new SolidColorBrush(color);
        LightbarHexText.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        LightbarRgbText.Text = $"RGB {color.R}, {color.G}, {color.B}";
        RedValue.Text = color.R.ToString();
        GreenValue.Text = color.G.ToString();
        BlueValue.Text = color.B.ToString();
    }
}

public sealed class OutputRumbleRequestedEventArgs(byte lowMotor, byte highMotor, int durationMs) : EventArgs
{
    public byte LowMotor { get; } = lowMotor;
    public byte HighMotor { get; } = highMotor;
    public int DurationMs { get; } = durationMs;
}

public sealed class OutputLightbarRequestedEventArgs(byte red, byte green, byte blue) : EventArgs
{
    public byte Red { get; } = red;
    public byte Green { get; } = green;
    public byte Blue { get; } = blue;
}
