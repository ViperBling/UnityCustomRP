using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

using LiteRP.FrameData;

namespace LiteRP
{
    public class LiteRenderPipeline : RenderPipeline
    {
        public const string k_ShaderTagName = "LiteRenderPipeline";
        
        private LiteRPAsset m_RPAsset = null;
        private RenderGraph m_RenderGraph = null;
        private LiteRGRecorder m_LiteRGRecorder = null;
        private ContextContainer m_ContextContainer = null;
        
        public static LiteRPAsset RPAsset
        {
            get => GraphicsSettings.currentRenderPipeline as LiteRPAsset;
        }

        public LiteRenderPipeline(LiteRPAsset rpAsset)
        {
            m_RPAsset = rpAsset;
            InitSupportedRenderingFeatures(rpAsset);
            InitializeRPSettings();
            InitializeRenderGraph();
        }
        
        protected override void Dispose(bool bDispose)
        {
            CleanupRenderGraph();
            base.Dispose(bDispose);
        }
        
        private void InitializeRPSettings()
        {
            GraphicsSettings.useScriptableRenderPipelineBatching = m_RPAsset.UseSRPBatcher;
            QualitySettings.antiAliasing = m_RPAsset.AntiAliasing;
        }

        // Older version
        protected override void Render(ScriptableRenderContext context, Camera[] cameras) { }

        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            CheckGlobalRenderingSettings();
            
            BeginContextRendering(context, cameras);
            
            for (int i = 0; i < cameras.Count; i++)
            {
                RenderCamera(context, cameras[i]);
            }
            // RG结束当前帧
            m_RenderGraph.EndFrame();
            
            EndContextRendering(context, cameras);
        }
        
        private void RenderCamera(ScriptableRenderContext context, Camera camera)
        {
            BeginCameraRendering(context, camera);
            
            // 准备FrameData
            if (!PrepareFrameData(context, camera)) return;
            
            // 为相机创建CommandBuffer
            CommandBuffer cmdBuffer = CommandBufferPool.Get();
            
            SetupPerCameraShaderConstants(cmdBuffer);
            
            RecordAndExecuteRenderGraph(context, camera, cmdBuffer);
            
            // 提交渲染命令
            context.ExecuteCommandBuffer(cmdBuffer);
            // 释放CommandBuffer
            cmdBuffer.Clear();
            
            CommandBufferPool.Release(cmdBuffer);
            
            // 提交渲染上下文
            context.Submit();
            
            EndCameraRendering(context, camera);
        }
        
        private void CheckGlobalRenderingSettings()
        {
            GraphicsSettings.lightsUseLinearIntensity = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            GraphicsSettings.lightsUseColorTemperature = true;
        }


        private void InitializeRenderGraph()
        {
            RTHandles.Initialize(Screen.width, Screen.height);
            m_RenderGraph = new RenderGraph("LiteRPRenderGraph");
            m_RenderGraph.nativeRenderPassesEnabled = LiteRPRenderGraphUtils.IsNativeRenderPassSupport();
            m_LiteRGRecorder = new LiteRGRecorder();
            m_ContextContainer = new ContextContainer();
        }
        
        private void CleanupRenderGraph()
        {
            m_ContextContainer?.Dispose();
            m_ContextContainer = null;
            m_LiteRGRecorder?.Dispose();
            m_LiteRGRecorder = null;
            m_RenderGraph?.Cleanup();
            m_RenderGraph = null;
        }
        
        private void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters, Camera camera)
        {
            float maxShadowDistance = Mathf.Min(m_RPAsset.mainLightShadowDistance, camera.farClipPlane);
            bool isShadowCastingDisabled = !m_RPAsset.mainLightShadowEnabled;
            bool isShadowDistanceZero = Mathf.Approximately(maxShadowDistance, 0.0f);
            if (isShadowCastingDisabled || isShadowDistanceZero)
            {
                cullingParameters.cullingOptions &= ~CullingOptions.ShadowCasters;
            }
            cullingParameters.maximumVisibleLights = 1;     //只有主光源
            cullingParameters.shadowDistance = maxShadowDistance;

            //设置保守剔除
            cullingParameters.conservativeEnclosingSphere = m_RPAsset.conservativeEnclosingSphere;
            cullingParameters.numIterationsEnclosingSphere = m_RPAsset.numIterationsEnclosingSphere;
        }
        
        private bool PrepareFrameData(ScriptableRenderContext context, Camera camera)
        {
            ScriptableCullingParameters cullingParameters;
            if (!camera.TryGetCullingParameters(out cullingParameters)) return false;
            SetupCullingParameters(ref cullingParameters, camera);
            CullingResults cullingResults = context.Cull(ref cullingParameters);
            
            CameraData cameraData = m_ContextContainer.GetOrCreate<CameraData>();
            cameraData.m_Camera = camera;
            cameraData.m_CullingResults = cullingResults;
            bool anyShadowsEnabled = m_RPAsset.mainLightShadowEnabled;
            cameraData.m_MaxShadowDistance = Mathf.Min(m_RPAsset.mainLightShadowDistance, camera.farClipPlane);
            cameraData.m_MaxShadowDistance = (anyShadowsEnabled && cameraData.m_MaxShadowDistance >= camera.nearClipPlane) ? cameraData.m_MaxShadowDistance : 0.0f;
            
            //初始化灯光帧数据
            LightData lightData = m_ContextContainer.GetOrCreate<LightData>();
            var visibleLights = cullingResults.visibleLights;
            lightData.m_MainLightIndex = LightUtils.GetMainLightIndex(visibleLights);
            lightData.m_AdditionalLightsCount = Math.Min((lightData.m_MainLightIndex != -1) ? visibleLights.Length - 1 : visibleLights.Length, LightUtils.maxVisibleAdditionalLights);
            // lightData.m_MaxPerObjectAdditionalLightsCount = Math.Min(m_RPAsset.maxAdd, LiteRPAsset.k_MaxPerObjectLights);
            lightData.m_MaxPerObjectAdditionalLightsCount = 0;
            lightData.m_VisibleLights = visibleLights;
            
            //初始化阴影帧数据
            ShadowData shadowData = m_ContextContainer.GetOrCreate<ShadowData>();
            // maxShadowDistance is set to 0.0f when the Render Shadows toggle is disabled on the camera
            bool cameraRenderShadows = cameraData.m_MaxShadowDistance > 0.0f;      
            shadowData.m_MainLightShadowEnable = anyShadowsEnabled;
            shadowData.m_SupportMainLightShadow = SystemInfo.supportsShadows && shadowData.m_MainLightShadowEnable && cameraRenderShadows;
            shadowData.m_MainLightShadowDistance = cullingParameters.shadowDistance;
            
            shadowData.m_ShadowmapDepthBits = 16;
            shadowData.m_MainLightShadowCascadeBorder = m_RPAsset.mainLightCascadeBorder;
            shadowData.m_MainLightShadowCascadesCount = m_RPAsset.mainLightShadowCascadesCount;
            shadowData.m_MainLightShadowCascadesSplit = ShadowUtils.GetMainLightCascadeSplit(shadowData.m_MainLightShadowCascadesCount, m_RPAsset);
            shadowData.m_MainLightShadowmapWidth = m_RPAsset.mainLightShadowmapResolution;
            shadowData.m_MainLightShadowmapHeight = m_RPAsset.mainLightShadowmapResolution;
            ShadowUtils.CreateShadowAtlasAndCullShadowCasters(shadowData, ref cameraData.m_CullingResults, ref context);
            
            var mainLightIndex = lightData.m_MainLightIndex;
            if (mainLightIndex < 0)     //注意这里小于0的情况
                return true;
            
            VisibleLight vl = visibleLights[mainLightIndex];
            Light light = vl.light;
            shadowData.m_SupportMainLightShadow &= mainLightIndex != -1 && light != null && light.shadows != LightShadows.None;
            
            if (!shadowData.m_SupportMainLightShadow)
            {
                shadowData.m_MainLightShadowBias = Vector4.zero;
                shadowData.m_MainLightShadowResolution = 0;
            }
            else
            {
                // // 初始化灯光附加管线数据
                // AdditionalLightData data = null;
                // if (light != null)
                //     light.gameObject.TryGetComponent(out data);
                // if (data && !data.usePipelineSettings)
                //     shadowData.mainLightShadowBias = new Vector4(light.shadowBias, light.shadowNormalBias, 0.0f, 0.0f);  
                // else
                //     shadowData.mainLightShadowBias = new Vector4(m_RPAsset.mainLightShadowDepthBias, m_RPAsset.mainLightShadowNormalBias, 0.0f, 0.0f);
                // shadowData.mainLightShadowmapResolution = m_RPAsset.mainLightShadowmapResolution;
            }
            shadowData.m_SupportSoftShadows = m_RPAsset.supportsSoftShadows && shadowData.m_SupportMainLightShadow;
            
            return true;
        }
        
        private void SetupPerCameraShaderConstants(CommandBuffer cmd)
        {
            // When glossy reflections are OFF in the shader we set a constant color to use as indirect specular
            SphericalHarmonicsL2 ambientSH = RenderSettings.ambientProbe;
            Color linearGlossyEnvColor = new Color(ambientSH[0, 0], ambientSH[1, 0], ambientSH[2, 0]) * RenderSettings.reflectionIntensity;
            Color glossyEnvColor = CoreUtils.ConvertLinearToActiveColorSpace(linearGlossyEnvColor);
            cmd.SetGlobalVector(ShaderPropertyID.glossyEnvironmentColor, glossyEnvColor);
            
            Vector4 unity_SHAr = new Vector4(ambientSH[0, 3], ambientSH[0, 1], ambientSH[0, 2], ambientSH[0, 0] - ambientSH[0, 6]);
            Vector4 unity_SHAg = new Vector4(ambientSH[1, 3], ambientSH[1, 1], ambientSH[1, 2], ambientSH[1, 0] - ambientSH[1, 6]);
            Vector4 unity_SHAb = new Vector4(ambientSH[2, 3], ambientSH[2, 1], ambientSH[2, 2], ambientSH[2, 0] - ambientSH[2, 6]);
            
            Vector4 unity_SHBr = new Vector4(ambientSH[0, 4], ambientSH[0, 6], ambientSH[0, 5] * 3, ambientSH[0, 7]);
            Vector4 unity_SHBg = new Vector4(ambientSH[1, 4], ambientSH[1, 6], ambientSH[1, 5] * 3, ambientSH[1, 7]);
            Vector4 unity_SHBb = new Vector4(ambientSH[2, 4], ambientSH[2, 6], ambientSH[2, 5] * 3, ambientSH[2, 7]);
            
            Vector4 unity_SHC = new Vector4(ambientSH[0, 8], ambientSH[2, 8], ambientSH[1, 8], 1);
            
            cmd.SetGlobalVector(ShaderPropertyID.shAr, unity_SHAr);
            cmd.SetGlobalVector(ShaderPropertyID.shAg, unity_SHAg);
            cmd.SetGlobalVector(ShaderPropertyID.shAb, unity_SHAb);
            cmd.SetGlobalVector(ShaderPropertyID.shBr, unity_SHBr);
            cmd.SetGlobalVector(ShaderPropertyID.shBg, unity_SHBg);
            cmd.SetGlobalVector(ShaderPropertyID.shBb, unity_SHBb);
            cmd.SetGlobalVector(ShaderPropertyID.shC, unity_SHC);
            
            // Used as fallback cubemap for reflections
            cmd.SetGlobalTexture(ShaderPropertyID.glossyEnvironmentCubeMap, ReflectionProbe.defaultTexture);
            cmd.SetGlobalVector(ShaderPropertyID.glossyEnvironmentCubeMapHDR, ReflectionProbe.defaultTextureHDRDecodeValues);

            // Ambient
            cmd.SetGlobalVector(ShaderPropertyID.ambientSkyColor, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.ambientSkyColor));
            cmd.SetGlobalVector(ShaderPropertyID.ambientEquatorColor, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.ambientEquatorColor));
            cmd.SetGlobalVector(ShaderPropertyID.ambientGroundColor, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.ambientGroundColor));
        }
        
        private void RecordAndExecuteRenderGraph(ScriptableRenderContext context, Camera camera, CommandBuffer cmdBuffer)
        {
            RenderGraphParameters rgParams = new RenderGraphParameters()
            {
                executionName = camera.name,
                commandBuffer = cmdBuffer,
                scriptableRenderContext = context,
                currentFrameIndex = Time.frameCount
            };
            
            m_RenderGraph.BeginRecording(rgParams);
            
            // 开启RenderGraph的记录线
            m_LiteRGRecorder.RecordRenderGraph(m_RenderGraph, m_ContextContainer);
            
            m_RenderGraph.EndRecordingAndExecute();
        }
        
        static void InitSupportedRenderingFeatures(LiteRPAsset pipelineAsset)
        {
#if UNITY_EDITOR
            SupportedRenderingFeatures.active = new SupportedRenderingFeatures()
            {
                ambientProbeBaking = true,
                defaultMixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeModes.Subtractive,
                defaultReflectionProbeBaking = true,
                editableMaterialRenderQueue = true,
                enlighten = true,
                lightmapBakeTypes = LightmapBakeType.Baked | LightmapBakeType.Mixed | LightmapBakeType.Realtime,
                lightmapsModes = LightmapsMode.CombinedDirectional | LightmapsMode.NonDirectional,
                lightProbeProxyVolumes = false,
                mixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeModes.Subtractive | SupportedRenderingFeatures.LightmapMixedBakeModes.IndirectOnly | SupportedRenderingFeatures.LightmapMixedBakeModes.Shadowmask,
                motionVectors = false,
                overridesEnableLODCrossFade = true,
                overridesEnvironmentLighting = false,
                overridesFog = false,
                overridesLightProbeSystem = false,
                overridesLODBias = false,
                overridesMaximumLODLevel = false,
                overridesOtherLightingSettings = false,
                overridesRealtimeReflectionProbes = false,
                overridesShadowmask = false,
                particleSystemInstancing = true,
                receiveShadows = false,
                reflectionProbeModes = SupportedRenderingFeatures.ReflectionProbeModes.None,
                reflectionProbes = false,
                reflectionProbesBlendDistance = false,
                rendererPriority = false,
                rendererProbes = true,
                rendersUIOverlay = false,
                skyOcclusion = false,
                supportsClouds = false,
                supportsHDR = false
            };
#endif

            // SupportedRenderingFeatures.active.supportsHDR = pipelineAsset.supportsHDR;
            SupportedRenderingFeatures.active.rendersUIOverlay = false;
        }

        static void ResetSupportedRenderingFeatures()
        {
            SupportedRenderingFeatures.active = new SupportedRenderingFeatures();
        }
    }
}