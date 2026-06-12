namespace PadScope.Core.Testing;

public enum TestStage
{
    BuildVerification = 0,
    EmptyScan = 1,
    UsbScan = 2,
    BluetoothScan = 3,
    ProfileValidation = 4,
    HidInspection = 5,
    Rumble = 6,
    Lightbar = 7,
    TouchpadAndGyro = 8,
    AudioEndpoint = 9,
    AudioProbe = 10,
    Packaging = 11
}
