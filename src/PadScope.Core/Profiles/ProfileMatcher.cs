using PadScope.Core.Models;

namespace PadScope.Core.Profiles;

public static class ProfileMatcher
{
    public static ProfileMatch Match(ControllerDevice device)
    {
        string text = $"{device.DisplayName} {device.Manufacturer} {device.DevicePath} {device.VendorId} {device.ProductId}";

        if (Contains(text, "marvo") || Contains(text, "gt-84"))
        {
            return new ProfileMatch(
                Name: "Marvo GT-84 / DS4-style clone",
                Confidence: "medium",
                RecommendedRiskLevel: RiskLevel.Safe,
                RecommendedNextAction: "Collect USB and Bluetooth scan reports before enabling feature tests."
            );
        }

        if (Contains(text, "skytech"))
        {
            return new ProfileMatch(
                Name: "SkyTech DS4-style clone",
                Confidence: "medium",
                RecommendedRiskLevel: RiskLevel.Safe,
                RecommendedNextAction: "Collect VID/PID and DS4Windows behavior before enabling feature tests."
            );
        }

        if (Contains(text, "zero"))
        {
            return new ProfileMatch(
                Name: "Zero DS4-style clone",
                Confidence: "medium",
                RecommendedRiskLevel: RiskLevel.Safe,
                RecommendedNextAction: "Collect HID identity and verify whether the unit is a DS4 clone or generic DInput pad."
            );
        }

        if (Contains(text, "aula") || Contains(text, "g1000"))
        {
            return new ProfileMatch(
                Name: "AULA G1000 / DirectInput PC gamepad",
                Confidence: "medium",
                RecommendedRiskLevel: RiskLevel.Safe,
                RecommendedNextAction: "Focus on DirectInput/XInput and rumble testing; DS4 audio does not apply."
            );
        }

        if (device.VendorId?.Equals("054C", StringComparison.OrdinalIgnoreCase) == true)
        {
            return new ProfileMatch(
                Name: "Sony PlayStation controller family",
                Confidence: "high",
                RecommendedRiskLevel: RiskLevel.Controlled,
                RecommendedNextAction: "Safe rumble/lightbar tests may be added after HID report inspection."
            );
        }

        if (Contains(text, "wireless controller"))
        {
            return new ProfileMatch(
                Name: "DS4-compatible Wireless Controller",
                Confidence: "medium",
                RecommendedRiskLevel: RiskLevel.Safe,
                RecommendedNextAction: "Identify VID/PID and report shape before assuming full DS4 compatibility."
            );
        }

        if (Contains(text, "xbox") || device.VendorId?.Equals("045E", StringComparison.OrdinalIgnoreCase) == true)
        {
            return new ProfileMatch(
                Name: "Xbox/XInput controller family",
                Confidence: "medium",
                RecommendedRiskLevel: RiskLevel.Safe,
                RecommendedNextAction: "PadScope can report identity, but DS4 audio features do not apply."
            );
        }

        if (device.Source.Equals("Win32_SoundDevice", StringComparison.OrdinalIgnoreCase))
        {
            return new ProfileMatch(
                Name: "Controller-like sound endpoint",
                Confidence: "low",
                RecommendedRiskLevel: RiskLevel.Safe,
                RecommendedNextAction: "Match this sound endpoint with the corresponding HID controller device."
            );
        }

        return new ProfileMatch(
            Name: "Unknown controller-like device",
            Confidence: "low",
            RecommendedRiskLevel: RiskLevel.Safe,
            RecommendedNextAction: "Export the scan report and add a compatibility profile after manual review."
        );
    }

    private static bool Contains(string value, string keyword)
    {
        return value.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }
}
