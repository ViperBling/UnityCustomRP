using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

using static Unity.Mathematics.math;

namespace CustomRP
{
    public partial class LightingPass
    {
        static readonly ProfilingSampler m_ProfileSampler = new("CustomRP.LightingPass");
        
        const int m_MaxDirectionalLightCount = 4;
        const int m_MaxOtherLightCount = 128;
        
        static readonly int m_DirectLightCountID = Shader.PropertyToID("_DirectionalLightCount");
        static readonly int m_DirectLightDataID = Shader.PropertyToID("_DirectionalLightData");
        static readonly int m_OtherLightCountID = Shader.PropertyToID("_OtherLightCount");
        static readonly int m_OtherLightDataID = Shader.PropertyToID("_OtherLightData");
        
        static readonly DirectionalLightData[] m_DirectionalLightData = new DirectionalLightData[m_MaxDirectionalLightCount];
        static readonly OtherLightData[] m_OtherLightData = new OtherLightData[m_MaxOtherLightCount];

        private BufferHandle m_DirectLightDataBuffer;
        private BufferHandle m_OtherLightDataBuffer;
        
        CullingResults m_CullingResults;
        
        int m_DirectLightCount, m_OtherLightCount;

        void Setup(CullingResults cullingResults, Vector2Int attachmentSize, int renderingLayerMask)
        {
            m_CullingResults = cullingResults;
            
            SetupLights(renderingLayerMask);
        }

        void SetupLights(int renderLayerMask)
        {
            NativeArray<VisibleLight> visibleLights = m_CullingResults.visibleLights;

            m_DirectLightCount = m_OtherLightCount = 0;
            for (int i = 0; i < visibleLights.Length; i++)
            {
                VisibleLight visibleLight = visibleLights[i];
                Light light = visibleLight.light;
                if ((light.renderingLayerMask & renderLayerMask) == 0) continue;

                switch (visibleLight.lightType)
                {
                    case LightType.Directional:
                        if (m_DirectLightCount < m_MaxDirectionalLightCount)
                        {
                            // TODO : Add shadow data
                            m_DirectionalLightData[m_DirectLightCount++] = new DirectionalLightData(ref visibleLight, light, Vector4.zero);
                        }
                        break;
                    case LightType.Point:
                        if (m_OtherLightCount < m_MaxOtherLightCount)
                        {
                            m_OtherLightData[m_OtherLightCount++] = OtherLightData.CreatePointLight(ref visibleLight, light, Vector4.zero);
                        }
                        break;
                    case LightType.Spot:
                        if (m_OtherLightCount < m_MaxOtherLightCount)
                        {
                            m_OtherLightData[m_OtherLightCount++] = OtherLightData.CreateSpotLight(ref visibleLight, light, Vector4.zero);
                        }
                        break;
                }
            }
        }
        
        void Render(RenderGraphContext rgContext)
        {
            CommandBuffer buffer = rgContext.cmd;
            buffer.SetGlobalInt(m_DirectLightCountID, m_DirectLightCount);
            buffer.SetBufferData(m_DirectLightDataBuffer, m_DirectionalLightData, 0, 0, m_DirectLightCount);
            buffer.SetGlobalBuffer(m_DirectLightDataID, m_DirectLightDataBuffer);

            buffer.SetGlobalInt(m_OtherLightCountID, m_OtherLightCount);
            buffer.SetBufferData(m_OtherLightDataBuffer, m_OtherLightData, 0, 0, m_OtherLightCount);
            buffer.SetGlobalBuffer(m_OtherLightDataID, m_OtherLightDataBuffer);
            
            rgContext.renderContext.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }
        
        public static LightResources Record(RenderGraph rg, CullingResults cullingResults, Vector2Int attachmentSize, int renderingLayerMask)
        {
            using RenderGraphBuilder rgBuilder =
                rg.AddRenderPass(m_ProfileSampler.name, out LightingPass pass, m_ProfileSampler);
            
            pass.Setup(cullingResults, attachmentSize, renderingLayerMask);
            pass.m_DirectLightDataBuffer = rgBuilder.WriteBuffer(rg.CreateBuffer(
                new BufferDesc(m_MaxDirectionalLightCount, DirectionalLightData.stride)
                {
                    name = "DirectionalLightDataBuffer",
                }));
            pass.m_OtherLightDataBuffer = rgBuilder.WriteBuffer(rg.CreateBuffer(
                new BufferDesc(m_MaxOtherLightCount, OtherLightData.m_Stride)
                {
                    name = "OtherLightDataBuffer",
                }));
            
            rgBuilder.SetRenderFunc<LightingPass>(static (pass, rgContext) => pass.Render(rgContext));
            rgBuilder.AllowPassCulling(false);
            return new LightResources(pass.m_DirectLightDataBuffer, pass.m_OtherLightDataBuffer);
        }
    }
}