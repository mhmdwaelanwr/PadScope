# DS4 output protocol notes

PadScope's native DS4 output implementation is written independently from protocol behavior documented by multiple open-source projects and public reverse-engineering references.

For standard DS4 USB output, PadScope uses report `0x05` (32 bytes) with the standard feature/header bytes `0x07, 0x04`. For Bluetooth output it uses report `0x11` (78 bytes), header byte `0xC0`, standard feature bytes `0x07, 0x04`, and the DS4 output CRC.

The implementation does not copy GPL source code or assets. External projects are used only to verify observable protocol structure and behavior.
