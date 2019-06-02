// <copyright file="HookController.cs" company="Zaafar Ahmed">
// Copyright (c) Zaafar Ahmed. All rights reserved.
// </copyright>

namespace ClickableTransparentOverlay
{
    using System.Numerics;
    using System.Windows.Forms;
    using Gma.System.MouseKeyHook;
    using ImGuiNET;

    /// <summary>
    /// This class Hooks the Global Window Mouse/Keyboard events
    /// and pass them into ImGui Overlay.
    /// </summary>
    public class HookController
    {
        private IKeyboardMouseEvents myHook;
        private bool enable;
        private int windowX;
        private int windowY;

        /// <summary>
        /// Initializes a new instance of the <see cref="HookController"/> class.
        /// </summary>
        /// <param name="x">
        /// Transparent SDL2Window top left corner X axis.
        /// </param>
        /// <param name="y">
        /// Transparent SDL2Window top left corner Y axis.
        /// </param>
        public HookController(int x, int y)
        {
            this.windowX = x;
            this.windowY = y;
            this.enable = true;
            this.myHook = Hook.GlobalEvents();
        }

        /// <summary>
        /// Enable this class functionality ( only call it once ).
        /// </summary>
        public void EnableHooks()
        {
            this.myHook.KeyDown += this.HookKeyDown;
            this.myHook.KeyUp += this.HookKeyUp;
            this.myHook.KeyPress += this.HookKeyPress;

            this.myHook.MouseDownExt += this.HookMouseDownExt;
            this.myHook.MouseMove += this.HookMouseMove;
            this.myHook.MouseUpExt += this.HookMouseUpExt;

            this.myHook.MouseWheelExt += this.HookMouseWheelExt;
        }

        /// <summary>
        /// Update transparent SDL2Window top left position.
        /// </summary>
        /// <param name="x">
        /// X axis of the SDL2Window top left corner.
        /// </param>
        /// <param name="y">
        /// Y axis of the SDL2Window top left corner.
        /// </param>
        public void UpdateWindowPosition(int x, int y)
        {
            this.windowX = x;
            this.windowY = y;
        }

        /// <summary>
        /// Pause the hooks.
        /// </summary>
        public void PauseHooks()
        {
            this.enable = false;
        }

        /// <summary>
        /// Resume the hooks.
        /// </summary>
        public void ResumeHooks()
        {
            this.enable = true;
        }

        /// <summary>
        /// Dispose the resources acquired by this class.
        /// </summary>
        public void Dispose()
        {
            this.myHook.KeyDown -= this.HookKeyDown;
            this.myHook.KeyUp -= this.HookKeyUp;
            this.myHook.KeyPress -= this.HookKeyPress;

            this.myHook.MouseDownExt -= this.HookMouseDownExt;
            this.myHook.MouseMove -= this.HookMouseMove;
            this.myHook.MouseUpExt -= this.HookMouseUpExt;

            this.myHook.MouseWheelExt -= this.HookMouseWheelExt;
            this.myHook.Dispose();
        }

        private void MouseButtonFunction(MouseEventExtArgs e, bool isDownEvent)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            switch (e.Button)
            {
                case MouseButtons.Left:
                    io.MouseDown[0] = isDownEvent;
                    break;
                case MouseButtons.Right:
                    io.MouseDown[1] = isDownEvent;
                    break;
                case MouseButtons.Middle:
                    io.MouseDown[2] = isDownEvent;
                    break;
                case MouseButtons.XButton1:
                    io.MouseDown[3] = isDownEvent;
                    break;
                case MouseButtons.XButton2:
                    io.MouseDown[4] = isDownEvent;
                    break;
                case MouseButtons.None:
                    // TODO: Find out what does this None mean
                    break;
                default:
                    // TODO: Make a Logger for the whole Overlay
                    break;
            }

            if (io.WantCaptureMouse)
            {
                e.Handled = true;
            }
        }

        private void HookMouseUpExt(object sender, MouseEventExtArgs e)
        {
            if (this.enable)
            {
                this.MouseButtonFunction(e, false);
            }
        }

        private void HookMouseDownExt(object sender, MouseEventExtArgs e)
        {
            if (this.enable)
            {
                this.MouseButtonFunction(e, true);
            }
        }

        private void HookMouseMove(object sender, MouseEventArgs e)
        {
            if (!this.enable)
            {
                return;
            }

            ImGuiIOPtr io = ImGui.GetIO();
            io.MousePos = new Vector2(e.X - this.windowX, e.Y - this.windowY);

            // TODO: Show ImGUI Cursor/Hide ImGui Cursor
            //     ImGui.GetIO().MouseDrawCursor = true;
            //     Window32 API ShowCursor(false)
        }

        private void HookMouseWheelExt(object sender, MouseEventExtArgs e)
        {
            if (!this.enable)
            {
                return;
            }

            ImGuiIOPtr io = ImGui.GetIO();
            if (io.WantCaptureMouse)
            {
                io.MouseWheel = e.Delta / SystemInformation.MouseWheelScrollDelta;
                e.Handled = true;
            }
        }

        private void HookKeyUp(object sender, KeyEventArgs e)
        {
            var io = ImGui.GetIO();
            io.KeysDown[e.KeyValue] = false;

            switch (e.KeyCode)
            {
                case Keys.LWin:
                case Keys.RWin:
                    io.KeySuper = false;
                    break;
                case Keys.LControlKey:
                case Keys.RControlKey:
                    io.KeyCtrl = false;
                    break;
                case Keys.LMenu:
                case Keys.RMenu:
                    io.KeyAlt = false;
                    break;
                case Keys.LShiftKey:
                case Keys.RShiftKey:
                    io.KeyShift = false;
                    break;
                default:
                    break;
            }
        }

        private void HookKeyDown(object sender, KeyEventArgs e)
        {
            if (!this.enable)
            {
                return;
            }

            var io = ImGui.GetIO();
            if (io.WantCaptureKeyboard)
            {
                io.KeysDown[e.KeyValue] = true;

                switch (e.KeyCode)
                {
                    case Keys.LWin:
                    case Keys.RWin:
                        io.KeySuper = true;
                        break;
                    case Keys.LControlKey:
                    case Keys.RControlKey:
                        io.KeyCtrl = true;
                        e.Handled = true;
                        break;
                    case Keys.LMenu: // LAlt is LMenu
                    case Keys.RMenu: // RAlt is RMenu
                        io.KeyAlt = true;
                        break;
                    case Keys.LShiftKey:
                    case Keys.RShiftKey:
                        io.KeyShift = true;
                        break;
                    default:
                        // Ignoring ALT key so we can do ALT+TAB or ALT+F4 etc.
                        // Not sure if ImGUI needs to use ALT+XXX key for anything.
                        // Ignoring Capital/NumLock key so Windows can use it.
                        // Ignoring Win/Super key so we can do Win+D or other stuff.
                        // Create a new issue on the repo if I miss any important key.
                        if (!io.KeyAlt && e.KeyCode != Keys.Capital && e.KeyCode != Keys.NumLock && !io.KeySuper &&
                            e.KeyCode != Keys.PrintScreen && e.KeyCode != Keys.Print)
                        {
                            e.Handled = true;
                        }

                        break;
                }
            }
        }

        private void HookKeyPress(object sender, KeyPressEventArgs e)
        {
            if (!this.enable)
            {
                return;
            }

            var io = ImGui.GetIO();

            // Ignoring Win/Super key so we can do Win+D or other stuff
            // Ignoring ALT key so we can do ALT+TAB or ALT+F4 etc.
            // Not sure if ImGUI needs to use ALT+XXX or Super+XXX key for anything.
            if (io.KeySuper || io.KeyAlt)
            {
                return;
            }

            if (io.WantTextInput || io.WantCaptureKeyboard)
            {
                io.AddInputCharacter(e.KeyChar);
                e.Handled = true;
            }
        }
    }
}
