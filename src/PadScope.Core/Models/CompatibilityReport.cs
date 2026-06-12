namespace PadScope.Core.Models;

public sealed record CompatibilityReport(
    ControllerDevice Device,
    FeatureStatus Input,
    FeatureStatus Rumble,
    FeatureStatus Lightbar,
    FeatureStatus Gyro,
    FeatureStatus Touchpad,
    FeatureStatus WindowsAudioEndpoint,
    FeatureStatus Ds4AudioProtocol,
    IReadOnlyList<string> Notes
);
