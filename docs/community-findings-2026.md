# Community findings and product decisions (2026)

This review turns recurring controller-community problems into PadScope product
decisions. Community reports are treated as leads, not protocol proof. Protocol
behavior remains grounded in specifications, driver implementations, tests, and
sanitized hardware captures.

## Sources reviewed

- Linux `hid-playstation` for authoritative DS4 packet structures and transport
  behavior: <https://github.com/torvalds/linux/blob/master/drivers/hid/hid-playstation.c>
- Active DS4Windows forks for current Windows workflows and feature direction:
  <https://github.com/ds4windowsapp/DS4Windows> and
  <https://github.com/hbashton/DS4Windows>
- ds4mac for protocol-fixture and reconnect testing strategy:
  <https://github.com/khallmark/ds4mac>
- HidHide issue discussions for physical/virtual device visibility and duplicate
  input: <https://github.com/nefarius/HidHide/issues>
- DS4Windows community discussions covering duplicate input, unstable Bluetooth
  latency, 2.4 GHz interference, pairing failures, and output-data tradeoffs:
  <https://www.reddit.com/r/DS4Windows/>

## Recurring user problems

| Problem | What users experience | PadScope response |
| --- | --- | --- |
| Physical + virtual double input | Menus move twice or games see two pads | Show the exact physical target, ViGEm state, and a HidHide warning before passthrough |
| Bluetooth latency spikes | Input feels intermittent even when average latency looks acceptable | Add interval, jitter, spike count, and connection-mode evidence rather than one average |
| Broken or ambiguous pairing | Windows lists audio/HID interfaces inconsistently | Keep controller and audio discovery separate; recommend a USB baseline before Bluetooth |
| Output worsens weak Bluetooth links | Rumble/lightbar traffic increases radio load on some adapters | Keep output tests opt-in, brief, independently flagged, and easy to reset |
| Clone capability overclaims | A “Wireless Controller” name is mistaken for full DS4 compatibility | Unknown remains unknown until input/output evidence is captured per connection mode |
| Dense configuration screens | Users cannot tell what is safe or what to do next | Use a diagnostics-first workspace with empty/loading states and staged next actions |

## Implemented in this pass

- A clearer workspace header and controller-focused product identity.
- Stronger typography, cards, selected rows, hover/focus states, and input sizing.
- Real scan states: not run, scanning, no device detected, and results available.
- A visible read-only scan explanation before any controller is selected.
- Last-scan time remains truthful after an empty scan.
- Safer visual hierarchy between scanning, exporting, and controlled actions.

## Next evidence-driven improvements

1. Record report arrival timestamps and show average interval, p95, jitter, and
   spikes over a fixed observation window.
2. Detect likely physical/virtual duplicates using VID/PID, container identity,
   and known ViGEm interfaces.
3. Add a connection comparison report so USB and Bluetooth observations for the
   same controller can be viewed side-by-side.
4. Store sanitized packet fixtures supplied by contributors and run them through
   parser regression tests.
5. Add reconnect and cancellation tests around the HID reader before continuous
   hardware sessions.

## Clean-room boundary

PadScope does not copy source code or visual assets from the reviewed projects.
External projects inform problem selection, protocol facts are independently
implemented, and PadScope retains its own diagnostics-first interface.
