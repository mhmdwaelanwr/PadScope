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
            EnabledByDefault: true,
            Goal: "Inspect HID identity and report shape without output writes.",
            PassCriteria: "Live input reports are read and parsed without sending output reports."
        ),
        new FeatureTestDefinition(
            DiagnosticFeature.RumbleTest,
            "Safe rumble pulse",
            TestStage.Rumble,
            RiskLevel.Controlled,
            RequiresSelectedDevice: true,
            RequiresUserConfirmation: true,
            EnabledByDefault: true,
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
            EnabledByDefault: true,
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
            EnabledByDefault: true,
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
            EnabledByDefault: true,
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
            RiskLevel.Controlled,
            RequiresSelectedDevice: true,
            RequiresUserConfirmation: true,
            EnabledByDefault: true,
            Goal: "Probe DS4-style audio behavior on an identified DS4-like target.",
            PassCriteria: "Result is not-run, unsupported, accepted, rejected, or error."
        ),
        new FeatureTestDefinition(
            DiagnosticFeature.Ds4AudioStreaming,
            "DS4 audio streaming",
            TestStage.AudioProbe,
            RiskLevel.Controlled,
            RequiresSelectedDevice: true,
            RequiresUserConfirmation: true,
            EnabledByDefault: true,
            Goal: "Stream audio between DS4 speaker/microphone and Windows audio endpoints via WASAPI.",
            PassCriteria: "Capture and playback routes are established, interruptible, and report real device names."
        ),
        new FeatureTestDefinition(
            DiagnosticFeature.VirtualControllerOutput,
            "Virtual controller passthrough",
            TestStage.VirtualController,
            RiskLevel.Controlled,
            RequiresSelectedDevice: true,
            RequiresUserConfirmation: true,
            EnabledByDefault: true,
            Goal: "Expose a virtual DualShock 4 or Xbox 360 device and mirror live input.",
            PassCriteria: "Virtual pad is visible, mirrors input, and forwards game feedback to the physical controller."
        ),
        new FeatureTestDefinition(
            DiagnosticFeature.RemappingProfiles,
            "Profile remapping",
            TestStage.Remapping,
            RiskLevel.Controlled,
            RequiresSelectedDevice: true,
            RequiresUserConfirmation: true,
            EnabledByDefault: true,
            Goal: "Apply saved button, stick, and trigger remaps before the virtual pad sees them.",
            PassCriteria: "Remapped input matches the loaded profile and game feedback still reaches the physical controller."
        ),
        new FeatureTestDefinition(
            DiagnosticFeature.HidHideDiagnostics,
            "HidHide integration",
            TestStage.HidHideIntegration,
            RiskLevel.Safe,
            RequiresSelectedDevice: false,
            RequiresUserConfirmation: false,
            EnabledByDefault: true,
            Goal: "Detect HidHide and explain how to hide the physical controller from games.",
            PassCriteria: "The HidHide driver status is reported and the next steps are shown."
        ),
        new FeatureTestDefinition(
            DiagnosticFeature.TouchpadMouseEmulation,
            "Touchpad to mouse",
            TestStage.TouchpadMouse,
            RiskLevel.Controlled,
            RequiresSelectedDevice: true,
            RequiresUserConfirmation: true,
            EnabledByDefault: true,
            Goal: "Move and click the Windows mouse from touchpad gestures.",
            PassCriteria: "Movement, tap-click, drag, and two-finger right-click match the configured sensitivity."
        ),
        new FeatureTestDefinition(
            DiagnosticFeature.GyroMouseEmulation,
            "Gyro to mouse",
            TestStage.GyroMouse,
            RiskLevel.Controlled,
            RequiresSelectedDevice: true,
            RequiresUserConfirmation: true,
            EnabledByDefault: true,
            Goal: "Move the Windows mouse from gyroscope input with smoothing.",
            PassCriteria: "Tilting moves the mouse and sensitivity, inversion, and smoothing are tunable."
        ),
        new FeatureTestDefinition(
            DiagnosticFeature.MacroEmulation,
            "Macros and combos",
            TestStage.Macros,
            RiskLevel.Controlled,
            RequiresSelectedDevice: true,
            RequiresUserConfirmation: true,
            EnabledByDefault: true,
            Goal: "Turn button chords into sends, rapid fire, toggles, and timed sequences on the virtual pad.",
            PassCriteria: "Combo output appears on the virtual pad, rapid fire pulses at the configured rate, and sequences complete in order."
        )
    };
}
