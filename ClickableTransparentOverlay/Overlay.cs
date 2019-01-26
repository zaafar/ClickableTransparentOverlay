﻿// <copyright file="Overlay.cs" company="Zaafar Ahmed">
// Copyright (c) Zaafar Ahmed. All rights reserved.
// </copyright>

namespace ClickableTransparentOverlay
{
    using System;
    using System.Numerics;
    using System.Threading;
    using System.Windows.Forms;
    using Veldrid;
    using Veldrid.Sdl2;
    using Veldrid.StartupUtilities;

    /// <summary>
    /// A class to create clickable transparent overlay
    /// </summary>
    public class Overlay
    {
        private static Sdl2Window window;
        private static GraphicsDevice graphicsDevice;
        private static CommandList commandList;
        private static ImGuiController imController;
        private static HookController hookController;
        private static Thread uiThread;

        // UI State
        private static Vector4 clearColor;
        private static Vector2 futurePos;
        private static Vector2 futureSize;
        private static int myFps;
        private static bool isVisible;
        private static bool isClosed;
        private static bool requireResize;

        /// <summary>
        /// Initializes a new instance of the <see cref="Overlay"/> class.
        /// </summary>
        /// <param name="x">
        /// x position of the overlay
        /// </param>
        /// <param name="y">
        /// y position of the overlay
        /// </param>
        /// <param name="width">
        /// width of the overlay
        /// </param>
        /// <param name="height">
        /// height of the Overlay
        /// </param>
        /// <param name="fps">
        /// fps of the overlay
        /// </param>
        public Overlay(int x, int y, int width, int height, int fps)
        {
            clearColor = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
            myFps = fps;
            isVisible = true;
            isClosed = false;

            // Stuff related to (thread safe) resizing of SDL2Window
            requireResize = false;
            futureSize = Vector2.Zero;
            futurePos = Vector2.Zero;

            window = new Sdl2Window("Overlay", x, y, width, height, SDL_WindowFlags.Borderless | SDL_WindowFlags.AlwaysOnTop | SDL_WindowFlags.SkipTaskbar, false);
            graphicsDevice = VeldridStartup.CreateGraphicsDevice(window, new GraphicsDeviceOptions(true, null, true), GraphicsBackend.Direct3D11);
            NativeMethods.EnableTransparent(window.Handle, new System.Drawing.Rectangle(window.X, window.Y, window.Width, window.Height));
            window.Resized += () =>
            {
                graphicsDevice.MainSwapchain.Resize((uint)window.Width, (uint)window.Height);
                imController.WindowResized(window.Width, window.Height);
                futureSize = Vector2.Zero;
                futurePos = Vector2.Zero;
                requireResize = false;
            };
            window.Closed += () =>
            {
                isClosed = true;
            };

            commandList = graphicsDevice.ResourceFactory.CreateCommandList();
            imController = new ImGuiController(graphicsDevice, graphicsDevice.MainSwapchain.Framebuffer.OutputDescription, window.Width, window.Height, myFps);
            uiThread = new Thread(this.WhileLoop);
            hookController = new HookController(window.X, window.Y);
        }

        /// <summary>
        /// To submit ImGui code for generating the UI.
        /// </summary>
        public event EventHandler SubmitUI;

        /// <summary>
        /// Starts the overlay
        /// </summary>
        public void Run()
        {
            uiThread.Start();
            hookController.EnableHooks();
            NativeMethods.HideConsoleWindow();
            Application.Run(new ApplicationContext());
        }

        /// <summary>
        /// Resizes the overlay
        /// </summary>
        /// <param name="x">
        /// x axis of the overlay
        /// </param>
        /// <param name="y">
        /// y axis of the overlay
        /// </param>
        /// <param name="width">
        /// width of the overlay
        /// </param>
        /// <param name="height">
        /// height of the overlay
        /// </param>
        public void Resize(int x, int y, int width, int height)
        {
            futurePos.X = x;
            futurePos.Y = y;
            futureSize.X = width;
            futureSize.Y = height;

            // TODO: move following two lines to _window.Moved
            hookController.UpdateWindowPosition(x, y);
            NativeMethods.EnableTransparent(window.Handle, new System.Drawing.Rectangle(x, y, width, height));
            requireResize = true;
        }

        /// <summary>
        /// Shows the overlay
        /// </summary>
        public void Show()
        {
            hookController.ResumeHooks();
            isVisible = true;
        }

        /// <summary>
        /// hides the overlay
        /// </summary>
        public void Hide()
        {
            // TODO: Improve this function to do the following
            //    1: Hide SDL2Window
            //    2: Pause WhileLoop
            // This will ensure we don't waste CPU/GPU resources while window is hidden
            hookController.PauseHooks();
            isVisible = false;
        }

        /// <summary>
        /// Free all resources acquired by the overlay
        /// </summary>
        public void Dispose()
        {
            isVisible = false;
            window.Close();
            while (!isClosed)
            {
                Thread.Sleep(10);
            }

            uiThread.Join();
            graphicsDevice.WaitForIdle();
            imController.Dispose();
            commandList.Dispose();
            graphicsDevice.Dispose();
            hookController.Dispose();
            NativeMethods.ShowConsoleWindow();
            this.SubmitUI = null;
            Console.WriteLine("All Overlay resources are cleared.");
            Application.Exit();
        }

        /// <summary>
        /// Infinite While Loop to render the ImGui.
        /// </summary>
        private void WhileLoop()
        {
            while (window.Exists)
            {
                if (requireResize)
                {
                    Sdl2Native.SDL_SetWindowPosition(window.SdlWindowHandle, (int)futurePos.X, (int)futurePos.Y);
                    Sdl2Native.SDL_SetWindowSize(window.SdlWindowHandle, (int)futureSize.X, (int)futureSize.Y);
                    window.PumpEvents();
                    continue;
                }

                if (!window.Visible)
                {
                    continue;
                }

                if (!window.Exists)
                {
                    break;
                }

                imController.InitlizeFrame(1f / myFps);

                if (isVisible)
                {
                    this.SubmitUI?.Invoke(this, new EventArgs());
                }

                commandList.Begin();
                commandList.SetFramebuffer(graphicsDevice.MainSwapchain.Framebuffer);
                commandList.ClearColorTarget(0, new RgbaFloat(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W));
                imController.Render(graphicsDevice, commandList);
                commandList.End();
                graphicsDevice.SubmitCommands(commandList);
                graphicsDevice.SwapBuffers(graphicsDevice.MainSwapchain);
            }
        }
    }
}
