using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace ClickableTransparentOverlay {
    public abstract partial class Overlay : IDisposable {
        /// <summary>
        /// Testing Opacity otion => not working att all :-(
        /// </summary>
        public void SetTrabsperentTwo() {
            window.Opacity = 0.3f;
        }
        Rectangle rect => window.Bounds;
        uint ntr_ex;
        const int GWL_STYLE = -16;
        const int GWL_EXSTYLE = -20;
        const uint WS_EX_LAYERED = 0x80000;
        const uint WS_EX_TRANSPARENT = 0x20;
        const uint LWA_COLORKEY = 1;
        const uint WS_POPUP = 0x80000000;
        const uint WS_VISIBLE = 0x10000000;
        const int HWND_TOPMOST = -1;
        IntPtr hWnd => window==null? IntPtr.Zero: window.Handle;
        /// <summary>
        /// MakeTransparent
        /// </summary>
        /// <param name="alpha"></param>
        public void MakeTransparent(byte alpha = 255) {

            SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT);
            SetLayeredWindowAttributes(hWnd, 0, alpha, 2);// Transparency=51=20%, LWA_ALPHA=2

            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, rect.Width, rect.Height, 32 | 64);
            var margins = Margins.FromRectangle(new Rectangle(-1, -1, -1, -1));
            DwmExtendFrameIntoClientArea(hWnd, ref margins);
            var wStyle = GetWindowLong(hWnd, GWL_STYLE);
        }
        /// <summary>
        /// SetNotTransperent
        /// </summary>
        public void SetNotTransperent() {
            MakeBaseTransperent(0x00000000);
            ntr_ex = GetWindowLong(hWnd, GWL_EXSTYLE);
            SetWindowLong(hWnd, GWL_EXSTYLE, ntr_ex | WS_EX_LAYERED);
            SetWindowLong(hWnd, GWL_EXSTYLE, ntr_ex & ~WS_EX_TRANSPARENT);
            // SetLayeredWindowAttributes(hWnd, 0x00000000, 0, LWA_COLORKEY);
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, rect.Width, rect.Height, 32 | 64);
        }
        /// <summary>
        /// MakeBaseTransperent
        /// </summary>
        /// <param name="transparentColorKey"></param>
        void MakeBaseTransperent(uint transparentColorKey) {
            var oldFlags = GetWindowLong(hWnd, GWL_EXSTYLE);
            SetWindowLong(hWnd, GWL_EXSTYLE, oldFlags | WS_EX_LAYERED);
            SetLayeredWindowAttributes(hWnd, transparentColorKey, 0, LWA_COLORKEY);
        }
        [DllImport("user32.dll")]
        static extern uint SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
        [DllImport("user32.dll")]
        static extern uint GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);
        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

        //https://docs.microsoft.com/en-us/windows/win32/winmsg/window-styles
        //https://docs.microsoft.com/en-us/windows/win32/winmsg/extended-window-styles

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref Margins pMarInset);
        [StructLayout(LayoutKind.Sequential)]
        private struct Margins {
            private int left;
            private int right;
            private int top;
            private int bottom;

            public static Margins FromRectangle(Rectangle rectangle) {
                var margins = new Margins {
                    left = rectangle.Left,
                    right = rectangle.Right,
                    top = rectangle.Top,
                    bottom = rectangle.Bottom,
                };
                return margins;
            }
        }
    }
}
