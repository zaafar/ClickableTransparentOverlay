namespace ClickableTransparentOverlay
{
    using ClickableTransparentOverlay.Win32;
    using SixLabors.ImageSharp;
    using SixLabors.ImageSharp.PixelFormats;
    using SixLabors.ImageSharp.Formats;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using Vortice.DXGI;
    using Vortice.Mathematics;
    using Point = System.Drawing.Point;
    using Size = System.Drawing.Size;
    using ImGuiNET;
    using System.Collections.Concurrent;

    /// <summary>
    /// A class to create clickable transparent overlay on windows machine.
    /// </summary>
    public abstract class Overlay : IDisposable
    {
        private readonly string title;
        private readonly Format format;
        private readonly int initialWindowWidth;
        private readonly int initialWindowHeight;
        private readonly Dictionary<string, (IntPtr Handle, uint Width, uint Height)> loadedTexturesPtrs;
        private readonly ConcurrentQueue<FontHelper.FontLoadDelegate> fontUpdates;

        private WNDCLASSEX wndClass;

        /// <summary>
        ///  Do not assume this class is initialized.
        ///  Consider using this variable only in <see cref="PostInitialized"/> or <see cref="Render"/> function.
        /// </summary>
        public Win32Window window;
        private ID3D11Device device;
        private ID3D11DeviceContext deviceContext;
        private IDXGISwapChain swapChain;
        private ID3D11Texture2D backBuffer;
        private ID3D11RenderTargetView? renderView;

        private ImGuiRenderer renderer;
        private ImGuiInputHandler inputhandler;

        private bool disposedValue;
        private IntPtr selfPointer;
        private Thread renderThread;
        private volatile CancellationTokenSource cancellationTokenSource;
        private volatile bool overlayIsReady;
        private int fpslimit;
        private bool isClickable;
        private bool noActivate;
        private bool showInTaskbar = true; //that is the default state of the window

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Overlay"/> class.
        /// </summary>
        public Overlay() : this("Overlay")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Overlay"/> class.
        /// </summary>
        /// <param name="windowTitle">
        /// Title of the window created by the overlay
        /// </param>
        public Overlay(string windowTitle) : this(windowTitle, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Overlay"/> class.
        /// </summary>
        /// <param name="DPIAware">
        /// should the overlay scale with windows scale value or not.
        /// </param>
        public Overlay(bool DPIAware) : this("Overlay", DPIAware)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Overlay"/> class.
        /// </summary>
        /// <param name="windowTitle">
        /// Title of the window created by the overlay
        /// </param>
        /// <param name="DPIAware">
        /// should the overlay scale with windows scale value or not.
        /// </param>
        public Overlay(string windowTitle, bool DPIAware) : this(windowTitle, DPIAware, 800, 600)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Overlay"/> class.
        /// </summary>
        /// <param name="windowWidth">
        /// width to use when creating the clickable  transparent overlay window
        /// </param>
        /// <param name="windowHeight">
        /// height to use when creating the clickable transparent overlay window
        /// </param>
        public Overlay(int windowWidth, int windowHeight) : this("Overlay", windowWidth, windowHeight)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Overlay"/> class.
        /// </summary>
        /// <param name="windowTitle">
        /// Title of the window created by the overlay
        /// </param>
        /// <param name="windowWidth">
        /// width to use when creating the clickable  transparent overlay window
        /// </param>
        /// <param name="windowHeight">
        /// height to use when creating the clickable transparent overlay window
        /// </param>
        public Overlay(string windowTitle, int windowWidth, int windowHeight) : this(windowTitle, false, windowWidth, windowHeight)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Overlay"/> class.
        /// </summary>
        /// <param name="windowTitle">
        /// Title of the window created by the overlay
        /// </param>
        /// <param name="DPIAware">
        /// should the overlay scale with windows scale value or not.
        /// </param>
        /// <param name="windowWidth">
        /// width to use when creating the clickable  transparent overlay window
        /// </param>
        /// <param name="windowHeight">
        /// height to use when creating the clickable transparent overlay window
        /// </param>
        public Overlay(string windowTitle, bool DPIAware, int windowWidth, int windowHeight)
        {
            this.initialWindowWidth = windowWidth;
            this.initialWindowHeight = windowHeight;
            this.VSync = false;
            this.FPSLimit = 60;
            this.disposedValue = false;
            this.overlayIsReady = false;
            this.title = windowTitle;
            this.cancellationTokenSource = new();
            this.format = Format.R8G8B8A8_UNorm;
            this.loadedTexturesPtrs = new();
            this.fontUpdates = new();
            if (DPIAware)
            {
                User32.SetProcessDPIAware();
            }
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether the window should be click-through (i.e., not interactable).
        /// </summary>
        public bool IsClickable { get; set; } = true;
        
        /// <summary>
        /// Gets or sets a value indicating whether the window should appear in the taskbar.
        /// </summary>
        public bool ShowInTaskbar { get; set; } = true;
        
        /// <summary>
        /// Gets or sets a value indicating whether the window should NOT be activated when clicked
        /// </summary>
        public bool NoActivate { get; set; }

        #endregion

        #region PublicAPI

        /// <summary>
        /// Starts the overlay
        /// </summary>
        /// <returns>A Task that finishes once the overlay window is ready</returns>
        public async Task Start()
        {
            this.renderThread = new Thread(async () =>
            {
                await this.InitializeResources();
                this.ReplaceFontIfRequired();
                this.RunInfiniteLoop(this.cancellationTokenSource.Token);
            });

            this.renderThread.Start();
            await WaitHelpers.SpinWait(() => this.overlayIsReady);
        }

        /// <summary>
        /// Starts the overlay and waits for the overlay window to be closed.
        /// </summary>
        /// <returns>A task that finishes once the overlay window closes</returns>
        public virtual async Task Run()
        {
            if (!this.overlayIsReady)
            {
                await this.Start();
            }

            await WaitHelpers.SpinWait(() => this.cancellationTokenSource.IsCancellationRequested);
        }

        /// <summary>
        /// Safely Closes the Overlay.
        /// </summary>
        public virtual void Close()
        {
            this.cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Safely dispose all the resources created by the overlay
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Replaces the ImGui font with another one.
        /// </summary>
        /// <param name="pathName">pathname to the TTF font file.</param>
        /// <param name="size">font size to load.</param>
        /// <param name="language">supported language by the font.</param>
        /// <returns>true if the font replacement is valid otherwise false.</returns>
        public unsafe bool ReplaceFont(string pathName, int size, FontGlyphRangeType language)
        {
            if (!File.Exists(pathName))
            {
                return false;
            }

            this.fontUpdates.Enqueue(config =>
            {
                var io = ImGui.GetIO();
                var glyphRange = language switch
                {
                    FontGlyphRangeType.English => io.Fonts.GetGlyphRangesDefault(),
                    FontGlyphRangeType.ChineseSimplifiedCommon => io.Fonts.GetGlyphRangesChineseSimplifiedCommon(),
                    FontGlyphRangeType.ChineseFull => io.Fonts.GetGlyphRangesChineseFull(),
                    FontGlyphRangeType.Japanese => io.Fonts.GetGlyphRangesJapanese(),
                    FontGlyphRangeType.Korean => io.Fonts.GetGlyphRangesKorean(),
                    FontGlyphRangeType.Thai => io.Fonts.GetGlyphRangesThai(),
                    FontGlyphRangeType.Vietnamese => io.Fonts.GetGlyphRangesVietnamese(),
                    FontGlyphRangeType.Cyrillic => io.Fonts.GetGlyphRangesCyrillic(),
                    _ => throw new Exception($"Font Glyph Range (${language}) is not supported.")
                };

                io.Fonts.AddFontFromFileTTF(pathName, size, config, glyphRange);
                ImGuiNative.igGetIO()->FontDefault = null;
            });

            return true;
        }

        /// <summary>
        /// Replaces the ImGui font with another one.
        /// </summary>
        /// <param name="pathName">pathname to the TTF font file.</param>
        /// <param name="size">font size to load.</param>
        /// <param name="glyphRange">custom glyph range of the font to load. Read <see cref="FontGlyphRangeType"/> for more detail.</param>
        /// <returns>>true if the font replacement is valid otherwise false.</returns>
        public unsafe bool ReplaceFont(string pathName, int size, ushort[] glyphRange)
        {
            if (!File.Exists(pathName))
            {
                return false;
            }

            this.fontUpdates.Enqueue(config =>
            {
                var io = ImGui.GetIO();
                fixed (ushort* p = &glyphRange[0])
                {
                    io.Fonts.AddFontFromFileTTF(pathName, size, config, new IntPtr(p));
                    ImGuiNative.igGetIO()->FontDefault = null;
                }
            });

            return true;
        }

        /// <summary>
        /// Replaces the ImGui font with the default ImGui font.
        /// </summary>
        /// <returns>always return true</returns>
        public unsafe bool ReplaceFont()
        {
            this.fontUpdates.Enqueue(config =>
            {
                var io = ImGui.GetIO();
                io.Fonts.AddFontDefault(config);
                ImGuiNative.igGetIO()->FontDefault = null;
            });

            return true;
        }

        /// <summary>
        /// Replaces the ImGui font with another one.
        /// </summary>
        /// <param name="fontLoadDelegate">instructions for loading the font</param>
        public unsafe bool ReplaceFont(FontHelper.FontLoadDelegate fontLoadDelegate)
        {
            // have to do this because of issue: https://github.com/ocornut/imgui/issues/6858
            ImGuiNative.igGetIO()->FontDefault = null;
            this.fontUpdates.Enqueue(fontLoadDelegate);
            return true;
        }

        /// <summary>
        /// Enable or disable the vsync on the overlay.
        /// You can either use the <see cref="FPSLimit"/> or <see cref="VSync"/>.
        /// VSync will be given the preference if both are set.
        /// </summary>
        public bool VSync;

        /// <summary>
        /// Gets or sets the FPS Limits of the overlay.
        /// You can either use the <see cref="FPSLimit"/> or <see cref="VSync"/>.
        /// VSync will be given the preference if both are set.
        /// </summary>
        public int FPSLimit
        {
            get => this.fpslimit;
            set
            {
                if (value == 0)
                {
                    this.fpslimit = value;
                    _ = Winmm.MM_EndPeriod(1);
                }
                else if (value > 0)
                {
                    this.fpslimit = value;
                    _ = Winmm.MM_BeginPeriod(1);
                }
                else
                {
                    // ignore negative values.
                }
            }
        }

        /// <summary>
        /// Gets or sets the position of the overlay window.
        /// </summary>
        public Point Position
        {
            get
            {
                return this.window.Dimensions.Location;
            }

            set
            {
                if (this.window.Dimensions.Location != value)
                {
                    this.window.Dimensions.Location = value;
                    User32.MoveWindow(this.window.Handle, value.X, value.Y, this.window.Dimensions.Width, this.window.Dimensions.Height, true);
                }
            }
        }

        /// <summary>
        /// Gets or sets the size of the overlay window.
        /// </summary>
        public Size Size
        {
            get
            {
                return this.window.Dimensions.Size;
            }
            set
            {
                if (this.window.Dimensions.Size != value)
                {
                    this.window.Dimensions.Size = value;
                    User32.MoveWindow(this.window.Handle, this.window.Dimensions.X, this.window.Dimensions.Y, value.Width, value.Height, true);
                }
            }
        }

        /// <summary>
        /// Gets the number of displays available on the computer.
        /// </summary>
        public static int NumberVideoDisplays
        {
            get
            {
                return User32.GetSystemMetrics(0x50); // SM_CMONITORS
            }
        }

        /// <summary>
        /// Adds the image to the Graphic Device as a texture.
        /// Then returns the pointer of the added texture. It also
        /// cache the image internally rather than creating a new texture on every call,
        /// so this function can be called multiple times per frame.
        /// </summary>
        /// <param name="filePath">Path to the image on disk.</param>
        /// <param name="srgb"> a value indicating whether pixel format is srgb or not.</param>
        /// <param name="handle">output pointer to the image in the graphic device.</param>
        /// <param name="width">width of the loaded texture.</param>
        /// <param name="height">height of the loaded texture.</param>
        public void AddOrGetImagePointer(string filePath, bool srgb, out IntPtr handle, out uint width, out uint height)
        {
            if (this.loadedTexturesPtrs.TryGetValue(filePath, out var data))
            {
                handle = data.Handle;
                width = data.Width;
                height = data.Height;
            }
            else
            {
                var decorderOptions = new DecoderOptions();
                decorderOptions.Configuration.PreferContiguousImageBuffers = true;
                using var image = Image.Load<Rgba32>(decorderOptions, filePath);
                handle = this.renderer.CreateImageTexture(image, srgb ? Format.R8G8B8A8_UNorm_SRgb : Format.R8G8B8A8_UNorm);
                width = (uint)image.Width;
                height = (uint)image.Height;
                this.loadedTexturesPtrs.Add(filePath, new(handle, width, height));
            }
        }

        /// <summary>
        /// Adds the image to the Graphic Device as a texture.
        /// Then returns the pointer of the added texture. It also
        /// cache the image internally rather than creating a new texture on every call,
        /// so this function can be called multiple times per frame.
        /// </summary>
        /// <param name="name">user friendly name given to the image.</param>
        /// <param name="image">Image data in <see cref="Image"> format.</param>
        /// <param name="srgb"> a value indicating whether pixel format is srgb or not.</param>
        /// <param name="handle">output pointer to the image in the graphic device.</param>
        public void AddOrGetImagePointer(string name, Image<Rgba32> image, bool srgb, out IntPtr handle)
        {
            if (this.loadedTexturesPtrs.TryGetValue(name, out var data))
            {
                handle = data.Handle;
            }
            else
            {
                handle = this.renderer.CreateImageTexture(image, srgb ? Format.R8G8B8A8_UNorm_SRgb : Format.R8G8B8A8_UNorm);
                this.loadedTexturesPtrs.Add(name, new(handle, (uint)image.Width, (uint)image.Height));
            }
        }

        /// <summary>
        /// Removes the image from the Overlay.
        /// </summary>
        /// <param name="key">name or pathname which was used to add the image in the first place.</param>
        /// <returns> true if the image is removed otherwise false.</returns>
        public bool RemoveImage(string key)
        {
            if (this.loadedTexturesPtrs.Remove(key, out var data))
            {
                return this.renderer.RemoveImageTexture(data.Handle);
            }

            return false;
        }

        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposedValue)
            {
                return;
            }

            if (disposing)
            {
                if (this.FPSLimit > 0)
                {
                    Winmm.MM_EndPeriod(1);
                }

                this.renderThread?.Join();
                foreach(var key in this.loadedTexturesPtrs.Keys.ToArray())
                {
                    this.RemoveImage(key);
                }

                this.cancellationTokenSource?.Dispose();
                this.fontUpdates?.Clear();
                this.swapChain?.Release();
                this.backBuffer?.Release();
                this.renderView?.Release();
                this.renderer?.Dispose();
                this.window?.Dispose();
                this.deviceContext?.Release();
                this.device?.Release();
            }

            if (this.selfPointer != IntPtr.Zero)
            {
                if (!User32.UnregisterClass(this.title, this.selfPointer))
                {
                    throw new Exception($"Failed to Unregister {this.title} class during dispose.");
                }

                this.selfPointer = IntPtr.Zero;
            }

            this.disposedValue = true;
        }

        /// <summary>
        /// Steps to execute after the overlay has fully initialized.
        /// </summary>
        protected virtual Task PostInitialized()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Abstract Task for creating the UI.
        /// </summary>
        /// <returns>Task that finishes once per frame</returns>
        protected abstract void Render();

        private void RunInfiniteLoop(CancellationToken token)
        {
            var stopwatch = Stopwatch.StartNew();
            var currentTimeSec = 0f;
            var clearColor = new Color4(0.0f);
            var delayMs = 0f;
            var sleepTimeMs = 0;
            while (!token.IsCancellationRequested)
            {
                currentTimeSec = stopwatch.ElapsedTicks / (float)Stopwatch.Frequency;
                stopwatch.Restart();
                this.window.PumpEvents();
                Utils.SetOverlayClickable(this.window.Handle, this.inputhandler.Update(), ref isClickable);
                Utils.SetShowInTaskbar(this.window.Handle, ShowInTaskbar, ref showInTaskbar);
                Utils.SetNoActivate(this.window.Handle, NoActivate, ref noActivate);
                this.renderer.Update(currentTimeSec, () => { Render(); });
                this.deviceContext.OMSetRenderTargets(renderView);
                this.deviceContext.ClearRenderTargetView(renderView, clearColor);
                this.renderer.Render();
                if (VSync)
                {
                    this.swapChain.Present(1, PresentFlags.None); // Present with vsync
                }
                else if (this.FPSLimit > 0)
                {
                    this.swapChain.Present(0, PresentFlags.None);
                    delayMs = 1000f / this.FPSLimit;
                    currentTimeSec = stopwatch.ElapsedTicks / (float)Stopwatch.Frequency;
                    sleepTimeMs = (int)(delayMs - (currentTimeSec * 1000));
                    if (sleepTimeMs > 0)
                    {
                        Thread.Sleep(sleepTimeMs);
                    }
                }
                else
                {
                    this.swapChain.Present(0, PresentFlags.None); // Present without vsync
                }

                this.ReplaceFontIfRequired();
            }
        }

        private void ReplaceFontIfRequired()
        {
            if (this.renderer != null)
            {
                while (this.fontUpdates.TryDequeue(out var update))
                {
                    this.renderer.UpdateFontTexture(update);
                }
            }
        }

        private void OnResize(int width, int height)
        {
            if (renderView == null)//first show
            {
                using var dxgiFactory = device.QueryInterface<IDXGIDevice>().GetParent<IDXGIAdapter>().GetParent<IDXGIFactory>();
                var swapchainDesc = new SwapChainDescription()
                {
                    BufferCount = 1,
                    BufferDescription = new ModeDescription(width, height, this.format),
                    Windowed = true,
                    OutputWindow = this.window.Handle,
                    SampleDescription = new SampleDescription(1, 0),
                    SwapEffect = SwapEffect.Discard,
                    BufferUsage = Usage.RenderTargetOutput,
                };

                this.swapChain = dxgiFactory.CreateSwapChain(this.device, swapchainDesc);
                dxgiFactory.MakeWindowAssociation(this.window.Handle, WindowAssociationFlags.IgnoreAll);

                this.backBuffer = this.swapChain.GetBuffer<ID3D11Texture2D>(0);
                this.renderView = this.device.CreateRenderTargetView(backBuffer);
            }
            else
            {
                this.renderView.Dispose();
                this.backBuffer.Dispose();

                this.swapChain.ResizeBuffers(1, width, height, this.format, SwapChainFlags.None);

                backBuffer = this.swapChain.GetBuffer<ID3D11Texture2D1>(0);
                renderView = this.device.CreateRenderTargetView(backBuffer);
            }

            this.renderer.Resize(width, height);
        }

        private async Task InitializeResources()
        {
            D3D11.D3D11CreateDevice(
                null,
                DriverType.Hardware,
                DeviceCreationFlags.None,
                new[] { FeatureLevel.Level_10_0 },
                out this.device,
                out this.deviceContext);
            this.selfPointer = Kernel32.GetModuleHandle(null);
            this.wndClass = new WNDCLASSEX
            {
                Size = Unsafe.SizeOf<WNDCLASSEX>(),
                Styles = WindowClassStyles.CS_HREDRAW | WindowClassStyles.CS_VREDRAW | WindowClassStyles.CS_PARENTDC,
                WindowProc = WndProc,
                InstanceHandle = this.selfPointer,
                CursorHandle = User32.LoadCursor(IntPtr.Zero, SystemCursor.IDC_ARROW),
                BackgroundBrushHandle = IntPtr.Zero,
                IconHandle = IntPtr.Zero,
                MenuName = string.Empty,
                ClassName = this.title,
                SmallIconHandle= IntPtr.Zero,
                ClassExtraBytes = 0,
                WindowExtraBytes = 0
            };

            if (User32.RegisterClassEx(ref this.wndClass) == 0)
            {
                throw new Exception($"Failed to Register class of name {this.wndClass.ClassName}");
            }

            this.window = new Win32Window(
                wndClass.ClassName,
                this.initialWindowWidth,
                this.initialWindowHeight,
                0,
                0,
                this.title,
                WindowStyles.WS_POPUP,
                WindowExStyles.WS_EX_ACCEPTFILES | WindowExStyles.WS_EX_TOPMOST);
            this.renderer = new ImGuiRenderer(device, deviceContext, this.initialWindowWidth, this.initialWindowHeight);
            this.inputhandler = new ImGuiInputHandler(this.window.Handle);
            this.overlayIsReady = true;
            await this.PostInitialized();
            User32.ShowWindow(this.window.Handle, ShowWindowCommand.Show);
            Utils.InitTransparency(this.window.Handle);
            Utils.SetOverlayClickable(this.window.Handle, true, ref isClickable);
        }

        private bool ProcessMessage(WindowMessage msg, UIntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WindowMessage.ShowWindow:
                    this.OnResize(this.window.Dimensions.Width, this.window.Dimensions.Height);
                    break;
                case WindowMessage.Size:
                    switch ((SizeMessage)wParam)
                    {
                        case SizeMessage.SIZE_RESTORED:
                        case SizeMessage.SIZE_MAXIMIZED:
                            var lp = (int)lParam;
                            this.OnResize(Utils.Loword(lp), Utils.Hiword(lp));
                            break;
                        default:
                            break;
                    }

                    break;
                case WindowMessage.Destroy:
                    this.Close();
                    break;
                default:
                    break;
            }

            return false;
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam)
        {
            if (this.overlayIsReady)
            {
                if (NoActivate && msg == User32.WM_MOUSEACTIVATE)
                {
                    return new IntPtr(User32.MA_NOACTIVATE);
                }
                
                if (this.inputhandler.ProcessMessage((WindowMessage)msg, wParam, lParam) ||
                    this.ProcessMessage((WindowMessage)msg, wParam, lParam))
                {
                    return IntPtr.Zero;
                }
            }

            return User32.DefWindowProc(hWnd, msg, wParam, lParam);
        }
    }
}
