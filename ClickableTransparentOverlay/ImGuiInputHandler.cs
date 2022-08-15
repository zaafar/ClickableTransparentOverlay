namespace ClickableTransparentOverlay
{
    using ImGuiNET;
    using System;
    using Win32;

    internal class ImGuiInputHandler
    {
        readonly IntPtr hwnd;
        ImGuiMouseCursor lastCursor;

        public ImGuiInputHandler(IntPtr hwnd)
        {
            this.hwnd = hwnd;
        }

        public bool Update()
        {
            var io = ImGui.GetIO();
            UpdateKeyModifiers(io);
            UpdateMousePosition(io, hwnd);
            var mouseCursor = io.MouseDrawCursor ? ImGuiMouseCursor.None : ImGui.GetMouseCursor();
            if (mouseCursor != lastCursor)
            {
                lastCursor = mouseCursor;

                // only required if mouse icon changes
                // while mouse isn't moved otherwise redundent.
                // so practically it's redundent.
                UpdateMouseCursor(io, mouseCursor);
            }

            return io.WantCaptureMouse;
        }

        public bool ProcessMessage(WindowMessage msg, UIntPtr wParam, IntPtr lParam)
        {
            if (ImGui.GetCurrentContext() == IntPtr.Zero)
                return false;

            var io = ImGui.GetIO();
            switch (msg)
            {
                case WindowMessage.LButtonDown:
                case WindowMessage.LButtonDoubleClick:
                case WindowMessage.LButtonUp:
                    io.AddMouseButtonEvent(0, msg != WindowMessage.LButtonUp);
                    break;
                case WindowMessage.RButtonDown:
                case WindowMessage.RButtonDoubleClick:
                case WindowMessage.RButtonUp:
                    io.AddMouseButtonEvent(1, msg != WindowMessage.RButtonUp);
                    break;
                case WindowMessage.MButtonDown:
                case WindowMessage.MButtonDoubleClick:
                case WindowMessage.MButtonUp:
                    io.AddMouseButtonEvent(2, msg != WindowMessage.MButtonUp);
                    break;
                case WindowMessage.XButtonDown:
                case WindowMessage.XButtonDoubleClick:
                case WindowMessage.XButtonUp:
                    io.AddMouseButtonEvent(
                        GET_XBUTTON_WPARAM(wParam) == 1 ? 3 : 4,
                        msg != WindowMessage.XButtonUp);
                    break;
                case WindowMessage.MouseWheel:
                    io.AddMouseWheelEvent(0.0f, GET_WHEEL_DELTA_WPARAM(wParam) / WHEEL_DELTA);
                    break;
                case WindowMessage.MouseHWheel:
                    io.AddMouseWheelEvent(GET_WHEEL_DELTA_WPARAM(wParam) / WHEEL_DELTA, 0.0f);
                    break;
                case WindowMessage.KeyDown:
                case WindowMessage.SysKeyDown:
                case WindowMessage.KeyUp:
                case WindowMessage.SysKeyUp:
                    bool is_key_down = msg == WindowMessage.SysKeyDown || msg == WindowMessage.KeyDown;
                    if ((ulong)wParam < 256 && TryMapKey((VK)wParam, out ImGuiKey imguikey))
                    {
                        io.AddKeyEvent(imguikey, is_key_down);
                    }

                    break;
                case WindowMessage.Char:
                    io.AddInputCharacterUTF16((ushort)wParam);
                    break;
                case WindowMessage.SetCursor:
                    if (Utils.Loword((int)(long)lParam) == 1)
                    {
                        var mouseCursor = io.MouseDrawCursor ? ImGuiMouseCursor.None : ImGui.GetMouseCursor();
                        lastCursor = mouseCursor;
                        if (UpdateMouseCursor(io, mouseCursor))
                        {
                            return true;
                        }
                    }

                    break;
            }

            return false;
        }

        private static void UpdateMousePosition(ImGuiIOPtr io, IntPtr handleWindow)
        {
            if (User32.GetCursorPos(out POINT pos) && User32.ScreenToClient(handleWindow, ref pos))
            {
                io.AddMousePosEvent(pos.X, pos.Y);
            }
        }

        private static void UpdateKeyModifiers(ImGuiIOPtr io)
        {
            io.AddKeyEvent(ImGuiKey.ModCtrl, (User32.GetKeyState(VK.CONTROL) & 0x8000) != 0);
            io.AddKeyEvent(ImGuiKey.ModShift, (User32.GetKeyState(VK.SHIFT) & 0x8000) != 0);
            io.AddKeyEvent(ImGuiKey.ModAlt, (User32.GetKeyState(VK.MENU) & 0x8000) != 0);
            io.AddKeyEvent(ImGuiKey.ModSuper, (User32.GetKeyState(VK.LWIN) & 0x8000) != 0);
        }

        private static bool UpdateMouseCursor(ImGuiIOPtr io, ImGuiMouseCursor requestedcursor)
        {
            if ((io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) != 0)
                return false;

            if (requestedcursor == ImGuiMouseCursor.None)
            {
                User32.SetCursor(IntPtr.Zero);
            }
            else
            {
                var cursor = SystemCursor.IDC_ARROW;
                switch (requestedcursor)
                {
                    case ImGuiMouseCursor.Arrow: cursor = SystemCursor.IDC_ARROW; break;
                    case ImGuiMouseCursor.TextInput: cursor = SystemCursor.IDC_IBEAM; break;
                    case ImGuiMouseCursor.ResizeAll: cursor = SystemCursor.IDC_SIZEALL; break;
                    case ImGuiMouseCursor.ResizeEW: cursor = SystemCursor.IDC_SIZEWE; break;
                    case ImGuiMouseCursor.ResizeNS: cursor = SystemCursor.IDC_SIZENS; break;
                    case ImGuiMouseCursor.ResizeNESW: cursor = SystemCursor.IDC_SIZENESW; break;
                    case ImGuiMouseCursor.ResizeNWSE: cursor = SystemCursor.IDC_SIZENWSE; break;
                    case ImGuiMouseCursor.Hand: cursor = SystemCursor.IDC_HAND; break;
                    case ImGuiMouseCursor.NotAllowed: cursor = SystemCursor.IDC_NO; break;
                }

                User32.SetCursor(User32.LoadCursor(IntPtr.Zero, cursor));
            }

            return true;
        }

        private static bool TryMapKey(VK key, out ImGuiKey result)
        {
            static ImGuiKey keyToImGuiKeyShortcut(VK keyToConvert, VK startKey1, ImGuiKey startKey2)
            {
                int changeFromStart1 = (int)keyToConvert - (int)startKey1;
                return startKey2 + changeFromStart1;
            }

            if (key >= VK.F1 && key <= VK.F12)
            {
                result = keyToImGuiKeyShortcut(key, VK.F1, ImGuiKey.F1);
                return true;
            }
            else if (key >= VK.NUMPAD0 && key <= VK.NUMPAD9)
            {
                result = keyToImGuiKeyShortcut(key, VK.NUMPAD0, ImGuiKey.Keypad0);
                return true;
            }
            else if (key >= VK.KEY_A && key <= VK.KEY_Z)
            {
                result = keyToImGuiKeyShortcut(key, VK.KEY_A, ImGuiKey.A);
                return true;
            }
            else if (key >= VK.KEY_0 && key <= VK.KEY_9)
            {
                result = keyToImGuiKeyShortcut(key, VK.KEY_0, ImGuiKey._0);
                return true;
            }

            switch (key)
            {
                case VK.TAB: result = ImGuiKey.Tab; return true;
                case VK.LEFT: result = ImGuiKey.LeftArrow; return true;
                case VK.RIGHT: result = ImGuiKey.RightArrow; return true;
                case VK.UP: result = ImGuiKey.UpArrow; return true;
                case VK.DOWN: result = ImGuiKey.DownArrow; return true;
                case VK.PRIOR: result = ImGuiKey.PageUp; return true;
                case VK.NEXT: result = ImGuiKey.PageDown; return true;
                case VK.HOME: result = ImGuiKey.Home; return true;
                case VK.END: result = ImGuiKey.End; return true;
                case VK.INSERT: result = ImGuiKey.Insert; return true;
                case VK.DELETE: result = ImGuiKey.Delete; return true;
                case VK.BACK: result = ImGuiKey.Backspace; return true;
                case VK.SPACE: result = ImGuiKey.Space; return true;
                case VK.RETURN: result = ImGuiKey.Enter; return true;
                case VK.ESCAPE: result = ImGuiKey.Escape; return true;
                case VK.OEM_7: result = ImGuiKey.Apostrophe; return true;
                case VK.OEM_COMMA: result = ImGuiKey.Comma; return true;
                case VK.OEM_MINUS: result = ImGuiKey.Minus; return true;
                case VK.OEM_PERIOD: result = ImGuiKey.Period; return true;
                case VK.OEM_2: result = ImGuiKey.Slash; return true;
                case VK.OEM_1: result = ImGuiKey.Semicolon; return true;
                case VK.OEM_PLUS: result = ImGuiKey.Equal; return true;
                case VK.OEM_4: result = ImGuiKey.LeftBracket; return true;
                case VK.OEM_5: result = ImGuiKey.Backslash; return true;
                case VK.OEM_6: result = ImGuiKey.RightBracket; return true;
                case VK.OEM_3: result = ImGuiKey.GraveAccent; return true;
                //case VK.OEM_8: // couldn't find what key this is.
                case VK.CAPITAL: result = ImGuiKey.CapsLock; return true;
                case VK.SCROLL: result = ImGuiKey.ScrollLock; return true;
                case VK.NUMLOCK: result = ImGuiKey.NumLock; return true;
                case VK.SNAPSHOT: result = ImGuiKey.PrintScreen; return true;
                case VK.PAUSE: result = ImGuiKey.Pause; return true;
                // numpad0-9 is already done above.
                case VK.DECIMAL: result = ImGuiKey.KeypadDecimal; return true;
                case VK.DIVIDE: result = ImGuiKey.KeypadDivide; return true;
                case VK.MULTIPLY: result = ImGuiKey.KeypadMultiply; return true;
                case VK.SUBTRACT: result = ImGuiKey.KeypadSubtract; return true;
                case VK.ADD: result = ImGuiKey.KeypadAdd; return true;
                //case IM.VK_KEYPAD_ENTER: return ImGuiKey_KeypadEnter;
                case VK.LSHIFT: result = ImGuiKey.LeftShift; return true;
                case VK.LCONTROL: result = ImGuiKey.LeftCtrl; return true;
                case VK.LMENU: result = ImGuiKey.LeftAlt; return true;
                case VK.LWIN: result = ImGuiKey.LeftSuper; return true;
                case VK.RSHIFT: result = ImGuiKey.RightShift; return true;
                case VK.RCONTROL: result = ImGuiKey.RightCtrl; return true;
                case VK.RMENU: result = ImGuiKey.RightAlt; return true;
                case VK.RWIN: result = ImGuiKey.RightSuper; return true;
                case VK.APPS: result = ImGuiKey.Menu; return true;
                // 0-9, a-z, F1-F12 is already done above.
                default:
                    result = ImGuiKey.None;
                    return false;
            }
        }

        private static readonly float WHEEL_DELTA = 120;

        private static int GET_WHEEL_DELTA_WPARAM(UIntPtr wParam) => Utils.Hiword((int)wParam);

        private static int GET_XBUTTON_WPARAM(UIntPtr wParam) => Utils.Hiword((int)wParam);
    }
}
