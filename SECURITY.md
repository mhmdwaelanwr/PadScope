# Security Policy

## Supported versions

PadScope is pre-1.0 software. Security fixes are applied to the latest code on the default branch and the newest release.

## Reporting a vulnerability

Please use GitHub's private vulnerability reporting for this repository. Do not open a public issue for a vulnerability that could expose users or allow unintended HID, mouse, audio, or virtual-controller behavior.

Include the affected version, reproduction steps, impact, and a suggested mitigation if available. Remove personal device paths and other identifying data.

## Scope and safety

Normal scanning must remain read-only. Changes that send HID output, create virtual input, control the mouse, or capture audio must be explicit, interruptible, and limited to the selected target.
