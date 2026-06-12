using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PadScope.Core.Diagnostics;
using PadScope.Core.Models;
using PadScope.Core.Reports;
using PadScope.Core.Scanning;

namespace PadScope.Desktop;

public partial class MainWindow : Window
{
    private readonly IControllerScanner _scanner = new WindowsDeviceScanner();
    private readonly ObservableCollection<CompatibilityReport> _reports = new();

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

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        ReportsGrid.ItemsSource = _reports;
        UpdateSummary();
    }

    private void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        RunScan();
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        _reports.Clear();
        DetailsText.Text = "Run a scan, then select a device.";
        StatusText.Text = "Cleared";
        UpdateSummary();
    }

    private void ExportJsonButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureReportData())
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

    private void ExportMarkdownButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureReportData())
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

    private void AudioLabButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            this,
            "Audio Lab is planned for Phase 3. It will stay opt-in because DS4-style audio packets are experimental, especially on clone controllers.",
            "PadScope Audio Lab",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
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

    private void RunScan()
    {
        _reports.Clear();
        StatusText.Text = "Scanning...";

        try
        {
            foreach (var report in _scanner.Scan().Select(ReportBuilder.BuildInitialReport))
            {
                _reports.Add(report);
            }

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
        }
    }

    private bool EnsureReportData()
    {
        if (_reports.Count == 0)
        {
            RunScan();
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
}

public sealed record ProfileRow(string Name, string Category, string Status, string EvidenceNeeded);
