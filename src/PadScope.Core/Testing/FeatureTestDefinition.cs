using PadScope.Core.Models;

namespace PadScope.Core.Testing;

public sealed record FeatureTestDefinition(
    DiagnosticFeature Feature,
    string Name,
    TestStage Stage,
    RiskLevel RiskLevel,
    bool RequiresSelectedDevice,
    bool RequiresUserConfirmation,
    bool EnabledByDefault,
    string Goal,
    string PassCriteria
);
