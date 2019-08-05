// <copyright file="HookController.cs" company="Zaafar Ahmed">
// Copyright (c) Zaafar Ahmed. All rights reserved.
// </copyright>

namespace ClickableTransparentOverlay
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using System.Windows.Forms;
    using Gma.System.MouseKeyHook;
    using ImGuiNET;

    /// <summary>
    /// This class Hooks the Global Window Keyboard/Mouse events
    /// and pass them into the ImGui Overlay.
    ///
    /// NOTE: This class might miss/skip a Keyboard/Mouse event
    /// if ImGui render function takes a lot of time. Report on GitHub
    /// if that happens.
    /// </summary>
    public class HookController
    {
        private readonly Stack<HookControllerMessage> messages;
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
            this.messages = new Stack<HookControllerMessage>();
            this.windowX = x;
            this.windowY = y;
            this.enable = true;
            this.myHook = Hook.GlobalEvents();
        }

        private enum HookControllerMessageType
        {
            MouseUpDown,
            MouseMove,
            MouseWheel,
            KeyUp,
            KeyDown,
            KeyPress,
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

        /// <summary>
        /// Consumes all (max limit 10 to avoid infinite loop) the
        /// keyboard/mouse messages from the message queue.
        /// </summary>
        public void PopMessages()
        {
            int counter = 0;
            int maxCounter = 10;
            while (counter < maxCounter && this.messages.Count > 0)
            {
                var message = this.messages.Pop();
                switch (message.Type)
                {
                    case HookControllerMessageType.MouseUpDown:
                        this.ProcessMouseUpDown((MouseEventExtArgs)message.E, message.MiscArg, true);
                        break;
                    case HookControllerMessageType.MouseMove:
                        this.ProcessMouseMove((MouseEventArgs)message.E, true);
                        break;
                    case HookControllerMessageType.MouseWheel:
                        this.ProcessMouseWheel((MouseEventExtArgs)message.E, true);
                        break;
                    case HookControllerMessageType.KeyUp:
                        this.ProcessKeyUp((KeyEventArgs)message.E, true);
                        break;
                    case HookControllerMessageType.KeyDown:
                        this.ProcessKeyDown((KeyEventArgs)message.E, true);
                        break;
                    case HookControllerMessageType.KeyPress:
                        this.ProcessKeyPress((KeyPressEventArgs)message.E, true);
                        break;
                    default:
                        break;
                }

                counter++;
            }
        }

        /// <summary>
        /// Push the keyboard/mouse message to the message queue.
        /// </summary>
        /// <param name="type">
        /// Message Type.
        /// </param>
        /// <param name="e">
        /// Message details.
        /// </param>
        /// <param name="miscArg">
        /// Only Mouse Up/Down hook uses this param to pass isDownEvent param.
        /// </param>
        private void PushMessage(HookControllerMessageType type, EventArgs e, bool miscArg = false)
        {
            var message = new HookControllerMessage()
            {
                Type = type,
                E = e,
                MiscArg = miscArg,
            };

            this.messages.Push(message);
        }

        private void ProcessMouseUpDown(MouseEventExtArgs e, bool isDownEvent, bool shouldSendToImGui)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            if (shouldSendToImGui)
            {
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
            }
            else
            {
                this.PushMessage(HookControllerMessageType.MouseUpDown, e, isDownEvent);
            }

            if (io.WantCaptureMouse)
            {
                e.Handled = true;
            }
        }

        private void ProcessMouseMove(MouseEventArgs e, bool shouldSendToImGui)
        {
            if (shouldSendToImGui)
            {
                ImGuiIOPtr io = ImGui.GetIO();
                io.MousePos = new Vector2(e.X - this.windowX, e.Y - this.windowY);

                // TODO: Show ImGUI Cursor/Hide ImGui Cursor
                //     ImGui.GetIO().MouseDrawCursor = true;
                //     Window32 API ShowCursor(false)
            }
            else
            {
                this.PushMessage(HookControllerMessageType.MouseMove, e);
            }
        }

        private void ProcessMouseWheel(MouseEventExtArgs e, bool shouldSendToImGui)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            if (io.WantCaptureMouse)
            {
                if (shouldSendToImGui)
                {
                    io.MouseWheel = e.Delta / SystemInformation.MouseWheelScrollDelta;
                }
                else
                {
                    this.PushMessage(HookControllerMessageType.MouseWheel, e);
                }

                e.Handled = true;
            }
        }

        private void ProcessKeyUp(KeyEventArgs e, bool shouldSendToImGui)
        {
            if (shouldSendToImGui)
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
            else
            {
                this.PushMessage(HookControllerMessageType.KeyUp, e);
            }
        }

        private void ProcessKeyDown(KeyEventArgs e, bool shouldSendToImGui)
        {
            var io = ImGui.GetIO();
            if (io.WantCaptureKeyboard)
            {
                if (shouldSendToImGui)
                {
                    io.KeysDown[e.KeyValue] = true;
                }
                else
                {
                    this.PushMessage(HookControllerMessageType.KeyDown, e);
                }

                switch (e.KeyCode)
                {
                    case Keys.LWin:
                    case Keys.RWin:
                        if (shouldSendToImGui)
                        {
                            io.KeySuper = true;
                        }

                        break;
                    case Keys.LControlKey:
                    case Keys.RControlKey:
                        if (shouldSendToImGui)
                        {
                            io.KeyCtrl = true;
                        }

                        e.Handled = true;
                        break;
                    case Keys.LMenu: // LAlt is LMenu
                    case Keys.RMenu: // RAlt is RMenu
                        if (shouldSendToImGui)
                        {
                            io.KeyAlt = true;
                        }

                        break;
                    case Keys.LShiftKey:
                    case Keys.RShiftKey:
                        if (shouldSendToImGui)
                        {
                            io.KeyShift = true;
                        }

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

        private void ProcessKeyPress(KeyPressEventArgs e, bool shouldSendToImGui)
        {
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
                if (shouldSendToImGui)
                {
                    io.AddInputCharacter(e.KeyChar);
                }
                else
                {
                    this.PushMessage(HookControllerMessageType.KeyPress, e);
                }

                e.Handled = true;
            }
        }

        private void HookMouseUpExt(object sender, MouseEventExtArgs e)
        {
            if (this.enable)
            {
                this.ProcessMouseUpDown(e, false, false);
            }
        }

        private void HookMouseDownExt(object sender, MouseEventExtArgs e)
        {
            if (this.enable)
            {
                this.ProcessMouseUpDown(e, true, false);
            }
        }

        private void HookMouseMove(object sender, MouseEventArgs e)
        {
            if (!this.enable)
            {
                return;
            }

            this.ProcessMouseMove(e, false);
        }

        private void HookMouseWheelExt(object sender, MouseEventExtArgs e)
        {
            if (!this.enable)
            {
                return;
            }

            this.ProcessMouseWheel(e, false);
        }

        private void HookKeyUp(object sender, KeyEventArgs e)
        {
            this.ProcessKeyUp(e, true);
        }

        private void HookKeyDown(object sender, KeyEventArgs e)
        {
            if (!this.enable)
            {
                return;
            }

            this.ProcessKeyDown(e, true);
        }

        private void HookKeyPress(object sender, KeyPressEventArgs e)
        {
            if (!this.enable)
            {
                return;
            }

            this.ProcessKeyPress(e, false);
        }

        private struct HookControllerMessage
        {
            public HookControllerMessageType Type { get; set; }

            public EventArgs E { get; set; }

            public bool MiscArg { get; set; }
        }
    }
}
