namespace PadScope.Core.Testing;

public static class TestStageRegistry
{
    public static IReadOnlyList<TestStageDefinition> All { get; } = new[]
    {
        new TestStageDefinition(
            TestStage.BuildVerification,
            "Build verification",
            "Ready now",
            "Prove the solution restores and builds on Windows.",
            "Run dotnet restore, dotnet build, then start the desktop app.",
            "Build succeeded and PadScope.Desktop opens."
        ),
        new TestStageDefinition(
            TestStage.EmptyScan,
            "No-controller scan",
            "Ready now",
            "Prove the app handles an empty or near-empty device state safely.",
            "Disconnect gamepads, run Scan Controllers, then observe the result.",
            "No crash, no misleading feature claims, clear empty or system-device state."
        ),
        new TestStageDefinition(
            TestStage.UsbScan,
            "USB controller scan",
            "Next real test",
            "Collect wired identity for the target controller.",
            "Connect Marvo GT-84 by USB, scan, export JSON and Markdown.",
            "Device appears with useful name, VID/PID when available, path, source, and profile match."
        ),
        new TestStageDefinition(
            TestStage.BluetoothScan,
            "Bluetooth controller scan",
            "After USB scan",
            "Collect wireless identity and compare it with USB.",
            "Disconnect USB, connect Bluetooth, scan, export JSON and Markdown.",
            "Bluetooth device identity is captured honestly and differences from USB are visible."
        ),
        new TestStageDefinition(
            TestStage.ProfileValidation,
            "Profile validation",
            "After both scans",
            "Turn scan evidence into compatibility profile updates.",
            "Compare exported reports against data/controllers profiles.",
            "Known facts are recorded; unknown features remain unknown."
        ),
        new TestStageDefinition(
            TestStage.HidInspection,
            "HID identity inspection",
            "Planned",
            "Inspect HID identity and report shape before output tests.",
            "Add read-only HID interface inspection and report shape notes.",
            "No HID output reports are sent. More identity evidence is shown."
        ),
        new TestStageDefinition(
            TestStage.Rumble,
            "Controlled rumble test",
            "Locked",
            "Run a short rumble pulse only after identity is known.",
            "Select a target, confirm warning, run one short pulse.",
            "Result is recorded as observed-working, failed, unsupported, or inconclusive."
        ),
        new TestStageDefinition(
            TestStage.Lightbar,
            "Controlled lightbar test",
            "Locked",
            "Run one controlled lightbar change on a known DS4-like target.",
            "Select target, confirm warning, apply one safe color change, then stop.",
            "Result is recorded and test stops cleanly."
        ),
        new TestStageDefinition(
            TestStage.TouchpadAndGyro,
            "Touchpad and gyro observation",
            "Planned",
            "Observe input-only motion and touch data where available.",
            "Read input reports only and display whether touch/gyro data exists.",
            "No output reports are sent; data is detected or marked unavailable."
        ),
        new TestStageDefinition(
            TestStage.AudioEndpoint,
            "Windows audio endpoint validation",
            "Partially ready",
            "Separate Windows sound-device visibility from DS4 HID audio support.",
            "Scan Win32_SoundDevice and match controller-like audio endpoints.",
            "The app does not claim DS4 audio works just because a sound endpoint exists."
        ),
        new TestStageDefinition(
            TestStage.AudioProbe,
            "DS4 Audio Lab probe",
            "Experimental locked",
            "Probe DS4-style audio behavior only after safety gates pass.",
            "Select known DS4-like target, confirm experimental warning, run probe.",
            "Result is not-run, unsupported, accepted, rejected, or error."
        ),
        new TestStageDefinition(
            TestStage.Packaging,
            "Portable release packaging",
            "Later",
            "Produce a usable build for non-developers.",
            "Create a portable ZIP after core diagnostics are stable.",
            "Release artifact launches on Windows and includes docs."
        )
    };
}
