using PadScope.Core.Models;

namespace PadScope.Core.Testing;

public static class FeatureTestRegistry
{
    public static IReadOnlyList<FeatureTestDefinition> All { get; } = new[]
    {
        new FeatureTestDefinition(
            DiagnosticFeature.DeviceDiscovery,
            "Device discovery scan",
            TestStage.UsbScan,
            RiskLevel.Safe,
            RequiresSelectedDevice: false,
            RequiresUserConfirmation: false,
            EnabledByDefault: true,
            Goal: "List controller-like devices and basic identity data.",
            PassCriteria: "Devices are listed without sending output reports."
        ),
        new FeatureTestDefinition(
            DiagnosticFeature.ProfileMatching,
            "Profile matching",
            TestStage.ProfileValidation,
            RiskLevel.Safe,
            RequiresSelectedDevice: false,
            RequiresUserConfirmation: false,
            EnabledByDefault: true,
            Goal: "Match detected controllers against conservative profiles.",
            PassCriteria: "Unknown devices remain unknown; matched devices show confidence and next action."
        ),
        new FeatureTestDefinition(
            DiagnosticFeature.ReportExport,
            "Report export",
            TestStage.UsbScan,
            RiskLevel.Safe,
            RequiresSelectedDevice: false,
            RequiresUserConfirmation: false,
            EnabledByDefault: true,
            Goal: "Export JSON and Markdown reports.",
            PassCriteria: "Reports contain identity, profile, feature statuses, and safety notes."
        ),
        new FeatureTestDefinition(
            DiagnosticFeature.HidIdentityInspection,
            "HID identity inspection",
            TestStage.HidInspection,
            RiskLevel.Safe,
            RequiresSelectedDevice: true,
            RequiresUserConfirmation: false,
            EnabledByDefault: false,
            Goal: "Inspect HID identity and report shape without output writes.",
            PassCriteria: "Report shape notes are captured while staying read-only."
        ),
        new FeatureTestDefinition(
            DiagnosticFeature.RumbleTest,
            "Safe rumble pulse",
            TestStage.Rumble,
            RiskLevel.Controlled,
            RequiresSelectedDevice: true,
            RequiresUserConfirmation: true,
            EnabledByDefault: false,
            Goal: "Run one short rumble pulse on an identified target.",
            PassCriteria: "Result is recorded as observed-working, failed, or unsupported."
        ),
        new FeatureTestDefinition(
            DiagnosticFeature.LightbarTest,
            "Safe lightbar test",
            TestStage.Lightbar,
            RiskLevel.Controlled,
            RequiresSelectedDevice: true,
            RequiresUserConfirmation: true,
            EnabledByDefault: false,
            Goal: "Run one controlled lightbar change on a known DS4-like target.",
            PassCriteria: "Result is recorded and the test stops cleanly."
        ),
        new FeatureTestDefinition(
            DiagnosticFeature.TouchpadTest,
            "Touchpad observation",
            TestStage.TouchpadAndGyro,
            RiskLevel.Safe,
            RequiresSelectedDevice: true,
            RequiresUserConfirmation: false,
            EnabledByDefault: false,
            Goal: "Observe touchpad data if present.",
            PassCriteria: "Touch data is detected or marked unavailable without output writes."
        ),
        new FeatureTestDefinition(
            DiagnosticFeature.GyroTest,
            "Gyro observation",
            TestStage.TouchpadAndGyro,
            RiskLevel.Safe,
            RequiresSelectedDevice: true,
            RequiresUserConfirmation: false,
            EnabledByDefault: false,
            Goal: "Observe gyro or accelerometer data if present.",
            PassCriteria: "Motion data is detected or marked unavailable without output writes."
        ),
        new FeatureTestDefinition(
            DiagnosticFeature.WindowsAudioEndpointCheck,
            "Windows audio endpoint check",
            TestStage.AudioEndpoint,
            RiskLevel.Safe,
            RequiresSelectedDevice: false,
            RequiresUserConfirmation: false,
            EnabledByDefault: true,
            Goal: "Detect controller-like Windows sound devices.",
            PassCriteria: "Sound endpoint presence is separated from DS4 HID audio support."
        ),
        new FeatureTestDefinition(
            DiagnosticFeature.Ds4AudioProbe,
            "DS4 audio probe",
            TestStage.AudioProbe,
            RiskLevel.Experimental,
            RequiresSelectedDevice: true,
            RequiresUserConfirmation: true,
            EnabledByDefault: false,
            Goal: "Probe DS4-style audio behavior on an identified DS4-like target.",
            PassCriteria: "Result is not-run, unsupported, accepted, rejected, or error."
        ),
        new FeatureTestDefinition(
            DiagnosticFeature.Ds4AudioStreaming,
            "DS4 audio streaming experiment",
            TestStage.AudioProbe,
            RiskLevel.Experimental,
            RequiresSelectedDevice: true,
            RequiresUserConfirmation: true,
            EnabledByDefault: false,
            Goal: "Research DS4-style audio streaming after probe support is known.",
            PassCriteria: "Experiment is opt-in, interruptible, and never part of normal scan."
        )
    };
}
