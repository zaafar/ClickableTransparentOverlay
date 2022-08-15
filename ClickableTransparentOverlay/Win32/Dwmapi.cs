namespace ClickableTransparentOverlay.Win32
{
    using System;
    using System.Runtime.InteropServices;

    internal static class Dwmapi
    {
        public const string LibraryName = "dwmapi.dll";

        [DllImport(LibraryName)]
        internal static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref Margins pMarInset);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Margins
        {
            private readonly int left;
            private readonly int right;
            private readonly int top;
            private readonly int bottom;

            internal Margins(int l)
            {
                this.left = this.right = this.top = this.bottom = l;
            }

            internal Margins(int l, int r, int t, int b)
            {
                this.left = l;
                this.right = r;
                this.top = t;
                this.bottom = b;
            }
        }
    }
}
