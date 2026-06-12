# Staged Test Plan

PadScope must be tested in stages. Do not jump from scanner directly to audio streaming.

## Stage 0 — Build verification

Goal: prove the project builds.

Commands:

```powershell
cd src
dotnet restore
dotnet build PadScope.sln
```

Pass criteria:

- Solution builds on Windows.
- Desktop app starts.
- CLI starts.

## Stage 1 — No-controller scan

Goal: prove the app handles empty state.

Steps:

1. Disconnect gamepads.
2. Run desktop scan.
3. Run CLI scan.

Pass criteria:

- No crash.
- Empty state is clear.
- Export does not create misleading data.

## Stage 2 — USB scan

Goal: collect identity for a wired controller.

Steps:

1. Connect controller by USB.
2. Run Scan Controllers.
3. Export JSON.
4. Export Markdown.

Collect:

- Device name
- Manufacturer
- VID/PID
- Source
- Device path
- Profile match
- Recommended next action

Pass criteria:

- Controller appears in the table.
- VID/PID is extracted when present.
- Report export works.

## Stage 3 — Bluetooth scan

Goal: compare wireless identity with wired identity.

Steps:

1. Disconnect USB.
2. Pair/connect over Bluetooth.
3. Run Scan Controllers.
4. Export JSON and Markdown.
5. Compare with USB scan.

Pass criteria:

- Connection type is detected or marked unknown honestly.
- Device path is available.
- Duplicates are not confusing.

## Stage 4 — Profile validation

Goal: make the profile database evidence-based.

Steps:

1. Compare scan output to `data/controllers` profile.
2. Update evidence fields.
3. Mark missing evidence as unknown.

Pass criteria:

- Profile does not overclaim support.
- Notes explain what is still untested.

## Stage 5 — HID report descriptor inspection

Goal: inspect shape before feature tests.

Rules:

- Read-only only.
- No rumble.
- No lightbar.
- No audio packets.

Pass criteria:

- The app can show report descriptor or equivalent report-shape notes.
- Unknown devices remain locked for controlled tests.

## Stage 6 — Safe rumble test

Goal: one short controlled test on a known target.

Rules:

- User must select one device.
- User must confirm.
- Test must be short.
- Stop on error.

Pass criteria:

- Rumble result is saved as observed-working, failed, or unsupported.

## Stage 7 — Safe lightbar test

Goal: one controlled lightbar change on a known DS4-like target.

Rules:

- User confirmation required.
- Restore default state if possible.
- No loop.

Pass criteria:

- Lightbar result is saved as observed-working, failed, or unsupported.

## Stage 8 — Touchpad and gyro read-only tests

Goal: observe input data without output reports.

Pass criteria:

- App detects whether touchpad/gyro data appears.
- No output packets are sent.

## Stage 9 — Audio endpoint validation

Goal: separate Windows audio device support from DS4 HID audio support.

Steps:

1. Scan sound devices.
2. Match controller-like sound endpoints.
3. Explain result.

Pass criteria:

- App never says audio works just because a controller exists.

## Stage 10 — DS4 Audio Lab probe

Goal: run opt-in DS4 audio probing only when all safety gates pass.

Rules:

- Experimental warning.
- User chooses target.
- Device must be identified as DS4-like.
- Stop on disconnect or write error.

Pass criteria:

- Result is one of: not-run, unsupported, accepted, rejected, error.

## Stage 11 — Release packaging

Goal: produce a usable build.

Pass criteria:

- Portable ZIP release.
- README has installation steps.
- GitHub Actions build is green.
