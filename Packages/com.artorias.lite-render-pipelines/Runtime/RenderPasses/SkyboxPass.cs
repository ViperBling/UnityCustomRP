using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace LiteRP
{
    public partial class LiteRGRecorder
    {
        private static readonly ProfilingSampler s_SkyboxPassSampler = new ProfilingSampler("LiteRP.SkyboxPass");
        internal class SkyboxPassData
        {
            internal RendererListHandle skyboxRenderListHandle;
        }

        private void AddSkyboxPass(RenderGraph rg, CameraData cameraData)
        {
            using var rgBuilder = rg.AddRasterRenderPass<SkyboxPassData>(s_SkyboxPassSampler.name, out var passData, s_SkyboxPassSampler);

            passData.skyboxRenderListHandle = rg.CreateSkyboxRendererList(cameraData.m_Camera);
            rgBuilder.UseRendererList(passData.skyboxRenderListHandle);
            
            if (m_BackBufferColorHandle.IsValid())
            {
                rgBuilder.SetRenderAttachment(m_BackBufferColorHandle, 0, AccessFlags.Write);
            }
            
            rgBuilder.SetRenderFunc<SkyboxPassData>((skyboxPassData, rgContext) =>
            {
                rgContext.cmd.DrawRendererList(skyboxPassData.skyboxRenderListHandle);
            });
        }
    }
}