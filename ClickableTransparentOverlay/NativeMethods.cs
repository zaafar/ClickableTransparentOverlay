using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;

namespace ClickableTransparentOverlay
{
    /// <summary>
    /// This class allow user to access Win32 API functions.
    /// </summary>
    public static class NativeMethods
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;

        private const int SW_HIDE = 0x00;
        private const int SW_SHOW = 0x05;

        public static bool IsClickable = true;
        private static IntPtr GWL_EXSTYLE_CLICKABLE = IntPtr.Zero;
        private static IntPtr GWL_EXSTYLE_NOT_CLICKABLE = IntPtr.Zero;

        private const int KEY_PRESSED = 0x8000;

        /// <summary>
        /// Allows the SDL2Window to become transparent.
        /// </summary>
        /// <param name="handle">
        /// Veldrid window handle in IntPtr format.
        /// </param>
        public static void InitTransparency(IntPtr handle)
        {
            GWL_EXSTYLE_CLICKABLE = GetWindowLongPtr(handle, GWL_EXSTYLE);
            GWL_EXSTYLE_NOT_CLICKABLE = new IntPtr(
                GWL_EXSTYLE_CLICKABLE.ToInt64() | WS_EX_LAYERED | WS_EX_TRANSPARENT);

            Margins margins = Margins.FromRectangle(new Rectangle(-1, -1, -1, -1));
            DwmExtendFrameIntoClientArea(handle, ref margins);
        }

        /// <summary>
        /// Enables (clickable) / Disables (not clickable) the SDL2Window keyboard/mouse inputs.
        /// NOTE: This function depends on InitTransparency being called when the SDL2Winhdow was created.
        /// </summary>
        /// <param name="handle">Veldrid window handle in IntPtr format.</param>
        /// <param name="WantClickable">Set to true if you want to make the window clickable otherwise false.</param>
        public static void SetOverlayClickable(IntPtr handle, bool WantClickable)
        {
            if (!IsClickable && WantClickable)
            {
                SetWindowLongPtr(handle, GWL_EXSTYLE, GWL_EXSTYLE_CLICKABLE);
                SetFocus(handle);
                IsClickable = true;
            }
            else if(IsClickable && !WantClickable)
            {
                SetWindowLongPtr(handle, GWL_EXSTYLE, GWL_EXSTYLE_NOT_CLICKABLE);
                IsClickable = false;
            }
        }

        /// <summary>
        /// Allows showing/hiding the console window.
        /// </summary>
        public static void SetConsoleWindow(bool visiable)
        {
            if (visiable)
            {
                var handle = GetConsoleWindow();
                ShowWindow(handle, SW_SHOW);
            }
            else
            {
                var handle = GetConsoleWindow();
                ShowWindow(handle, SW_HIDE);
            }
        }

        /// <summary>
        /// Returns the current mouse position w.r.t the window in Vector2 format.
        /// Also, returns Zero in case of any errors.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        public static Vector2 GetCursorPosition(IntPtr hWnd)
        {
            if (GetCursorPos(out var lpPoint))
            {
                ScreenToClient(hWnd, ref lpPoint);
                return lpPoint;
            }

            return Vector2.Zero;
        }

        /// <summary>
        /// Returns true if the key is pressed.
        /// For keycode information visit: https://www.pinvoke.net/default.aspx/user32.getkeystate
        /// </summary>
        /// <param name="nVirtKey">key to look for.</param>
        /// <returns>weather the key is pressed or not.</returns>
        public static bool isKeyPressed(int nVirtKey)
        {
            return Convert.ToBoolean(GetKeyState(nVirtKey) & KEY_PRESSED);
        }

        [DllImport("USER32.dll")]
        static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref Margins pMarInset);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [StructLayout(LayoutKind.Sequential)]
        private struct Margins
        {
            private int left;
            private int right;
            private int top;
            private int bottom;

            public static Margins FromRectangle(Rectangle rectangle)
            {
                var margins = new Margins
                {
                    left = rectangle.Left,
                    right = rectangle.Right,
                    top = rectangle.Top,
                    bottom = rectangle.Bottom,
                };
                return margins;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }

            public static implicit operator Point(POINT p)
            {
                return new Point(p.X, p.Y);
            }

            public static implicit operator Vector2(POINT p)
            {
                return new Vector2(p.X, p.Y);
            }
        }
    }
}
