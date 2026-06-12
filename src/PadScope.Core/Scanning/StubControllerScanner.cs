using PadScope.Core.Models;

namespace PadScope.Core.Scanning;

public sealed class StubControllerScanner : IControllerScanner
{
    public IReadOnlyList<ControllerDevice> Scan()
    {
        return new[]
        {
            new ControllerDevice(
                DisplayName: "Sample DS4-style controller",
                Manufacturer: "Unknown",
                VendorId: null,
                ProductId: null,
                DevicePath: null,
                ConnectionType: ConnectionType.Unknown,
                Source: "stub"
            )
        };
    }
}
