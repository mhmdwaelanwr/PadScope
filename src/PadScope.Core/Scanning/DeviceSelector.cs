using System.Diagnostics.CodeAnalysis;
using PadScope.Core.Models;

namespace PadScope.Core.Scanning;

public static class DeviceSelector
{
    public static bool TrySelect(
        IReadOnlyList<CompatibilityReport> reports,
        string? vendorId,
        string? productId,
        [NotNullWhen(true)] out ControllerDevice? device,
        out string? error)
    {
        device = null;

        if (reports.Count == 0)
        {
            error = "No controller-like device was detected. Connect the controller first, then try again.";
            return false;
        }

        bool hasFilter = !string.IsNullOrWhiteSpace(vendorId) || !string.IsNullOrWhiteSpace(productId);
        IEnumerable<CompatibilityReport> candidates = reports;

        if (!string.IsNullOrWhiteSpace(vendorId))
        {
            candidates = candidates.Where(report =>
                string.Equals(report.Device.VendorId, vendorId, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(productId))
        {
            candidates = candidates.Where(report =>
                string.Equals(report.Device.ProductId, productId, StringComparison.OrdinalIgnoreCase));
        }

        List<CompatibilityReport> matches = candidates.ToList();
        if (matches.Count == 0)
        {
            error = hasFilter
                ? $"No device matches VID {vendorId ?? "*"} / PID {productId ?? "*"}. Run 'scan' and use an exact detected identity."
                : "No matching device was found.";
            return false;
        }

        if (!hasFilter && matches.Count > 1)
        {
            error = "Multiple controllers were detected. Select one explicitly with --vid and --pid.";
            return false;
        }

        if (matches.Count > 1)
        {
            error = "More than one device matches the supplied identity. Disconnect extra controllers or provide a unique VID/PID pair.";
            return false;
        }

        device = matches[0].Device;
        error = null;
        return true;
    }
}
