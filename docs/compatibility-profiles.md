# Compatibility Profiles

PadScope compatibility profiles are evidence records, not marketing claims.

A controller profile should explain what is known, what is unknown, and what data is still needed before feature tests become safe.

## Why many profiles start as unknown

Clone controllers often share the same public name while using different internal boards, firmware, Bluetooth identities, and HID report layouts. Two controllers sold under the same brand can behave differently on PC.

For that reason, PadScope should not mark a feature as working unless it has evidence from a scan report or a controlled test.

## Current starter profiles

- Marvo GT-84
- SkyTech DS4-style clone
- Zero DS4-style clone
- Generic Wireless Controller
- AULA G1000
- Sony DualShock 4
- Sony DualSense

## Evidence checklist

Every useful profile should eventually include:

- USB VID/PID
- Bluetooth device path or identity
- Windows display name
- Manufacturer string
- HID report descriptor or report shape notes
- Windows audio endpoint behavior
- Rumble test result
- Lightbar test result
- Gyro/touchpad test result, where applicable
- DS4 audio probe result, only where applicable

## Status language

Use conservative wording:

- `unknown`: no evidence yet
- `reported-working`: user reported it works, but no scan attached
- `observed-working`: verified by a PadScope report or controlled test
- `not-expected`: feature is not expected for that controller category
- `untested`: feature exists in theory but has not been tested
- `not-applicable`: feature does not apply to the controller family

## Rule

If PadScope does not know, it should say `unknown`, not guess.
