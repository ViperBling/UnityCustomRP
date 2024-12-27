using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

using LiteRP.FrameData;

namespace LiteRP
{
    public partial class LiteRGRecorder
    {
        private static readonly ProfilingSampler s_GBufferPassSampler = new ProfilingSampler("LiteRP.GBufferPass");
        
        internal class GBufferPassData
        {
            internal RendererListHandle gBufferRendererListHandle;
            internal TextureHandle[] mMRTs;
            internal int gBufferCount;
        }

        private void AddGBufferPass(RenderGraph rg, CameraData cameraData, LightData lightData)
        {
            // using var rgBuilder = rg.AddRasterRenderPass<GBufferPassData>(s_OpaqueObjectPassSampler.name, out var passData, s_OpaqueObjectPassSampler);
            //
            // // 声明或引用资源
            // RendererListDesc opaqueListDesc = new RendererListDesc(s_ShaderTagIDs, cameraData.m_CullingResults, cameraData.m_Camera);
            // opaqueListDesc.sortingCriteria = SortingCriteria.CommonOpaque;
            // opaqueListDesc.renderQueueRange = RenderQueueRange.opaque;
            // passData.gBufferRendererListHandle = rg.CreateRendererList(opaqueListDesc);
            // rgBuilder.UseRendererList(passData.gBufferRendererListHandle);
            //
            // // 导入BackBuffer
            // if (m_BackBufferColorHandle.IsValid())
            // {
            //     rgBuilder.SetRenderAttachment(m_BackBufferColorHandle, 0, AccessFlags.Write);
            // }
            // if (m_BackBufferDepthHandle.IsValid())
            // {
            //     rgBuilder.SetRenderAttachmentDepth(m_BackBufferDepthHandle, AccessFlags.Write);
            // }
            //
            // // 阴影
            // if (m_MainLightShadowHandle.IsValid())
            // {
            //     rgBuilder.UseTexture(m_MainLightShadowHandle, AccessFlags.Read);
            // }
            //
            // // 设置全局渲染状态
            // rgBuilder.AllowPassCulling(false);
            // rgBuilder.AllowGlobalStateModification(true);
            //
            // // 设置渲染函数
            // rgBuilder.SetRenderFunc<GBufferPassData>((gBufferPassData, rgContext) =>
            // {
            //     rgContext.cmd.SetGlobalFloat(ShaderPropertyID.alphaToMaskAvailable, 1.0f);
            //     rgContext.cmd.DrawRendererList(gBufferPassData.gBufferRendererListHandle);
            // });
        }
    }
}