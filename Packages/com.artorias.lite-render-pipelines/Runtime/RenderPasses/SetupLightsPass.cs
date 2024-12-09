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
            SetupMainLightConstants(cmdBuffer, lightData);
        }
        
        private void SetupMainLightConstants(UnsafeCommandBuffer cmd, LightData lightData)
        {
            Vector4 lightPos, lightColor, lightAttenuation, lightSpotDir;
            LightUtils.InitializeLightConstants(lightData.m_VisibleLights, lightData.m_MainLightIndex, out lightPos, out lightColor, out lightAttenuation, out lightSpotDir);
            lightColor.w = 1f;

            cmd.SetGlobalVector(ShaderPropertyID.mainLightPosition, lightPos);
            cmd.SetGlobalVector(ShaderPropertyID.mainLightColor, lightColor);
        }
        
        void SetupAdditionalLightConstants(CommandBuffer cmdBuffer, LightData lightData)
        {
            
        }
    }
}