using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace LiteRP
{
    public partial class LiteRGRecorder
    {
        private static readonly ProfilingSampler s_GeometryPassSampler = new ProfilingSampler("LiteRP.GeometryPass");
        private static readonly ShaderTagId s_ShaderTagID = new ShaderTagId("SRPDefaultUnlit");
        internal class GeometryPassData
        {
            internal TextureHandle backBufferHandle;
            internal RendererListHandle opaqueRendererListHandle;
            internal RendererListHandle transRendererListHandle;
        }

        private void AddGeometryPass(RenderGraph rg, ContextContainer frameData)
        {
            CameraData cameraData = frameData.Get<CameraData>();
            
            using var rgBuilder = rg.AddRasterRenderPass<GeometryPassData>(s_GeometryPassSampler.name, out var passData, s_GeometryPassSampler);
            
            // 声明或引用资源
            // 不透
            RendererListDesc opaqueListDesc = new RendererListDesc(s_ShaderTagID, cameraData.m_CullingResults, cameraData.m_Camera);
            opaqueListDesc.sortingCriteria = SortingCriteria.CommonOpaque;
            opaqueListDesc.renderQueueRange = RenderQueueRange.opaque;
            passData.opaqueRendererListHandle = rg.CreateRendererList(opaqueListDesc);
            rgBuilder.UseRendererList(passData.opaqueRendererListHandle);
            
            // 半透
            RendererListDesc transListDesc = new RendererListDesc(s_ShaderTagID, cameraData.m_CullingResults, cameraData.m_Camera);
            transListDesc.sortingCriteria = SortingCriteria.CommonTransparent;
            transListDesc.renderQueueRange = RenderQueueRange.transparent;
            passData.transRendererListHandle = rg.CreateRendererList(transListDesc);
            rgBuilder.UseRendererList(passData.transRendererListHandle);
            
            // 导入BackBuffer
            passData.backBufferHandle = rg.ImportBackbuffer(BuiltinRenderTextureType.CurrentActive);
            rgBuilder.SetRenderAttachment(passData.backBufferHandle, 0, AccessFlags.Write);
            
            // 设置全局渲染状态
            rgBuilder.AllowPassCulling(false);
            
            // 设置渲染函数
            rgBuilder.SetRenderFunc<GeometryPassData>((geoPassData, rgContext) =>
            {
                rgContext.cmd.DrawRendererList(passData.opaqueRendererListHandle);
                rgContext.cmd.DrawRendererList(passData.transRendererListHandle);
            });
            
        }
    }
}