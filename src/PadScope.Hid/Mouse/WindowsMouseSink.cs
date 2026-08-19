using System.Runtime.InteropServices;
using PadScope.Core.Input;

namespace PadScope.Hid.Mouse;

public sealed class WindowsMouseSink : IMouseSink
{
    private const uint InputMouse = 0;
    private const uint MouseEventMove = 0x0001;
    private const uint MouseEventLeftDown = 0x0002;
    private const uint MouseEventLeftUp = 0x0004;
    private const uint MouseEventRightDown = 0x0008;
    private const uint MouseEventRightUp = 0x0010;
    private const uint MouseEventMiddleDown = 0x0020;
    private const uint MouseEventMiddleUp = 0x0040;
    private const uint MouseEventWheel = 0x0800;

    public void Send(MouseAction action)
    {
        switch (action.Kind)
        {
            case MouseActionKind.Move:
                if (action.DeltaX != 0 || action.DeltaY != 0)
                {
                    SendMouse(action.DeltaX, action.DeltaY, 0, MouseEventMove);
                }
                break;

            case MouseActionKind.ButtonDown:
                SendButton(action.Button, down: true);
                break;

            case MouseActionKind.ButtonUp:
                SendButton(action.Button, down: false);
                break;

            case MouseActionKind.Wheel:
                SendMouse(0, 0, (uint)(action.WheelDelta * 120), MouseEventWheel);
                break;
        }
    }

    private static void SendButton(MouseButton button, bool down)
    {
        uint flag = button switch
        {
            MouseButton.Left => down ? MouseEventLeftDown : MouseEventLeftUp,
            MouseButton.Right => down ? MouseEventRightDown : MouseEventRightUp,
            MouseButton.Middle => down ? MouseEventMiddleDown : MouseEventMiddleUp,
            _ => MouseEventMove
        };

        SendMouse(0, 0, 0, flag);
    }

    private static void SendMouse(int dx, int dy, uint mouseData, uint flags)
    {
        var input = new Input
        {
            Type = InputMouse,
            U = new InputUnion
            {
                Mouse = new MouseInput
                {
                    Dx = dx,
                    Dy = dy,
                    MouseData = mouseData,
                    Flags = flags,
                    Time = 0,
                    ExtraInfo = UIntPtr.Zero
                }
            }
        };

        _ = SendInput(1, new[] { input }, Marshal.SizeOf<Input>());
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MouseInput Mouse;

        [FieldOffset(0)]
        public KeyboardInput Keyboard;

        [FieldOffset(0)]
        public HardwareInput Hardware;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort Vk;
        public ushort Scan;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HardwareInput
    {
        public uint Message;
        public ushort ParamL;
        public ushort ParamH;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);
}