using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace CustomRP
{
    public class SetupPass
    {
        static readonly ProfilingSampler m_ProfileSampler = new("CustomRP.SetupPass");
        
        static readonly int m_AttachmentSizeID = Shader.PropertyToID("_CameraAttachmentSize");
        Vector2Int m_AttachmentSize;
        
        TextureHandle m_ColorAttachment;
        TextureHandle m_DepthAttachment;
        
        Camera m_Camera;
        CameraClearFlags m_ClearFlags;

        void Render(RenderGraphContext rgContext)
        {
            rgContext.renderContext.SetupCameraProperties(m_Camera);
            CommandBuffer cmdBuffer = rgContext.cmd;
            cmdBuffer.SetRenderTarget(
                m_ColorAttachment, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                m_DepthAttachment, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmdBuffer.ClearRenderTarget(
                m_ClearFlags <= CameraClearFlags.Depth, m_ClearFlags == CameraClearFlags.Color,
                m_ClearFlags == CameraClearFlags.Color ? m_Camera.backgroundColor.linear : Color.clear);
            cmdBuffer.SetGlobalVector(m_AttachmentSizeID,
                new Vector4(m_AttachmentSize.x, m_AttachmentSize.y, 1f / m_AttachmentSize.x, 1f / m_AttachmentSize.y));
            rgContext.renderContext.ExecuteCommandBuffer(cmdBuffer);
            cmdBuffer.Clear();
        }

        public static CameraRendererTextures Record(RenderGraph rg, bool isCopyColor, bool isCopyDepth, bool isUseHDR, 
            Vector2Int attachmentSize, Camera camera)
        {
            using RenderGraphBuilder rgBuilder =
                rg.AddRenderPass(m_ProfileSampler.name, out SetupPass pass, m_ProfileSampler);
            pass.m_AttachmentSize = attachmentSize;
            pass.m_Camera = camera;
            pass.m_ClearFlags = camera.clearFlags;

            TextureHandle colorCopyAttachment = default;
            TextureHandle depthCopyAttachment = default;
            if (pass.m_ClearFlags > CameraClearFlags.Color)
            {
                pass.m_ClearFlags = CameraClearFlags.Color;
            }

            var texDesc = new TextureDesc(attachmentSize.x, attachmentSize.y)
            {
                colorFormat = SystemInfo.GetGraphicsFormat(isUseHDR ? DefaultFormat.HDR : DefaultFormat.LDR),
                name = "ColorAttachment"
            };
            TextureHandle outColorAttachment =
                pass.m_ColorAttachment = rgBuilder.WriteTexture(rg.CreateTexture(texDesc));
            if (isCopyColor)
            {
                texDesc.name = "ColorCopyAttachment";
                colorCopyAttachment = rg.CreateTexture(texDesc);
            }

            texDesc.depthBufferBits = DepthBits.Depth32;
            texDesc.name = "DepthAttachment";
            TextureHandle outDepthAttachment =
                pass.m_DepthAttachment = rgBuilder.WriteTexture(rg.CreateTexture(texDesc));
            if (isCopyDepth)
            {
                texDesc.name = "DepthCopyAttachment";
                depthCopyAttachment = rg.CreateTexture(texDesc);
            }
            
            rgBuilder.AllowPassCulling(false);
            rgBuilder.SetRenderFunc<SetupPass>(static (pass, rgContext) => pass.Render(rgContext));
            
            return new CameraRendererTextures(outColorAttachment, outDepthAttachment, colorCopyAttachment, depthCopyAttachment);
        }
    }
}