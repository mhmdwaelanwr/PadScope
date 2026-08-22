# Changelog

All notable changes will be documented here. PadScope follows Semantic Versioning once releases are published.

## [Unreleased]

### Added

- Central project version metadata.
- Explicit device selection rules and regression tests.
- GUI confirmations for controller, mouse, virtual-device, and audio actions.
- About dialog, runtime version display, and an explicit audio routing stop action.
- Self-contained Windows packaging and tagged GitHub Release publishing.
- Contribution, security, bug-report, and dependency-update configuration.
- USB/Bluetooth DS4 packet-layout and output CRC regression tests.
- Community-research findings translated into a product and diagnostics roadmap.
- Explicit desktop states for first scan, scanning, and no-device results.

### Changed

- Documentation now reflects the implemented desktop, CLI, virtual controller, mouse, profile, and audio features.
- Windows packages include the runtime and can run without a separate .NET installation.
- Desktop visual hierarchy, cards, typography, tabs, table selection, focus states,
  status presentation, and scan guidance now form a cohesive diagnostics workspace.

### Fixed

- CLI no longer falls back to the first controller when a requested VID/PID does not match.
- CLI requires explicit selection when multiple controllers are detected.
- Audio routing cannot register the same route repeatedly.
- DS4 buttons, triggers, IMU, touch, and battery now use the protocol-defined
  common-state offsets for both USB and full Bluetooth reports.
- DS4 output uses the correct 32-byte USB and 78-byte Bluetooth layouts, with
  Bluetooth CRC-32 and independent rumble/lightbar validity flags.
- HID opening no longer falls back to an unrelated device when VID/PID is absent.
- Controller scanning no longer mixes controller audio endpoints into the
  physical-controller list and now tolerates restricted WMI classes.
- Audio Lab volume sliders now update the selected WASAPI endpoint instead of
  only updating UI events.

### Removed

- Obsolete development prompt files, redundant root solution, and unused placeholder file.
