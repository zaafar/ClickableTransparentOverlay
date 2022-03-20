namespace ClickableTransparentOverlay
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using ImGuiNET;
    using Veldrid;

    /// <summary>
    /// A modified version of Veldrid.ImGui's ImGuiRenderer.
    /// Manages input for ImGui and handles rendering ImGui's DrawLists with Veldrid.
    /// </summary>
    internal sealed class ImGuiController : IDisposable
    {
        private readonly GraphicsDevice _gd;
        private bool _frameBegun;

        // Veldrid objects
        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _indexBuffer;
        private DeviceBuffer _projMatrixBuffer;
        private Texture _fontTexture;
        private Shader _vertexShader;
        private Shader _fragmentShader;
        private ResourceLayout _layout;
        private ResourceLayout _textureLayout;
        private Pipeline _pipeline;
        private ResourceSet _mainResourceSet;
        private ResourceSet _fontTextureResourceSet;

        private readonly IntPtr _fontAtlasID = (IntPtr)1;

        private int _windowWidth;
        private int _windowHeight;
        private Vector2 _scaleFactor = Vector2.One;

        // Image trackers
        private readonly Dictionary<TextureView, ResourceSetInfo> _setsByView = new();
        private readonly Dictionary<Texture, TextureView> _autoViewsByTexture = new();
        private readonly Dictionary<IntPtr, ResourceSetInfo> _viewsById = new();
        private readonly List<IDisposable> _ownedResources = new();
        private int _lastAssignedID = 100;

        /// <summary>
        /// Constructs a new ImGuiController.
        /// </summary>
        public ImGuiController(GraphicsDevice gd, int width, int height)
        {
            _gd = gd;
            _windowWidth = width;
            _windowHeight = height;

            ImGui.CreateContext();
            ImGui.GetIO().BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
            SetPerFrameImGuiData(1f / 60f);
        }

        /// <summary>
        /// Starts the ImGui Controller by creating required resources and first new frame.
        /// </summary>
        public void Start()
        {
            CreateDeviceResources();
            ImGui.NewFrame();
            _frameBegun = true;
        }

        /// <summary>
        /// Updates the controller with new window size information.
        /// </summary>
        /// <param name="width">width of the screen.</param>
        /// <param name="height">height of the screen.</param>
        public void WindowResized(int width, int height)
        {
            _windowWidth = width;
            _windowHeight = height;
        }

        public void CreateDeviceResources()
        {
            ResourceFactory factory = _gd.ResourceFactory;
            _vertexBuffer = factory.CreateBuffer(new BufferDescription(10000, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
            _vertexBuffer.Name = "ImGui.NET Vertex Buffer";
            _indexBuffer = factory.CreateBuffer(new BufferDescription(2000, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
            _indexBuffer.Name = "ImGui.NET Index Buffer";

            _projMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            _projMatrixBuffer.Name = "ImGui.NET Projection Buffer";

            byte[] vertexShaderBytes = LoadEmbeddedShaderCode(_gd.ResourceFactory, "imgui-vertex", ShaderStages.Vertex);
            byte[] fragmentShaderBytes = LoadEmbeddedShaderCode(_gd.ResourceFactory, "imgui-frag", ShaderStages.Fragment);
            _vertexShader = factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, vertexShaderBytes, "main"));
            _fragmentShader = factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, fragmentShaderBytes, "main"));

            VertexLayoutDescription[] vertexLayouts = new VertexLayoutDescription[]
            {
                new VertexLayoutDescription(
                    new VertexElementDescription("in_position", VertexElementSemantic.Position, VertexElementFormat.Float2),
                    new VertexElementDescription("in_texCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                    new VertexElementDescription("in_color", VertexElementSemantic.Color, VertexElementFormat.Byte4_Norm))
            };

            _layout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("ProjectionMatrixBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("MainSampler", ResourceKind.Sampler, ShaderStages.Fragment)));
            _textureLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("MainTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment)));

            GraphicsPipelineDescription pd = new(
                BlendStateDescription.SingleAlphaBlend,
                new DepthStencilStateDescription(false, false, ComparisonKind.Always),
                new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, false, true),
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(vertexLayouts, new[] { _vertexShader, _fragmentShader }),
                new ResourceLayout[] { _layout, _textureLayout },
                _gd.MainSwapchain.Framebuffer.OutputDescription,
                ResourceBindingModel.Default);
            _pipeline = factory.CreateGraphicsPipeline(ref pd);

            _mainResourceSet = factory.CreateResourceSet(new ResourceSetDescription(_layout,
                _projMatrixBuffer,
                _gd.PointSampler));

            RecreateFontDeviceTexture(_gd);
        }

        /// <summary>
        /// Gets or creates a handle for a texture to be drawn with ImGui.
        /// E.G. Pass the returned handle to Image() or ImageButton().
        /// </summary>
        public IntPtr GetOrCreateImGuiBinding(ResourceFactory factory, TextureView textureView)
        {
            if (!_setsByView.TryGetValue(textureView, out ResourceSetInfo rsi))
            {
                ResourceSet resourceSet = factory.CreateResourceSet(new ResourceSetDescription(_textureLayout, textureView));
                rsi = new ResourceSetInfo(GetNextImGuiBindingID(), resourceSet);

                _setsByView.Add(textureView, rsi);
                _viewsById.Add(rsi.ImGuiBinding, rsi);
                _ownedResources.Add(resourceSet);
            }

            return rsi.ImGuiBinding;
        }

        /// <summary>
        /// Removes the texture from the resources.
        /// </summary>
        /// <param name="textureView">texture to remove</param>
        public void RemoveImGuiBinding(TextureView textureView)
        {
            if (_setsByView.TryGetValue(textureView, out ResourceSetInfo rsi))
            {
                _setsByView.Remove(textureView);
                _viewsById.Remove(rsi.ImGuiBinding);
                _ownedResources.Remove(rsi.ResourceSet);
                rsi.ResourceSet.Dispose();
            }
        }

        /// <summary>
        /// Removes the texture from the resources.
        /// </summary>
        /// <param name="texture">texture to remove</param>
        public void RemoveImGuiBinding(Texture texture)
        {
            if (_autoViewsByTexture.TryGetValue(texture, out TextureView textureView))
            {
                _autoViewsByTexture.Remove(texture);
                _ownedResources.Remove(textureView);
                RemoveImGuiBinding(textureView);
                textureView.Dispose();
            }
        }

        private IntPtr GetNextImGuiBindingID()
        {
            int newID = _lastAssignedID++;
            return (IntPtr)newID;
        }

        /// <summary>
        /// Gets or creates a handle for a texture to be drawn with ImGui.
        /// Pass the returned handle to Image() or ImageButton().
        /// </summary>
        public IntPtr GetOrCreateImGuiBinding(ResourceFactory factory, Texture texture)
        {
            if (!_autoViewsByTexture.TryGetValue(texture, out TextureView textureView))
            {
                textureView = factory.CreateTextureView(texture);
                _autoViewsByTexture.Add(texture, textureView);
                _ownedResources.Add(textureView);
            }

            return GetOrCreateImGuiBinding(factory, textureView);
        }

        /// <summary>
        /// Retrieves the shader texture binding for the given helper handle.
        /// </summary>
        public ResourceSet GetImageResourceSet(IntPtr imGuiBinding)
        {
            if (!_viewsById.TryGetValue(imGuiBinding, out ResourceSetInfo tvi))
            {
                throw new InvalidOperationException("No registered ImGui binding with id " + imGuiBinding.ToString());
            }

            return tvi.ResourceSet;
        }

        public void ClearCachedImageResources()
        {
            foreach (IDisposable resource in _ownedResources)
            {
                resource.Dispose();
            }

            _ownedResources.Clear();
            _setsByView.Clear();
            _viewsById.Clear();
            _autoViewsByTexture.Clear();
            _lastAssignedID = 100;
        }

        /// <summary>
        /// Recreates the device texture used to render text.
        /// </summary>
        public void RecreateFontDeviceTexture() => RecreateFontDeviceTexture(_gd);

        /// <summary>
        /// Recreates the device texture used to render text.
        /// </summary>
        public void RecreateFontDeviceTexture(GraphicsDevice gd)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            // Build
            io.Fonts.GetTexDataAsRGBA32(
                out IntPtr pixels,
                out int width,
                out int height,
                out int bytesPerPixel);
            // Store our identifier
            io.Fonts.SetTexID(_fontAtlasID);

            // Clear old font texture & related data if they exists.
            _fontTextureResourceSet?.Dispose();
            _fontTexture?.Dispose();

            _fontTexture = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                (uint)width,
                (uint)height,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.Sampled));
            _fontTexture.Name = "ImGui.NET Font Texture";
            gd.UpdateTexture(
                _fontTexture,
                pixels,
                (uint)(bytesPerPixel * width * height),
                0,
                0,
                0,
                (uint)width,
                (uint)height,
                1,
                0,
                0);
            _fontTextureResourceSet = gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(_textureLayout, _fontTexture));
            io.Fonts.ClearTexData();
        }

        /// <summary>
        /// Renders the ImGui draw list data.
        /// This method requires a <see cref="GraphicsDevice"/> because it may create new DeviceBuffers if the size of vertex
        /// or index data has increased beyond the capacity of the existing buffers.
        /// A <see cref="CommandList"/> is needed to submit drawing and resource update commands.
        /// </summary>
        public void Render(GraphicsDevice gd, CommandList cl)
        {
            if (_frameBegun)
            {
                _frameBegun = false;
                ImGui.Render();
                RenderImDrawData(ImGui.GetDrawData(), gd, cl);
            }
        }

        /// <summary>
        /// Updates ImGui input and IO configuration state.
        /// </summary>
        public void Update(float deltaSeconds, InputSnapshot snapshot, IntPtr handle)
        {
            if (_frameBegun)
            {
                ImGui.Render();
            }

            SetPerFrameImGuiData(deltaSeconds);
            UpdateImGuiInput(snapshot, handle);

            _frameBegun = true;
            ImGui.NewFrame();
        }

        /// <summary>
        /// Sets per-frame data based on the associated window.
        /// This is called by Update(float).
        /// </summary>
        private void SetPerFrameImGuiData(float deltaSeconds)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.DisplaySize = new Vector2(
                _windowWidth / _scaleFactor.X,
                _windowHeight / _scaleFactor.Y);
            io.DisplayFramebufferScale = _scaleFactor;
            io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
        }

        private bool TryMapKey(Key key, out ImGuiKey result)
        {
            ImGuiKey keyToImGuiKeyShortcut(Key keyToConvert, Key startKey1, ImGuiKey startKey2)
            {
                var tmpKey = (int)keyToConvert - (int)startKey1;
                return startKey2 + tmpKey;
            }

            if (key >= Key.F1 && key <= Key.F12)
            {
                result = keyToImGuiKeyShortcut(key, Key.F1, ImGuiKey.F1);
                return true;
            }
            else if (key >= Key.Keypad0 && key <= Key.Keypad9)
            {
                result = keyToImGuiKeyShortcut(key, Key.Keypad0, ImGuiKey.Keypad0);
                return true;
            }
            else if (key >= Key.A && key <= Key.Z)
            {
                result = keyToImGuiKeyShortcut(key, Key.A, ImGuiKey.A);
                return true;
            }
            else if (key >= Key.Number0 && key <= Key.Number9)
            {
                result = keyToImGuiKeyShortcut(key, Key.Number0, ImGuiKey._0);
                return true;
            }

            switch (key)
            {
                case Key.ShiftLeft:
                case Key.ShiftRight:
                    result = ImGuiKey.ModShift;
                    return true;
                case Key.ControlLeft:
                case Key.ControlRight:
                    result = ImGuiKey.ModCtrl;
                    return true;
                case Key.AltLeft:
                case Key.AltRight:
                    result = ImGuiKey.ModAlt;
                    return true;
                case Key.WinLeft:
                case Key.WinRight:
                    result = ImGuiKey.ModSuper;
                    return true;
                case Key.Menu:
                    result = ImGuiKey.Menu;
                    return true;
                case Key.Up:
                    result = ImGuiKey.UpArrow;
                    return true;
                case Key.Down:
                    result = ImGuiKey.DownArrow;
                    return true;
                case Key.Left:
                    result = ImGuiKey.LeftArrow;
                    return true;
                case Key.Right:
                    result = ImGuiKey.RightArrow;
                    return true;
                case Key.Enter:
                    result = ImGuiKey.Enter;
                    return true;
                case Key.Escape:
                    result = ImGuiKey.Escape;
                    return true;
                case Key.Space:
                    result = ImGuiKey.Space;
                    return true;
                case Key.Tab:
                    result = ImGuiKey.Tab;
                    return true;
                case Key.BackSpace:
                    result = ImGuiKey.Backspace;
                    return true;
                case Key.Insert:
                    result = ImGuiKey.Insert;
                    return true;
                case Key.Delete:
                    result = ImGuiKey.Delete;
                    return true;
                case Key.PageUp:
                    result = ImGuiKey.PageUp;
                    return true;
                case Key.PageDown:
                    result = ImGuiKey.PageDown;
                    return true;
                case Key.Home:
                    result = ImGuiKey.Home;
                    return true;
                case Key.End:
                    result = ImGuiKey.End;
                    return true;
                case Key.CapsLock:
                    result = ImGuiKey.CapsLock;
                    return true;
                case Key.ScrollLock:
                    result = ImGuiKey.ScrollLock;
                    return true;
                case Key.PrintScreen:
                    result = ImGuiKey.PrintScreen;
                    return true;
                case Key.Pause:
                    result = ImGuiKey.Pause;
                    return true;
                case Key.NumLock:
                    result = ImGuiKey.NumLock;
                    return true;
                case Key.KeypadDivide:
                    result = ImGuiKey.KeypadDivide;
                    return true;
                case Key.KeypadMultiply:
                    result = ImGuiKey.KeypadMultiply;
                    return true;
                case Key.KeypadSubtract:
                    result = ImGuiKey.KeypadSubtract;
                    return true;
                case Key.KeypadAdd:
                    result = ImGuiKey.KeypadAdd;
                    return true;
                case Key.KeypadDecimal:
                    result = ImGuiKey.KeypadDecimal;
                    return true;
                case Key.KeypadEnter:
                    result = ImGuiKey.KeypadEnter;
                    return true;
                case Key.Tilde:
                    result = ImGuiKey.GraveAccent;
                    return true;
                case Key.Minus:
                    result = ImGuiKey.Minus;
                    return true;
                case Key.Plus:
                    result = ImGuiKey.Equal;
                    return true;
                case Key.BracketLeft:
                    result = ImGuiKey.LeftBracket;
                    return true;
                case Key.BracketRight:
                    result = ImGuiKey.RightBracket;
                    return true;
                case Key.Semicolon:
                    result = ImGuiKey.Semicolon;
                    return true;
                case Key.Quote:
                    result = ImGuiKey.Apostrophe;
                    return true;
                case Key.Comma:
                    result = ImGuiKey.Comma;
                    return true;
                case Key.Period:
                    result = ImGuiKey.Period;
                    return true;
                case Key.Slash:
                    result = ImGuiKey.Slash;
                    return true;
                case Key.BackSlash:
                case Key.NonUSBackSlash:
                    result = ImGuiKey.Backslash;
                    return true;
                default:
                    result = ImGuiKey.GamepadBack;
                    return false;
            }
        }

        private void UpdateImGuiInput(InputSnapshot snapshot, IntPtr handle)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            var mousePos = NativeMethods.GetCursorPosition(handle);
            io.AddMousePosEvent(mousePos.X, mousePos.Y);
            if (NativeMethods.IsClickable)
            {
                io.AddMouseButtonEvent(0, snapshot.IsMouseDown(MouseButton.Left));
                io.AddMouseButtonEvent(1, snapshot.IsMouseDown(MouseButton.Right));
                io.AddMouseButtonEvent(2, snapshot.IsMouseDown(MouseButton.Middle));
                io.AddMouseButtonEvent(3, snapshot.IsMouseDown(MouseButton.Button1));
                io.AddMouseButtonEvent(4, snapshot.IsMouseDown(MouseButton.Button2));
                io.AddMouseWheelEvent(0f, snapshot.WheelDelta);
            }

            for (int i = 0; i < snapshot.KeyCharPresses.Count; i++)
            {
                io.AddInputCharacter(snapshot.KeyCharPresses[i]);
            }

            for (int i = 0; i < snapshot.KeyEvents.Count; i++)
            {
                KeyEvent keyEvent = snapshot.KeyEvents[i];
                if (TryMapKey(keyEvent.Key, out ImGuiKey imguikey))
                {
                    io.AddKeyEvent(imguikey, keyEvent.Down);
                }
            }

            if (io.WantCaptureMouse)
            {
                NativeMethods.SetOverlayClickable(handle, true);
            }
            else
            {
                NativeMethods.SetOverlayClickable(handle, false);
            }
        }

        private void RenderImDrawData(ImDrawDataPtr draw_data, GraphicsDevice gd, CommandList cl)
        {
            uint vertexOffsetInVertices = 0;
            uint indexOffsetInElements = 0;

            if (draw_data.CmdListsCount == 0)
            {
                return;
            }

            uint totalVBSize = (uint)(draw_data.TotalVtxCount * Unsafe.SizeOf<ImDrawVert>());
            if (totalVBSize > _vertexBuffer.SizeInBytes)
            {
                _vertexBuffer.Dispose();
                _vertexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalVBSize * 1.5f), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
            }

            uint totalIBSize = (uint)(draw_data.TotalIdxCount * sizeof(ushort));
            if (totalIBSize > _indexBuffer.SizeInBytes)
            {
                _indexBuffer.Dispose();
                _indexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalIBSize * 1.5f), BufferUsage.IndexBuffer | BufferUsage.Dynamic));
            }

            for (int i = 0; i < draw_data.CmdListsCount; i++)
            {
                ImDrawListPtr cmd_list = draw_data.CmdListsRange[i];
                cl.UpdateBuffer(
                    _vertexBuffer,
                    vertexOffsetInVertices * (uint)Unsafe.SizeOf<ImDrawVert>(),
                    cmd_list.VtxBuffer.Data,
                    (uint)(cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>()));

                cl.UpdateBuffer(
                    _indexBuffer,
                    indexOffsetInElements * sizeof(ushort),
                    cmd_list.IdxBuffer.Data,
                    (uint)(cmd_list.IdxBuffer.Size * sizeof(ushort)));

                vertexOffsetInVertices += (uint)cmd_list.VtxBuffer.Size;
                indexOffsetInElements += (uint)cmd_list.IdxBuffer.Size;
            }

            // Setup orthographic projection matrix into our constant buffer
            ImGuiIOPtr io = ImGui.GetIO();
            Matrix4x4 mvp = Matrix4x4.CreateOrthographicOffCenter(
                0f,
                io.DisplaySize.X,
                io.DisplaySize.Y,
                0.0f,
                -1.0f,
                1.0f);

            _gd.UpdateBuffer(_projMatrixBuffer, 0, ref mvp);

            cl.SetVertexBuffer(0, _vertexBuffer);
            cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            cl.SetPipeline(_pipeline);
            cl.SetGraphicsResourceSet(0, _mainResourceSet);

            draw_data.ScaleClipRects(io.DisplayFramebufferScale);

            // Render command lists
            int vtx_offset = 0;
            int idx_offset = 0;
            for (int n = 0; n < draw_data.CmdListsCount; n++)
            {
                ImDrawListPtr cmd_list = draw_data.CmdListsRange[n];
                for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
                {
                    ImDrawCmdPtr pcmd = cmd_list.CmdBuffer[cmd_i];
                    if (pcmd.UserCallback != IntPtr.Zero)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        if (pcmd.TextureId != IntPtr.Zero)
                        {
                            if (pcmd.TextureId == _fontAtlasID)
                            {
                                cl.SetGraphicsResourceSet(1, _fontTextureResourceSet);
                            }
                            else
                            {
                                cl.SetGraphicsResourceSet(1, GetImageResourceSet(pcmd.TextureId));
                            }
                        }

                        cl.SetScissorRect(
                            0,
                            (uint)pcmd.ClipRect.X,
                            (uint)pcmd.ClipRect.Y,
                            (uint)(pcmd.ClipRect.Z - pcmd.ClipRect.X),
                            (uint)(pcmd.ClipRect.W - pcmd.ClipRect.Y));

                        cl.DrawIndexed(pcmd.ElemCount, 1, pcmd.IdxOffset + (uint)idx_offset, (int)pcmd.VtxOffset + vtx_offset, 0);
                    }
                }

                vtx_offset += cmd_list.VtxBuffer.Size;
                idx_offset += cmd_list.IdxBuffer.Size;
            }
        }

        /// <summary>
        /// Frees all graphics resources used by the renderer.
        /// </summary>
        public void Dispose()
        {
            ClearCachedImageResources();

            _fontTextureResourceSet.Dispose();
            _fontTexture.Dispose();

            _mainResourceSet.Dispose();
            _pipeline.Dispose();
            _textureLayout.Dispose();
            _layout.Dispose();
            _fragmentShader.Dispose();
            _vertexShader.Dispose();
            _projMatrixBuffer.Dispose();
            _indexBuffer.Dispose();
            _vertexBuffer.Dispose();
        }

        private static byte[] LoadEmbeddedShaderCode(ResourceFactory factory, string name, ShaderStages _)
        {
            switch (factory.BackendType)
            {
                case GraphicsBackend.Direct3D11:
                    {
                        string resourceName = name + ".hlsl";
                        return GetEmbeddedResourceBytes(resourceName);
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        private static byte[] GetEmbeddedResourceBytes(string resourceName)
        {
            Assembly assembly = typeof(ImGuiController).Assembly;
            using var s = assembly.GetManifestResourceStream(resourceName);
            byte[] ret = new byte[s.Length];
            s.Read(ret, 0, (int)s.Length);
            return ret;
        }

        private struct ResourceSetInfo
        {
            public readonly IntPtr ImGuiBinding;
            public readonly ResourceSet ResourceSet;

            public ResourceSetInfo(IntPtr imGuiBinding, ResourceSet resourceSet)
            {
                ImGuiBinding = imGuiBinding;
                ResourceSet = resourceSet;
            }
        }
    }
}
