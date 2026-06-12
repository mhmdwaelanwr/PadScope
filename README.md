# PadScope

**Gamepad diagnostics and compatibility toolkit for Windows.**

PadScope is an open-source toolkit for inspecting, testing, and documenting gamepads on PC, with a special focus on DualShock 4-compatible controllers and low-cost DS4 clones such as Marvo, SkyTech, Zero, and generic PS4-style controllers.

The goal is not to promise that every clone controller can magically support every feature. The goal is to tell the truth clearly:

- What does Windows detect?
- Is the controller connected through USB or Bluetooth?
- Does it behave like a real DS4, a DirectInput device, or a generic HID gamepad?
- Do rumble, lightbar, gyro, touchpad, headset jack, or internal speaker features work?
- Is the problem fixable through software, or is the controller firmware/hardware missing the required protocol?

## Why this project exists

Many PC players buy affordable PS4-style controllers and discover that features work inconsistently:

- Bluetooth reconnect works randomly.
- Rumble works over USB but not Bluetooth.
- Games show Xbox button prompts instead of PlayStation prompts.
- DS4Windows detects the controller sometimes and ignores it other times.
- Lightbar behavior changes between games.
- The 3.5mm jack or internal speaker never appears as a Windows audio device.
- Steam, DS4Windows, HidHide, and the game may fight over the same input device.

PadScope turns this mess into a structured compatibility report.

## Project scope

### Phase 1 — Scanner

The first version will scan connected gamepads and produce a report containing:

- Device name
- Vendor ID / Product ID
- USB/Bluetooth connection hints
- HID path and usage information
- Windows audio-device visibility
- Known controller profile match
- Compatibility notes

### Phase 2 — Feature tests

The next version will add controlled tests:

- Button test
- Analog stick test
- Trigger test
- Rumble test
- Lightbar test
- Touchpad test
- Gyro/IMU test
- Bluetooth reconnect test
- Speaker/headset protocol probe

### Phase 3 — Audio Lab

The advanced audio experiment will investigate DS4-style audio streaming:

1. Capture default Windows output audio.
2. Downmix/resample to a DS4-compatible format.
3. Encode audio as SBC frames.
4. Send DS4-style HID audio packets.
5. Report whether the controller accepts or rejects the protocol.

This feature will be experimental and will not be enabled blindly for unknown devices.

## What PadScope is not

PadScope is **not** a kernel driver, not a DS4Windows replacement, and not a magic fix for missing hardware features.

If a clone controller does not implement the DS4 audio protocol, PadScope should say that clearly instead of pretending there is a software setting that can fix it.

## Compatibility database

PadScope will maintain JSON profiles for tested controllers. Example:

```json
{
  "name": "Marvo GT-84",
  "category": "DS4-style clone",
  "connection": ["USB", "Bluetooth"],
  "input": "works",
  "rumble": "unknown",
  "lightbar": "unknown",
  "gyro": "unknown",
  "touchpad": "unknown",
  "windowsAudioDevice": "not observed",
  "ds4AudioProtocol": "untested"
}
```

## Engineering principles

- Diagnose before attempting fixes.
- Never send risky HID packets to unknown devices without user confirmation.
- Keep protocol notes documented.
- Prefer clean-room implementation and avoid copying GPL code into MIT-licensed modules.
- Make clone-controller compatibility visible and searchable.

## Tech direction

Initial target:

- Windows 10/11
- .NET 8
- C#
- CLI first, GUI later

Possible later components:

- WPF or WinUI desktop app
- NAudio-based audio capture
- SBC encoder integration
- HidHide detection helper
- Controller compatibility report exporter

## Current status

Early research and architecture phase.

The first implementation target is a safe read-only scanner that lists connected HID/gamepad/audio devices and produces a compatibility report.
