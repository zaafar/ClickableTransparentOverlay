namespace ClickableTransparentOverlay
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Numerics;
    using System.Threading;
    using System.Threading.Tasks;
    using Veldrid;
    using Veldrid.ImageSharp;
    using Veldrid.Sdl2;
    using Veldrid.StartupUtilities;

    // TODO: Implement overlay info, warn, error logger.
    /// <summary>
    /// A class to create clickable transparent overlay.
    /// </summary>
    public abstract class Overlay : IDisposable
    {
        private volatile Sdl2Window window;
        private GraphicsDevice graphicsDevice;
        private CommandList commandList;
        private ImGuiController imController;
        private Vector4 clearColor;
        private Dictionary<string, Texture> loadedImages;

        private Thread renderThread;
        private volatile CancellationTokenSource cancellationTokenSource;
        private volatile bool overlayIsReady;

        /// <summary>
        /// Initializes a new instance of the <see cref="Overlay"/> class.
        /// </summary>
        public Overlay()
        {
            clearColor = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
            loadedImages = new Dictionary<string, Texture>();
        }

        /// <summary>
        /// Starts the overlay
        /// </summary>
        /// <returns>A Task that finishes once the overlay window is ready</returns>
        public async Task Start()
        {
            cancellationTokenSource = new CancellationTokenSource();
            renderThread = new Thread(async () =>
            {
                window = new Sdl2Window(
                    "Overlay",
                    0,
                    0,
                    2560,
                    1440,
                    SDL_WindowFlags.Borderless |
                    SDL_WindowFlags.AlwaysOnTop |
                    SDL_WindowFlags.SkipTaskbar,
                    false);
                graphicsDevice = VeldridStartup.CreateDefaultD3D11GraphicsDevice(
                    new GraphicsDeviceOptions(false, null, true),
                    window);
                commandList = graphicsDevice.ResourceFactory.CreateCommandList();
                imController = new ImGuiController(
                    graphicsDevice,
                    graphicsDevice.MainSwapchain.Framebuffer.OutputDescription,
                    window.Width,
                    window.Height);
                window.Resized += () =>
                {
                    graphicsDevice.MainSwapchain.Resize((uint)window.Width, (uint)window.Height);
                    imController.WindowResized(window.Width, window.Height);
                };

                NativeMethods.InitKeyTimeoutMechanism();
                NativeMethods.InitTransparency(window.Handle);
                NativeMethods.SetOverlayClickable(window.Handle, false);
                if (!overlayIsReady)
                {
                    overlayIsReady = true;
                }

                await RunInfiniteLoop(cancellationTokenSource.Token);
            });
            
            renderThread.Start();
            await WaitHelpers.SpinWait(() => overlayIsReady);
        }

        /// <summary>
        /// Starts the overlay and waits for the overlay window to be closed.
        /// </summary>
        /// <returns>A task that finishes once the overlay window closes</returns>
        public virtual async Task Run()
        {
            if (!overlayIsReady)
            {
                await Start();
            }

            await WaitHelpers.SpinWait(() => !window.Exists);
        }

        /// <summary>
        /// Infinitely calls the Render task until the overlay closes.
        /// </summary>
        private async Task RunInfiniteLoop(CancellationToken cancellationToken)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            while (window.Exists && !cancellationToken.IsCancellationRequested)
            {
                InputSnapshot snapshot = window.PumpEvents();
                if (!window.Exists) { break; }

                var deltaSeconds = (float)stopwatch.ElapsedTicks / Stopwatch.Frequency;
                stopwatch.Restart();
                imController.Update(deltaSeconds, snapshot, window.Handle);
                
                await Render();

                commandList.Begin();
                commandList.SetFramebuffer(graphicsDevice.MainSwapchain.Framebuffer);
                commandList.ClearColorTarget(0, new RgbaFloat(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W));
                imController.Render(graphicsDevice, commandList);
                commandList.End();
                graphicsDevice.SubmitCommands(commandList);
                graphicsDevice.SwapBuffers(graphicsDevice.MainSwapchain);
            }

            if (window.Exists)
                window.Close();
        }

        /// <summary>
        /// Abstract Task for creating the UI.
        /// </summary>
        /// <returns>Task that finishes once per frame</returns>
        protected abstract Task Render();

        /// <summary>
        /// Safely Closes the Overlay.
        /// </summary>
        public virtual void Close()
        {
            cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Gets or sets the position of the overlay window.
        /// </summary>
        public Point Position
        {
            get
            {
                return new Point(window.X, window.Y);
            }
            set
            {
                Sdl2Native.SDL_SetWindowPosition(window.SdlWindowHandle, value.X, value.Y);
            }
        }

        /// <summary>
        /// Gets the number of displays available on the computer.
        /// </summary>
        public static int NumberVideoDisplays
        {
            get
            {
                return Sdl2Native.SDL_GetNumVideoDisplays();
            }
        }

        /// <summary>
        /// Gets the monitor bounds based on the monitor number.
        /// </summary>
        /// <param name="num">Monitor number starting from 0.</param>
        /// <returns>screen box in which the window is moved to.</returns>
        public Rectangle GetDisplayBounds(int num)
        {
            int numDisplays = NumberVideoDisplays;
            if ( num >= numDisplays || num < 0)
            {
                return new Rectangle(Position, Size);
            }

            var bounds = new Rectangle();
            SDL2Functions.SDL_GetDisplayBounds(num, ref bounds);
            return bounds;
        }

        /// <summary>
        /// Gets or sets the size of the overlay window.
        /// </summary>
        public Point Size
        {
            get
            {
                return new Point(window.Width, window.Height);
            }
            set
            {
                Sdl2Native.SDL_SetWindowSize(window.SdlWindowHandle, value.X, value.Y);
            }
        }

        /// <summary>
        /// Adds the image to the Graphic Device as a texture.
        /// Then returns the pointer of the added texture. It also
        /// cache the image internally rather than creating a new texture on every call,
        /// so this function can be called multiple times per frame.
        /// </summary>
        /// <param name="filePath">
        /// Path to the image on disk. If the image is loaded in the memory
        /// save it on the disk before sending to this function. Reason for this
        /// is to cache the Image Texture using filePath as the key.
        /// </param>
        /// <param name="handle">output pointer to the image in the graphic device.</param>
        /// <param name="width">width of the loaded image.</param>
        /// <param name="height">height of the loaded image.</param>
        public void AddOrGetImagePointer(
            string filePath,
            out IntPtr handle,
            out uint width,
            out uint height)
        {
            if (!loadedImages.TryGetValue(filePath, out Texture texture))
            {
                ImageSharpTexture imgSharpTexture = new ImageSharpTexture(filePath);
                texture = imgSharpTexture.CreateDeviceTexture(graphicsDevice, graphicsDevice.ResourceFactory);
                loadedImages.Add(filePath, texture);
            }

            width = texture.Width;
            height = texture.Height;
            handle = imController.GetOrCreateImGuiBinding(graphicsDevice.ResourceFactory, texture);
        }

        /// <summary>
        /// Free all resources acquired by the overlay.
        /// </summary>
        public virtual void Dispose()
        {
            if (renderThread.IsAlive)
            {
                Close();
            }

            graphicsDevice.WaitForIdle();
            imController.Dispose();
            commandList.Dispose();
            graphicsDevice.WaitForIdle();
            graphicsDevice.Dispose();
            loadedImages.Clear();
        }
    }
}
