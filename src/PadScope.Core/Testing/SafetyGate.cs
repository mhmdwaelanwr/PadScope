using PadScope.Core.Models;

namespace PadScope.Core.Testing;

public static class SafetyGate
{
    public static bool CanRun(FeatureTestDefinition test, CompatibilityReport? selectedReport, bool userConfirmed)
    {
        if (!test.EnabledByDefault && test.RiskLevel == RiskLevel.Safe)
        {
            return selectedReport is not null || !test.RequiresSelectedDevice;
        }

        if (test.RequiresSelectedDevice && selectedReport is null)
        {
            return false;
        }

        if (test.RequiresUserConfirmation && !userConfirmed)
        {
            return false;
        }

        return test.RiskLevel switch
        {
            RiskLevel.Safe => true,
            RiskLevel.Controlled => selectedReport is not null && userConfirmed,
            RiskLevel.Experimental => selectedReport is not null && userConfirmed && selectedReport.RecommendedRiskLevel != RiskLevel.Safe,
            _ => false
        };
    }

    public static string ExplainBlocked(FeatureTestDefinition test, CompatibilityReport? selectedReport, bool userConfirmed)
    {
        if (test.RequiresSelectedDevice && selectedReport is null)
        {
            return "Select a target device before running this test.";
        }

        if (test.RequiresUserConfirmation && !userConfirmed)
        {
            return "This test requires explicit user confirmation.";
        }

        if (test.RiskLevel == RiskLevel.Experimental && selectedReport?.RecommendedRiskLevel == RiskLevel.Safe)
        {
            return "Experimental tests are locked until the selected device has stronger evidence and a supported profile.";
        }

        return "This test is not enabled for the current stage.";
    }
}
