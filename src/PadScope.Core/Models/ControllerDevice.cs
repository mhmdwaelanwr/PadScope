namespace PadScope.Core.Models;

public sealed record ControllerDevice(
    string DisplayName,
    string? Manufacturer,
    string? VendorId,
    string? ProductId,
    string? DevicePath,
    ConnectionType ConnectionType,
    string Source
);
