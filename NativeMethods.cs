using System;
using System.Runtime.InteropServices;
using System.Text;

namespace TouchAssistBall
{
    internal static class NativeMethods
    {
        public const int WS_EX_TOPMOST = 0x00000008;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;

        public const int GWL_EXSTYLE = -20;
        public const int GWL_STYLE = -16;

        public const int WM_MOUSEACTIVATE = 0x0021;
        public const int MA_NOACTIVATE = 3;

        // Tablet / Touch gestures disabling constants
        public const int WM_TABLET_DEFBASE = 0x02C0;
        public const int WM_TABLET_QUERYSYSTEMGESTURESTATUS = WM_TABLET_DEFBASE + 12; // 0x02CC
        public const uint TABLET_DISABLE_PRESSANDHOLD = 0x00000001;
        public const uint TABLET_DISABLE_PENTAPFEEDBACK = 0x00000008;
        public const uint TABLET_DISABLE_PENBARRELFEEDBACK = 0x00000010;
        public const uint TABLET_DISABLE_FLICKS = 0x00010000;

        public const int SWP_NOSIZE = 0x0001;
        public const int SWP_NOMOVE = 0x0002;
        public const int SWP_NOACTIVATE = 0x0010;
        public const int SWP_SHOWWINDOW = 0x0040;

        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        // Keystroke simulation constants
        public const uint INPUT_KEYBOARD = 1;
        public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        public const uint KEYEVENTF_KEYUP = 0x0002;
        public const uint KEYEVENTF_SCANCODE = 0x0008;
        public const uint KEYEVENTF_UNICODE = 0x0004;

        // Virtual Key Codes (Standard Generic)
        public const byte VK_BACK = 0x08;
        public const byte VK_TAB = 0x09;
        public const byte VK_RETURN = 0x0D;
        public const byte VK_SHIFT = 0x10;
        public const byte VK_CONTROL = 0x11;
        public const byte VK_MENU = 0x12; // Generic Alt
        public const byte VK_PAUSE = 0x13;
        public const byte VK_CAPITAL = 0x14;
        public const byte VK_ESCAPE = 0x1B;
        public const byte VK_SPACE = 0x20;
        public const byte VK_PRIOR = 0x21;
        public const byte VK_NEXT = 0x22;
        public const byte VK_END = 0x23;
        public const byte VK_HOME = 0x24;
        public const byte VK_LEFT = 0x25;
        public const byte VK_UP = 0x26;
        public const byte VK_RIGHT = 0x27;
        public const byte VK_DOWN = 0x28;
        public const byte VK_SNAPSHOT = 0x2C; // PrintScreen (Full screen screenshot)
        public const byte VK_INSERT = 0x2D;
        public const byte VK_DELETE = 0x2E;
        public const byte VK_LWIN = 0x5B;
        public const byte VK_RWIN = 0x5C;
        public const byte VK_APPS = 0x5D;

        // Specific Left / Right Modifier Keys
        public const byte VK_LSHIFT = 0xA0;
        public const byte VK_RSHIFT = 0xA1;
        public const byte VK_LCONTROL = 0xA2;
        public const byte VK_RCONTROL = 0xA3;
        public const byte VK_LMENU = 0xA4; // Left Alt
        public const byte VK_RMENU = 0xA5; // Right Alt (AltGr)

        public const byte VK_KEY_A = 0x41;
        public const byte VK_KEY_C = 0x43;
        public const byte VK_KEY_D = 0x44;
        public const byte VK_KEY_H = 0x48;
        public const byte VK_KEY_S = 0x53;
        public const byte VK_KEY_V = 0x56;
        public const byte VK_KEY_W = 0x57;
        public const byte VK_KEY_X = 0x58;
        public const byte VK_KEY_Y = 0x59;
        public const byte VK_KEY_Z = 0x5A;

        public const uint GUI_CARETBLINKING = 0x00000001;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
            public POINT(int x, int y) { X = x; Y = y; }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
            public int Width { get { return Right - Left; } }
            public int Height { get { return Bottom - Top; } }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct GUITHREADINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hwndActive;
            public IntPtr hwndFocus;
            public IntPtr hwndCapture;
            public IntPtr hwndMenuOwner;
            public IntPtr hwndMoveSize;
            public IntPtr hwndCaret;
            public RECT rcCaret;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        // CRITICAL: The native INPUT union's largest member is MOUSEINPUT. Omitting it makes
        // Marshal.SizeOf(INPUT) smaller than native sizeof(INPUT); SendInput validates cbSize
        // strictly and silently rejects the entire call (returns 0, ERROR 87) - which broke
        // EVERY synthesized key action (swipe copy/paste/screenshot, click backspace...).
        [StructLayout(LayoutKind.Explicit)]
        public struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint type;
            public MOUSEKEYBDHARDWAREINPUT u;
        }

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        public static extern ushort MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        // Window Display Affinity (Exclude from screen captures and screen recording in Windows 10/11)
        public const uint WDA_NONE = 0x00000000;
        public const uint WDA_MONITOR = 0x00000001;
        public const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
    }
}
