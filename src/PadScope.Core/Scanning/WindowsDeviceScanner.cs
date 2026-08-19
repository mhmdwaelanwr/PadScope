using System.Management;
using System.Text.RegularExpressions;
using PadScope.Core.Models;

namespace PadScope.Core.Scanning;

public sealed class WindowsDeviceScanner : IControllerScanner
{
    private const string HidClassGuid = "{745a17a0-74d3-11d0-b6fe-00a0c90f57da}";

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
        devices.AddRange(ScanGameControllers());
        devices.AddRange(ScanAudioDevices());

        return devices
            .DistinctBy(device => device.DevicePath ?? $"{device.DisplayName}:{device.VendorId}:{device.ProductId}:{device.Source}")
            .OrderBy(device => device.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<ControllerDevice> ScanPnPDevices()
    {
        using ManagementObjectSearcher searcher = new(
            $"SELECT Name, Manufacturer, DeviceID, PNPDeviceID, Service, ClassGuid FROM Win32_PnPEntity WHERE ClassGuid = '{HidClassGuid}'"
        );

        List<string> gameControllerIds = EnumerateGameControllerDeviceIds();

        foreach (ManagementObject item in searcher.Get().OfType<ManagementObject>())
        {
            string name = ReadString(item, "Name") ?? string.Empty;
            string pnpDeviceId = ReadString(item, "PNPDeviceID") ?? ReadString(item, "DeviceID") ?? string.Empty;
            string manufacturer = ReadString(item, "Manufacturer") ?? string.Empty;

            if (!LooksLikeController(name, pnpDeviceId, manufacturer, gameControllerIds))
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

    private static IEnumerable<ControllerDevice> ScanGameControllers()
    {
        using ManagementObjectSearcher searcher = new(
            "SELECT Name, Manufacturer, DeviceID, PNPDeviceID FROM Win32_GameController"
        );

        foreach (ManagementObject item in searcher.Get().OfType<ManagementObject>())
        {
            string name = ReadString(item, "Name") ?? string.Empty;
            string pnpDeviceId = ReadString(item, "PNPDeviceID") ?? ReadString(item, "DeviceID") ?? string.Empty;
            string manufacturer = ReadString(item, "Manufacturer") ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(pnpDeviceId))
            {
                continue;
            }

            (string? vendorId, string? productId) = ExtractVidPid(pnpDeviceId);

            yield return new ControllerDevice(
                DisplayName: string.IsNullOrWhiteSpace(name) ? "Game controller" : name,
                Manufacturer: string.IsNullOrWhiteSpace(manufacturer) ? null : manufacturer,
                VendorId: vendorId,
                ProductId: productId,
                DevicePath: string.IsNullOrWhiteSpace(pnpDeviceId) ? null : pnpDeviceId,
                ConnectionType: InferConnectionType(pnpDeviceId),
                Source: "Win32_GameController"
            );
        }
    }

    private static List<string> EnumerateGameControllerDeviceIds()
    {
        List<string> ids = new();

        try
        {
            using ManagementObjectSearcher searcher = new(
                "SELECT DeviceID, PNPDeviceID FROM Win32_GameController"
            );

            foreach (ManagementObject item in searcher.Get().OfType<ManagementObject>())
            {
                string? pnpDeviceId = ReadString(item, "PNPDeviceID") ?? ReadString(item, "DeviceID");
                if (!string.IsNullOrWhiteSpace(pnpDeviceId))
                {
                    ids.Add(pnpDeviceId);
                }
            }
        }
        catch (ManagementException)
        {
            // The game controller class is not available on every Windows build.
        }

        return ids;
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

    private static bool LooksLikeController(
        string name,
        string pnpDeviceId,
        string manufacturer,
        IReadOnlyCollection<string> gameControllerIds)
    {
        string combined = $"{name} {pnpDeviceId} {manufacturer}";

        if (ContainsAny(combined, ExcludedControllerLikeKeywords) &&
            !ContainsAny(combined, StrongControllerKeywords))
        {
            return false;
        }

        if (ContainsAny(combined, StrongControllerKeywords))
        {
            return true;
        }

        return gameControllerIds.Any(id =>
            id.Equals(pnpDeviceId, StringComparison.OrdinalIgnoreCase));
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
            pnpDeviceId.StartsWith("BTHLE", StringComparison.OrdinalIgnoreCase) ||
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
        Match usb = Regex.Match(
            value,
            "VID_([0-9A-Fa-f]{4})[^0-9A-Fa-f]*PID_([0-9A-Fa-f]{4})",
            RegexOptions.IgnoreCase);
        if (usb.Success)
        {
            return (usb.Groups[1].Value.ToUpperInvariant(), usb.Groups[2].Value.ToUpperInvariant());
        }

        Match bluetooth = Regex.Match(
            value,
            "VID&([0-9A-Fa-f]{4,8})_PID&([0-9A-Fa-f]{4})",
            RegexOptions.IgnoreCase);
        if (bluetooth.Success)
        {
            string vidDigits = bluetooth.Groups[1].Value;
            string vendorId = vidDigits.Substring(vidDigits.Length - 4).ToUpperInvariant();
            return (vendorId, bluetooth.Groups[2].Value.ToUpperInvariant());
        }

        return (null, null);
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