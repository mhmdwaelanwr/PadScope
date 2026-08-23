using System.Collections.Specialized;
using System.Windows.Controls;
using System.Windows.Threading;
using PadScope.Core.Models;

namespace PadScope.Desktop;

public partial class MainWindow
{
    private bool _reportSelectionPersistenceInitialized;
    private CompatibilityReport? _rememberedReport;
    private string? _rememberedReportDetails;

    /// <summary>
    /// Keeps the scan selection stable while ObservableCollection refreshes are
    /// happening. WPF temporarily clears DataGrid.SelectedItem during a reset;
    /// without this guard the original SelectionChanged handler replaces valid
    /// device details with the empty-state text for a frame (and sometimes leaves
    /// it there until the user clicks the row again).
    /// </summary>
    private void InitializeReportSelectionPersistence()
    {
        if (_reportSelectionPersistenceInitialized)
        {
            return;
        }

        ReportsGrid.SelectionChanged += RememberReportSelection;
        _reports.CollectionChanged += ReportsCollectionChanged;
        _reportSelectionPersistenceInitialized = true;
    }

    private void RememberReportSelection(object? sender, SelectionChangedEventArgs e)
    {
        if (ReportsGrid.SelectedItem is CompatibilityReport report)
        {
            _rememberedReport = report;
            _rememberedReportDetails = DetailsText.Text;
            return;
        }

        // The XAML-wired handler runs first and may have written the placeholder.
        // Restore the last valid details during transient selection loss. An
        // explicit Clear still wins because ClearButton_Click writes its empty
        // state after the collection reset completes.
        if (_rememberedReport is not null && !string.IsNullOrWhiteSpace(_rememberedReportDetails))
        {
            DetailsText.Text = _rememberedReportDetails;
        }
    }

    private void ReportsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add || ReportsGrid.SelectedItem is not null)
        {
            return;
        }

        Dispatcher.BeginInvoke(
            new Action(() =>
            {
                if (_reports.Count == 0 || ReportsGrid.SelectedItem is not null)
                {
                    return;
                }

                CompatibilityReport? target = _rememberedReport is null
                    ? null
                    : _reports.FirstOrDefault(report => SameController(report.Device, _rememberedReport.Device));

                target ??= _reports[0];
                ReportsGrid.SelectedItem = target;
                ReportsGrid.ScrollIntoView(target);
            }),
            DispatcherPriority.Background);
    }

    private static bool SameController(ControllerDevice a, ControllerDevice b)
    {
        if (!string.IsNullOrWhiteSpace(a.DevicePath) || !string.IsNullOrWhiteSpace(b.DevicePath))
        {
            return string.Equals(a.DevicePath, b.DevicePath, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(a.VendorId, b.VendorId, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(a.ProductId, b.ProductId, StringComparison.OrdinalIgnoreCase);
    }
}
