namespace PadScope.Core.Testing;

public static class TestStageRegistry
{
    public static IReadOnlyList<TestStageDefinition> All { get; } = new[]
    {
        new TestStageDefinition(
            TestStage.BuildVerification,
            "Build verification",
            "Implemented",
            "Prove the solution restores, builds, and starts on Windows.",
            "Run dotnet restore, dotnet build, then start PadScope.Desktop.",
            "Build succeeds and the desktop window opens without a startup exception."
        ),
        new TestStageDefinition(
            TestStage.EmptyScan,
            "No-controller scan",
            "Implemented",
            "Prove the app handles no physical gamepad safely.",
            "Disconnect gamepads, run Scan Controllers, then review the detected list.",
            "No crash, no fake feature claims, and only honest system or virtual entries appear."
        ),
        new TestStageDefinition(
            TestStage.UsbScan,
            "USB controller scan",
            "Implemented",
            "Collect wired identity for the target controller.",
            "Connect the controller by USB, run Scan Controllers, then export JSON and Markdown.",
            "Device name, VID/PID when available, connection type, source, path, profile, and notes are captured."
        ),
        new TestStageDefinition(
            TestStage.BluetoothScan,
            "Bluetooth controller scan",
            "Implemented",
            "Collect wireless identity and compare it with USB.",
            "Disconnect USB, connect by Bluetooth, run Scan Controllers, then export reports.",
            "Bluetooth device identity is captured honestly and differences from USB are visible."
        ),
        new TestStageDefinition(
            TestStage.ProfileValidation,
            "Profile validation",
            "Implemented baseline",
            "Turn scan evidence into conservative compatibility profile matching.",
            "Compare detected name, VID/PID, connection, and source against the starter profiles.",
            "Known facts are matched; untested features remain NotTested or Unknown."
        ),
        new TestStageDefinition(
            TestStage.HidInspection,
            "HID identity inspection",
            "Implemented - read-only HID read",
            "Inspect HID identity and report shape before any output tests.",
            "Open the HID interface, capture live input reports, and review buttons, sticks, triggers, gyro, and touch data.",
            "No HID output reports are sent during inspection. Identity evidence is shown before rumble or lightbar."
        ),
        new TestStageDefinition(
            TestStage.Rumble,
            "Controlled rumble test",
            "Implemented - confirmation required",
            "Run one short rumble pulse only after HID identity is known.",
            "Select a device, confirm the warning, then send a short rumble output report.",
            "Result is recorded as observed-working, failed, unsupported, or inconclusive."
        ),
        new TestStageDefinition(
            TestStage.Lightbar,
            "Controlled lightbar test",
            "Implemented - confirmation required",
            "Run one controlled lightbar change on a known DS4-like target.",
            "Select a device, confirm the warning, then send one lightbar output report.",
            "Result is recorded and the test stops cleanly."
        ),
        new TestStageDefinition(
            TestStage.TouchpadAndGyro,
            "Touchpad and gyro observation",
            "Implemented - read-only",
            "Observe input-only motion and touch data where available.",
            "Capture live input reports and read gyro, accelerometer, and touchpad fields.",
            "No output reports are sent; data is detected or marked unavailable."
        ),
        new TestStageDefinition(
            TestStage.AudioEndpoint,
            "Windows audio endpoint validation",
            "Implemented baseline",
            "Separate Windows sound-device visibility from DS4 HID audio support.",
            "Scan Win32_SoundDevice and match only controller-like audio endpoints.",
            "The app does not claim DS4 audio works just because a Windows sound endpoint exists."
        ),
        new TestStageDefinition(
            TestStage.AudioProbe,
            "DS4 Audio Lab probe",
            "Experimental locked - needs Stage 5 evidence",
            "Probe DS4-style audio behavior only after safety gates pass.",
            "Unlock only for a known DS4-like target after explicit experimental confirmation.",
            "Result is not-run, unsupported, accepted, rejected, or error; nothing runs automatically."
        ),
        new TestStageDefinition(
            TestStage.Packaging,
            "Portable release packaging",
            "Implemented",
            "Produce a usable Windows build for testers.",
            "Use the Package Windows GitHub Actions workflow or dotnet publish for win-x64.",
            "A ZIP artifact is produced and PadScope.Desktop launches on Windows."
        ),
        new TestStageDefinition(
            TestStage.VirtualController,
            "Virtual controller passthrough",
            "Implemented - requires ViGEmBus driver",
            "Expose the physical controller as a virtual DualShock 4 or Xbox 360 device for games.",
            "Install ViGEmBus, then use the Virtual Controller tab or the CLI virtual command.",
            "The virtual pad appears in Windows, mirrors live input, and forwards rumble, lightbar, and LED requests back."
        ),
        new TestStageDefinition(
            TestStage.Remapping,
            "Profile remapping",
            "Implemented - requires ViGEmBus and a JSON profile",
            "Remap buttons, sticks, and triggers through a saved profile before the virtual pad sees them.",
            "Create a JSON profile with ProfileStore, then start the virtual command or tab with --profile.",
            "Remapped buttons and axes are visible on the virtual pad and forwarded game feedback still reaches the physical controller."
        ),
        new TestStageDefinition(
            TestStage.HidHideIntegration,
            "HidHide integration",
            "Implemented - requires HidHide driver",
            "Hide the physical controller from games so only the virtual pad is visible.",
            "Install HidHide, then review its detected status and add PadScope as a trusted application.",
            "The HidHide driver status is shown and the physical controller can be hidden while the virtual pad stays visible."
        )
    };
}
