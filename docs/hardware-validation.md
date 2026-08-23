# Hardware Validation and Release Gate

PadScope is diagnostics-first and evidence-driven. Automated tests can verify parsing, packet layout, safety gates, capture/replay integrity, and session coordination, but they cannot prove that a specific physical controller or clone implements a capability correctly.

This document defines the minimum real-hardware validation required before the first tagged public release.

## Principles

- Do not mark a capability as supported unless it was observed on real hardware.
- Record USB and Bluetooth results separately.
- Treat controller names and DS4-like appearance as hints, not proof of protocol compatibility.
- Keep normal scanning read-only.
- Run rumble, lightbar, virtual-controller, mouse, and audio experiments only after confirming the selected target.
- Stop testing if the controller disconnects, reports become malformed, or output behavior is unexpected.

## Test environment

Record the following before testing:

- Windows version
- PadScope commit or build version
- Controller brand and exact model
- Connection mode: USB or Bluetooth
- VID/PID when available
- Windows device name
- Whether ViGEmBus is installed
- Whether HidHide is installed or active

## Stage 1 — No-controller baseline

- Launch PadScope with no gamepad connected.
- Run Scan.
- Confirm the UI reaches the expected no-device state without crashing.
- Confirm no HID output is sent.

Expected result: PadScope reports no compatible controller and remains usable.

## Stage 2 — USB identity and input

Connect one physical controller over USB.

Verify:

- Scan finds the intended physical controller.
- Display name, manufacturer, VID/PID, path, and connection hints are sensible.
- Selecting a controller does not silently fall back to another device.
- Buttons, sticks, and triggers update correctly in Live Input.
- Battery, gyro, and touchpad are recorded as working, unsupported, unknown, or inconclusive as appropriate.
- Report-health metrics populate: rate, average interval, p95, jitter, maximum interval, and spike count.
- A short HID capture can be recorded and saved.
- The saved capture replays offline without opening physical HID output.

Export both JSON and Markdown reports and retain them as evidence.

## Stage 3 — Controlled output

Only after Stage 2 succeeds:

- Run a short confirmed rumble test.
- Run a single confirmed lightbar test where the device is expected to support it.
- Confirm output stops when the operation ends.
- Confirm output stops safely after disconnect or error.

Record each result independently. A controller may support rumble while rejecting DS4-style lightbar output.

## Stage 4 — Bluetooth validation

Pair the same controller over Bluetooth where supported.

Repeat the identity and input checks, then verify:

- Bluetooth identity/path is recorded separately from USB.
- Live input remains stable.
- Full-report CRC validation does not reject valid traffic unexpectedly.
- Report-health metrics are captured for comparison with USB.
- Disconnect and reconnect behavior is safe.
- Rumble and lightbar behavior are tested and recorded separately from USB.

Do not assume USB and Bluetooth capabilities are identical.

## Stage 5 — Virtual controller

With ViGEmBus installed:

- Start virtual DS4 or Xbox 360 passthrough.
- Confirm the intended virtual device appears.
- Verify buttons, sticks, and triggers mirror correctly.
- Confirm stopping the virtual session removes or disconnects the virtual device cleanly.
- Check for double input in a controller test application or game.

If double input occurs, document HidHide or game-side configuration guidance rather than hiding the limitation.

## Stage 6 — Mouse emulation

On hardware with supported touchpad and/or gyro input:

- Test touchpad mouse mode.
- Test gyro mouse mode.
- Verify sensitivity changes are applied.
- Confirm starting mouse emulation coordinates correctly with other exclusive physical HID sessions.
- Confirm stopping the mode restores normal behavior.

## Stage 7 — Audio Lab

Audio support must remain conservative.

- Record whether Windows exposes a controller-related audio endpoint.
- Verify endpoint enumeration and volume/routing controls only against endpoints Windows actually exposes.
- Do not claim DS4 HID audio support merely because an endpoint exists.
- Keep experimental probe results distinct: not-run, unsupported, accepted, rejected, or error.

A clone that lacks the relevant firmware or hardware cannot be made compatible by PadScope.

## Stage 8 — Disconnect and mode switching

Exercise failure-prone transitions:

- Disconnect during Live Input.
- Disconnect during a controlled output test.
- Rescan after disconnect.
- Switch USB to Bluetooth.
- Switch Bluetooth to USB.
- Start Live Input, then Virtual Controller.
- Start Virtual Controller, then Mouse Emulation.

Expected result: only one physical HID owner is active where required, stale selections are cleared, and the app remains responsive.

## Release evidence

Before creating a public tag, collect:

- At least one successful physical USB validation report.
- At least one successful Bluetooth validation report when supported by the tested hardware.
- Results for rumble and lightbar on real hardware.
- Virtual-controller result when ViGEmBus is available.
- Dark-theme screenshot.
- Light-theme screenshot.
- Live Input/controller screenshot.
- Final green GitHub Actions build and test run.
- Successful self-contained Windows package artifact.

## Release decision

A release is ready when:

1. Real-hardware results are documented.
2. Known limitations match observed behavior.
3. No unsupported capability is presented as confirmed.
4. CI is green.
5. The self-contained Windows package is produced successfully.
6. README screenshots and release notes reflect the tested build.

Tracking issues:

- #4 — Marvo GT-84 USB/Bluetooth evidence
- #5 — first-release hardware validation gate
