using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using PadScope.Core.Diagnostics;
using PadScope.Core.Models;
using PadScope.Core.Reports;
using PadScope.Core.Scanning;
using PadScope.Core.Testing;
using PadScope.Hid.Audio;

namespace PadScope.Desktop;

public partial class MainWindow : Window
{
    private readonly IControllerScanner _scanner = new WindowsDeviceScanner();
    private readonly ObservableCollection<CompatibilityReport> _reports = new();
    private readonly AudioStreamBridge _audioBridge = new();
    private bool _isLightTheme;
    private bool _hasScanned;
    private bool _isScanning;
    private DateTime? _lastScanAt;

    public IReadOnlyList<StageRow> StageRows { get; } = TestStageRegistry.All
        .Select(stage => new StageRow(
            Stage: stage.Stage.ToString(),
            Name: stage.Name,
            Status: stage.Status,
            Goal: stage.Goal,
            WhatToDo: stage.WhatToDo,
            PassCriteria: stage.PassCriteria
        ))
        .ToList();

    public IReadOnlyList<ProfileRow> ProfileRows { get; } = new[]
    {
        new ProfileRow("Marvo GT-84", "DS4-style clone", "starter profile", "USB/Bluetooth VID/PID, HID descriptor, rumble, lightbar, audio endpoint"),
        new ProfileRow("SkyTech DS4-style clone", "DS4-style clone", "research needed", "VID/PID, game mode, Bluetooth behavior, DS4Windows detection"),
        new ProfileRow("Zero DS4-style clone", "DS4-style clone", "research needed", "VID/PID, input mode, rumble, lightbar, touchpad behavior"),
        new ProfileRow("Generic Wireless Controller", "Unknown DS4-compatible", "research needed", "Device path, vendor strings, report shape, audio endpoint check"),
        new ProfileRow("AULA G1000", "DirectInput PC gamepad", "research needed", "VID/PID, DirectInput layout, rumble behavior, XInput compatibility"),
        new ProfileRow("Sony DualShock 4", "Reference hardware", "baseline", "USB/Bluetooth identity, known DS4 report shape"),
        new ProfileRow("Sony DualSense", "Reference hardware", "baseline", "USB/Bluetooth identity, adaptive trigger scope, audio endpoint behavior")
    };

    public IReadOnlyList<FeatureTestRow> FeatureTestRows { get; } = FeatureTestRegistry.All
        .Select(test => new FeatureTestRow(
            Name: test.Name,
            Stage: test.Stage.ToString(),
            RiskLevel: test.RiskLevel.ToString(),
            RequiresSelectedDevice: test.RequiresSelectedDevice ? "Yes" : "No",
            RequiresUserConfirmation: test.RequiresUserConfirmation ? "Yes" : "No",
            State: test.EnabledByDefault ? "Enabled" : "Locked",
            Goal: test.Goal,
            PassCriteria: test.PassCriteria
        ))
        .ToList();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        ReportsGrid.ItemsSource = _reports;
        ApplyDarkTheme();
        UpdateSummary();
        EnableOutputControls(false);
        VirtualStatusText.Text = ViGEmBusDetector.DescribeStatus();
        VersionText.Text = $"PadScope v{GetAppVersion()} · Windows gamepad toolkit";
        _audioBridge.Log += OnAudioBridgeLog;
    }

    protected override void OnClosed(EventArgs e)
    {
        _liveTimer?.Stop();
        _liveSession?.Dispose();
        StopVirtualPassthrough();
        StopMouseEmulation();
        _audioBridge.Log -= OnAudioBridgeLog;
        _audioBridge.Dispose();
        base.OnClosed(e);
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        await RunScanAsync();
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        StopLiveInput();
        _reports.Clear();
        DetailsText.Text = "Run a scan, then select a device.";
        StatusText.Text = "Cleared";
        CurrentStageText.Text = "0/1 Ready";
        _hasScanned = false;
        UpdateSummary();
        ClearLiveDeviceList();
        ClearVirtualDeviceList();
        ClearMouseDeviceList();
    }

    private void ToggleThemeButton_Click(object sender, RoutedEventArgs e)
    {
        _isLightTheme = !_isLightTheme;

        if (_isLightTheme)
        {
            ApplyLightTheme();
        }
        else
        {
            ApplyDarkTheme();
        }
    }

    private void AboutButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBoxResult result = MessageBox.Show(
            this,
            $"PadScope v{GetAppVersion()}\n\n" +
            "Windows gamepad diagnostics, compatibility, remapping, and experimentation toolkit.\n\n" +
            "Normal scans are read-only. Controlled and experimental actions may require additional drivers and explicit confirmation.\n\n" +
            "Open the PadScope GitHub repository?",
            "About PadScope",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://github.com/mhmdwaelanwr/PadScope") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Could not open GitHub", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void ExportJsonButton_Click(object sender, RoutedEventArgs e)
    {
        if (!await EnsureReportDataAsync())
        {
            return;
        }

        SaveFileDialog dialog = new()
        {
            Title = "Export PadScope JSON report",
            Filter = "JSON report (*.json)|*.json|All files (*.*)|*.*",
            FileName = "padscope-report.json"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(_reports, options));
        StatusText.Text = $"JSON report exported: {dialog.FileName}";
    }

    private async void ExportMarkdownButton_Click(object sender, RoutedEventArgs e)
    {
        if (!await EnsureReportDataAsync())
        {
            return;
        }

        SaveFileDialog dialog = new()
        {
            Title = "Export PadScope Markdown report",
            Filter = "Markdown report (*.md)|*.md|Text file (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = "padscope-report.md"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        File.WriteAllText(dialog.FileName, MarkdownReportExporter.Export(_reports));
        StatusText.Text = $"Markdown report exported: {dialog.FileName}";
    }

    private void AudioRefreshButton_Click(object sender, RoutedEventArgs e)
    {
        _audioBridge.RefreshDevices();
        AudioSpeakerComboBox.ItemsSource = _audioBridge.AvailableSpeakers;
        AudioMicComboBox.ItemsSource = _audioBridge.AvailableMicrophones;
        AudioSpeakerComboBox.SelectedIndex = _audioBridge.AvailableSpeakers.Count > 0 ? 0 : -1;
        AudioMicComboBox.SelectedIndex = _audioBridge.AvailableMicrophones.Count > 0 ? 0 : -1;

        int speakers = _audioBridge.AvailableSpeakers.Count;
        int mics = _audioBridge.AvailableMicrophones.Count;
        AudioDeviceCountText.Text = $"Found {speakers} speaker(s), {mics} microphone(s).";
        AudioStatusText.Text = _audioBridge.DescribeStatus();
    }

    private void AudioCaptureStartButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfirmControlledAction("Start microphone capture from the selected Windows controller audio endpoint."))
        {
            return;
        }

        int micIndex = AudioMicComboBox.SelectedIndex;
        if (_audioBridge.StartCapture(micIndex >= 0 ? micIndex : 0))
        {
            AudioCaptureStartButton.IsEnabled = false;
            AudioCaptureStopButton.IsEnabled = true;
            AudioStatusText.Text = _audioBridge.DescribeStatus();
        }
    }

    private void AudioCaptureStopButton_Click(object sender, RoutedEventArgs e)
    {
        _audioBridge.StopCapture();
        AudioCaptureStartButton.IsEnabled = true;
        AudioCaptureStopButton.IsEnabled = false;
        AudioPlaybackStartButton.IsEnabled = !_audioBridge.IsPlaying;
        AudioPlaybackStopButton.IsEnabled = _audioBridge.IsPlaying;
        AudioRouteStatusText.Text = _audioBridge.IsRouting ? "Routing active." : "Routing stopped.";
        AudioStatusText.Text = _audioBridge.DescribeStatus();
    }

    private void AudioPlaybackStartButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfirmControlledAction("Open the selected Windows controller speaker endpoint for playback."))
        {
            return;
        }

        int speakerIndex = AudioSpeakerComboBox.SelectedIndex;
        if (_audioBridge.StartPlayback(speakerIndex >= 0 ? speakerIndex : 0))
        {
            AudioPlaybackStartButton.IsEnabled = false;
            AudioPlaybackStopButton.IsEnabled = true;
            AudioStatusText.Text = _audioBridge.DescribeStatus();
        }
    }

    private void AudioPlaybackStopButton_Click(object sender, RoutedEventArgs e)
    {
        _audioBridge.StopPlayback();
        AudioPlaybackStartButton.IsEnabled = true;
        AudioPlaybackStopButton.IsEnabled = false;
        AudioStatusText.Text = _audioBridge.DescribeStatus();
    }

    private void AudioRouteButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfirmControlledAction("Route captured controller microphone audio to the selected controller speaker until stopped."))
        {
            return;
        }

        _audioBridge.RouteMicToSpeaker();
        AudioRouteStatusText.Text = _audioBridge.IsRouting
            ? "Routing active: mic → speaker."
            : "Routing did not start. Start capture and playback first.";
        AudioStatusText.Text = _audioBridge.DescribeStatus();
    }

    private void AudioRouteStopButton_Click(object sender, RoutedEventArgs e)
    {
        _audioBridge.StopRoute();
        AudioPlaybackStartButton.IsEnabled = true;
        AudioPlaybackStopButton.IsEnabled = false;
        AudioRouteStatusText.Text = "Routing stopped.";
        AudioStatusText.Text = _audioBridge.DescribeStatus();
    }

    private void AudioSpeakerVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        int volume = (int)e.NewValue;
        if (AudioSpeakerVolumeText is null || _audioBridge is null)
        {
            return;
        }

        AudioSpeakerVolumeText.Text = $"{volume}%";
        _audioBridge.SetSpeakerVolume(volume);
    }

    private void AudioMicVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        int volume = (int)e.NewValue;
        if (AudioMicVolumeText is null || _audioBridge is null)
        {
            return;
        }

        AudioMicVolumeText.Text = $"{volume}%";
        _audioBridge.SetMicVolume(volume);
    }

    private void OnAudioBridgeLog(string message)
    {
        Dispatcher.Invoke(() =>
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            AudioLogText.Text = $"[{timestamp}] {message}\n{AudioLogText.Text}";
        });
    }

    private void CopyDetailsButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DetailsText.Text))
        {
            return;
        }

        Clipboard.SetText(DetailsText.Text);
        StatusText.Text = "Details copied";
    }

    private void ExplainSelectedStageButton_Click(object sender, RoutedEventArgs e)
    {
        if (StagesGrid.SelectedItem is not StageRow row)
        {
            MessageBox.Show(
                this,
                "Select a stage first.",
                "PadScope Stages",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            return;
        }

        MessageBox.Show(
            this,
            $"Stage: {row.Stage}\n" +
            $"Name: {row.Name}\n" +
            $"Status: {row.Status}\n\n" +
            $"Goal:\n{row.Goal}\n\n" +
            $"What to do:\n{row.WhatToDo}\n\n" +
            $"Pass criteria:\n{row.PassCriteria}",
            "Stage details",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
    }

    private void ExplainSelectedFeatureButton_Click(object sender, RoutedEventArgs e)
    {
        if (FeatureTestsGrid.SelectedItem is not FeatureTestRow row)
        {
            MessageBox.Show(
                this,
                "Select a feature test first.",
                "PadScope Feature Tests",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            return;
        }

        MessageBox.Show(
            this,
            $"Feature: {row.Name}\n" +
            $"Stage: {row.Stage}\n" +
            $"Risk: {row.RiskLevel}\n" +
            $"Requires selected device: {row.RequiresSelectedDevice}\n" +
            $"Requires confirmation: {row.RequiresUserConfirmation}\n" +
            $"State: {row.State}\n\n" +
            $"Goal:\n{row.Goal}\n\n" +
            $"Pass criteria:\n{row.PassCriteria}",
            "Feature test details",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
    }

    private void ReportsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ReportsGrid.SelectedItem is not CompatibilityReport report)
        {
            DetailsText.Text = "Run a scan, then select a device.";
            return;
        }

        DetailsText.Text = $"Device: {report.Device.DisplayName}\n" +
                           $"Profile: {report.ProfileName}\n" +
                           $"Confidence: {report.ProfileConfidence}\n" +
                           $"Recommended risk: {report.RecommendedRiskLevel}\n" +
                           $"Next action: {report.RecommendedNextAction}\n\n" +
                           $"Manufacturer: {report.Device.Manufacturer ?? "Unknown"}\n" +
                           $"VID/PID: {report.Device.VendorId ?? "?"}/{report.Device.ProductId ?? "?"}\n" +
                           $"Connection: {report.Device.ConnectionType}\n" +
                           $"Source: {report.Device.Source}\n" +
                           $"Path: {report.Device.DevicePath ?? "Unknown"}\n\n" +
                           $"Input: {report.Input}\n" +
                           $"Rumble: {report.Rumble}\n" +
                           $"Lightbar: {report.Lightbar}\n" +
                           $"Gyro: {report.Gyro}\n" +
                           $"Touchpad: {report.Touchpad}\n" +
                           $"Windows audio endpoint: {report.WindowsAudioEndpoint}\n" +
                           $"DS4 audio protocol: {report.Ds4AudioProtocol}\n\n" +
                           "Notes:\n- " + string.Join("\n- ", report.Notes);
    }

    private async Task RunScanAsync()
    {
        StopLiveInput();
        StopVirtualPassthrough();
        StopMouseEmulation();
        ClearLiveDeviceList();
        ClearVirtualDeviceList();
        ClearMouseDeviceList();
        _reports.Clear();
        _isScanning = true;
        ScanButton.IsEnabled = false;
        StatusText.Text = "Scanning...";
        ScanProgress.Visibility = Visibility.Visible;
        UpdateSummary();

        try
        {
            var reports = await Task.Run(() => _scanner.Scan().Select(ReportBuilder.BuildInitialReport).ToList());

            foreach (var report in reports)
            {
                _reports.Add(report);
            }

            _hasScanned = true;
            _lastScanAt = DateTime.Now;

            CurrentStageText.Text = _reports.Count == 0 ? "1 Empty Scan" : "2 USB/BT Scan";
            StatusText.Text = _reports.Count == 0
                ? "No controller-like devices detected"
                : $"Detected {_reports.Count} controller-like device(s)";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Scan failed";
            MessageBox.Show(
                this,
                ex.Message,
                "PadScope scan failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
        finally
        {
            _isScanning = false;
            ScanButton.IsEnabled = true;
            ScanProgress.Visibility = Visibility.Collapsed;
            UpdateSummary();
            RefreshLiveDeviceList();
            RefreshVirtualDeviceList();
            RefreshMouseDeviceList();
        }
    }

    private bool ConfirmControlledAction(string action, ControllerDevice? device = null)
    {
        string identity = device is null
            ? "No controller identity is associated with this Windows audio action."
            : $"Device: {device.DisplayName}\nVID/PID: {device.VendorId ?? "?"}/{device.ProductId ?? "?"}\nConnection: {device.ConnectionType}\nSource: {device.Source}";

        return MessageBox.Show(
            this,
            $"{action}\n\n{identity}\n\nContinue only if this is the intended target.",
            "PadScope controlled action",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }

    private static string GetAppVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0";
    }

    private async Task<bool> EnsureReportDataAsync()
    {
        if (_reports.Count == 0)
        {
            await RunScanAsync();
        }

        if (_reports.Count != 0)
        {
            return true;
        }

        MessageBox.Show(
            this,
            "No report data is available to export yet.",
            "PadScope",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
        return false;
    }

    private void UpdateSummary()
    {
        DeviceCountText.Text = _reports.Count.ToString();
        ProfileCountText.Text = _reports.Count(report => !report.ProfileName.StartsWith("Unknown", StringComparison.OrdinalIgnoreCase)).ToString();
        LastScanText.Text = _lastScanAt?.ToString("HH:mm:ss") ?? "Not run";

        ScanEmptyState.Visibility = _reports.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (_isScanning)
        {
            ScanEmptyTitle.Text = "Inspecting Windows controller interfaces…";
            ScanEmptySubtitle.Text = "This scan is read-only. PadScope is collecting identity, connection, and compatibility evidence.";
            StatusDot.Fill = (Brush)FindResource("B_Warning");
        }
        else if (_hasScanned && _reports.Count == 0)
        {
            ScanEmptyTitle.Text = "No controller-like device was detected";
            ScanEmptySubtitle.Text = "Connect a controller by USB first, then scan again. Bluetooth pairing can be checked after the wired baseline.";
            StatusDot.Fill = (Brush)FindResource("B_Warning");
        }
        else
        {
            ScanEmptyTitle.Text = "Start with a safe controller scan";
            ScanEmptySubtitle.Text = "PadScope reads Windows device metadata only. It will not send rumble, lightbar, or audio output.";
            StatusDot.Fill = (Brush)FindResource(_reports.Count > 0 ? "B_Success" : "B_Primary");
        }
    }

    private static readonly (string Key, string Light, string Dark)[] ThemeColors =
    [
        ("C_Background",  "#F4F6FC", "#050713"),
        ("C_BackdropMid", "#EEF1FA", "#0A0D22"),
        ("C_BackdropEnd", "#F7FBFF", "#071523"),
        ("C_Card",        "#CCFFFFFF", "#B30C1021"),
        ("C_CardAlt",     "#B3EEF1FA", "#99121830"),
        ("C_Border",      "#99AAB5D1", "#667583B8"),
        ("C_Primary",     "#0891B2", "#22D3EE"),
        ("C_PrimaryDim",  "#7C3AED", "#8B5CF6"),
        ("C_Text",        "#11162A", "#F7F9FF"),
        ("C_TextDim",     "#66708D", "#8490AF"),
        ("C_Success",     "#059669", "#34D399"),
        ("C_Warning",     "#D97706", "#FBBF24"),
        ("C_Danger",      "#E11D48", "#FB7185"),
        ("C_PrimarySoft", "#227C3AED", "#3324264F"),
        ("C_SurfaceHover","#CCE8ECF7", "#CC202747"),
    ];

    private void ApplyDarkTheme()
    {
        foreach (var (key, _, dark) in ThemeColors)
        {
            Application.Current.Resources[key] = (Color)ColorConverter.ConvertFromString(dark);
        }
        ThemeButton.Content = "☀  Light";
        Background = (Brush)Application.Current.Resources["B_WindowBackdrop"];
    }

    private void ApplyLightTheme()
    {
        foreach (var (key, light, _) in ThemeColors)
        {
            Application.Current.Resources[key] = (Color)ColorConverter.ConvertFromString(light);
        }
        ThemeButton.Content = "◐  Dark";
        Background = (Brush)Application.Current.Resources["B_WindowBackdrop"];
    }
}

public sealed record StageRow(
    string Stage,
    string Name,
    string Status,
    string Goal,
    string WhatToDo,
    string PassCriteria
);

public sealed record ProfileRow(string Name, string Category, string Status, string EvidenceNeeded);

public sealed record FeatureTestRow(
    string Name,
    string Stage,
    string RiskLevel,
    string RequiresSelectedDevice,
    string RequiresUserConfirmation,
    string State,
    string Goal,
    string PassCriteria
);
