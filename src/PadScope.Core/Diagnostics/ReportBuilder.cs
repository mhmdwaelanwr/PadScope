using PadScope.Core.Models;

namespace PadScope.Core.Diagnostics;

public static class ReportBuilder
{
    public static CompatibilityReport BuildInitialReport(ControllerDevice device)
    {
        bool isSoundDevice = device.Source.Equals("Win32_SoundDevice", StringComparison.OrdinalIgnoreCase);

        List<string> notes = new()
        {
            "This is an initial read-only report.",
            "Active feature tests have not been executed yet.",
            "Do not assume clone controllers fully implement the DualShock 4 protocol."
        };

        if (isSoundDevice)
        {
            notes.Add("Windows lists this controller-like item as a sound device. Further testing is still required.");
        }

        return new CompatibilityReport(
            device,
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
