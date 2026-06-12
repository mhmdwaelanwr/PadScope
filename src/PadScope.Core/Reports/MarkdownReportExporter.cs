using System.Text;
using PadScope.Core.Models;

namespace PadScope.Core.Reports;

public static class MarkdownReportExporter
{
    public static string Export(IEnumerable<CompatibilityReport> reports)
    {
        List<CompatibilityReport> items = reports.ToList();
        StringBuilder builder = new();

        builder.AppendLine("# PadScope Compatibility Report");
        builder.AppendLine();
        builder.AppendLine($"Generated at: `{DateTimeOffset.UtcNow:O}`");
        builder.AppendLine();
        builder.AppendLine($"Detected devices: **{items.Count}**");
        builder.AppendLine();

        if (items.Count == 0)
        {
            builder.AppendLine("No controller-like devices were detected.");
            return builder.ToString();
        }

        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine("| Device | Profile | Confidence | Risk | Connection | VID | PID | Source |");
        builder.AppendLine("|---|---|---|---|---|---|---|---|");

        foreach (CompatibilityReport report in items)
        {
            builder.AppendLine($"| {Escape(report.Device.DisplayName)} | {Escape(report.ProfileName)} | {Escape(report.ProfileConfidence)} | {report.RecommendedRiskLevel} | {report.Device.ConnectionType} | {Escape(report.Device.VendorId ?? "?")} | {Escape(report.Device.ProductId ?? "?")} | {Escape(report.Device.Source)} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Devices");
        builder.AppendLine();

        foreach (CompatibilityReport report in items)
        {
            builder.AppendLine($"### {report.Device.DisplayName}");
            builder.AppendLine();
            builder.AppendLine($"- **Profile:** {report.ProfileName}");
            builder.AppendLine($"- **Confidence:** {report.ProfileConfidence}");
            builder.AppendLine($"- **Recommended risk:** {report.RecommendedRiskLevel}");
            builder.AppendLine($"- **Next action:** {report.RecommendedNextAction}");
            builder.AppendLine($"- **Manufacturer:** {report.Device.Manufacturer ?? "Unknown"}");
            builder.AppendLine($"- **VID/PID:** {report.Device.VendorId ?? "?"}/{report.Device.ProductId ?? "?"}");
            builder.AppendLine($"- **Connection:** {report.Device.ConnectionType}");
            builder.AppendLine($"- **Source:** {report.Device.Source}");
            builder.AppendLine($"- **Path:** `{report.Device.DevicePath ?? "Unknown"}`");
            builder.AppendLine();
            builder.AppendLine("#### Feature status");
            builder.AppendLine();
            builder.AppendLine("| Feature | Status |");
            builder.AppendLine("|---|---|");
            builder.AppendLine($"| Input | {report.Input} |");
            builder.AppendLine($"| Rumble | {report.Rumble} |");
            builder.AppendLine($"| Lightbar | {report.Lightbar} |");
            builder.AppendLine($"| Gyro | {report.Gyro} |");
            builder.AppendLine($"| Touchpad | {report.Touchpad} |");
            builder.AppendLine($"| Windows audio endpoint | {report.WindowsAudioEndpoint} |");
            builder.AppendLine($"| DS4 audio protocol | {report.Ds4AudioProtocol} |");
            builder.AppendLine();
            builder.AppendLine("#### Notes");
            builder.AppendLine();

            foreach (string note in report.Notes)
            {
                builder.AppendLine($"- {note}");
            }

            builder.AppendLine();
        }

        builder.AppendLine("## Safety note");
        builder.AppendLine();
        builder.AppendLine("This report was generated from read-only diagnostics. It does not prove that rumble, lightbar, speaker, headset, or DS4 audio packets are safe for this controller.");

        return builder.ToString();
    }

    private static string Escape(string value)
    {
        return value.Replace("|", "\\|");
    }
}
