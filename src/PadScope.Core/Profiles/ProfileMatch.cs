using PadScope.Core.Models;

namespace PadScope.Core.Profiles;

public sealed record ProfileMatch(
    string Name,
    string Confidence,
    RiskLevel RecommendedRiskLevel,
    string RecommendedNextAction
);
