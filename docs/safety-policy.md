# Safety Policy

PadScope is a diagnostics-first tool. It must prefer truthful reporting over risky attempts to force unsupported features.

## Safety levels

### Safe

Read-only actions:

- Device enumeration
- Audio endpoint enumeration
- Profile matching
- JSON/Markdown export
- Copying report details

Safe actions may run during normal scans.

### Controlled

Short, known, user-triggered actions:

- Rumble pulse
- Lightbar color change
- Output report for a known controller family

Controlled actions require:

- Selected target device
- Clear warning
- User confirmation
- Known test definition
- Timeout or stop condition

### Experimental

Research actions that may fail or disconnect the controller:

- DS4 audio probe
- Speaker/headset packet experiments
- Continuous output loops
- Audio streaming experiments

Experimental actions require:

- All controlled-action requirements
- Extra warning
- A known DS4-like profile or explicit developer override
- Immediate stop on failure
- No automatic startup

## Rules

1. Normal scan must never send HID output reports.
2. Unknown devices default to safe/read-only mode.
3. Profile names do not prove compatibility.
4. User-reported support is not the same as observed support.
5. Audio Lab stays locked until identity and report behavior are known.
6. If evidence is missing, PadScope should say `unknown`.

## User-facing wording

Bad:

```text
Audio supported.
```

Good:

```text
Windows lists a controller-like sound endpoint. DS4 HID audio behavior has not been tested.
```

Bad:

```text
This clone is full DS4.
```

Good:

```text
This device appears DS4-like, but full protocol compatibility is not confirmed.
```
