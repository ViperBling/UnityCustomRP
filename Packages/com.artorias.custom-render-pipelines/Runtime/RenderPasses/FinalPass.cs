using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace CustomRP
{
    public class FinalPass
    {
        static readonly ProfilingSampler m_ProfileSampler = new("CustomRP.FinalPass");

        CameraRendererCopier m_Copier;
        TextureHandle m_ColorAttachment;

        void Render(RenderGraphContext rgContext)
        {
            CommandBuffer cmdBuffer = rgContext.cmd;
            m_Copier.CopyToCameraTarget(cmdBuffer, m_ColorAttachment);
            rgContext.renderContext.ExecuteCommandBuffer(cmdBuffer);
            cmdBuffer.Clear();
        }

        public static void Record(RenderGraph rg, CameraRendererCopier copier, in CameraRendererTextures textures)
        {
            using RenderGraphBuilder rgBuilder =
                rg.AddRenderPass(m_ProfileSampler.name, out FinalPass pass, m_ProfileSampler);
            pass.m_Copier = copier;
            pass.m_ColorAttachment = rgBuilder.ReadTexture(textures.m_ColorAttachment);
            
            rgBuilder.SetRenderFunc<FinalPass>(static (pass, rgContext) => pass.Render(rgContext));
        }
    }
}