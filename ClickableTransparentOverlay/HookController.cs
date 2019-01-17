namespace ClickableTransparentOverlay
{
    using Gma.System.MouseKeyHook;
    using ImGuiNET;
    using System.Numerics;
    using System.Windows.Forms;

    public class HookController
    {
        private IKeyboardMouseEvents _hook;
        private bool Enable;
        private int WindowX;
        private int WindowY;

        public HookController(int x, int y)
        {
            WindowX = x;
            WindowY = y;
            Enable = true;
            _hook = Hook.GlobalEvents();
        }

        public void EnableHooks()
        {
            _hook.KeyDown += _hook_KeyDown;
            _hook.KeyUp += _hook_KeyUp;
            _hook.KeyPress += _hook_KeyPress;

            _hook.MouseDownExt += _hook_MouseDownExt;
            _hook.MouseMove += _hook_MouseMove;
            _hook.MouseUpExt += _hook_MouseUpExt;

            _hook.MouseWheelExt += _hook_MouseWheelExt;
        }

        public void UpdateWindowPosition(int x, int y)
        {
            WindowX = x;
            WindowY = y;
        }

        public void PauseHooks()
        {
            Enable = false;
        }

        public void ResumeHooks()
        {
            Enable = true;
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

        private void _hook_MouseUpExt(object sender, MouseEventExtArgs e)
        {
            if (Enable)
            {
                MouseButtonFunction(e, false);
            }
        }

        private void _hook_MouseDownExt(object sender, MouseEventExtArgs e)
        {
            if (Enable)
            {
                MouseButtonFunction(e, true);
            }
        }

        private void _hook_MouseMove(object sender, MouseEventArgs e)
        {
            if (!Enable)
            {
                return;
            }

            ImGuiIOPtr io = ImGui.GetIO();
            io.MousePos = new Vector2(e.X - WindowX, e.Y - WindowY);
            // TODO: Show ImGUI Cursor/Hide ImGui Cursor 
            //     ImGui.GetIO().MouseDrawCursor = true;
            //     Window32 API ShowCursor(false)
        }

        private void _hook_MouseWheelExt(object sender, MouseEventExtArgs e)
        {
            if (!Enable)
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


        private void _hook_KeyUp(object sender, KeyEventArgs e)
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

        private void _hook_KeyDown(object sender, KeyEventArgs e)
        {
            if (!Enable)
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
                    case Keys.LMenu:
                    case Keys.RMenu:
                        io.KeyAlt = true;
                        break;
                    // Alt is LMenu/RMenu
                    case Keys.LShiftKey:
                    case Keys.RShiftKey:
                        io.KeyShift = true;
                        break;
                    default:
                        // Ignoring ALT key so we can do ALT+TAB or ALT+F4 etc.
                        // Not sure if ImGUI needs to use ALT+XXX key for anything.
                        // Ignoring Capital/NumLock key so Windows can use it
                        // Ignoring Win/Super key so we can do Win+D or other stuff
                        if (!io.KeyAlt && e.KeyCode != Keys.Capital && e.KeyCode != Keys.NumLock && !io.KeySuper)
                        {
                            e.Handled = true;
                        }
                        break;
                }
            }
        }

        private void _hook_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!Enable)
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

        public void Dispose()
        {
            _hook.KeyDown -= _hook_KeyDown;
            _hook.KeyUp -= _hook_KeyUp;
            _hook.KeyPress -= _hook_KeyPress;

            _hook.MouseDownExt -= _hook_MouseDownExt;
            _hook.MouseMove -= _hook_MouseMove;
            _hook.MouseUpExt -= _hook_MouseUpExt;

            _hook.MouseWheelExt -= _hook_MouseWheelExt;
            _hook.Dispose();
        }
    }
}
