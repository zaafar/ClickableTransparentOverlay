﻿namespace ClickableTransparentOverlay.Win32
{
    using System;
    using System.Diagnostics;

    public static class Utils
    {
        public static int Loword(int number) => number & 0x0000FFFF;
        public static int Hiword(int number) => number >> 16;

        /// <summary>
        /// Gets a value indicating whether the overlay is clickable or not.
        /// </summary>
        internal static bool IsClickable { get; private set; } = true;

        private static WindowExStyles Clickable = 0;
        private static WindowExStyles NotClickable = 0;

        private static readonly Stopwatch sw = Stopwatch.StartNew();
        private static readonly long[] nVirtKeyTimeouts = new long[256]; // Total VirtKeys are 256.

        /// <summary>
        /// Returns true if the key is pressed.
        /// For keycode information visit: https://www.pinvoke.net/default.aspx/user32.getkeystate.
        ///
        /// This function can return True multiple times (in multiple calls) per keypress. It
        /// depends on how long the application user pressed the key for and how many times
        /// caller called this function while the key was pressed. Caller of this function is
        /// responsible to mitigate this behaviour.
        /// </summary>
        /// <param name="nVirtKey">key code to look.</param>
        /// <returns>weather the key is pressed or not.</returns>
        public static bool IsKeyPressed(VK nVirtKey)
        {
            return Convert.ToBoolean(User32.GetKeyState(nVirtKey) & 0x8000);
        }

        /// <summary>
        /// A wrapper function around <see cref="IsKeyPressed"/> to ensure a single key-press
        /// yield single true even if the function is called multiple times.
        ///
        /// This function might miss a key-press, which may degrade the user-experience,
        /// so use this function to the minimum e.g. just to enable/disable/show/hide the overlay.
        /// And, it would be nice to allow application user to configure the timeout value to
        /// their liking.
        /// </summary>
        /// <param name="nVirtKey">key to look for, for details read <see cref="IsKeyPressed"/> description.</param>
        /// <param name="timeout">timeout in milliseconds</param>
        /// <returns>true if the key is pressed and key is not in timeout.</returns>
        public static bool IsKeyPressedAndNotTimeout(VK nVirtKey, int timeout = 200)
        {
            var actual = IsKeyPressed(nVirtKey);
            var currTime = sw.ElapsedMilliseconds;
            if (actual && currTime > nVirtKeyTimeouts[(int)nVirtKey])
            {
                nVirtKeyTimeouts[(int)nVirtKey] = currTime + timeout;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Allows the window to become transparent.
        /// </summary>
        /// <param name="handle">
        /// Window native pointer.
        /// </param>
        internal static void InitTransparency(IntPtr handle)
        {
            Clickable = (WindowExStyles)User32.GetWindowLong(handle, (int)WindowLongParam.GWL_EXSTYLE);
            NotClickable = Clickable | WindowExStyles.WS_EX_LAYERED | WindowExStyles.WS_EX_TRANSPARENT;
            var margins = new Dwmapi.Margins(-1);
            _ = Dwmapi.DwmExtendFrameIntoClientArea(handle, ref margins);
            SetOverlayClickable(handle, true);
        }

        /// <summary>
        /// Enables (clickable) / Disables (not clickable) the Window keyboard/mouse inputs.
        /// NOTE: This function depends on InitTransparency being called when the Window was created.
        /// </summary>
        /// <param name="handle">Veldrid window handle in IntPtr format.</param>
        /// <param name="WantClickable">Set to true if you want to make the window clickable otherwise false.</param>
        internal static void SetOverlayClickable(IntPtr handle, bool WantClickable)
        {
            if (IsClickable ^ WantClickable)
            {
                if (WantClickable)
                {
                    User32.SetWindowLong(handle, (int)WindowLongParam.GWL_EXSTYLE, (uint)Clickable);
                    User32.SetFocus(handle);
                }
                else
                {
                    User32.SetWindowLong(handle, (int)WindowLongParam.GWL_EXSTYLE, (uint)NotClickable);
                }

                IsClickable = WantClickable;
            }
        }
    }
}
