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

### DS4Windows — GPL family

Repositories/forks and public documentation around DS4Windows were used only to understand expected DualShock behavior, deadzone/calibration concepts, controller output behavior and user-facing terminology.

Because GPL code is copyleft and PadScope is MIT-licensed, GPL implementation code is **not copied or translated line-for-line** into PadScope. PadScope's analyzers, WPF UI, HID report handling and output test logic are independently written.

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
