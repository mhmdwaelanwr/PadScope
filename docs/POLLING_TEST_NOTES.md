# Raw polling-rate test

The Diagnostics Lab polling test consumes the timestamps of validated native HID reports. It calculates instantaneous rate from each adjacent packet interval, plus average interval, jitter, p95 and spike counts.

This intentionally differs from the general live status card, which uses a smoothed `ReportTimingSnapshot`. A perfectly flat graph is therefore no longer produced merely because the smoothed rate stays near one value.

The result is HID report cadence, not end-to-end game input latency.
