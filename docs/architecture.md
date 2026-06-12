# Architecture

PadScope is designed as a diagnostics-first toolkit.

The first implementation should avoid risky device writes. Read-only scanning comes first. Active tests come later behind explicit user actions.

## High-level components

```text
PadScope
├─ PadScope.Cli
├─ PadScope.Core
├─ PadScope.Hid
├─ PadScope.Audio
├─ PadScope.Diagnostics
└─ data/controllers
```

## Component responsibilities

### PadScope.Cli

Command-line entry point.

Initial commands:

```text
padscope scan
padscope scan --json report.json
padscope profiles
```

Later commands:

```text
padscope test rumble
padscope test lightbar
padscope test audio-probe
padscope export
```

### PadScope.Core

Shared domain models and report builders.

Examples:

- ControllerDevice
- ControllerProfile
- CompatibilityReport
- FeatureStatus
- ConnectionType

### PadScope.Hid

Safe HID enumeration and later controlled report I/O.

Phase 1:

- Enumerate HID devices.
- Extract VID/PID.
- Extract manufacturer/product strings when available.
- Extract path and usage information when available.

Phase 2:

- Read input report samples.
- Detect DS4-like report shape.

Phase 3:

- Controlled output report tests for rumble/lightbar/audio.

### PadScope.Audio

Windows audio endpoint inspection first.

Phase 1:

- List Windows output/input audio devices.
- Detect controller-like audio endpoints.

Phase 3:

- Loopback capture.
- Resample.
- SBC encoding.
- DS4 audio packet preparation.

### PadScope.Diagnostics

Rules engine that turns raw device info into human-readable conclusions.

Examples:

- “Controller appears as generic HID, not DS4.”
- “No matching Windows audio endpoint detected.”
- “Device path suggests Bluetooth connection.”
- “Known clone profile: Marvo GT-84.”
- “Advanced DS4 audio protocol has not been tested.”

### data/controllers

JSON compatibility profiles.

Each profile should be factual and evidence-based.

## Safety model

PadScope should classify actions into three safety levels.

### Safe

Read-only actions:

- Device enumeration
- Audio endpoint listing
- Report descriptor reading
- Exporting reports

### Controlled

Actions that write harmless known reports to known controller families:

- Rumble test
- Lightbar test
- Output toggles

### Experimental

Actions that stream or send unusual HID reports:

- DS4 audio packet probe
- Speaker/headset mode toggle
- Raw report fuzzing

Experimental actions require explicit confirmation and should never be run automatically.

## Report philosophy

PadScope reports should be honest and actionable.

Bad output:

```text
Audio failed.
```

Good output:

```text
Windows does not expose this controller as an audio endpoint. DS4-style Bluetooth audio packet probing has not been run. If the controller firmware does not implement DS4 audio reports, software cannot force the internal speaker to work.
```
