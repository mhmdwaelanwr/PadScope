using HidSharp;
using PadScope.Core.Models;

namespace PadScope.Hid;

/// <summary>
/// Writes an HID output report through a short-lived stream that is independent
/// from the live input read stream. Some Windows HID stacks will continuously
/// read from one handle but stall writes issued on that same handle.
/// </summary>
internal static class HidSharpIndependentOutput
{
    private const int WriteTimeoutMilliseconds = 300;

    public static bool TryWrite(ControllerDevice device, byte[] report, out string? status, out string? error)
    {
        status = null;
        int vendorId = ParseHexId(device.VendorId);
        int productId = ParseHexId(device.ProductId);
        if (vendorId <= 0 || productId <= 0)
        {
            error = "Independent output fallback requires a valid VID/PID.";
            return false;
        }

        List<string> failures = new();
        try
        {
            IEnumerable<HidDevice> candidates = DeviceList.Local
                .GetHidDevices(vendorId, productId)
                .Select(candidate => new
                {
                    Device = candidate,
                    OutputLength = SafeOutputLength(candidate),
                    InputLength = SafeInputLength(candidate)
                })
                .Where(item => item.OutputLength >= report.Length)
                .OrderBy(item => Math.Abs(item.OutputLength - report.Length))
                .ThenByDescending(item => item.InputLength)
                .Select(item => item.Device);

            foreach (HidDevice candidate in candidates.Take(4))
            {
                int outputLength = SafeOutputLength(candidate);
                byte[] prepared = Prepare(report, outputLength);
                try
                {
                    using HidStream stream = candidate.Open();
                    stream.WriteTimeout = WriteTimeoutMilliseconds;
                    stream.Write(prepared);
                    status = $"Output OK · independent interrupt stream · report 0x{report[0]:X2} · {report.Length} B · HID max {outputLength} B";
                    error = null;
                    return true;
                }
                catch (Exception ex)
                {
                    string message = ex is TimeoutException ? "operation timed out" : ex.Message;
                    failures.Add($"out {outputLength} B: {message}");
                }
            }
        }
        catch (Exception ex)
        {
            failures.Add($"enumeration failed: {ex.Message}");
        }

        error = failures.Count == 0
            ? "no writable HID interface with a compatible output-report length was found"
            : string.Join("; ", failures.Take(4));
        return false;
    }

    private static byte[] Prepare(byte[] report, int outputLength)
    {
        if (outputLength <= report.Length)
        {
            return report;
        }

        byte[] padded = new byte[outputLength];
        Buffer.BlockCopy(report, 0, padded, 0, report.Length);
        return padded;
    }

    private static int SafeOutputLength(HidDevice device)
    {
        try { return device.GetMaxOutputReportLength(); }
        catch { return 0; }
    }

    private static int SafeInputLength(HidDevice device)
    {
        try { return device.GetMaxInputReportLength(); }
        catch { return 0; }
    }

    private static int ParseHexId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        string text = value.Trim();
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            text = text[2..];
        }

        return int.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out int parsed)
            ? parsed
            : 0;
    }
}
