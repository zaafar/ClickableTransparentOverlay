namespace ClickableTransparentOverlay
{
    using ImGuiNET;
    using ImDrawIdx = System.UInt16;
    using Vortice.DXGI;
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using Vortice.D3DCompiler;
    using Vortice.Mathematics;
    using System.Numerics;
    using System.Collections.Generic;
    using System;
    using System.Linq;
    using SixLabors.ImageSharp;
    using SixLabors.ImageSharp.PixelFormats;
    using System.Buffers;

    unsafe internal sealed class ImGuiRenderer : IDisposable
    {
        const int VertexConstantBufferSize = 16 * 4;

        ID3D11Device device;
        ID3D11DeviceContext deviceContext;
        ID3D11Buffer vertexBuffer;
        ID3D11Buffer indexBuffer;
        Blob vertexShaderBlob;
        ID3D11VertexShader vertexShader;
        ID3D11InputLayout inputLayout;
        ID3D11Buffer constantBuffer;
        Blob pixelShaderBlob;
        ID3D11PixelShader pixelShader;
        ID3D11SamplerState fontSampler;
        ID3D11RasterizerState rasterizerState;
        ID3D11BlendState blendState;
        ID3D11DepthStencilState depthStencilState;
        int vertexBufferSize = 5000, indexBufferSize = 10000;
        readonly Dictionary<IntPtr, ID3D11ShaderResourceView> textureResources = new();

        public ImGuiRenderer(ID3D11Device device, ID3D11DeviceContext deviceContext, int width, int height)
        {
            this.device = device;
            this.deviceContext = deviceContext;

            device.AddRef();
            deviceContext.AddRef();

            ImGui.CreateContext();
            var io = ImGui.GetIO();
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;  // We can honor the ImDrawCmd::VtxOffset field, allowing for large meshes.
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            ImGui.StyleColorsDark();
            this.Resize(width, height);
            this.CreateDeviceObjects();
        }

        public void Start()
        {
            ImGui.NewFrame();
        }

        public void Update(float deltaTime, Action DoRender)
        {
            var io = ImGui.GetIO();
            io.DeltaTime = deltaTime;
            ImGui.NewFrame();
            DoRender?.Invoke();
            ImGui.Render();
        }

        public void Render()
        {
            ImDrawDataPtr data = ImGui.GetDrawData();
            // Avoid rendering when minimized
            if (data.DisplaySize.X <= 0.0f || data.DisplaySize.Y <= 0.0f)
                return;

            ID3D11DeviceContext ctx = deviceContext;

            if (vertexBuffer == null || vertexBufferSize < data.TotalVtxCount)
            {
                vertexBuffer?.Release();

                vertexBufferSize = data.TotalVtxCount + 5000;
                var desc = new BufferDescription(
                    vertexBufferSize * sizeof(ImDrawVert),
                    BindFlags.VertexBuffer,
                    ResourceUsage.Dynamic,
                    CpuAccessFlags.Write);
                vertexBuffer = device.CreateBuffer(desc);
            }

            if (indexBuffer == null || indexBufferSize < data.TotalIdxCount)
            {
                indexBuffer?.Release();

                indexBufferSize = data.TotalIdxCount + 10000;

                var desc = new BufferDescription(
                    indexBufferSize * sizeof(ImDrawIdx),
                    BindFlags.IndexBuffer,
                    ResourceUsage.Dynamic,
                    CpuAccessFlags.Write);

                indexBuffer = device.CreateBuffer(desc);
            }

            // Upload vertex/index data into a single contiguous GPU buffer
            var vertexResource = ctx.Map(vertexBuffer, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
            var indexResource = ctx.Map(indexBuffer, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
            var vertexResourcePointer = (ImDrawVert*)vertexResource.DataPointer;
            var indexResourcePointer = (ImDrawIdx*)indexResource.DataPointer;
            for (int n = 0; n < data.CmdListsCount; n++)
            {
                var cmdlList = data.CmdLists[n];

                var vertBytes = cmdlList.VtxBuffer.Size * sizeof(ImDrawVert);
                Buffer.MemoryCopy((void*)cmdlList.VtxBuffer.Data, vertexResourcePointer, vertBytes, vertBytes);

                var idxBytes = cmdlList.IdxBuffer.Size * sizeof(ImDrawIdx);
                Buffer.MemoryCopy((void*)cmdlList.IdxBuffer.Data, indexResourcePointer, idxBytes, idxBytes);

                vertexResourcePointer += cmdlList.VtxBuffer.Size;
                indexResourcePointer += cmdlList.IdxBuffer.Size;
            }
            ctx.Unmap(vertexBuffer, 0);
            ctx.Unmap(indexBuffer, 0);

            // Setup orthographic projection matrix into our constant buffer
            // Our visible imgui space lies from draw_data.DisplayPos (top left) to draw_data.DisplayPos+data_data.DisplaySize (bottom right). DisplayPos is (0,0) for single viewport apps.

            var constResource = ctx.Map(constantBuffer, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
            var span = constResource.AsSpan<float>(VertexConstantBufferSize);
            float L = data.DisplayPos.X;
            float R = data.DisplayPos.X + data.DisplaySize.X;
            float T = data.DisplayPos.Y;
            float B = data.DisplayPos.Y + data.DisplaySize.Y;
            float[] mvp =
            {
                    2.0f/(R-L),   0.0f,           0.0f,       0.0f,
                    0.0f,         2.0f/(T-B),     0.0f,       0.0f,
                    0.0f,         0.0f,           0.5f,       0.0f,
                    (R+L)/(L-R),  (T+B)/(B-T),    0.5f,       1.0f,
            };
            mvp.CopyTo(span);
            ctx.Unmap(constantBuffer, 0);
            //BackupDX11State(ctx); // only required if imgui is injected + drawn on existing process.
            SetupRenderState(data, ctx);
            // Render command lists
            // (Because we merged all buffers into a single one, we maintain our own offset into them)
            int global_idx_offset = 0;
            int global_vtx_offset = 0;
            for (int n = 0; n < data.CmdListsCount; n++)
            {
                var cmdList = data.CmdLists[n];
                for (int i = 0; i < cmdList.CmdBuffer.Size; i++)
                {
                    var cmd = cmdList.CmdBuffer[i];
                    if (cmd.UserCallback != IntPtr.Zero)
                    {
                        throw new NotImplementedException("user callbacks not implemented");
                    }
                    else
                    {
                        ctx.RSSetScissorRect(
                            (int)cmd.ClipRect.X,
                            (int)cmd.ClipRect.Y,
                            (int)(cmd.ClipRect.Z - cmd.ClipRect.X),
                            (int)(cmd.ClipRect.W - cmd.ClipRect.Y));

                        if (textureResources.TryGetValue(cmd.GetTexID(), out var texture))
                        {
                            ctx.PSSetShaderResource(0, texture);
                        }

                        ctx.DrawIndexed((int)cmd.ElemCount, (int)(cmd.IdxOffset + global_idx_offset), (int)(cmd.VtxOffset + global_vtx_offset));
                    }
                }
                global_idx_offset += cmdList.IdxBuffer.Size;
                global_vtx_offset += cmdList.VtxBuffer.Size;
            }

            //RestoreDX11State(ctx); // only required if imgui is injected + drawn on existing process.
        }

        public void Dispose()
        {
            if (device == null)
                return;

            this.DeRegisterAllTexture();
            fontSampler?.Release();
            indexBuffer?.Release();
            vertexBuffer?.Release();
            blendState?.Release();
            depthStencilState?.Release();
            rasterizerState?.Release();
            pixelShader?.Release();
            pixelShaderBlob?.Release();
            constantBuffer?.Release();
            inputLayout?.Release();
            vertexShader?.Release();
            vertexShaderBlob?.Release();
        }

        public void Resize(int width, int height)
        {
            ImGui.GetIO().DisplaySize = new Vector2(width, height);
        }

        public IntPtr CreateImageTexture(Image<Rgba32> image, Format format)
        {
            var texDesc = new Texture2DDescription(format, image.Width, image.Height, 1, 1);
            if (!image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> memory))
            {
                throw new Exception("Make sure to initialize MemoryAllocator.Default!");
            }

            using MemoryHandle imageMemoryHandle = memory.Pin();
            var subResource = new SubresourceData(imageMemoryHandle.Pointer, texDesc.Width * 4);
            using var texture = device.CreateTexture2D(texDesc, new[] { subResource });
            var resViewDesc = new ShaderResourceViewDescription(texture, ShaderResourceViewDimension.Texture2D, format, 0, texDesc.MipLevels);
            return RegisterTexture(device.CreateShaderResourceView(texture, resViewDesc));
        }

        public bool RemoveImageTexture(IntPtr handle)
        {
            using var tex = this.DeRegisterTexture(handle);
            return tex != null;
        }

        public void UpdateFontTexture(FontHelper.FontLoadDelegate fontLoadFunc)
        {
            var io = ImGui.GetIO();
            this.DeRegisterTexture(io.Fonts.TexID)?.Dispose();
            io.Fonts.Clear();
            var config = ImGuiNative.ImFontConfig_ImFontConfig();
            fontLoadFunc(config);
            this.CreateFontsTexture();
            ImGuiNative.ImFontConfig_destroy(config);
        }

        void SetupRenderState(ImDrawDataPtr drawData, ID3D11DeviceContext ctx)
        {
            var viewport = new Viewport(0f, 0f, drawData.DisplaySize.X, drawData.DisplaySize.Y, 0f, 1f);
            ctx.RSSetViewport(viewport);
            int stride = sizeof(ImDrawVert);
            ctx.IASetInputLayout(inputLayout);
            ctx.IASetVertexBuffer(0, vertexBuffer, stride);
            ctx.IASetIndexBuffer(indexBuffer, sizeof(ImDrawIdx) == 2 ? Format.R16_UInt : Format.R32_UInt, 0);
            ctx.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            ctx.VSSetShader(vertexShader);
            ctx.VSSetConstantBuffer(0, constantBuffer);
            ctx.PSSetShader(pixelShader);
            ctx.PSSetSampler(0, fontSampler);
            ctx.GSSetShader(null);
            ctx.HSSetShader(null);
            ctx.DSSetShader(null);
            ctx.CSSetShader(null);

            ctx.OMSetBlendState(blendState, new Color4(0f, 0f, 0f, 0f));
            ctx.OMSetDepthStencilState(depthStencilState);
            ctx.RSSetState(rasterizerState);
        }

        void CreateFontsTexture()
        {
            var io = ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out var width, out var height);
            var texDesc = new Texture2DDescription(Format.R8G8B8A8_UNorm, width, height, 1, 1);
            var subResource = new SubresourceData(pixels, texDesc.Width * 4);
            using var texture = device.CreateTexture2D(texDesc, new[] { subResource });
            var resViewDesc = new ShaderResourceViewDescription(
                texture,
                ShaderResourceViewDimension.Texture2D,
                Format.R8G8B8A8_UNorm,
                0,
                texDesc.MipLevels);
            io.Fonts.SetTexID(RegisterTexture(device.CreateShaderResourceView(texture, resViewDesc)));
            io.Fonts.ClearTexData();
        }

        void CreateFontSampler()
        {
            var samplerDesc = new SamplerDescription(
                Filter.MinMagMipLinear,
                TextureAddressMode.Wrap,
                TextureAddressMode.Wrap,
                TextureAddressMode.Wrap,
                0f,
                0,
                ComparisonFunction.Always,
                0f,
                0f);

            this.fontSampler = device.CreateSamplerState(samplerDesc);
        }

        IntPtr RegisterTexture(ID3D11ShaderResourceView texture)
        {
            var imguiID = texture.NativePointer;
            textureResources.TryAdd(imguiID, texture);
            return imguiID;
        }

        ID3D11ShaderResourceView? DeRegisterTexture(IntPtr texturePtr)
        {
            if (textureResources.Remove(texturePtr, out var texture))
            {
                return texture;
            }

            return null;
        }

        void DeRegisterAllTexture()
        {
            foreach (var key in textureResources.Keys.ToArray())
            {
                this.DeRegisterTexture(key)?.Release();
            }
        }

        void CreateDeviceObjects()
        {
            var vertexShaderCode =
                @"
                    cbuffer vertexBuffer : register(b0)
                    {
                        float4x4 ProjectionMatrix;
                    };

                    struct VS_INPUT
                    {
                        float2 pos : POSITION;
                        float4 col : COLOR0;
                        float2 uv  : TEXCOORD0;
                    };

                    struct PS_INPUT
                    {
                        float4 pos : SV_POSITION;
                        float4 col : COLOR0;
                        float2 uv  : TEXCOORD0;
                    };

                    PS_INPUT main(VS_INPUT input)
                    {
                        PS_INPUT output;
                        output.pos = mul(ProjectionMatrix, float4(input.pos.xy, 0.f, 1.f));
                        output.col = input.col;
                        output.uv  = input.uv;
                        return output;
                    }";
            Compiler.Compile(vertexShaderCode, "main", "vs", "vs_4_0", out vertexShaderBlob, out _);
            if (vertexShaderBlob == null)
                throw new Exception("error compiling vertex shader");

            vertexShader = device.CreateVertexShader(vertexShaderBlob);

            var inputElements = new[]
            {
                new InputElementDescription( "POSITION", 0, Format.R32G32_Float,   0, 0, InputClassification.PerVertexData, 0 ),
                new InputElementDescription( "TEXCOORD", 0, Format.R32G32_Float,   8,  0, InputClassification.PerVertexData, 0 ),
                new InputElementDescription( "COLOR",    0, Format.R8G8B8A8_UNorm, 16, 0, InputClassification.PerVertexData, 0 ),
            };

            inputLayout = device.CreateInputLayout(inputElements, vertexShaderBlob);

            var constBufferDesc = new BufferDescription(
                VertexConstantBufferSize,
                BindFlags.ConstantBuffer,
                ResourceUsage.Dynamic,
                CpuAccessFlags.Write);

            constantBuffer = device.CreateBuffer(constBufferDesc);

            var pixelShaderCode =
                @"struct PS_INPUT
                    {
                        float4 pos : SV_POSITION;
                        float4 col : COLOR0;
                        float2 uv  : TEXCOORD0;
                    };

                    sampler sampler0;
                    Texture2D texture0;

                    float4 main(PS_INPUT input) : SV_Target
                    {
                        return input.col * texture0.Sample(sampler0, input.uv);
                    }";
            Compiler.Compile(pixelShaderCode, "main", "ps", "ps_4_0", out pixelShaderBlob, out _);
            if (pixelShaderBlob == null)
                throw new Exception("error compiling pixel shader");

            pixelShader = device.CreatePixelShader(pixelShaderBlob);

            var blendDesc = new BlendDescription(Blend.SourceAlpha, Blend.InverseSourceAlpha, Blend.One, Blend.InverseSourceAlpha);
            blendState = device.CreateBlendState(blendDesc);

            var rasterDesc = new RasterizerDescription(CullMode.None, FillMode.Solid)
            {
                MultisampleEnable = false,
                ScissorEnable = true
            };
            rasterizerState = device.CreateRasterizerState(rasterDesc);

            var depthDesc = new DepthStencilDescription(false, DepthWriteMask.All, ComparisonFunction.Always);
            depthStencilState = device.CreateDepthStencilState(depthDesc);

            this.CreateFontsTexture();
            this.CreateFontSampler();
        }

#if false
        void BackupDX11State(ID3D11DeviceContext ctx)
        {
            old.ScissorRectsCount = ctx.RSGetScissorRects();
            if (old.ScissorRectsCount > 0)
            {
                ctx.RSGetScissorRects(ref old.ScissorRectsCount, old.ScissorRects);
            }

            old.ViewportsCount = ctx.RSGetViewports();
            if (old.ViewportsCount > 0)
            {
                ctx.RSGetScissorRects(ref old.ViewportsCount, old.ScissorRects);
            }

            old.RS = ctx.RSGetState();
            old.BlendState = ctx.OMGetBlendState(out old.BlendFactor, out old.SampleMask);
            ctx.OMGetDepthStencilState(out old.DepthStencilState, out old.StencilRef);
            ctx.PSGetShaderResources(0, old.PSShaderResource);
            ctx.PSGetSamplers(0, old.PSSampler);
            ctx.PSGetShader(out old.PS, old.PSInstances, ref old.PSInstancesCount);
            ctx.VSGetShader(out old.VS, old.VSInstances, ref old.VSInstancesCount);
            ctx.VSGetConstantBuffers(0, old.VSConstantBuffer);
            ctx.GSGetShader(out old.GS, old.GSInstances, ref old.GSInstancesCount);
            old.PrimitiveTopology = ctx.IAGetPrimitiveTopology();
            ctx.IAGetIndexBuffer(out old.IndexBuffer, out old.IndexBufferFormat, out old.IndexBufferOffset);
            ctx.IAGetVertexBuffers(0, 1, old.VertexBuffer, old.VertexBufferStride, old.VertexBufferOffset);
            old.InputLayout = ctx.IAGetInputLayout();
        }

        void RestoreDX11State(ID3D11DeviceContext ctx)
        {
            ctx.RSSetScissorRects(old.ScissorRects);
            ctx.RSSetViewports(old.Viewports);
            ctx.RSSetState(old.RS);
            old.RS?.Release();
            ctx.OMSetBlendState(old.BlendState, old.BlendFactor, old.SampleMask);
            old.BlendState?.Release();
            ctx.OMSetDepthStencilState(old.DepthStencilState, old.StencilRef);
            old.DepthStencilState?.Release();
            ctx.PSSetShaderResources(0, old.PSShaderResource);
            old.PSShaderResource[0]?.Release();
            ctx.PSSetSamplers(0, old.PSSampler);
            old.PSSampler[0]?.Release();
            ctx.PSSetShader(old.PS, old.PSInstances, old.PSInstancesCount);
            old.PS?.Release();
            for (int i = 0; i < old.PSInstancesCount; i++) old.PSInstances[i]?.Release();
            ctx.VSSetShader(old.VS, old.VSInstances, old.VSInstancesCount);
            old.VS?.Release();
            ctx.VSSetConstantBuffers(0, old.VSConstantBuffer);
            old.VSConstantBuffer[0]?.Release();
            ctx.GSSetShader(old.GS, old.GSInstances, old.GSInstancesCount);
            for (int i = 0; i < old.VSInstancesCount; i++) old.VSInstances[i]?.Release();
            ctx.IASetPrimitiveTopology(old.PrimitiveTopology);
            ctx.IASetIndexBuffer(old.IndexBuffer, old.IndexBufferFormat, old.IndexBufferOffset);
            old.IndexBuffer?.Release();
            ctx.IASetVertexBuffers(0, 1, old.VertexBuffer, old.VertexBufferStride, old.VertexBufferOffset);
            old.VertexBuffer[0]?.Release();
            ctx.IASetInputLayout(old.InputLayout);
            old.InputLayout?.Release();
        }

        class BACKUP_DX11_STATE
        {
            public int ScissorRectsCount = 0, ViewportsCount = 0;
            public RawRect[] ScissorRects = new RawRect[16];
            public Viewport[] Viewports = new Viewport[16];
            public ID3D11RasterizerState RS = default;
            public ID3D11BlendState BlendState = default;
            public Color4 BlendFactor = default;
            public int SampleMask = 0;
            public int StencilRef = 0;
            public ID3D11DepthStencilState DepthStencilState = default;
            public ID3D11ShaderResourceView[] PSShaderResource = new ID3D11ShaderResourceView[1];
            public ID3D11SamplerState[] PSSampler = new ID3D11SamplerState[1];
            public ID3D11PixelShader PS = default;
            public ID3D11VertexShader VS = default;
            public ID3D11GeometryShader GS = default;
            public int PSInstancesCount = 256, VSInstancesCount = 256, GSInstancesCount = 256;
            public ID3D11ClassInstance[] PSInstances = new ID3D11ClassInstance[256];
            public ID3D11ClassInstance[] VSInstances = new ID3D11ClassInstance[256];
            public ID3D11ClassInstance[] GSInstances = new ID3D11ClassInstance[256];
            public PrimitiveTopology PrimitiveTopology = 0;
            public ID3D11Buffer IndexBuffer = default;
            public ID3D11Buffer[] VertexBuffer = new ID3D11Buffer[1], VSConstantBuffer = new ID3D11Buffer[1];
            public int IndexBufferOffset = 0;
            public int[] VertexBufferStride = new int[1], VertexBufferOffset = new int[1];
            public Format IndexBufferFormat = 0;
            public ID3D11InputLayout InputLayout = default;
        } readonly BACKUP_DX11_STATE old = new();
#endif
    }

}
