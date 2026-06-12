using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PadScope.Core.Diagnostics;
using PadScope.Core.Models;
using PadScope.Core.Scanning;

namespace PadScope.Desktop;

public partial class MainWindow : Window
{
    private readonly IControllerScanner _scanner = new WindowsDeviceScanner();
    private readonly ObservableCollection<CompatibilityReport> _reports = new();

    public MainWindow()
    {
        InitializeComponent();
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
        if (_reports.Count == 0)
        {
            RunScan();
        }

        if (_reports.Count == 0)
        {
            MessageBox.Show(
                this,
                "No report data is available to export yet.",
                "PadScope",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            return;
        }

        SaveFileDialog dialog = new()
        {
            Title = "Export PadScope report",
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
        StatusText.Text = $"Report exported: {dialog.FileName}";
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

    private void UpdateSummary()
    {
        DeviceCountText.Text = _reports.Count.ToString();
        ProfileCountText.Text = _reports.Count(report => !report.ProfileName.StartsWith("Unknown", StringComparison.OrdinalIgnoreCase)).ToString();
        LastScanText.Text = _reports.Count == 0 ? "Never" : DateTime.Now.ToString("HH:mm:ss");
    }
}
