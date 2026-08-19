using System.Management;

namespace PadScope.Core.Diagnostics;

public static class ViGEmBusDetector
{
    public static bool IsDriverInstalled()
    {
        return GetDriverVersion() is not null;
    }

    public static string? GetDriverVersion()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            using ManagementObjectSearcher searcher = new(
                "SELECT Name, Version, State FROM Win32_SystemDriver WHERE Name = 'ViGEmBus'"
            );

            foreach (ManagementObject item in searcher.Get().OfType<ManagementObject>())
            {
                return item["Version"]?.ToString();
            }
        }
        catch (ManagementException)
        {
            // Driver enumeration is unavailable; report as not installed.
        }

        return null;
    }

    public static string DescribeStatus()
    {
        string? version = GetDriverVersion();

        return version is null
            ? "ViGEmBus driver is not installed. Install it from https://github.com/nefarius/ViGEmBus/releases, then restart PadScope."
            : $"ViGEmBus driver detected (version {version}).";
    }
}