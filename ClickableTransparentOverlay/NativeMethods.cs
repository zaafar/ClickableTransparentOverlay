namespace ClickableTransparentOverlay
{
    using System;
    using System.Runtime.InteropServices;

    public static class NativeMethods
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;

        private const int SW_HIDE = 0x00;
        private const int SW_SHOW = 0x05;

        public static void EnableTransparent(IntPtr handle, System.Drawing.Rectangle size)
        {
            IntPtr windowLong = GetWindowLongPtr(handle, GWL_EXSTYLE);
            windowLong = new IntPtr(windowLong.ToInt64() | WS_EX_LAYERED | WS_EX_TRANSPARENT);
            SetWindowLongPtr(handle, GWL_EXSTYLE, windowLong);
            Margins margins = Margins.FromRectangle(size);
            DwmExtendFrameIntoClientArea(handle, ref margins);
        }

        public static void HideConsoleWindow()
        {
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);
        }

        public static void ShowConsoleWindow()
        {
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_SHOW);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Margins
        {
            private int left, right, top, bottom;

            public static Margins FromRectangle(System.Drawing.Rectangle rectangle)
            {
                var margins = new Margins
                {
                    left = rectangle.Left,
                    right = rectangle.Right,
                    top = rectangle.Top,
                    bottom = rectangle.Bottom
                };
                return margins;
            }
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref Margins pMarInset);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}