using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using IOFile = System.IO.File;
using IOPath = System.IO.Path;

namespace PadScope.Desktop;

public partial class MainWindow
{
    private bool _scanListPanelControlsInstalled;
    private Grid? _scanListHostGrid;
    private StackPanel? _stagesHeader;
    private StackPanel? _featureTestsHeader;
    private Border? _stagesPanel;
    private Border? _featureTestsPanel;
    private Button? _stagesExpandButton;
    private Button? _stagesCollapseButton;
    private Button? _featureExpandButton;
    private Button? _featureCollapseButton;
    private ScanPanelMode _stagesMode = ScanPanelMode.Normal;
    private ScanPanelMode _featureMode = ScanPanelMode.Normal;

    private enum ScanPanelMode
    {
        Normal,
        Collapsed,
        Expanded
    }

    private enum ScanPanelKind
    {
        Stages,
        Features
    }

    private void InstallScanListPanelControls()
    {
        if (_scanListPanelControlsInstalled)
        {
            return;
        }

        _stagesPanel = StagesGrid.Parent as Border;
        _featureTestsPanel = FeatureTestsGrid.Parent as Border;
        _scanListHostGrid = _stagesPanel?.Parent as Grid;
        if (_stagesPanel is null || _featureTestsPanel is null || _scanListHostGrid is null)
        {
            return;
        }

        _stagesHeader = FindHeaderStack(_scanListHostGrid, row: 0, "Test Stages");
        _featureTestsHeader = FindHeaderStack(_scanListHostGrid, row: 2, "Feature Tests");
        if (_stagesHeader is null || _featureTestsHeader is null)
        {
            return;
        }

        AddPanelActions(
            _stagesHeader,
            StagesGrid,
            "test-stages",
            () => ToggleExpanded(ScanPanelKind.Stages),
            () => ToggleCollapsed(ScanPanelKind.Stages),
            out _stagesExpandButton,
            out _stagesCollapseButton);

        AddPanelActions(
            _featureTestsHeader,
            FeatureTestsGrid,
            "feature-tests",
            () => ToggleExpanded(ScanPanelKind.Features),
            () => ToggleCollapsed(ScanPanelKind.Features),
            out _featureExpandButton,
            out _featureCollapseButton);

        ApplyScanPanelLayout();
        _scanListPanelControlsInstalled = true;
    }

    private static StackPanel? FindHeaderStack(Grid host, int row, string title)
    {
        return host.Children
            .OfType<StackPanel>()
            .FirstOrDefault(panel =>
                Grid.GetRow(panel) == row &&
                panel.Children.OfType<TextBlock>().Any(text => string.Equals(text.Text, title, StringComparison.Ordinal)));
    }

    private void AddPanelActions(
        StackPanel header,
        DataGrid grid,
        string exportStem,
        Action expandAction,
        Action collapseAction,
        out Button expandButton,
        out Button collapseButton)
    {
        Button exportButton = CreatePanelActionButton("Export", "Export this list as CSV or JSON");
        exportButton.Margin = new Thickness(10, 0, 0, 0);
        exportButton.Click += (_, _) => ExportDataGrid(grid, exportStem);
        header.Children.Add(exportButton);

        expandButton = CreatePanelActionButton("Expand", "Maximize this list inside the Scan workspace");
        expandButton.Margin = new Thickness(6, 0, 0, 0);
        expandButton.Click += (_, _) => expandAction();
        header.Children.Add(expandButton);

        collapseButton = CreatePanelActionButton("Collapse", "Minimize this list while keeping its header available");
        collapseButton.Margin = new Thickness(6, 0, 0, 0);
        collapseButton.Click += (_, _) => collapseAction();
        header.Children.Add(collapseButton);
    }

    private Button CreatePanelActionButton(string content, string toolTip)
    {
        return new Button
        {
            Content = content,
            Height = 28,
            MinWidth = 0,
            Padding = new Thickness(10, 0, 10, 0),
            FontSize = 10.8,
            ToolTip = toolTip,
            Style = (Style)FindResource("Sec")
        };
    }

    private void ToggleExpanded(ScanPanelKind kind)
    {
        if (kind == ScanPanelKind.Stages)
        {
            bool willExpand = _stagesMode != ScanPanelMode.Expanded;
            _stagesMode = willExpand ? ScanPanelMode.Expanded : ScanPanelMode.Normal;
            if (willExpand)
            {
                _featureMode = ScanPanelMode.Normal;
            }
        }
        else
        {
            bool willExpand = _featureMode != ScanPanelMode.Expanded;
            _featureMode = willExpand ? ScanPanelMode.Expanded : ScanPanelMode.Normal;
            if (willExpand)
            {
                _stagesMode = ScanPanelMode.Normal;
            }
        }

        ApplyScanPanelLayout();
    }

    private void ToggleCollapsed(ScanPanelKind kind)
    {
        if (kind == ScanPanelKind.Stages)
        {
            _stagesMode = _stagesMode == ScanPanelMode.Collapsed ? ScanPanelMode.Normal : ScanPanelMode.Collapsed;
            if (_stagesMode == ScanPanelMode.Collapsed && _featureMode == ScanPanelMode.Expanded)
            {
                _featureMode = ScanPanelMode.Normal;
            }
        }
        else
        {
            _featureMode = _featureMode == ScanPanelMode.Collapsed ? ScanPanelMode.Normal : ScanPanelMode.Collapsed;
            if (_featureMode == ScanPanelMode.Collapsed && _stagesMode == ScanPanelMode.Expanded)
            {
                _stagesMode = ScanPanelMode.Normal;
            }
        }

        ApplyScanPanelLayout();
    }

    private void ApplyScanPanelLayout()
    {
        if (_scanListHostGrid is null || _stagesHeader is null || _featureTestsHeader is null ||
            _stagesPanel is null || _featureTestsPanel is null)
        {
            return;
        }

        bool stagesExpanded = _stagesMode == ScanPanelMode.Expanded;
        bool featuresExpanded = _featureMode == ScanPanelMode.Expanded;

        if (stagesExpanded)
        {
            _stagesHeader.Visibility = Visibility.Visible;
            _stagesPanel.Visibility = Visibility.Visible;
            _featureTestsHeader.Visibility = Visibility.Collapsed;
            _featureTestsPanel.Visibility = Visibility.Collapsed;

            _scanListHostGrid.RowDefinitions[0].Height = GridLength.Auto;
            _scanListHostGrid.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
            _scanListHostGrid.RowDefinitions[2].Height = new GridLength(0);
            _scanListHostGrid.RowDefinitions[3].Height = new GridLength(0);
        }
        else if (featuresExpanded)
        {
            _stagesHeader.Visibility = Visibility.Collapsed;
            _stagesPanel.Visibility = Visibility.Collapsed;
            _featureTestsHeader.Visibility = Visibility.Visible;
            _featureTestsPanel.Visibility = Visibility.Visible;

            _scanListHostGrid.RowDefinitions[0].Height = new GridLength(0);
            _scanListHostGrid.RowDefinitions[1].Height = new GridLength(0);
            _scanListHostGrid.RowDefinitions[2].Height = GridLength.Auto;
            _scanListHostGrid.RowDefinitions[3].Height = new GridLength(1, GridUnitType.Star);
        }
        else
        {
            _stagesHeader.Visibility = Visibility.Visible;
            _featureTestsHeader.Visibility = Visibility.Visible;
            _stagesPanel.Visibility = _stagesMode == ScanPanelMode.Collapsed ? Visibility.Collapsed : Visibility.Visible;
            _featureTestsPanel.Visibility = _featureMode == ScanPanelMode.Collapsed ? Visibility.Collapsed : Visibility.Visible;

            _scanListHostGrid.RowDefinitions[0].Height = GridLength.Auto;
            _scanListHostGrid.RowDefinitions[1].Height = _stagesMode == ScanPanelMode.Collapsed
                ? new GridLength(0)
                : new GridLength(1, GridUnitType.Star);
            _scanListHostGrid.RowDefinitions[2].Height = GridLength.Auto;
            _scanListHostGrid.RowDefinitions[3].Height = _featureMode == ScanPanelMode.Collapsed
                ? new GridLength(0)
                : new GridLength(1, GridUnitType.Star);
        }

        if (_stagesExpandButton is not null)
        {
            _stagesExpandButton.Content = stagesExpanded ? "Restore" : "Expand";
        }
        if (_featureExpandButton is not null)
        {
            _featureExpandButton.Content = featuresExpanded ? "Restore" : "Expand";
        }
        if (_stagesCollapseButton is not null)
        {
            _stagesCollapseButton.Content = _stagesMode == ScanPanelMode.Collapsed ? "Show" : "Collapse";
        }
        if (_featureCollapseButton is not null)
        {
            _featureCollapseButton.Content = _featureMode == ScanPanelMode.Collapsed ? "Show" : "Collapse";
        }
    }

    private void ExportDataGrid(DataGrid grid, string exportStem)
    {
        try
        {
            List<ExportColumn> columns = grid.Columns
                .OfType<DataGridBoundColumn>()
                .Select(column => new ExportColumn(
                    column.Header?.ToString() ?? "Column",
                    (column.Binding as Binding)?.Path?.Path ?? string.Empty))
                .Where(column => !string.IsNullOrWhiteSpace(column.Path))
                .ToList();

            List<object> rows = grid.Items
                .Cast<object>()
                .Where(item => item != CollectionView.NewItemPlaceholder)
                .ToList();

            SaveFileDialog dialog = new()
            {
                Title = "Export PadScope list",
                Filter = "CSV file (*.csv)|*.csv|JSON file (*.json)|*.json",
                AddExtension = true,
                DefaultExt = ".csv",
                FileName = $"padscope-{exportStem}-{DateTime.Now:yyyyMMdd-HHmmss}"
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            if (dialog.FilterIndex == 2 || string.Equals(IOPath.GetExtension(dialog.FileName), ".json", StringComparison.OrdinalIgnoreCase))
            {
                ExportRowsAsJson(dialog.FileName, rows, columns);
            }
            else
            {
                ExportRowsAsCsv(dialog.FileName, rows, columns);
            }

            StatusText.Text = $"Exported {rows.Count:N0} row(s) · {IOPath.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "PadScope export failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static void ExportRowsAsCsv(string fileName, IReadOnlyList<object> rows, IReadOnlyList<ExportColumn> columns)
    {
        StringBuilder builder = new();
        builder.AppendLine(string.Join(",", columns.Select(column => EscapeCsv(column.Header))));

        foreach (object row in rows)
        {
            builder.AppendLine(string.Join(",", columns.Select(column => EscapeCsv(
                Convert.ToString(ReadPropertyPath(row, column.Path), CultureInfo.InvariantCulture) ?? string.Empty))));
        }

        IOFile.WriteAllText(fileName, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static void ExportRowsAsJson(string fileName, IReadOnlyList<object> rows, IReadOnlyList<ExportColumn> columns)
    {
        List<Dictionary<string, object?>> payload = rows
            .Select(row => columns.ToDictionary(
                column => column.Header,
                column => ReadPropertyPath(row, column.Path),
                StringComparer.OrdinalIgnoreCase))
            .ToList();

        IOFile.WriteAllText(
            fileName,
            JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static object? ReadPropertyPath(object? source, string path)
    {
        object? current = source;
        foreach (string segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (current is null)
            {
                return null;
            }

            PropertyInfo? property = current.GetType().GetProperty(segment, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (property is null)
            {
                return null;
            }

            current = property.GetValue(current);
        }

        return current;
    }

    private static string EscapeCsv(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\r') && !value.Contains('\n'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private sealed record ExportColumn(string Header, string Path);
}
