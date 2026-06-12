# Research notes

This document summarizes the first research pass behind PadScope.

## Problem space

PC controller support is fragmented across multiple layers:

- Raw HID devices
- DirectInput
- XInput
- Virtual controller drivers
- Steam Input
- DS4Windows
- HidHide/HidGuardian-style exclusive access
- Windows audio endpoints
- Bluetooth pairing behavior

DS4-style clone controllers often implement only part of the real DualShock 4 behavior. A controller may expose usable buttons and sticks but fail to implement rumble, lightbar, gyro, touchpad, headset, or speaker protocols.

## Relevant existing projects

### DS4Windows

DS4Windows focuses on making DS4 and related controllers usable in games by emulating Xbox 360 or DS4 controllers. It is not designed primarily as a clone-controller diagnostics database.

Important lessons:

- Virtual controller emulation improves game compatibility.
- Output data settings affect rumble and lightbar.
- Steam Input and DS4Windows can conflict.
- Supported hardware policies often focus on first-party controllers.

### DS4AudioStreamer

DS4AudioStreamer demonstrates Bluetooth audio streaming to a DualShock 4 controller.

Important lessons:

- DS4 speaker/headset audio is not a normal Windows audio endpoint in the Bluetooth path.
- Audio must be captured, resampled, encoded as SBC, packetized, checksummed, and sent as HID reports.
- The known implementation is proof-of-concept quality and not robust against disconnects or audio device changes.

### dual-pod-shock / ds4audio-gui

These projects demonstrate sending SBC audio files to the DS4 speaker.

Important lessons:

- File playback is easier than stable real-time system audio streaming.
- System-audio streaming is timing-sensitive.
- A GUI alone does not solve protocol reliability.

### hidapi / hidapitester

hidapi provides cross-platform HID access. hidapitester is a simple CLI tool for listing devices and sending/reading reports.

Important lessons:

- PadScope should start with safe read-only enumeration.
- HID output/feature report testing should be explicit and guarded.
- A CLI diagnostic mode is valuable before a GUI.

### ViGEmBus / XOutput

ViGEmBus enabled kernel-level virtual Xbox 360 and DS4 controllers. XOutput converts DirectInput to XInput.

Important lessons:

- Virtual controllers solve game compatibility, not hardware capability detection.
- The original physical controller may need to be hidden to avoid double input.
- PadScope should detect, not duplicate, these layers at first.

### HidHide

HidHide helps hide the physical controller from games while allowing specific apps to access it.

Important lessons:

- Double-input problems are common.
- PadScope can detect HidHide configuration issues later.
- Driver-level changes should not be part of the first MVP.

### JoyShockLibrary / JoyShockMapper

These projects focus on modern controller features like gyro, touchpad, calibration, and mapping.

Important lessons:

- Gyro and touchpad need specific feature detection and calibration.
- Controller diagnostics should expose poll rate, connection quality, and IMU availability.

## PadScope gap

Existing tools help users play games, remap input, emulate controllers, or test HID reports. PadScope fills a different gap:

> A structured compatibility and diagnostics toolkit for real-world PC gamepads, especially DS4-style clones.

PadScope should answer:

- What exactly is this controller exposing to Windows?
- Which advanced features are actually supported?
- Which layer is causing the issue: hardware, firmware, Windows, Bluetooth, DS4Windows, Steam, HidHide, or the game?
- Can the issue be fixed safely?

## Initial safe approach

1. Enumerate devices.
2. Identify likely gamepads.
3. Collect VID/PID/product/manufacturer/path/usage data.
4. Detect connection hints.
5. Detect Windows audio endpoints that look related.
6. Match against known profiles.
7. Export JSON reports.
8. Add active feature tests later.
