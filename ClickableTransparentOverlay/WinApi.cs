namespace ClickableTransparentOverlay
{
    using System;
    using System.Runtime.InteropServices;

    public static class WinApi
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int LWA_ALPHA = 0x02;
        private const int LWA_COLORKEY = 0x01;

        private const int SW_HIDE = 0x00;
        private const int SW_SHOW = 0x05;

        public static void EnableTransparent(IntPtr handle)
        {
            int windowLong = GetWindowLong(handle, GWL_EXSTYLE) | WS_EX_LAYERED | WS_EX_TRANSPARENT;
            SetWindowLong(handle, GWL_EXSTYLE, new IntPtr(windowLong));
            SetLayeredWindowAttributes(handle, 0, 255, LWA_ALPHA | LWA_COLORKEY);
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

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
