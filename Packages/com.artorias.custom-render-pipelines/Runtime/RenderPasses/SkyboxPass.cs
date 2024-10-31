using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace CustomRP
{
    public class SkyboxPass
    {
        static readonly ProfilingSampler m_ProfileSampler = new("CustomRP.SkyboxPass");
        
        RendererListHandle m_RendererList;

        void Render(RenderGraphContext rgContext)
        {
            rgContext.cmd.DrawRendererList(m_RendererList);
            rgContext.renderContext.ExecuteCommandBuffer(rgContext.cmd);
            rgContext.cmd.Clear();
        }

        public static void Record(RenderGraph rg, Camera camera, in CameraRendererTextures renderTextures)
        {
            if (camera.clearFlags == CameraClearFlags.Skybox)
            {
                using RenderGraphBuilder rgBuilder =
                    rg.AddRenderPass(m_ProfileSampler.name, out SkyboxPass pass, m_ProfileSampler);
                pass.m_RendererList = rgBuilder.UseRendererList(rg.CreateSkyboxRendererList(camera));

                rgBuilder.ReadWriteTexture(renderTextures.m_ColorAttachment);
                rgBuilder.ReadTexture(renderTextures.m_DepthAttachment);
                rgBuilder.AllowPassCulling(false);
                rgBuilder.SetRenderFunc<SkyboxPass>(static (pass, rgContext) => pass.Render(rgContext));
            }
        }
    }
}