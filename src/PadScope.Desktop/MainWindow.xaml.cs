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
    }

    private void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        RunScan();
    }

    private void ExportJsonButton_Click(object sender, RoutedEventArgs e)
    {
        if (_reports.Count == 0)
        {
            RunScan();
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

    private void ReportsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ReportsGrid.SelectedItem is not CompatibilityReport report)
        {
            DetailsText.Text = "Run a scan, then select a device.";
            return;
        }

        DetailsText.Text = $"Device: {report.Device.DisplayName}\n" +
                           $"Manufacturer: {report.Device.Manufacturer ?? "Unknown"}\n" +
                           $"VID/PID: {report.Device.VendorId ?? "?"}/{report.Device.ProductId ?? "?"}\n" +
                           $"Connection: {report.Device.ConnectionType}\n" +
                           $"Source: {report.Device.Source}\n\n" +
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

        foreach (var report in _scanner.Scan().Select(ReportBuilder.BuildInitialReport))
        {
            _reports.Add(report);
        }

        StatusText.Text = _reports.Count == 0
            ? "No controller-like devices detected. Try USB first, then scan again."
            : $"Detected {_reports.Count} controller-like device(s).";
    }
}
