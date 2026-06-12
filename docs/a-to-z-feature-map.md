# PadScope A-to-Z Feature Map

This document defines the full product vision. Features are grouped by delivery stage so the project can be built and tested safely.

## A. Device discovery

- List controller-like Windows devices.
- List controller-like audio endpoints.
- Show display name, manufacturer, VID, PID, connection type, source, and device path.
- Export raw scan data to JSON and Markdown.

## B. Profile matching

- Match controllers against known profiles.
- Start with conservative confidence levels.
- Keep unknown devices as unknown until evidence is collected.
- Maintain JSON profiles under `data/controllers`.

## C. Compatibility database

- Store profiles for DS4-style clones, DirectInput pads, Sony reference hardware, and future controller families.
- Track known behavior, unknown behavior, and evidence needed.
- Support community-submitted scan reports.

## D. Professional desktop app

- WPF dashboard.
- Dark professional theme.
- Scan table.
- Selected-device details panel.
- Profiles tab.
- Feature Tests tab.
- Audio Lab tab.
- Roadmap tab.

## E. Report export

- JSON export for machine-readable diagnostics.
- Markdown export for GitHub issues and documentation.
- Copy selected-device details to clipboard.

## F. Evidence workflow

- Issue templates for compatibility reports.
- Required fields: controller name, connection type, VID/PID, scan output, screenshots, feature observations.
- Evidence status language: unknown, reported-working, observed-working, untested, not-applicable.

## G. HID identity inspection

- Enumerate HID-class devices more precisely.
- Extract usage page, usage, report descriptor length, input/output report lengths where possible.
- Detect DS4-like report shapes.
- Avoid output reports in this stage.

## H. Input visualization

- Display button state.
- Display analog stick values.
- Display trigger values.
- Detect drifting or dead-zone problems.
- Export input snapshot.

## I. Rumble test

- Controlled opt-in test.
- Only enabled after the device is identified as safe or known enough.
- Short pulse only.
- User confirmation required.

## J. Lightbar test

- Controlled opt-in test.
- Only for known DS4-like devices.
- Test one color change, then restore safe default.
- Do not run automatically.

## K. Touchpad test

- Read-only input observation first.
- Detect whether touch data exists.
- Later add gesture visualization.

## L. Gyro and IMU test

- Read-only observation first.
- Detect whether gyro/accelerometer values exist.
- Show live movement graph later.
- Add calibration helper later.

## M. Bluetooth reconnect diagnostics

- Compare USB and Bluetooth identity.
- Detect duplicate device entries.
- Explain why a controller may need re-pairing.
- Provide safe troubleshooting instructions.

## N. HidHide and double-input diagnostics

- Detect common double-input patterns.
- Explain when physical and virtual controllers are both visible.
- Do not change HidHide settings automatically in the MVP.

## O. DS4Windows compatibility assistant

- Explain whether the controller is likely DS4, DS4 clone, DirectInput, XInput, or unknown.
- Recommend virtual DS4 or virtual Xbox output depending on game compatibility.
- Keep this as guidance, not a DS4Windows replacement.

## P. Windows audio endpoint check

- Detect whether Windows exposes any controller-like sound device.
- Separate normal Windows audio endpoint support from DS4-specific HID audio streaming.

## Q. Audio Lab safety gate

- Locked by default.
- Requires selected controller, profile match, explicit warning, and supported test mode.
- Never runs during normal scan.

## R. DS4 audio probe

- Experimental opt-in feature.
- Check whether a DS4-style controller accepts audio-related reports.
- Stop immediately on write failure or disconnect.
- Report accepted, rejected, unsupported, or error.

## S. Audio streaming experiment

- Capture Windows output.
- Resample to DS4-compatible stream.
- Encode to SBC.
- Send framed DS4 audio packets.
- Treat as experimental research, not guaranteed support.

## T. Packaging

- Build Windows release artifacts.
- Add versioned releases.
- Provide portable ZIP first.
- Installer can come later.

## U. CI and quality

- GitHub Actions Windows build.
- Add unit tests for report export, profile matching, and parsing.
- Add lint/build gate before releases.

## V. Documentation

- Getting started guide.
- Compatibility profile policy.
- Safety policy.
- DS4 audio notes.
- Troubleshooting guide.

## W. Community workflow

- Compatibility report issue template.
- Feature test issue template.
- Pull request checklist.
- Contribution guide.

## X. Data-driven decisions

- New feature support must be backed by scans or controlled tests.
- Unknown should remain unknown until verified.
- Avoid claiming support based on product box text alone.

## Y. Future platform support

- Windows is the first-class target.
- Linux support may be researched later through hidraw/hidapi.
- WebHID demo may be researched later for browser-based diagnostics.

## Z. Final product direction

PadScope should become the tool that answers one clear question:

> What does my controller actually support on PC, and what is safe to try next?
