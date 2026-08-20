using System.Collections.ObjectModel;
using System.IO;
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
        AudioStatusText.Text = _audioBridge.DescribeStatus();
    }

    private void AudioPlaybackStartButton_Click(object sender, RoutedEventArgs e)
    {
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
        _audioBridge.RouteMicToSpeaker();
        AudioRouteStatusText.Text = "Routing active: mic → speaker.";
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
        _reports.Clear();
        StatusText.Text = "Scanning...";
        UpdateSummary();

        try
        {
            var reports = await Task.Run(() => _scanner.Scan().Select(ReportBuilder.BuildInitialReport).ToList());

            foreach (var report in reports)
            {
                _reports.Add(report);
            }

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
            UpdateSummary();
            RefreshLiveDeviceList();
            RefreshVirtualDeviceList();
            RefreshMouseDeviceList();
        }
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
        LastScanText.Text = _reports.Count == 0 ? "Never" : DateTime.Now.ToString("HH:mm:ss");
    }

    private void ApplyDarkTheme()
    {
        LoadTheme("Themes/DarkTheme.xaml");
        ThemeButton.Content = "Light";
        Background = (Brush)Application.Current.Resources["B_Background"];
    }

    private void ApplyLightTheme()
    {
        LoadTheme("Themes/LightTheme.xaml");
        ThemeButton.Content = "Dark";
        Background = (Brush)Application.Current.Resources["B_Background"];
    }

    private static void LoadTheme(string relativePath)
    {
        var dict = new ResourceDictionary { Source = new Uri(relativePath, UriKind.Relative) };
        Application.Current.Resources.MergedDictionaries.Clear();
        Application.Current.Resources.MergedDictionaries.Add(dict);
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
