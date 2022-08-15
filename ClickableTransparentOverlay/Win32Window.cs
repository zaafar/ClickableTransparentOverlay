namespace ClickableTransparentOverlay
{
    using ClickableTransparentOverlay.Win32;
    using System;
    using System.Drawing;

    internal sealed class Win32Window : IDisposable
    {
        public IntPtr Handle;
        public Rectangle Dimensions;

        public Win32Window(string wndClass, int width, int height, int x, int y, string title, WindowStyles style, WindowExStyles exStyle)
        {
            this.Dimensions = new Rectangle(x, y, width, height);
            this.Handle = User32.CreateWindowEx((int)exStyle, wndClass, title, (int)style,
                this.Dimensions.X, this.Dimensions.Y, this.Dimensions.Width, this.Dimensions.Height,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        }

        public void PumpEvents()
        {
            if (User32.PeekMessage(out var msg, IntPtr.Zero, 0, 0, 1))
            {
                User32.TranslateMessage(ref msg);
                User32.DispatchMessage(ref msg);
            }
        }

        public void Dispose()
        {
            if (this.Handle != IntPtr.Zero && User32.DestroyWindow(this.Handle))
            {
                this.Handle = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);
        }
    }
}
