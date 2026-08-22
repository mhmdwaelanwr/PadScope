# Research notes

This document summarizes the research behind PadScope. Protocol facts are
implemented independently; source code from differently licensed projects is
not copied into PadScope.

## 2026 pre-hardware protocol review

The August 2026 review compared PadScope with three active reference points:

- The Linux `hid-playstation` driver is the primary protocol reference for DS4
  USB/Bluetooth report IDs, fixed packet sizes, common-state layout, output
  validity flags, and seeded CRC-32 behavior.
- The maintained `ds4windowsapp/DS4Windows` fork is a compatibility reference
  for real Windows deployments and driver-conflict behavior.
- `hbashton/DS4Windows` and `khallmark/ds4mac` were reviewed for recent problem
  areas and test strategy. Claims around Bluetooth audio remain experimental;
  they are not treated as proof that PadScope supports that transport.

Resulting PadScope decisions:

- USB input is accepted as report `0x01`; full Bluetooth input uses `0x11` and
  a distinct two-byte transport header.
- The 10-byte Bluetooth minimal report is treated as basic state only. PadScope
  does not invent gyro, touch, or battery values when those bytes are absent.
- USB output is always 32 bytes. Bluetooth output is always 78 bytes, uses the
  DS4 output seed `0xA2`, and stores CRC-32 little-endian in the final 4 bytes.
- Rumble and lightbar validity flags are independent so changing one does not
  unintentionally reset the other.
- Protocol layout and CRC are covered by synthetic packet fixtures before any
  physical output test.

References:

- <https://github.com/torvalds/linux/blob/master/drivers/hid/hid-playstation.c>
- <https://github.com/ds4windowsapp/DS4Windows>
- <https://github.com/hbashton/DS4Windows>
- <https://github.com/khallmark/ds4mac>

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
- Recent community forks make large Bluetooth-audio claims, but this work is
  transport- and timing-sensitive and must remain behind an experimental gate.

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
