using PadScope.Core.Models;
using PadScope.Core.Profiles;

namespace PadScope.Core.Diagnostics;

public static class ReportBuilder
{
    public static CompatibilityReport BuildInitialReport(ControllerDevice device)
    {
        bool isSoundDevice = device.Source.Equals("Win32_SoundDevice", StringComparison.OrdinalIgnoreCase);
        ProfileMatch profile = ProfileMatcher.Match(device);

        List<string> notes = new()
        {
            "This is an initial read-only report.",
            "Active feature tests have not been executed yet.",
            "Clone controllers may not implement the full DualShock 4 protocol.",
            $"Profile match: {profile.Name} ({profile.Confidence} confidence).",
            $"Next action: {profile.RecommendedNextAction}"
        };

        if (isSoundDevice)
        {
            notes.Add("Windows lists this controller-like item as a sound device. Further testing is still required.");
        }

        return new CompatibilityReport(
            device,
            ProfileName: profile.Name,
            ProfileConfidence: profile.Confidence,
            RecommendedRiskLevel: profile.RecommendedRiskLevel,
            RecommendedNextAction: profile.RecommendedNextAction,
            Input: isSoundDevice ? FeatureStatus.Unknown : FeatureStatus.RequiresManualTest,
            Rumble: FeatureStatus.NotTested,
            Lightbar: FeatureStatus.NotTested,
            Gyro: FeatureStatus.NotTested,
            Touchpad: FeatureStatus.NotTested,
            WindowsAudioEndpoint: isSoundDevice ? FeatureStatus.Supported : FeatureStatus.NotTested,
            Ds4AudioProtocol: FeatureStatus.NotTested,
            Notes: notes
        );
    }
}
