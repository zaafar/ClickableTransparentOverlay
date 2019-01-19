namespace ClickableTransparentOverlay
{
    using System;
    using System.Numerics;
    using System.Threading;
    using System.Windows.Forms;
    using Veldrid;
    using Veldrid.Sdl2;
    using Veldrid.StartupUtilities;

    public class Overlay
    {
        private static Sdl2Window _window;
        private static GraphicsDevice _gd;
        private static CommandList _cl;
        private static ImGuiController _im_controller;
        private static HookController _hook_controller;
        private static Thread _ui_thread;
        public event EventHandler SubmitUI;

        // UI State
        private static Vector4 _clearColor;
        private static Vector2 _future_pos;
        private static Vector2 _future_size;
        private static int _fps;
        private static bool _is_visible;
        private static bool _is_closed;
        private static bool _require_resize;
        private static bool _start_resizing;
        private static object _resize_thread_lock;

        public Overlay(int x, int y, int width, int height, int fps)
        {
            _clearColor = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
            _fps = fps;
            _is_visible = true;
            _is_closed = false;
            // Stuff related to (thread safe) resizing of SDL2Window
            _require_resize = false;
            _start_resizing = false;
            _resize_thread_lock = new object();
            _future_size = Vector2.Zero;
            _future_pos = Vector2.Zero;

            _window = new Sdl2Window("Overlay", x, x, width, height, SDL_WindowFlags.Borderless | SDL_WindowFlags.AlwaysOnTop | SDL_WindowFlags.SkipTaskbar, true);
            // TODO: Create a new branch for Non-Veldrid dependent version. Ideally, we can directly use SDL2Window.
            _gd = VeldridStartup.CreateGraphicsDevice(_window, new GraphicsDeviceOptions(true, null, true), GraphicsBackend.Direct3D11);
            NativeMethods.EnableTransparent(_window.Handle, new System.Drawing.Rectangle(_window.X , _window.Y, _window.Width, _window.Height));
            _window.Resized += () =>
            {
                _gd.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
                _im_controller.WindowResized(_window.Width, _window.Height);
                lock (_resize_thread_lock)
                {
                    _require_resize = false;
                    _start_resizing = false;
                }
            };
            _window.Closed += () =>
            {
                _is_closed = true;
            };

            _cl = _gd.ResourceFactory.CreateCommandList();
            _im_controller = new ImGuiController(_gd, _gd.MainSwapchain.Framebuffer.OutputDescription, _window.Width, _window.Height, _fps);
            _ui_thread = new Thread(WhileLoop);
            _hook_controller = new HookController(_window.X, _window.Y);
        }

        public void Run()
        {
            _ui_thread.Start();
            _hook_controller.EnableHooks();
            NativeMethods.HideConsoleWindow();
            Application.Run(new ApplicationContext());
        }

        public void Dispose()
        {
            _is_visible = false;
            _window.Close();
            while (!_is_closed)
            {
                Thread.Sleep(10);
            }

            _ui_thread.Join();
            _gd.WaitForIdle();
            _im_controller.Dispose();
            _cl.Dispose();
            _gd.Dispose();
            _hook_controller.Dispose();
            NativeMethods.ShowConsoleWindow();
            _resize_thread_lock = null;
            SubmitUI = null;
            Console.WriteLine("All Overlay resources are cleared.");
            Application.Exit();
        }

        public void ResizeWindow(int x, int y, int width, int height)
        {
            _future_pos.X = x;
            _future_pos.Y = y;
            _future_size.X = width;
            _future_size.Y = height;
            // TODO: move following two lines to _window.Moved
            _hook_controller.UpdateWindowPosition(x, y);
            NativeMethods.EnableTransparent(_window.Handle, new System.Drawing.Rectangle(x, y, width, height));
            _require_resize = true;
        }

        public void ShowWindow()
        {
            _hook_controller.ResumeHooks();
            _is_visible = true;
        }

        public void HideWindow()
        {
            // TODO: Improve this function to do the following
            //    1: Hide SDL2Window
            //    2: Pause WhileLoop
            // This will ensure we don't waste CPU/GPU resources while window is hidden
            _hook_controller.PauseHooks();
            _is_visible = false;
        }

        private void WhileLoop()
        {
            while (_window.Exists)
            {
                lock (_resize_thread_lock)
                {
                    if (_require_resize)
                    {
                        if (!_start_resizing)
                        {
                            Sdl2Native.SDL_SetWindowPosition(_window.SdlWindowHandle, (int)_future_pos.X, (int)_future_pos.Y);
                            Sdl2Native.SDL_SetWindowSize(_window.SdlWindowHandle, (int)_future_size.X, (int)_future_size.Y);
                            _start_resizing = true;
                        }
                        continue;
                    }
                }

                if (!_window.Exists)
                {
                    break;
                }

                _im_controller.InitlizeFrame(1f / _fps);

                if (_is_visible)
                {
                    SubmitUI?.Invoke(this, new EventArgs());
                }

                _cl.Begin();
                _cl.SetFramebuffer(_gd.MainSwapchain.Framebuffer);
                _cl.ClearColorTarget(0, new RgbaFloat(_clearColor.X, _clearColor.Y, _clearColor.Z, _clearColor.W));
                _im_controller.Render(_gd, _cl);
                _cl.End();
                _gd.SubmitCommands(_cl);
                _gd.SwapBuffers(_gd.MainSwapchain);
            }
        }
    }
}
