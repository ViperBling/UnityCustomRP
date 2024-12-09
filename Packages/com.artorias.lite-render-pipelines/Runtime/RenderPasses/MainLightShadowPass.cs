using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

using LiteRP.FrameData;

namespace LiteRP
{
    public partial class LiteRGRecorder
    {
        private static readonly ProfilingSampler s_MainLightShadowPassSampler = new ProfilingSampler("LiteRP.MainLightShadowPass");
        private const string k_MainLightShadowmapTextureName = "_MainLightShadowmapTexture";
        private const int k_MaxCascadeCount = 4;
        private const int k_ShadowmapBufferBits = 16;
        
        private TextureHandle m_MainLightShadowHandle = TextureHandle.nullHandle;
        private RTHandle m_MainLightShadowmapTexture = null;

        private Matrix4x4[] m_MainLightShadowMatrices;
        private ShadowSliceData[] m_CascadeSlices;
        private Vector4[] m_CascadeSplitDistances;

        internal class MainLightShadowPassData
        {
            internal int mainLightIndex;
            internal Vector3 worldSpaceCameraPos;
            internal ShadowData shadowData;
            
            internal RendererListHandle[] shadowRendererListHandles = new RendererListHandle[k_MaxCascadeCount];
        }

        void Clear()
        {
            for (int i = 0; i < m_MainLightShadowMatrices.Length; i++)
            {
                m_MainLightShadowMatrices[i] = Matrix4x4.identity;
            }
            for (int i = 0; i < m_CascadeSplitDistances.Length; i++)
            {
                m_CascadeSplitDistances[i] = Vector4.zero;
            }
            for (int i = 0; i < m_CascadeSlices.Length; i++)
            {
                m_CascadeSlices[i].Clear();
            }
        }

        private void InitializeMainLightShadowPass()
        {
            m_MainLightShadowMatrices = new Matrix4x4[k_MaxCascadeCount + 1];
            m_CascadeSlices = new ShadowSliceData[k_MaxCascadeCount];
            m_CascadeSplitDistances = new Vector4[k_MaxCascadeCount];
        }

        private bool NeedMainLightShadowPass(CameraData cameraData, LightData lightData, ShadowData shadowData)
        {
            if (!shadowData.m_MainLightShadowEnable) return false;
            if (!shadowData.m_SupportMainLightShadow) return false;

            int shadowLightIndex = lightData.m_MainLightIndex;
            if (shadowLightIndex == -1) return false;

            VisibleLight shadowLight = lightData.m_VisibleLights[shadowLightIndex];
            Light light = shadowLight.light;
            if (light.shadows == LightShadows.None) return false;
            
            if (shadowLight.lightType != LightType.Directional) return false;

            Bounds bounds;
            if (!cameraData.m_CullingResults.GetShadowCasterBounds(shadowLightIndex, out bounds)) return false;
            
            Clear();
            ref readonly LightShadowCullingInfos shadowCullingInfos = ref shadowData.m_VisibleLightsShadowCullingInfos.UnsafeElementAt(shadowLightIndex);
            for (int cascadeIndex = 0; cascadeIndex < shadowData.m_MainLightShadowCascadesCount; ++cascadeIndex)
            {
                if (shadowCullingInfos.IsSliceValid(cascadeIndex))
                {
                    ref readonly ShadowSliceData sliceData = ref shadowCullingInfos.shadowSlices.UnsafeElementAt(cascadeIndex);
                    m_CascadeSplitDistances[cascadeIndex] = sliceData.splitData.cullingSphere;
                    m_CascadeSlices[cascadeIndex] = sliceData;
                }
            }

            ShadowUtils.ShadowRTReAllocateIfNeed(ref m_MainLightShadowmapTexture, shadowData.m_MainLightRenderTargetWidth, shadowData.m_MainLightRenderTargetHeight, k_ShadowmapBufferBits, name: k_MainLightShadowmapTextureName);
            return true;
        }
        
        private void ReleaseMainLightShadowPass()
        {
            m_MainLightShadowmapTexture?.Release();
        }

        private void SetupMainLightShadowReceiverConstants(RasterCommandBuffer rsCmdBuffer, ref VisibleLight shadowLight, ShadowData shadowData)
        {
            Light light = shadowLight.light;
            bool softShadowsSupported = shadowLight.light.shadows == LightShadows.Soft && shadowData.m_SupportSoftShadows;
            
            int cascadeCount = shadowData.m_MainLightShadowCascadesCount;
            for (int i = 0; i < cascadeCount; i++) m_MainLightShadowMatrices[i] = m_CascadeSlices[i].shadowTransform;
            
            // We setup and additional a no-op WorldToShadow matrix in the last index
            // because the ComputeCascadeIndex function in Shadows.hlsl can return an index
            // out of bounds. (position not inside any cascade) and we want to avoid branching
            Matrix4x4 noOpShadowMatrix = Matrix4x4.zero;
            noOpShadowMatrix.m22 = (SystemInfo.usesReversedZBuffer) ? 1.0f : 0.0f;
            for (int i = cascadeCount; i <= k_MaxCascadeCount; ++i) m_MainLightShadowMatrices[i] = noOpShadowMatrix;

            int renderTargetWidth = shadowData.m_MainLightShadowmapWidth;
            int renderTargetHeight = shadowData.m_MainLightShadowmapHeight;
            float invShadowAtlasWidth = 1.0f / renderTargetWidth;
            float invShadowAtlasHeight = 1.0f / renderTargetHeight;
            float invHalfShadowAtlasWidth = 0.5f * invShadowAtlasWidth;
            float invHalfShadowAtlasHeight = 0.5f * invShadowAtlasHeight;
            float softShadowProp = ShadowUtils.SoftShadowQualityToShaderProperty(light, softShadowsSupported);
            
            float maxShadowDistanceSq = shadowData.m_MainLightShadowDistance * shadowData.m_MainLightShadowDistance;
            ShadowUtils.GetScaleAndBiasForLinearDistanceFade(maxShadowDistanceSq, shadowData.m_MainLightShadowCascadeBorder, out float shadowFadeScale, out float shadowFadeBias);
            
            rsCmdBuffer.SetGlobalMatrixArray(ShaderPropertyID.mainLightWorldToShadow, m_MainLightShadowMatrices);
            rsCmdBuffer.SetGlobalVector(ShaderPropertyID.mainLightShadowParams, new Vector4(light.shadowStrength, softShadowProp, shadowFadeScale, shadowFadeBias));

            if (cascadeCount > 1)
            {
                rsCmdBuffer.SetGlobalVector(ShaderPropertyID.mainLightCascadeShadowSplitSpheres0, m_CascadeSplitDistances[0]);
                rsCmdBuffer.SetGlobalVector(ShaderPropertyID.mainLightCascadeShadowSplitSpheres1, m_CascadeSplitDistances[1]);
                rsCmdBuffer.SetGlobalVector(ShaderPropertyID.mainLightCascadeShadowSplitSpheres2, m_CascadeSplitDistances[2]);
                rsCmdBuffer.SetGlobalVector(ShaderPropertyID.mainLightCascadeShadowSplitSpheres3, m_CascadeSplitDistances[3]);
                rsCmdBuffer.SetGlobalVector(ShaderPropertyID.mainLightCascadeShadowSplitSphereRadii, new Vector4(
                    m_CascadeSplitDistances[0].w * m_CascadeSplitDistances[0].w,
                    m_CascadeSplitDistances[1].w * m_CascadeSplitDistances[1].w,
                    m_CascadeSplitDistances[2].w * m_CascadeSplitDistances[2].w,
                    m_CascadeSplitDistances[3].w * m_CascadeSplitDistances[3].w));
            }
            
            // Inside shader soft shadows are controlled through global keyword.
            // If any additional light has soft shadows it will force soft shadows on main light too.
            // As it is not trivial finding out which additional light has soft shadows, we will pass main light properties if soft shadows are supported.
            // This workaround will be removed once we will support soft shadows per light.
            if (shadowData.m_SupportSoftShadows)
            {
                rsCmdBuffer.SetGlobalVector(ShaderPropertyID.mainLightShadowOffset0,
                    new Vector4(-invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight,
                                invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight));
                rsCmdBuffer.SetGlobalVector(ShaderPropertyID.mainLightShadowOffset1,
                    new Vector4(-invHalfShadowAtlasWidth, invHalfShadowAtlasHeight,
                                invHalfShadowAtlasWidth, invHalfShadowAtlasHeight));

                rsCmdBuffer.SetGlobalVector(ShaderPropertyID.mainLightShadowmapSize, new Vector4(invShadowAtlasWidth, invShadowAtlasHeight, renderTargetWidth, renderTargetHeight));
            }
        }

        private void AddMainLightShadowmapPass(RenderGraph rg, CameraData cameraData, LightData lightData, ShadowData shadowData)
        {
            using var rgBuilder = rg.AddRasterRenderPass<MainLightShadowPassData>(s_MainLightShadowPassSampler.name, out var passData, s_MainLightShadowPassSampler);
            
            passData.mainLightIndex = lightData.m_MainLightIndex;
            passData.worldSpaceCameraPos = cameraData.m_Camera.transform.position;
            passData.shadowData = shadowData;

            var settings = new ShadowDrawingSettings(cameraData.m_CullingResults, passData.mainLightIndex);
            settings.useRenderingLayerMaskTest = false;
            for (int cascadeIndex = 0; cascadeIndex < shadowData.m_MainLightShadowCascadesCount; ++cascadeIndex)
            {
                passData.shadowRendererListHandles[cascadeIndex] = rg.CreateShadowRendererList(ref settings);
                rgBuilder.UseRendererList(passData.shadowRendererListHandles[cascadeIndex]);
            }
            
            m_MainLightShadowHandle = LiteRPRenderGraphUtils.CreateRenderGraphTexture(rg, m_MainLightShadowmapTexture.rt.descriptor, k_MainLightShadowmapTextureName, true, ShadowUtils.m_ForceShadowPointSampling ? FilterMode.Point : FilterMode.Bilinear);
            rgBuilder.SetRenderAttachmentDepth(m_MainLightShadowHandle, AccessFlags.Write);
            
            rgBuilder.AllowPassCulling(false);
            rgBuilder.AllowGlobalStateModification(true);

            if (m_MainLightShadowHandle.IsValid())
            {
                rgBuilder.SetGlobalTextureAfterPass(m_MainLightShadowHandle, ShaderPropertyID.mainLightShadowmap);
            }

            rgBuilder.SetRenderFunc<MainLightShadowPassData>((shadowPassData, rgContext) =>
            {
                int shadowLightIndex = shadowPassData.mainLightIndex;
                if (shadowLightIndex == -1) return;
                
                VisibleLight shadowLight = lightData.m_VisibleLights[shadowLightIndex];
                
                rgContext.cmd.SetGlobalVector(ShaderPropertyID.worldSpaceCameraPos, shadowPassData.worldSpaceCameraPos);
                for (int cascadeIndex = 0; cascadeIndex < shadowPassData.shadowData.m_MainLightShadowCascadesCount; ++cascadeIndex)
                {
                    var shadowSliceData = m_CascadeSlices[cascadeIndex];
                    Vector4 shadowBias = ShadowUtils.GeMainLightShadowBias(ref shadowLight, shadowPassData.shadowData.m_MainLightShadowBias, shadowPassData.shadowData.m_SupportSoftShadows, shadowSliceData.projectionMatrix, shadowSliceData.resolution);
                    rgContext.cmd.SetGlobalVector(ShaderPropertyID.shadowBias, shadowBias);

                    // Light direction is currently used in shadow caster pass to apply shadow normal offset (normal bias).
                    Vector3 lightDirection = -shadowLight.localToWorldMatrix.GetColumn(2);
                    rgContext.cmd.SetGlobalVector(ShaderPropertyID.lightDirection, new Vector4(lightDirection.x, lightDirection.y, lightDirection.z, 0.0f));

                    // For punctual lights, computing light direction at each vertex position provides more consistent results (shadow shape does not change when "rotating the point light" for example)
                    Vector3 lightPosition = shadowLight.localToWorldMatrix.GetColumn(3);
                    rgContext.cmd.SetGlobalVector(ShaderPropertyID.lightPosition, new Vector4(lightPosition.x, lightPosition.y, lightPosition.z, 1.0f));
                    
                    // 绘制Shadow RenderList
                    RendererListHandle shadowRendererListHandle = shadowPassData.shadowRendererListHandles[cascadeIndex];
                    rgContext.cmd.SetGlobalDepthBias(1.0f, 2.5f); // these values match HDRP defaults (see https://github.com/Unity-Technologies/Graphics/blob/9544b8ed2f98c62803d285096c91b44e9d8cbc47/com.unity.render-pipelines.high-definition/Runtime/Lighting/Shadow/HDShadowAtlas.cs#L197 )
                    rgContext.cmd.SetViewport(new Rect(shadowSliceData.offsetX, shadowSliceData.offsetY, shadowSliceData.resolution, shadowSliceData.resolution));
                    rgContext.cmd.SetViewProjectionMatrices(shadowSliceData.viewMatrix, shadowSliceData.projectionMatrix);
                    if(shadowRendererListHandle.IsValid())
                    {
                        rgContext.cmd.DrawRendererList(shadowRendererListHandle);
                    }
                    rgContext.cmd.DisableScissorRect();
                    rgContext.cmd.SetGlobalDepthBias(0.0f, 0.0f); // Restore previous depth bias values
                }
                    
                // 设置阴影Shader关键字
                bool isKeywordSoftShadowsEnabled = shadowLight.light.shadows == LightShadows.Soft && shadowPassData.shadowData.m_SupportSoftShadows;
                rgContext.cmd.SetKeyword(ShaderGlobalKeywords.MainLightShadows, shadowPassData.shadowData.m_MainLightShadowCascadesCount == 1);
                rgContext.cmd.SetKeyword(ShaderGlobalKeywords.MainLightShadowCascades, shadowPassData.shadowData.m_MainLightShadowCascadesCount > 1);
                
                rgContext.cmd.SetKeyword(ShaderGlobalKeywords.SoftShadows, isKeywordSoftShadowsEnabled);
                if (isKeywordSoftShadowsEnabled && LiteRPUtils.s_LiteRPAsset?.softShadowQuality == SoftShadowQuality.Low)
                {
                    rgContext.cmd.SetKeyword(ShaderGlobalKeywords.SoftShadowsLow, true);
                    rgContext.cmd.SetKeyword(ShaderGlobalKeywords.SoftShadowsMedium, false);
                    rgContext.cmd.SetKeyword(ShaderGlobalKeywords.SoftShadowsHigh, false);
                    rgContext.cmd.SetKeyword(ShaderGlobalKeywords.SoftShadows, false);
                }
                else if (isKeywordSoftShadowsEnabled && LiteRPUtils.s_LiteRPAsset?.softShadowQuality == SoftShadowQuality.Medium)
                {
                    rgContext.cmd.SetKeyword(ShaderGlobalKeywords.SoftShadowsLow, false);
                    rgContext.cmd.SetKeyword(ShaderGlobalKeywords.SoftShadowsMedium, true);
                    rgContext.cmd.SetKeyword(ShaderGlobalKeywords.SoftShadowsHigh, false);
                    rgContext.cmd.SetKeyword(ShaderGlobalKeywords.SoftShadows, false);
                }
                else if (isKeywordSoftShadowsEnabled && LiteRPUtils.s_LiteRPAsset?.softShadowQuality == SoftShadowQuality.High)
                {
                    rgContext.cmd.SetKeyword(ShaderGlobalKeywords.SoftShadowsLow, false);
                    rgContext.cmd.SetKeyword(ShaderGlobalKeywords.SoftShadowsMedium, false);
                    rgContext.cmd.SetKeyword(ShaderGlobalKeywords.SoftShadowsHigh, true);
                    rgContext.cmd.SetKeyword(ShaderGlobalKeywords.SoftShadows, false);
                }

                SetupMainLightShadowReceiverConstants(rgContext.cmd, ref shadowLight, shadowPassData.shadowData);
            });
        }
    }
}