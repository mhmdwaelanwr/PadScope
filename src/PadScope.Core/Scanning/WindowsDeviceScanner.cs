using System.Management;
using System.Text.RegularExpressions;
using PadScope.Core.Models;

namespace PadScope.Core.Scanning;

public sealed class WindowsDeviceScanner : IControllerScanner
{
    private static readonly string[] StrongControllerKeywords =
    {
        "gamepad",
        "joystick",
        "game controller",
        "hid-compliant game controller",
        "wireless controller",
        "dualshock",
        "dualsense",
        "xbox",
        "xinput",
        "marvo",
        "skytech"
    };

    private static readonly string[] ExcludedControllerLikeKeywords =
    {
        "audio controller",
        "host controller",
        "usb controller",
        "sata controller",
        "nvme controller",
        "raid controller",
        "storage controller",
        "network controller",
        "ethernet controller",
        "memory controller",
        "system controller",
        "smbus controller",
        "pci controller",
        "thunderbolt controller",
        "high definition audio controller",
        "intel",
        "realtek",
        "nvidia"
    };

    public IReadOnlyList<ControllerDevice> Scan()
    {
        if (!OperatingSystem.IsWindows())
        {
            return Array.Empty<ControllerDevice>();
        }

        List<ControllerDevice> devices = new();
        devices.AddRange(ScanPnPDevices());
        devices.AddRange(ScanAudioDevices());

        return devices
            .DistinctBy(device => device.DevicePath ?? $"{device.DisplayName}:{device.VendorId}:{device.ProductId}:{device.Source}")
            .OrderBy(device => device.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<ControllerDevice> ScanPnPDevices()
    {
        using ManagementObjectSearcher searcher = new(
            "SELECT Name, Manufacturer, DeviceID, PNPDeviceID, Service, ClassGuid FROM Win32_PnPEntity"
        );

        foreach (ManagementObject item in searcher.Get().OfType<ManagementObject>())
        {
            string name = ReadString(item, "Name") ?? string.Empty;
            string pnpDeviceId = ReadString(item, "PNPDeviceID") ?? ReadString(item, "DeviceID") ?? string.Empty;
            string manufacturer = ReadString(item, "Manufacturer") ?? string.Empty;
            string service = ReadString(item, "Service") ?? string.Empty;

            if (!LooksLikeController(name, pnpDeviceId, manufacturer, service))
            {
                continue;
            }

            (string? vendorId, string? productId) = ExtractVidPid(pnpDeviceId);

            yield return new ControllerDevice(
                DisplayName: string.IsNullOrWhiteSpace(name) ? "Unknown controller-like device" : name,
                Manufacturer: string.IsNullOrWhiteSpace(manufacturer) ? null : manufacturer,
                VendorId: vendorId,
                ProductId: productId,
                DevicePath: string.IsNullOrWhiteSpace(pnpDeviceId) ? null : pnpDeviceId,
                ConnectionType: InferConnectionType(pnpDeviceId),
                Source: "Win32_PnPEntity"
            );
        }
    }

    private static IEnumerable<ControllerDevice> ScanAudioDevices()
    {
        using ManagementObjectSearcher searcher = new(
            "SELECT Name, Manufacturer, DeviceID, PNPDeviceID FROM Win32_SoundDevice"
        );

        foreach (ManagementObject item in searcher.Get().OfType<ManagementObject>())
        {
            string name = ReadString(item, "Name") ?? string.Empty;
            string pnpDeviceId = ReadString(item, "PNPDeviceID") ?? ReadString(item, "DeviceID") ?? string.Empty;
            string manufacturer = ReadString(item, "Manufacturer") ?? string.Empty;

            if (!LooksLikeControllerAudioEndpoint(name, pnpDeviceId, manufacturer))
            {
                continue;
            }

            (string? vendorId, string? productId) = ExtractVidPid(pnpDeviceId);

            yield return new ControllerDevice(
                DisplayName: string.IsNullOrWhiteSpace(name) ? "Unknown controller audio endpoint" : name,
                Manufacturer: string.IsNullOrWhiteSpace(manufacturer) ? null : manufacturer,
                VendorId: vendorId,
                ProductId: productId,
                DevicePath: string.IsNullOrWhiteSpace(pnpDeviceId) ? null : pnpDeviceId,
                ConnectionType: InferConnectionType(pnpDeviceId),
                Source: "Win32_SoundDevice"
            );
        }
    }

    private static bool LooksLikeController(string name, string pnpDeviceId, string manufacturer, string service)
    {
        string combined = $"{name} {pnpDeviceId} {manufacturer} {service}";

        if (ContainsAny(combined, ExcludedControllerLikeKeywords) &&
            !ContainsAny(combined, StrongControllerKeywords))
        {
            return false;
        }

        if (ContainsAny(combined, StrongControllerKeywords))
        {
            return true;
        }

        if (pnpDeviceId.StartsWith("HID\\VID_", StringComparison.OrdinalIgnoreCase) &&
            pnpDeviceId.Contains("IG_", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool LooksLikeControllerAudioEndpoint(string name, string pnpDeviceId, string manufacturer)
    {
        string combined = $"{name} {pnpDeviceId} {manufacturer}";
        return ContainsAny(combined, StrongControllerKeywords) &&
               !ContainsAny(combined, ExcludedControllerLikeKeywords);
    }

    private static bool ContainsAny(string value, IEnumerable<string> keywords)
    {
        return keywords.Any(keyword => value.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static ConnectionType InferConnectionType(string pnpDeviceId)
    {
        if (pnpDeviceId.StartsWith("BTH", StringComparison.OrdinalIgnoreCase) ||
            pnpDeviceId.Contains("BTHENUM", StringComparison.OrdinalIgnoreCase) ||
            pnpDeviceId.Contains("BLUETOOTH", StringComparison.OrdinalIgnoreCase))
        {
            return ConnectionType.Bluetooth;
        }

        if (pnpDeviceId.StartsWith("USB", StringComparison.OrdinalIgnoreCase) ||
            pnpDeviceId.Contains("USB", StringComparison.OrdinalIgnoreCase) ||
            pnpDeviceId.StartsWith("HID\\VID_", StringComparison.OrdinalIgnoreCase))
        {
            return ConnectionType.Usb;
        }

        return ConnectionType.Unknown;
    }

    private static (string? VendorId, string? ProductId) ExtractVidPid(string value)
    {
        Match match = Regex.Match(value, "VID_([0-9A-Fa-f]{4}).*PID_([0-9A-Fa-f]{4})");
        if (!match.Success)
        {
            return (null, null);
        }

        return (match.Groups[1].Value.ToUpperInvariant(), match.Groups[2].Value.ToUpperInvariant());
    }

    private static string? ReadString(ManagementBaseObject item, string propertyName)
    {
        try
        {
            return item[propertyName]?.ToString();
        }
        catch (ManagementException)
        {
            return null;
        }
    }
}
