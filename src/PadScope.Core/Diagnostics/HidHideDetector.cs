using System.Management;

namespace PadScope.Core.Diagnostics;

public static class HidHideDetector
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
                "SELECT Name, Version, State FROM Win32_SystemDriver WHERE Name = 'HidHide'"
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
            ? "HidHide driver is not installed. Install it from https://github.com/ViGEm/HidHide/releases to hide the physical controller from games."
            : $"HidHide driver detected (version {version}).";
    }
}