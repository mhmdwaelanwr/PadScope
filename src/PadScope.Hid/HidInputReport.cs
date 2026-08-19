namespace PadScope.Hid;

public sealed record HidInputReport(
    byte[] Data,
    int ReportId,
    DateTimeOffset Timestamp
);
