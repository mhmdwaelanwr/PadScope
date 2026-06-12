using PadScope.Core.Models;

namespace PadScope.Core.Diagnostics;

public static class ReportBuilder
{
    public static CompatibilityReport BuildInitialReport(ControllerDevice device)
    {
        List<string> notes = new()
        {
            "This is an initial read-only report.",
            "Active feature tests have not been executed yet.",
            "Do not assume clone controllers fully implement the DualShock 4 protocol."
        };

        return new CompatibilityReport(
            device,
            Input: FeatureStatus.RequiresManualTest,
            Rumble: FeatureStatus.NotTested,
            Lightbar: FeatureStatus.NotTested,
            Gyro: FeatureStatus.NotTested,
            Touchpad: FeatureStatus.NotTested,
            WindowsAudioEndpoint: FeatureStatus.NotTested,
            Ds4AudioProtocol: FeatureStatus.NotTested,
            Notes: notes
        );
    }
}
