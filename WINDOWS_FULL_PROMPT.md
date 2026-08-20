# PadScope - Full Session Prompt (Copy to Windows AI assistant)

```
I'm continuing development on PadScope (https://github.com/mhmdwaelanwr/PadScope.git), a .NET 8 WPF gamepad diagnostics toolkit that achieves full DS4Windows parity on Windows.

## Project structure
- src/PadScope.Core - Models, input parsing, diagnostics, profiles, macros, testing
- src/PadScope.Hid - HID communication, virtual controllers (ViGEm), WASAPI audio, mouse emulation
- src/PadScope.Cli - CLI tool with all commands
- src/PadScope.Desktop - WPF app with tabs for each feature
- src/PadScope.Tests - Unit tests

## Tech stack
- .NET 8.0, WPF, C#
- HidSharp 2.1.0 (HID), NAudio 2.2.1 (WASAPI), Nefarius.ViGEm.Client 1.21.256 (virtual pads)
- ViGEmBus driver for virtual DS4/Xbox360 controllers
- WMI for device detection (Win32_SoundDevice, Win32_USBControllerDevice)
- System.Text.Json for profile serialization

## What is done (all pushed to main)
Everything is implemented. Last commit fcb92a4 unlocked the Audio Lab.

Key features:
- Live input reading from DS4 controllers
- Rumble and lightbar output via HID
- Virtual DS4 and Xbox 360 controllers via ViGEmBus
- Button remapping with profiles (JSON save/load)
- Touchpad-to-mouse and gyro-to-mouse emulation
- Macros, combos, rapid fire, timed sequences
- Audio Lab: WASAPI capture/playback/mic-to-speaker routing
- HidHide integration for hiding physical controllers
- CLI with scan, input, rumble, lightbar, virtual, mouse, audio, profile-example, stages commands
- Desktop WPF app with tabs for each feature

## DS4 audio details
- DS4 audio works via USB Audio Class (not HID reports)
- Windows sees DS4 speaker/mic as audio endpoints
- NAudio WASAPI captures from mic and plays to speaker
- BufferedWaveProvider + MediaFoundationResampler for mic-to-speaker routing
- AudioProbe uses WMI Win32_SoundDevice to detect controller endpoints

## DS4 input report format (for reference)
- Report ID 0x01 (USB) or 0x31 (BT)
- Bytes 0-3: Left stick X/Y, Right stick X/Y (0-255, center 128)
- Byte 4: L2 trigger, Byte 5: R2 trigger (0-255)
- Byte 6: DPad + face buttons (bitfield)
- Byte 7: shoulder buttons + options/share (bitfield)
- Byte 8: PS button, touchpad click (bitfield)
- Bytes 12-19: Touchpad point 1 (id, x low/high, y low/high)
- Bytes 20-27: Touchpad point 2
- Gyro: bytes 28-39 (3x 16-bit LE accelerometer, 3x 16-bit LE gyro)
- Timestamp: bytes 40-43

## DS4 output report format (for reference)
- Report ID 0x05 (USB) or 0x15 (BT) for rumble/lightbar
- Byte 2: Right motor (weak), Byte 3: Left motor (strong)
- Byte 6-8: R, G, B lightbar color
- Report ID 0x02 (USB) or 0x34 (BT) for speaker audio

## What to do next
Test everything with real hardware on Windows. The user has a DS4/DualSense controller.

1. Clone and build
2. Connect controller via USB
3. Run CLI commands to test each feature
4. Open Desktop app and test each tab
5. Fix any issues found during testing
6. Test Bluetooth connection
7. Test with third-party DS4 clones (Marvo GT-84, SkyTech, Zero)
8. Package for release

## Commit style
Conventional commits, lowercase: fix:, feat:, chore:
```
