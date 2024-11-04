using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RendererUtils;

namespace CustomRP
{
    public class GeometryPass
    {
        static readonly ProfilingSampler m_OpaqueSampler = new("CustomRP.OpaquePass");
        static readonly ProfilingSampler m_TransparentSampler = new("CustomRP.TransparentPass");

        private static readonly ShaderTagId[] m_ShaderTagIDs =
        {
            new("SRPDefaultUnlit"),
            new("CustomRPLit"),
        };
        
        RendererListHandle m_RendererList;

        void Render(RenderGraphContext rgContext)
        {
            rgContext.cmd.DrawRendererList(m_RendererList);
            rgContext.renderContext.ExecuteCommandBuffer(rgContext.cmd);
            rgContext.cmd.Clear();
        }

        public static void Record(RenderGraph rg, Camera camera, 
            CullingResults cullingResults, uint renderingLayerMask, bool isOpaque, 
            in CameraRendererTextures renderTextures, in LightResources lightData)
        {
            ProfilingSampler curSampler = isOpaque ? m_OpaqueSampler : m_TransparentSampler;

            using RenderGraphBuilder rgBuilder = rg.AddRenderPass(curSampler.name, out GeometryPass pass, curSampler);

            pass.m_RendererList = rgBuilder.UseRendererList(rg.CreateRendererList(
                new RendererListDesc(m_ShaderTagIDs, cullingResults, camera)
                {
                    sortingCriteria = isOpaque ? SortingCriteria.CommonOpaque : SortingCriteria.CommonTransparent,
                    rendererConfiguration =
                        PerObjectData.ReflectionProbes
                        | PerObjectData.Lightmaps
                        | PerObjectData.ShadowMask
                        | PerObjectData.LightProbe
                        | PerObjectData.OcclusionProbe
                        | PerObjectData.LightProbeProxyVolume
                        | PerObjectData.OcclusionProbeProxyVolume,
                    renderQueueRange = isOpaque ? RenderQueueRange.opaque : RenderQueueRange.transparent,
                    renderingLayerMask = renderingLayerMask
                }));

            rgBuilder.ReadWriteTexture(renderTextures.m_ColorAttachment);
            rgBuilder.ReadWriteTexture(renderTextures.m_DepthAttachment);

            if (!isOpaque)
            {
                if (renderTextures.m_ColorCopyAttachment.IsValid())
                {
                    rgBuilder.ReadTexture(renderTextures.m_ColorCopyAttachment);
                }
                if (renderTextures.m_DepthCopyAttachment.IsValid())
                {
                    rgBuilder.ReadTexture(renderTextures.m_DepthCopyAttachment);
                }
            }

            rgBuilder.ReadBuffer(lightData.m_DirectLightDataBuffer);
            rgBuilder.ReadBuffer(lightData.m_OtherLightDataBuffer);
            
            rgBuilder.SetRenderFunc<GeometryPass>(static (pass, rgContext) => pass.Render(rgContext));
        }
    }
}