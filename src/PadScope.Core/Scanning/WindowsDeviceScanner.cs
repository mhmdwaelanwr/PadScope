using PadScope.Core.Models;

namespace PadScope.Core.Scanning;

public sealed class WindowsDeviceScanner : IControllerScanner
{
    public CompatibilityReport Scan()
    {
        var notes = new List<string>
        {
            "MVP scanner placeholder: direct Windows HID enumeration will be implemented next.",
            "Current goal is to establish project structure before adding device APIs."
        };

        return new CompatibilityReport(
            DateTimeOffset.UtcNow,
            Array.Empty<ControllerDevice>(),
            notes
        );
    }
}
