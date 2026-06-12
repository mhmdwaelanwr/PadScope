namespace PadScope.Core.Models;

public sealed record CompatibilityReport(
    ControllerDevice Device,
    string ProfileName,
    string ProfileConfidence,
    RiskLevel RecommendedRiskLevel,
    string RecommendedNextAction,
    FeatureStatus Input,
    FeatureStatus Rumble,
    FeatureStatus Lightbar,
    FeatureStatus Gyro,
    FeatureStatus Touchpad,
    FeatureStatus WindowsAudioEndpoint,
    FeatureStatus Ds4AudioProtocol,
    IReadOnlyList<string> Notes
);
