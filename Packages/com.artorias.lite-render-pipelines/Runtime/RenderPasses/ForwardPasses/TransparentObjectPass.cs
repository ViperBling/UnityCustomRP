using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

using LiteRP.FrameData;

namespace LiteRP
{
    public partial class LiteRGRecorder
    {
        private static readonly ProfilingSampler s_TransparentObjectPassSampler = new ProfilingSampler("LiteRP.TransparentObjectPass");
        
        internal class TransparentObjectPassData
        {
            internal RendererListHandle transparentRendererListHandle;
        }

        private void AddTransparentObjectPass(RenderGraph rg, CameraData cameraData)
        {
            using var rgBuilder = rg.AddRasterRenderPass<TransparentObjectPassData>(s_TransparentObjectPassSampler.name, out var passData, s_TransparentObjectPassSampler);
            
            // 声明或引用资源
            RendererListDesc opaqueListDesc = new RendererListDesc(s_ShaderTagIDs, cameraData.m_CullingResults, cameraData.m_Camera);
            opaqueListDesc.sortingCriteria = SortingCriteria.CommonTransparent;
            opaqueListDesc.renderQueueRange = RenderQueueRange.transparent;
            passData.transparentRendererListHandle = rg.CreateRendererList(opaqueListDesc);
            rgBuilder.UseRendererList(passData.transparentRendererListHandle);
            
            // 导入BackBuffer
            if (m_BackBufferColorHandle.IsValid())
            {
                rgBuilder.SetRenderAttachment(m_BackBufferColorHandle, 0, AccessFlags.Write);
            }
            if (m_BackBufferDepthHandle.IsValid())
            {
                rgBuilder.SetRenderAttachmentDepth(m_BackBufferDepthHandle, AccessFlags.Write);
            }
            
            // 阴影
            if (m_MainLightShadowHandle.IsValid())
            {
                rgBuilder.UseTexture(m_MainLightShadowHandle, AccessFlags.Read);
            }
            
            // 设置全局渲染状态
            rgBuilder.AllowPassCulling(false);
            rgBuilder.AllowGlobalStateModification(true);
            
            // 设置渲染函数
            rgBuilder.SetRenderFunc<TransparentObjectPassData>((transPassData, rgContext) =>
            {
                rgContext.cmd.SetGlobalFloat(ShaderPropertyID.alphaToMaskAvailable, 1.0f);
                rgContext.cmd.DrawRendererList(transPassData.transparentRendererListHandle);
            });
        }
    }
}