using System.Management;

namespace PadScope.Core.Diagnostics;

public static class AudioProbe
{
    public static IReadOnlyList<AudioDeviceInfo> FindControllerAudioEndpoints()
    {
        if (!OperatingSystem.IsWindows())
        {
            return Array.Empty<AudioDeviceInfo>();
        }

        List<AudioDeviceInfo> results = new();

        try
        {
            using ManagementObjectSearcher searcher = new(
                "SELECT Name, DeviceId, PNPDeviceID, Status FROM Win32_SoundDevice"
            );

            foreach (ManagementObject item in searcher.Get().OfType<ManagementObject>())
            {
                string name = item["Name"]?.ToString() ?? string.Empty;
                string deviceId = item["DeviceId"]?.ToString() ?? string.Empty;
                string pnpDeviceId = item["PNPDeviceID"]?.ToString() ?? string.Empty;
                string status = item["Status"]?.ToString() ?? "Unknown";

                bool isControllerLike = IsControllerAudioEndpoint(name, pnpDeviceId);

                results.Add(new AudioDeviceInfo
                {
                    Name = name,
                    DeviceId = deviceId,
                    PnpDeviceId = pnpDeviceId,
                    Status = status,
                    IsControllerLike = isControllerLike
                });
            }
        }
        catch (ManagementException)
        {
            // WMI enumeration unavailable.
        }

        return results;
    }

    public static IReadOnlyList<AudioDeviceInfo> FindControllerSpeakers()
    {
        return FindControllerAudioEndpoints()
            .Where(e => e.IsControllerLike && IsSpeakerLike(e.Name))
            .ToList();
    }

    public static IReadOnlyList<AudioDeviceInfo> FindControllerMicrophones()
    {
        return FindControllerAudioEndpoints()
            .Where(e => e.IsControllerLike && IsMicrophoneLike(e.Name))
            .ToList();
    }

    public static bool HasControllerAudioEndpoint()
    {
        return FindControllerAudioEndpoints().Any(e => e.IsControllerLike);
    }

    public static string DescribeStatus()
    {
        var endpoints = FindControllerAudioEndpoints();
        var controllerEndpoints = endpoints.Where(e => e.IsControllerLike).ToList();

        if (controllerEndpoints.Count == 0)
        {
            return "No controller audio endpoints detected. Connect a DS4/DualSense via USB or Bluetooth with audio support.";
        }

        int speakers = controllerEndpoints.Count(e => IsSpeakerLike(e.Name));
        int mics = controllerEndpoints.Count(e => IsMicrophoneLike(e.Name));

        return $"Found {controllerEndpoints.Count} controller audio endpoint(s): {speakers} speaker(s), {mics} microphone(s). " +
               $"Ready for audio streaming probe.";
    }

    private static bool IsControllerAudioEndpoint(string name, string pnpDeviceId)
    {
        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(pnpDeviceId))
        {
            return false;
        }

        string combined = $"{name} {pnpDeviceId}".ToLowerInvariant();

        return combined.Contains("dualshock") ||
               combined.Contains("ds4") ||
               combined.Contains("dualsense") ||
               combined.Contains("wireless controller") ||
               combined.Contains("ps4") ||
               combined.Contains("ps5") ||
               combined.Contains("sony") ||
               combined.Contains("054c") || // Sony VID
               combined.Contains("0ce6");   // Sony DS5 PID prefix
    }

    private static bool IsSpeakerLike(string name)
    {
        string lower = name.ToLowerInvariant();
        return lower.Contains("speaker") ||
               lower.Contains("headphone") ||
               lower.Contains("headset") ||
               (!lower.Contains("microphone") && !lower.Contains("mic"));
    }

    private static bool IsMicrophoneLike(string name)
    {
        string lower = name.ToLowerInvariant();
        return lower.Contains("microphone") ||
               lower.Contains("mic") ||
               lower.Contains("input");
    }
}

public sealed class AudioDeviceInfo
{
    public required string Name { get; init; }
    public required string DeviceId { get; init; }
    public required string PnpDeviceId { get; init; }
    public required string Status { get; init; }
    public required bool IsControllerLike { get; init; }

    public override string ToString() => $"{Name} [{Status}] (Controller: {IsControllerLike})";
}
