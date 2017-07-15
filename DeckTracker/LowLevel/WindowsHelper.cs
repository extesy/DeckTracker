using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

namespace DeckTracker.LowLevel
{
    internal static class WindowsHelper
    {
        public static double DpiScalingX = 1.0, DpiScalingY = 1.0;

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public readonly int left;
            public readonly int top;
            public readonly int right;
            public readonly int bottom;
        }

        private const int WsExTransparent = 0x00000020;
        private const int WsExToolWindow = 0x00000080;
        private const int GwlExstyle = -20;
        private const int GwlStyle = -16;
        private const int WsMinimize = 0x20000000;
        private const int WsMaximize = 0x1000000;
        private const int SwRestore = 9;
        private const int SwShow = 5;
        private const int Alt = 0xA4;
        private const int ExtendedKey = 0x1;
        private const int KeyUp = 0x2;

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        private static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);
        private delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern IntPtr GetClientRect(IntPtr hWnd, ref Rect rect);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetOpenClipboardWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int index);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int index, int newStyle);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        private static void SetWindowExStyle(IntPtr hwnd, int style) => SetWindowLong(hwnd, GwlExstyle, GetWindowLong(hwnd, GwlExstyle) | style);

        private static IEnumerable<IntPtr> EnumerateProcessWindowHandles(int processId)
        {
            var handles = new List<IntPtr>();
            foreach (ProcessThread thread in Process.GetProcessById(processId).Threads)
                EnumThreadWindows(thread.Id, (hWnd, lParam) => {
                    handles.Add(hWnd);
                    return true;
                }, IntPtr.Zero);
            return handles;
        }

        public static IntPtr FindUnityWindow(int processId)
        {
            foreach (var handle in EnumerateProcessWindowHandles(processId)) {
                var sb = new StringBuilder(256);
                GetClassName(handle, sb, 256);
                if (sb.ToString().Equals("UnityWndClass", StringComparison.InvariantCultureIgnoreCase))
                    return handle;
            }
            return IntPtr.Zero;
        }

        public static bool IsForegroundWindow(IntPtr hWindow)
        {
            return GetForegroundWindow() == hWindow;
        }

        public static WindowState GetWindowState(IntPtr hWindow)
        {
            int state = GetWindowLong(hWindow, GwlStyle);
            if ((state & WsMaximize) == WsMaximize)
                return WindowState.Maximized;
            if ((state & WsMinimize) == WsMinimize)
                return WindowState.Minimized;
            return WindowState.Normal;
        }

        public static Rectangle GetScreenCoordinates(IntPtr hWindow)
        {
            var rect = new Rect();
            GetClientRect(hWindow, ref rect);

            var topLeft = new Point {
                X = rect.left,
                Y = rect.top
            };
            var bottomRight = new Point {
                X = rect.right,
                Y = rect.bottom
            };

            ClientToScreen(hWindow, ref topLeft);
            ClientToScreen(hWindow, ref bottomRight);

            topLeft.X = (int)(topLeft.X / DpiScalingX);
            topLeft.Y = (int)(topLeft.Y / DpiScalingY);
            bottomRight.X = (int)(bottomRight.X / DpiScalingX);
            bottomRight.Y = (int)(bottomRight.Y / DpiScalingY);

            return new Rectangle(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
        }

        public static void EnableClickthrough(IntPtr hWindow)
        {
            SetWindowExStyle(hWindow, WsExTransparent | WsExToolWindow);
        }

        public static void EnsureWindowOrder(IntPtr hGameWindow, IntPtr hOverlayWindow)
        {
            if (GetForegroundWindow() != hOverlayWindow)
                return;
            ShowWindow(hGameWindow, GetWindowState(hGameWindow) == WindowState.Minimized ? SwRestore : SwShow);
            keybd_event(Alt, 0x45, ExtendedKey | 0, 0);
            keybd_event(Alt, 0x45, ExtendedKey | KeyUp, 0);
            SetForegroundWindow(hGameWindow);
        }

        private static string GetOpenClipboardWindowText()
        {
            var hWindow = GetOpenClipboardWindow();
            if (hWindow == IntPtr.Zero) return null;
            GetWindowThreadProcessId(hWindow, out int processId);
            var process = Process.GetProcessById(processId);
            return process.MainWindowTitle;
        }

        public static bool TryCopyToClipboard(string text, out string blockingWindowText)
        {
            try {
                Clipboard.SetText(text, TextDataFormat.Text);
                blockingWindowText = null;
                return true;
            } catch {
                blockingWindowText = GetOpenClipboardWindowText();
                return false;
            }
        }
    }
}
