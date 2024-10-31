using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace CustomRP
{
    public class CopyAttachmentsPass
    {
        static readonly ProfilingSampler m_ProfileSampler = new("CustomRP.CopyAttachmentsPass");

        private static readonly int m_ColorCopyAttachmentID = Shader.PropertyToID("_CameraColorTexture");
        private static readonly int m_DepthCopyAttachmentID = Shader.PropertyToID("_CameraDepthAttachment");
        
        bool m_IsCopyColor, m_IsCopyDepth;

        private CameraRendererCopier m_Copier;
        
        TextureHandle m_ColorAttachment, m_DepthAttachment;
        TextureHandle m_ColorCopyAttachment, m_DepthCopyAttachment;

        void Render(RenderGraphContext rgContext)
        {
            CommandBuffer cmdBuffer = rgContext.cmd;
            if (m_IsCopyColor)
            {
                m_Copier.Copy(cmdBuffer, m_ColorAttachment, m_ColorCopyAttachment, false);
                cmdBuffer.SetGlobalTexture(m_ColorCopyAttachmentID, m_ColorCopyAttachment);
            }
            if (m_IsCopyDepth)
            {
                m_Copier.Copy(cmdBuffer, m_DepthAttachment, m_DepthCopyAttachment, true);
                cmdBuffer.SetGlobalTexture(m_DepthCopyAttachmentID, m_DepthCopyAttachment);
            }

            if (CameraRendererCopier.m_RequiresRTResetAfterCopy)
            {
                cmdBuffer.SetRenderTarget(
                    m_ColorAttachment, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
                    m_DepthAttachment, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
            }
            rgContext.renderContext.ExecuteCommandBuffer(cmdBuffer);
            cmdBuffer.Clear();
        }

        public static void Record(RenderGraph rg, bool isCopyColor, bool isCopyDepth, 
            CameraRendererCopier copier, in CameraRendererTextures renderTextures)
        {
            using RenderGraphBuilder rgBuilder =
                rg.AddRenderPass(m_ProfileSampler.name, out CopyAttachmentsPass pass, m_ProfileSampler);
            pass.m_IsCopyColor = isCopyColor;
            pass.m_IsCopyDepth = isCopyDepth;
            pass.m_Copier = copier;
            
            pass.m_ColorAttachment = rgBuilder.ReadTexture(renderTextures.m_ColorAttachment);
            pass.m_DepthAttachment = rgBuilder.ReadTexture(renderTextures.m_DepthAttachment);

            if (isCopyColor)
            {
                pass.m_ColorCopyAttachment = rgBuilder.WriteTexture(renderTextures.m_ColorCopyAttachment);
            }
            if (isCopyDepth)
            {
                pass.m_DepthCopyAttachment = rgBuilder.WriteTexture(renderTextures.m_DepthCopyAttachment);
            }
            
            rgBuilder.AllowPassCulling(true);
            rgBuilder.SetRenderFunc<CopyAttachmentsPass>(static (pass, rgContext) => pass.Render(rgContext));
        }
    }
}