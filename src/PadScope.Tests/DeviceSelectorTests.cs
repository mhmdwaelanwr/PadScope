using PadScope.Core.Models;
using PadScope.Core.Scanning;
using Xunit;

namespace PadScope.Tests;

public sealed class DeviceSelectorTests
{
    [Fact]
    public void SelectsTheOnlyDeviceWithoutFilters()
    {
        CompatibilityReport report = CreateReport("054C", "09CC", "DualShock 4");

        bool selected = DeviceSelector.TrySelect([report], null, null, out ControllerDevice? device, out string? error);

        Assert.True(selected);
        Assert.Equal("DualShock 4", device?.DisplayName);
        Assert.Null(error);
    }

    [Fact]
    public void RejectsAnIdentityThatDoesNotMatch()
    {
        CompatibilityReport report = CreateReport("054C", "09CC", "DualShock 4");

        bool selected = DeviceSelector.TrySelect([report], "1234", "5678", out ControllerDevice? device, out string? error);

        Assert.False(selected);
        Assert.Null(device);
        Assert.Contains("No device matches", error);
    }

    [Fact]
    public void RequiresAnExplicitIdentityWhenSeveralDevicesExist()
    {
        CompatibilityReport first = CreateReport("054C", "09CC", "DualShock 4");
        CompatibilityReport second = CreateReport("054C", "0CE6", "DualSense");

        bool selected = DeviceSelector.TrySelect([first, second], null, null, out ControllerDevice? device, out string? error);

        Assert.False(selected);
        Assert.Null(device);
        Assert.Contains("Multiple controllers", error);
    }

    private static CompatibilityReport CreateReport(string vendorId, string productId, string name)
    {
        ControllerDevice device = new(name, "Test", vendorId, productId, "test-path", ConnectionType.Usb, "Test");
        return new CompatibilityReport(
            device,
            "Test profile",
            "High",
            RiskLevel.Controlled,
            "Test carefully",
            FeatureStatus.Unknown,
            FeatureStatus.Unknown,
            FeatureStatus.Unknown,
            FeatureStatus.Unknown,
            FeatureStatus.Unknown,
            FeatureStatus.Unknown,
            FeatureStatus.Unknown,
            []);
    }
}
