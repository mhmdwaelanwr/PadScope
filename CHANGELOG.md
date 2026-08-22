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

### Changed

- Documentation now reflects the implemented desktop, CLI, virtual controller, mouse, profile, and audio features.
- Windows packages include the runtime and can run without a separate .NET installation.

### Fixed

- CLI no longer falls back to the first controller when a requested VID/PID does not match.
- CLI requires explicit selection when multiple controllers are detected.
- Audio routing cannot register the same route repeatedly.

### Removed

- Obsolete development prompt files, redundant root solution, and unused placeholder file.
