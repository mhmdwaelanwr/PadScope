# Open-source research notes

PadScope is MIT-licensed. Controller diagnostics features in this repository are implemented independently against PadScope's own HID parser and output layer.

## References studied

### Gamepadla+ — MIT

Repository: `WyvernIXTL/gamepadla-plus`

Useful concepts studied:
- polling-rate presentation and report-cadence terminology;
- separating peak rate, average rate and synthetic latency/report interval;
- exporting test results for later comparison.

No source code was copied into PadScope.

### Gamepad Tester — Apache-2.0

Repository: `zoltcode/Gamepad_Tester`

Useful concepts studied:
- presenting polling rate, jitter and hardware-test telemetry together;
- separating weak/high-frequency and strong/low-frequency rumble motors;
- using live hardware metrics as a diagnostic workflow rather than a simple input viewer.

No source code or assets were copied into PadScope.

### Linux `hid-playstation` — GPL-2.0

The Linux kernel DualShock 4 driver was used as a protocol-behavior reference for facts such as:
- native DS4 USB and Bluetooth input/output report identifiers and packet sizes;
- Bluetooth output CRC framing;
- the DS4 Bluetooth hardware-control byte and polling-interval field;
- independent validity flags for motors and the lightbar.

PadScope does **not** copy kernel implementation code. These observable protocol facts are represented by PadScope's own report builder and tests.

### DS4Windows — GPL family

Repositories/forks and public documentation around DS4Windows were used only to understand expected DualShock behavior, deadzone/calibration concepts, controller output behavior and user-facing terminology.

A particularly useful behavioral observation is that Windows controller stacks may expose two practical HID output paths: an interrupt output write and a HID control-transfer output report. PadScope implements its own small Windows `HidD_SetOutputReport` wrapper and adaptive fallback logic rather than copying DS4Windows source.

Because GPL code is copyleft and PadScope is MIT-licensed, GPL implementation code is **not copied or translated line-for-line** into PadScope. PadScope's analyzers, WPF UI, HID report handling and output test logic are independently written.

### `ds4-tool` and other public DS4 utilities

Small public DS4 utilities were used to cross-check observable transport behavior, especially the distinction between Bluetooth control-output paths and USB interrupt-output paths. PadScope's implementation remains independent and keeps all Windows HID transport code inside its own `PadScope.Hid` layer.

### Browser gamepad testers — MIT/GPL variants

Several public gamepad tester projects expose common product ideas such as:
- stick drift magnitude;
- range/circularity maps;
- touchpad/button counters;
- vibration presets;
- raw axis/button views.

Those ideas are generic diagnostic concepts. PadScope's implementation uses native Windows HID data and original WPF presentation code.

## Clean-room rule for future contributions

When a useful GPL project is encountered:
1. study behavior, protocol documentation and observable inputs/outputs;
2. write a short specification in PadScope terms;
3. implement the behavior independently using PadScope types and architecture;
4. do not copy source blocks, assets, comments, tests, or distinctive UI markup;
5. prefer protocol specifications and permissively licensed references when they are available.
