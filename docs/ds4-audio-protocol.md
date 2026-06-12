# DS4 Audio Protocol Notes

> Status: research notes, not production documentation.

DualShock 4 audio is not the same thing as a normal Windows audio output device.

On Windows, a controller can expose audio in more than one way:

1. As a normal USB audio endpoint.
2. As controller-specific HID audio packets.
3. Not at all.

Many DS4-style clone controllers implement only the gamepad input part and do not implement the full DS4 audio behavior.

## Known concept

DS4-style Bluetooth audio experiments generally follow this model:

```text
PCM audio
-> stereo downmix
-> 32 kHz resample
-> SBC encode
-> HID audio packet framing
-> CRC/checksum
-> write output report
```

## Why this matters for clones

A clone controller may work for:

- Buttons
- Analog sticks
- Basic rumble
- Lightbar

But fail for:

- Internal speaker
- Headset jack
- Microphone
- Gyro
- Touchpad
- DS4-specific output reports

PadScope should test each capability independently.

## Safety rules

Audio probing must not run automatically.

Before sending DS4-style audio packets, PadScope must:

- Identify the device.
- Show the target VID/PID.
- Explain that the test is experimental.
- Ask for explicit confirmation.
- Stop immediately if the device disconnects or rejects writes.

## Diagnostic outcomes

Possible results:

```text
windows-audio-endpoint-detected
windows-audio-endpoint-not-detected
ds4-audio-probe-not-run
ds4-audio-probe-accepted
ds4-audio-probe-rejected
ds4-audio-probe-unsupported-device
ds4-audio-probe-error
```

## User-facing wording

Bad:

```text
Speaker not working.
```

Good:

```text
Windows does not expose this controller as an audio endpoint. DS4-style audio packet probing was not run. If this clone controller does not implement the DS4 audio protocol, the internal speaker cannot be enabled by software.
```
