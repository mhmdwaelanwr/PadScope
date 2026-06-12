# PadScope

**Gamepad diagnostics and compatibility toolkit for Windows.**

PadScope is an open-source toolkit for inspecting, testing, and documenting gamepads on PC, with a special focus on DualShock 4-compatible controllers and low-cost DS4 clones such as Marvo, SkyTech, Zero, and generic PS4-style controllers.

The goal is not to promise that every clone controller can magically support every feature. The goal is to tell the truth clearly:

- What does Windows detect?
- Is the controller connected through USB or Bluetooth?
- Does it behave like a real DS4, a DirectInput device, or a generic HID gamepad?
- Do rumble, lightbar, gyro, touchpad, headset jack, or internal speaker features work?
- Is the problem fixable through software, or is the controller firmware/hardware missing the required protocol?

## Current GitHub stage

PadScope is currently at **Stage 0 / Stage 1 readiness**:

- Stage 0: build verification is ready.
- Stage 1: no-controller scan is ready to test.
- Stage 2: USB scan is the first real controller test.
- Stage 3: Bluetooth scan comes after USB.

Do **not** start rumble, lightbar, gyro, touchpad, or audio experiments yet. Those features are planned and registered, but locked until scanner and identity evidence are validated.

## Quick build without Visual Studio

This is the lowest-data setup for testing:

Requirements:

- Windows 10/11
- .NET 8 SDK
- Git for Windows

Commands:

```powershell
cd Desktop
git clone https://github.com/mhmdwaelanwr/PadScope.git
cd PadScope\src
dotnet restore
dotnet build PadScope.sln
dotnet run --project PadScope.Desktop
```

If the repository already exists:

```powershell
cd Desktop\PadScope
git pull
cd src
dotnet restore
dotnet build PadScope.sln
dotnet run --project PadScope.Desktop
```

## First test order

1. Build the solution.
2. Run the desktop app.
3. Scan with no controller connected.
4. Connect Marvo GT-84 by USB.
5. Scan again.
6. Export JSON and Markdown.
7. Disconnect USB, connect Bluetooth, and scan again.
8. Compare USB and Bluetooth reports.

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

The first version scans connected gamepads and produces a report containing:

- Device name
- Vendor ID / Product ID
- USB/Bluetooth connection hints
- Windows audio-device visibility
- Known controller profile match
- Compatibility notes
- JSON export
- Markdown export

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

PadScope maintains JSON profiles for tested and research-needed controllers under `data/controllers`.

Current starter profiles include:

- Marvo GT-84
- SkyTech DS4-style clone
- Zero DS4-style clone
- Generic Wireless Controller
- AULA G1000
- Sony DualShock 4
- Sony DualSense

Profiles are evidence records. Unknown features should remain `unknown` until a scan or controlled test proves otherwise.

## Engineering principles

- Diagnose before attempting fixes.
- Never send risky HID packets to unknown devices without user confirmation.
- Keep protocol notes documented.
- Prefer clean-room implementation and avoid copying GPL code into MIT-licensed modules.
- Make clone-controller compatibility visible and searchable.

## Tech stack

- Windows 10/11
- .NET 8
- C#
- WPF desktop app
- CLI app
- GitHub Actions Windows build

## Key docs

- `docs/getting-started.md`
- `docs/a-to-z-feature-map.md`
- `docs/staged-test-plan.md`
- `docs/safety-policy.md`
- `docs/compatibility-profiles.md`
- `docs/ds4-audio-protocol.md`
