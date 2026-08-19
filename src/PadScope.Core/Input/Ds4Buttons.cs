namespace PadScope.Core.Input;

[Flags]
public enum Ds4Buttons
{
    None = 0,
    Square = 1 << 0,
    Cross = 1 << 1,
    Circle = 1 << 2,
    Triangle = 1 << 3,
    L1 = 1 << 4,
    R1 = 1 << 5,
    L2 = 1 << 6,
    R2 = 1 << 7,
    Share = 1 << 8,
    Options = 1 << 9,
    L3 = 1 << 10,
    R3 = 1 << 11,
    Ps = 1 << 12,
    TouchpadClick = 1 << 13,
    DpadUp = 1 << 14,
    DpadDown = 1 << 15,
    DpadLeft = 1 << 16,
    DpadRight = 1 << 17
}
