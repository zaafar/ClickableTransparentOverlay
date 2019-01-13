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

        private void _hook_MouseUpExt(object sender, MouseEventExtArgs e)
        {
            if (!Enable)
            {
                return;
            }

            ImGuiIOPtr io = ImGui.GetIO();

            switch (e.Button)
            {
                case MouseButtons.Left:
                    io.MouseDown[0] = false;
                    break;
                case MouseButtons.None:
                    // TODO: Find out what does this None mean
                    break;
                case MouseButtons.Right:
                    io.MouseDown[1] = false;
                    break;
                case MouseButtons.Middle:
                    io.MouseDown[2] = false;
                    break;
                case MouseButtons.XButton1:
                    io.MouseDown[3] = false;
                    break;
                case MouseButtons.XButton2:
                    io.MouseDown[4] = false;
                    break;
                default:
                    // Make a Logger for the whole Overlay
                    break;
            }

            if (io.WantCaptureMouse)
            {
                e.Handled = true;
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

        private void _hook_MouseDownExt(object sender, MouseEventExtArgs e)
        {
            if (!Enable)
            {
                return;
            }

            ImGuiIOPtr io = ImGui.GetIO();
            if (io.WantCaptureMouse)
            {
                switch (e.Button)
                {
                    case MouseButtons.Left:
                        io.MouseDown[0] = true;
                        e.Handled = true;
                        break;
                    case MouseButtons.Right:
                        io.MouseDown[1] = true;
                        e.Handled = true;
                        break;
                    case MouseButtons.Middle:
                        io.MouseDown[2] = true;
                        e.Handled = true;
                        break;
                    case MouseButtons.XButton1:
                        io.MouseDown[3] = true;
                        e.Handled = true;
                        break;
                    case MouseButtons.XButton2:
                        io.MouseDown[4] = true;
                        e.Handled = true;
                        break;
                    case MouseButtons.None:
                        // TODO: Find out what does this None mean
                        break;
                    default:
                        // TODO: Make a Logger for the whole Overlay
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
            // TODO:
        }

        private void _hook_KeyUp(object sender, KeyEventArgs e)
        {
            if (!Enable)
            {
                return;
            }
            // TODO:
        }

        private void _hook_KeyDown(object sender, KeyEventArgs e)
        {
            if (!Enable)
            {
                return;
            }
            // TODO:
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
