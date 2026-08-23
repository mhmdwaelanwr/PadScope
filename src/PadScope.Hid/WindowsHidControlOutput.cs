using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace PadScope.Hid;

internal static class WindowsHidControlOutput
{
    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint FileShareRead = 0x00000001;
    private const uint FileShareWrite = 0x00000002;
    private const uint OpenExisting = 3;
    private const uint FileAttributeNormal = 0x00000080;

    public static bool TryWrite(string? devicePath, byte[] report, out string? error)
    {
        if (!OperatingSystem.IsWindows())
        {
            error = "HID control output is available only on Windows.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(devicePath))
        {
            error = "The HID interface does not expose a device path.";
            return false;
        }
        if (report.Length == 0)
        {
            error = "The HID output report is empty.";
            return false;
        }

        using SafeFileHandle handle = CreateFile(devicePath, GenericRead | GenericWrite,
            FileShareRead | FileShareWrite, IntPtr.Zero, OpenExisting, FileAttributeNormal, IntPtr.Zero);
        if (handle.IsInvalid)
        {
            error = FormatLastWin32Error("Could not open the HID interface for control output");
            return false;
        }
        if (!HidD_SetOutputReport(handle, report, report.Length))
        {
            error = FormatLastWin32Error("HidD_SetOutputReport failed");
            return false;
        }
        error = null;
        return true;
    }

    private static string FormatLastWin32Error(string prefix)
    {
        int code = Marshal.GetLastWin32Error();
        string message = code == 0 ? "unknown Windows HID error" : new Win32Exception(code).Message;
        return $"{prefix}: {message} (Win32 {code}).";
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode,
        IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [DllImport("hid.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool HidD_SetOutputReport(SafeFileHandle hidDeviceObject, byte[] reportBuffer, int reportBufferLength);
}
