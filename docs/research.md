# Research Notes

This document tracks the projects and concepts PadScope is inspired by. It is not a license to copy code. The implementation should be clean, documented, and respectful of upstream licenses.

## DS4 audio streaming

### DS4AudioStreamer

A proof-of-concept for Bluetooth audio streaming to the DualShock 4 controller. It demonstrates the rough pipeline:

```text
Windows default output audio
-> loopback capture
-> downmix/resample
-> SBC encode
-> DS4 HID audio packets
-> controller speaker/headset
```

Important lesson: audio streaming is fragile and should be treated as experimental.

### dual-pod-shock

Low-level experiments around playing audio through a DS4 speaker. Useful as protocol research.

### web-dualshock

Browser/WebHID approach for PS4/PS5 controllers. Useful as a reference for interactive demos and WebHID limitations.

## Input mapping and emulation

### DS4Windows

A mature tool that makes DualShock controllers more usable on Windows, often through virtual Xbox/DS4 controllers.

Important lesson: input remapping and virtual devices are a separate layer from raw hardware diagnostics.

### ViGEmBus

Kernel-level virtual gamepad bus historically used by many tools. The original project is retired, so PadScope should not depend on it for the MVP.

### XOutput

DirectInput-to-XInput conversion tool. Important for understanding why older/generic controllers behave differently in modern games.

## HID and diagnostics

### HIDAPI

Cross-platform library for USB and Bluetooth HID devices. Important for direct HID report enumeration and possible future report I/O.

### hidapitester

A small CLI tool that exercises HIDAPI functionality. Good reference for how PadScope CLI can expose diagnostics clearly.

### Nefarius.Utilities.DeviceManagement

Managed .NET wrappers around Windows device-management APIs. Useful for Windows-first device enumeration and plug/unplug detection.

### HidHide

Used by controller tools to hide physical devices from games and avoid double input. PadScope can detect HidHide state and explain conflicts.

## Gyro and advanced input

### JoyShockLibrary

Library for reading DS4, DualSense, Joy-Con, and Switch Pro controller features including gyro, accelerometer, touchpad, buttons, triggers, and sticks.

### JoyShockMapper

Advanced gyro mapping tool. Good reference for calibration concepts and user-facing diagnostics.

## PadScope research conclusion

The strongest project direction is not to become another DS4Windows clone.

PadScope should become a diagnostics and compatibility layer:

1. Detect what Windows sees.
2. Match known controller profiles.
3. Run safe feature tests.
4. Produce clear reports.
5. Keep experimental DS4 audio probing separate and opt-in.
