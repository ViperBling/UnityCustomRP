using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

using LiteRP.FrameData;

namespace LiteRP
{
    public partial class LiteRGRecorder
    {
        private static readonly ProfilingSampler s_SetupLightsPassSampler = new ProfilingSampler("LiteRP.SetupLightsPass");
        
        private Vector4[] m_AdditionalLightPositions;
        private Vector4[] m_AdditionalLightColors;
        private Vector4[] m_AdditionalLightAttenuation;
        private Vector4[] m_AdditionalLightSpotDirections;
        
        internal class SetupLightsPassData
        {
            internal CameraData cameraData;
            internal LightData lightData;
        }
        
        private void AddSetupLightsPass(RenderGraph rg, CameraData cameraData, LightData lightData)
        {
            using var rgBuilder = rg.AddUnsafePass<SetupLightsPassData>(s_SetupLightsPassSampler.name, out var passData, s_SetupLightsPassSampler);

            passData.cameraData = cameraData;
            passData.lightData = lightData;

            rgBuilder.AllowPassCulling(false);
            
            rgBuilder.SetRenderFunc((SetupLightsPassData data, UnsafeGraphContext rgContext) =>
            {
                SetLightsShaderVariables(rgContext.cmd, data.cameraData, data.lightData);
            });
        }
        
        private void SetLightsShaderVariables(UnsafeCommandBuffer cmdBuffer, CameraData cameraData, LightData lightData)
        {
            int additionalLightCount = lightData.m_AdditionalLightsCount;
            SetupMainLightConstants(cmdBuffer, lightData);
            SetupAdditionalLightConstants(cmdBuffer, ref cameraData.m_CullingResults, lightData);
            
            bool lightCountCheck =  additionalLightCount > 0;  //lwwhb 可能添加其他条件，如是否剔除了多光源Shader变体
            cmdBuffer.SetKeyword(ShaderGlobalKeywords.AdditionalLights,  lightCountCheck);
        }
        
        private void SetupMainLightConstants(UnsafeCommandBuffer cmd, LightData lightData)
        {
            Vector4 lightPos, lightColor, lightAttenuation, lightSpotDir;
            LightUtils.InitializeLightConstants(lightData.m_VisibleLights, lightData.m_MainLightIndex, out lightPos, out lightColor, out lightAttenuation, out lightSpotDir);
            lightColor.w = 1f;

            cmd.SetGlobalVector(ShaderPropertyID.mainLightPosition, lightPos);
            cmd.SetGlobalVector(ShaderPropertyID.mainLightColor, lightColor);
        }
        
        void SetupAdditionalLightConstants(UnsafeCommandBuffer cmdBuffer, ref CullingResults cullResults, LightData lightData)
        {
            var lights = lightData.m_VisibleLights;
            int maxAdditionalLightsCount = LightUtils.maxVisibleAdditionalLights;
            int additionalLightsCount = SetupPerObjectLightIndices(cullResults, lightData);
            if (additionalLightsCount > 0)
            {
                for (int i = 0, lightIter = 0; i < lights.Length && lightIter < maxAdditionalLightsCount; ++i)
                {
                    if (lightData.m_MainLightIndex != i)
                    {
                        LightUtils.InitializeLightConstants(
                            lights,
                            i,
                            out m_AdditionalLightPositions[lightIter],
                            out m_AdditionalLightColors[lightIter],
                            out m_AdditionalLightAttenuation[lightIter],
                            out m_AdditionalLightSpotDirections[lightIter]);
                        m_AdditionalLightColors[lightIter].w = 0f;
                        lightIter++;
                    }
                }

                cmdBuffer.SetGlobalVectorArray(ShaderPropertyID.additionalLightsPosition, m_AdditionalLightPositions);
                cmdBuffer.SetGlobalVectorArray(ShaderPropertyID.additionalLightsColor, m_AdditionalLightColors);
                cmdBuffer.SetGlobalVectorArray(ShaderPropertyID.additionalLightsAttenuation, m_AdditionalLightAttenuation);
                cmdBuffer.SetGlobalVectorArray(ShaderPropertyID.additionalLightsSpotDir, m_AdditionalLightSpotDirections);

                cmdBuffer.SetGlobalVector(ShaderPropertyID.additionalLightsCount, new Vector4(lightData.m_MaxPerObjectAdditionalLightsCount, 0.0f, 0.0f, 0.0f));
            }
            else
            {
                cmdBuffer.SetGlobalVector(ShaderPropertyID.additionalLightsCount, Vector4.zero);
            }
        }
        
        int SetupPerObjectLightIndices(CullingResults cullResults, LightData lightData)
        {
            if (lightData.m_AdditionalLightsCount == 0) return lightData.m_AdditionalLightsCount;

            var perObjectLightIndexMap = cullResults.GetLightIndexMap(Allocator.Temp);
            int globalDirectionalLightsCount = 0;
            int additionalLightsCount = 0;

            // Disable all directional lights from the perobject light indices
            // Pipeline handles main light globally and there's no support for additional directional lights atm.
            int maxVisibleAdditionalLightsCount = LightUtils.maxVisibleAdditionalLights;
            int len = lightData.m_VisibleLights.Length;
            for (int i = 0; i < len; ++i)
            {
                if (additionalLightsCount >= maxVisibleAdditionalLightsCount)
                    break;

                if (i == lightData.m_MainLightIndex)
                {
                    perObjectLightIndexMap[i] = -1;
                    ++globalDirectionalLightsCount;
                }
                else
                {
                    if (lightData.m_VisibleLights[i].lightType == LightType.Directional ||
                        lightData.m_VisibleLights[i].lightType == LightType.Spot ||
                        lightData.m_VisibleLights[i].lightType == LightType.Point)
                    {
                        // Light type is supported
                        perObjectLightIndexMap[i] -= globalDirectionalLightsCount;
                    }
                    else
                    {
                        // Light type is not supported. Skip the light.
                        perObjectLightIndexMap[i] = -1;
                    }

                    ++additionalLightsCount;
                }
            }

            // Disable all remaining lights we cannot fit into the global light buffer.
            for (int i = globalDirectionalLightsCount + additionalLightsCount; i < perObjectLightIndexMap.Length; ++i)
                perObjectLightIndexMap[i] = -1;

            cullResults.SetLightIndexMap(perObjectLightIndexMap);
            
            perObjectLightIndexMap.Dispose();
            return additionalLightsCount;
        }
    }
}