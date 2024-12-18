﻿using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

using LiteRP.FrameData;
using LiteRP.AdditionalData;

namespace LiteRP
{
    public struct ShadowSliceData
    {
        /// <summary>
        /// The view matrix.
        /// </summary>
        public Matrix4x4 m_ViewMatrix;

        /// <summary>
        /// The projection matrix.
        /// </summary>
        public Matrix4x4 m_ProjectionMatrix;

        /// <summary>
        /// The shadow transform matrix.
        /// </summary>
        public Matrix4x4 m_ShadowTransform;

        /// <summary>
        /// The X offset to the shadow map.
        /// </summary>
        public int m_OffsetX;

        /// <summary>
        /// The Y offset to the shadow map.
        /// </summary>
        public int m_OffsetY;

        /// <summary>
        /// The maximum tile resolution in an Atlas.
        /// </summary>
        public int m_Resolution;

        /// <summary>
        /// The shadow split data containing culling information.
        /// </summary>
        public ShadowSplitData m_SplitData;

        /// <summary>
        /// Clears and resets the data.
        /// </summary>
        public void Clear()
        {
            m_ViewMatrix = Matrix4x4.identity;
            m_ProjectionMatrix = Matrix4x4.identity;
            m_ShadowTransform = Matrix4x4.identity;
            m_OffsetX = m_OffsetY = 0;
            m_Resolution = 1024;
        }
    }

    public struct LightShadowCullingInfos
    {
        public NativeArray<ShadowSliceData> shadowSlices;
        public uint slicesValidMask;
        
        public readonly bool IsSliceValid(int i) => (slicesValidMask & (1u << i)) != 0; 
    }

    public static class ShadowUtils
    {
        internal static readonly bool m_ForceShadowPointSampling;

        static ShadowUtils()
        {
            m_ForceShadowPointSampling = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal &&
                                         GraphicsSettings.HasShaderDefine(Graphics.activeTier, BuiltinShaderDefine.UNITY_METAL_SHADOWS_USE_POINT_FILTERING);
        }
        
        public static bool ExtractDirectionalLightMatrix(ref CullingResults cullResults, ShadowData shadowData, int shadowLightIndex, int cascadeIndex, int shadowmapWidth, int shadowmapHeight, int shadowResolution, float shadowNearPlane, out Vector4 cascadeSplitDistance, out ShadowSliceData shadowSliceData)
        {
            bool success = cullResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                shadowLightIndex,
                cascadeIndex, shadowData.m_MainLightShadowCascadesCount, shadowData.m_MainLightShadowCascadesSplit, shadowResolution, shadowNearPlane, 
                out shadowSliceData.m_ViewMatrix, out shadowSliceData.m_ProjectionMatrix, out shadowSliceData.m_SplitData);

            cascadeSplitDistance = shadowSliceData.m_SplitData.cullingSphere;
            shadowSliceData.m_OffsetX = (cascadeIndex % 2) * shadowResolution;
            shadowSliceData.m_OffsetY = (cascadeIndex / 2) * shadowResolution;
            shadowSliceData.m_Resolution = shadowResolution;
            shadowSliceData.m_ShadowTransform = GetShadowTransform(shadowSliceData.m_ProjectionMatrix, shadowSliceData.m_ViewMatrix);

            // It is the culling sphere radius multiplier for shadow cascade blending
            // If this is less than 1.0, then it will begin to cull castors across cascades
            shadowSliceData.m_SplitData.shadowCascadeBlendCullingFactor = 1.0f;

            // If we have shadow cascades baked into the atlas we bake cascade transform
            // in each shadow matrix to save shader ALU and L/S
            if (shadowData.m_MainLightShadowCascadesCount > 1)
            {
                ApplySliceTransform(ref shadowSliceData, shadowmapWidth, shadowmapHeight);
            }

            return success;
        }
        
        public static void ApplySliceTransform(ref ShadowSliceData shadowSliceData, int atlasWidth, int atlasHeight)
        {
            Matrix4x4 sliceTransform = Matrix4x4.identity;
            float oneOverAtlasWidth = 1.0f / atlasWidth;
            float oneOverAtlasHeight = 1.0f / atlasHeight;
            sliceTransform.m00 = shadowSliceData.m_Resolution * oneOverAtlasWidth;
            sliceTransform.m11 = shadowSliceData.m_Resolution * oneOverAtlasHeight;
            sliceTransform.m03 = shadowSliceData.m_OffsetX * oneOverAtlasWidth;
            sliceTransform.m13 = shadowSliceData.m_OffsetY * oneOverAtlasHeight;

            // Apply shadow slice scale and offset
            shadowSliceData.m_ShadowTransform = sliceTransform * shadowSliceData.m_ShadowTransform;
        }
        
        static Matrix4x4 GetShadowTransform(Matrix4x4 proj, Matrix4x4 view)
        {
            // Currently CullResults ComputeDirectionalShadowMatricesAndCullingPrimitives doesn't
            // apply z reversal to projection matrix. We need to do it manually here.
            if (SystemInfo.usesReversedZBuffer)
            {
                proj.m20 = -proj.m20;
                proj.m21 = -proj.m21;
                proj.m22 = -proj.m22;
                proj.m23 = -proj.m23;
            }

            Matrix4x4 worldToShadow = proj * view;

            var textureScaleAndBias = Matrix4x4.identity;
            textureScaleAndBias.m00 = 0.5f;
            textureScaleAndBias.m11 = 0.5f;
            textureScaleAndBias.m22 = 0.5f;
            textureScaleAndBias.m03 = 0.5f;
            textureScaleAndBias.m23 = 0.5f;
            textureScaleAndBias.m13 = 0.5f;
            // textureScaleAndBias maps texture space coordinates from [-1,1] to [0,1]

            // Apply texture scale and offset to save a MAD in shader.
            return textureScaleAndBias * worldToShadow;
        }
        
        public static void CreateShadowAtlasAndCullShadowCasters(ShadowData shadowData, ref CullingResults cullingResults, ref ScriptableRenderContext context)
        {
            if (!shadowData.m_SupportMainLightShadow) return;
            
            shadowData.m_MainLightTileShadowResolution = ShadowUtils.GetMaxTileResolutionInAtlas(shadowData.m_MainLightShadowmapWidth, shadowData.m_MainLightShadowmapHeight, shadowData.m_MainLightShadowCascadesCount);
            shadowData.m_MainLightRenderTargetWidth = shadowData.m_MainLightShadowmapWidth;
            shadowData.m_MainLightRenderTargetHeight = (shadowData.m_MainLightShadowCascadesCount == 2) ? shadowData.m_MainLightShadowmapHeight >> 1 : shadowData.m_MainLightShadowmapHeight;

            shadowData.m_VisibleLightsShadowCullingInfos = ShadowUtils.CullShadowCasters(ref context, shadowData, ref cullingResults);
        }
        
        private static NativeArray<LightShadowCullingInfos> CullShadowCasters(ref ScriptableRenderContext context, ShadowData shadowData, ref CullingResults cullingResults)
        {
            ShadowCastersCullingInfos shadowCullingInfos;
            NativeArray<LightShadowCullingInfos> visibleLightsShadowCullingInfos;
            ComputeShadowCasterCullingInfos(shadowData, ref cullingResults, out shadowCullingInfos, out visibleLightsShadowCullingInfos);

            context.CullShadowCasters(cullingResults, shadowCullingInfos);

            return visibleLightsShadowCullingInfos;
        }
        
        private static void ComputeShadowCasterCullingInfos(ShadowData shadowData, ref CullingResults cullingResults, out ShadowCastersCullingInfos shadowCullingInfos, out NativeArray<LightShadowCullingInfos> visibleLightsShadowCullingInfos)
        {
            const int MaxShadowSplitCount = 6;

            NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
            NativeArray<ShadowSplitData> splitBuffer = new NativeArray<ShadowSplitData>(visibleLights.Length * MaxShadowSplitCount, Allocator.Temp);
            NativeArray<LightShadowCasterCullingInfo> perLightInfos = new NativeArray<LightShadowCasterCullingInfo>(visibleLights.Length, Allocator.Temp);
            visibleLightsShadowCullingInfos = new NativeArray<LightShadowCullingInfos>(visibleLights.Length, Allocator.Temp);

            int totalSplitCount = 0;
            int splitBufferOffset = 0;

            for (int lightIndex = 0; lightIndex < visibleLights.Length; ++lightIndex)
            {
                ref VisibleLight visibleLight = ref cullingResults.visibleLights.UnsafeElementAt(lightIndex);
                if(visibleLight.lightType != LightType.Directional)
                    continue;

                NativeArray<ShadowSliceData> slices = default;
                uint slicesValidMask = 0;
                
                if (!shadowData.m_SupportMainLightShadow)
                    continue;

                int splitCount = shadowData.m_MainLightShadowCascadesCount;
                int renderTargetWidth = shadowData.m_MainLightRenderTargetWidth;
                int renderTargetHeight = shadowData.m_MainLightRenderTargetHeight;
                int shadowResolution = shadowData.m_MainLightTileShadowResolution;

                slices = new NativeArray<ShadowSliceData>(splitCount, Allocator.Temp);
                slicesValidMask = 0;

                for (int i = 0; i < splitCount; ++i)
                {
                    ShadowSliceData slice = default;
                    bool isValid = ShadowUtils.ExtractDirectionalLightMatrix(ref cullingResults, shadowData,
                        lightIndex, i, renderTargetWidth, renderTargetHeight, shadowResolution, visibleLight.light.shadowNearPlane,
                        out _, // Vector4 cascadeSplitDistance. This is basically just the culling sphere which is already present in ShadowSplitData
                        out slice);

                    if (isValid)
                        slicesValidMask |= 1u << i;

                    slices[i] = slice;
                    splitBuffer[splitBufferOffset + i] = slice.m_SplitData;
                }
                
                LightShadowCullingInfos infos = default;
                infos.shadowSlices = slices;
                infos.slicesValidMask = slicesValidMask;

                visibleLightsShadowCullingInfos[lightIndex] = infos;
                perLightInfos[lightIndex] = new LightShadowCasterCullingInfo
                {
                    splitRange = new RangeInt(splitBufferOffset, slices.Length),
                    projectionType = BatchCullingProjectionType.Orthographic,
                };
                splitBufferOffset += slices.Length;
                totalSplitCount += slices.Length;
            }

            shadowCullingInfos = default;
            shadowCullingInfos.splitBuffer = splitBuffer.GetSubArray(0, totalSplitCount);
            shadowCullingInfos.perLightInfos = perLightInfos;
        }
        
        public static Vector3 GetMainLightCascadeSplit(int mainLightShadowCascadesCount, LiteRPAsset asset)
        {
            switch (mainLightShadowCascadesCount)
            {
                case 1:  return new Vector3(1.0f, 0.0f, 0.0f);
                case 2:  return new Vector3(asset.mainLightCascade2Split, 1.0f, 0.0f);
                case 3:  return asset.mainLightCascade3Split;
                default: return asset.mainLightCascade4Split;
            }
        }
        
        public static int GetMaxTileResolutionInAtlas(int atlasWidth, int atlasHeight, int tileCount)
        {
            int resolution = Mathf.Min(atlasWidth, atlasHeight);
            int currentTileCount = atlasWidth / resolution * atlasHeight / resolution;
            while (currentTileCount < tileCount)
            {
                resolution = resolution >> 1;
                currentTileCount = atlasWidth / resolution * atlasHeight / resolution;
            }
            return resolution;
        }

        public static bool ShadowRTReAllocateIfNeed(ref RTHandle handle, int width, int height, int bits, int anisoLevel = 1, float mipMapBias = 0, string name = "")
        {
            if (ShadowRTNeedsReAllocate(handle, width, height, bits, anisoLevel, mipMapBias, name))
            {
                handle?.Release();
                handle = AllocateShadowRT(width, height, bits, anisoLevel, mipMapBias, name);
                return true;
            }
            return false;
        }

        public static bool ShadowRTNeedsReAllocate(RTHandle handle, int width, int height, int bits, int anisoLevel, float mipMapBias, string name)
        {
            if (handle == null || handle.rt == null) return true;
            
            var descriptor = GetTemporaryShadowTextureDescriptor(width, height, bits);
            if (m_ForceShadowPointSampling)
            {
                if (handle.rt.filterMode != FilterMode.Point) return true;
            }
            else
            {
                if (handle.rt.filterMode != FilterMode.Bilinear) return true;
            }
            TextureDesc shadowDesc = RTHandleResourcePool.CreateTextureDesc(descriptor, TextureSizeMode.Explicit, anisoLevel, mipMapBias, m_ForceShadowPointSampling ? FilterMode.Point : FilterMode.Bilinear, TextureWrapMode.Clamp, name);
            return RenderingUtils.RTHandleNeedReAllocate(handle, shadowDesc, false);
        }
        
        public static RTHandle AllocateShadowRT(int width, int height, int bits, int anisoLevel = 1, float mipMapBias = 0, string name = "")
        {
            var rtd = GetTemporaryShadowTextureDescriptor(width, height, bits);
            return RTHandles.Alloc(rtd, m_ForceShadowPointSampling ? FilterMode.Point : FilterMode.Bilinear, TextureWrapMode.Clamp, isShadowMap: true, name: name);
        }
        
        private static RenderTextureDescriptor GetTemporaryShadowTextureDescriptor(int width, int height, int bits)
        {
            var format = GraphicsFormatUtility.GetDepthStencilFormat(bits, 0);
            RenderTextureDescriptor rtd = new RenderTextureDescriptor(width, height, GraphicsFormat.None, format);
            rtd.shadowSamplingMode = RenderingUtils.SupportsRenderTextureFormat(RenderTextureFormat.Shadowmap) ? ShadowSamplingMode.CompareDepths : ShadowSamplingMode.None;
            return rtd;
        }
        
        public static Vector4 GeMainLightShadowBias(ref VisibleLight shadowLight, Vector4 mainLightShadowBias, bool supportsSoftShadows, Matrix4x4 lightProjectionMatrix, int shadowResolution)
        {
            float frustumSize;
            if (shadowLight.lightType == LightType.Directional)
            {
                // Frustum size is guaranteed to be a cube as we wrap shadow frustum around a sphere
                frustumSize = 2.0f / lightProjectionMatrix.m00;
            }
            else
            {
                Debug.LogWarning("Only directional shadow casters are supported in Lite Render Pipeline");
                frustumSize = 0.0f;
            }

            // depth and normal bias scale is in shadowmap texel size in world space
            float texelSize = frustumSize / shadowResolution;
            float depthBias = -mainLightShadowBias.x * texelSize;
            float normalBias = -mainLightShadowBias.y * texelSize;
            
            if (supportsSoftShadows && shadowLight.light.shadows == LightShadows.Soft)
            {
                SoftShadowQuality softShadowQuality = SoftShadowQuality.Medium;
                if (shadowLight.light.TryGetComponent(out AdditionalLightData additionalLightData))
                    softShadowQuality = additionalLightData.softShadowQuality;

                // TODO: depth and normal bias assume sample is no more than 1 texel away from shadowmap
                // This is not true with PCF. Ideally we need to do either
                // cone base bias (based on distance to center sample)
                // or receiver place bias based on derivatives.
                // For now we scale it by the PCF kernel size of non-mobile platforms (5x5)
                float kernelRadius = 2.5f;

                switch (softShadowQuality)
                {
                    case SoftShadowQuality.High: kernelRadius = 3.5f; break; // 7x7
                    case SoftShadowQuality.Medium: kernelRadius = 2.5f; break; // 5x5
                    case SoftShadowQuality.Low: kernelRadius = 1.5f; break; // 3x3
                    default: break;
                }

                depthBias *= kernelRadius;
                normalBias *= kernelRadius;
            }

            return new Vector4(depthBias, normalBias, 0.0f, 0.0f);
        }

        internal static float SoftShadowQualityToShaderProperty(Light light, bool softShadowEnabled)
        {
            float softShadows = softShadowEnabled ? 1.0f : 0.0f;
            var softShadowQuality = LiteRPUtils.s_LiteRPAsset?.softShadowQuality;
            softShadows *= Math.Max((int)softShadowQuality, (int)SoftShadowQuality.Low);
            
            return softShadows;
        }

        internal static void GetScaleAndBiasForLinearDistanceFade(float fadeDistance, float border, out float scale, out float bias)
        {
            // To avoid division from zero
            // This values ensure that fade within cascade will be 0 and outside 1
            if (border < 0.0001f)
            {
                float multiplier = 1000f; // To avoid blending if difference is in fractions
                scale = multiplier;
                bias = -fadeDistance * multiplier;
                return;
            }
            
            border = 1 - border;
            border *= border;

            // Fade with distance calculation is just a linear fade from 90% of fade distance to fade distance. 90% arbitrarily chosen but should work well enough.
            float distanceFadeNear = border * fadeDistance;
            scale = 1.0f / (fadeDistance - distanceFadeNear);
            bias = -distanceFadeNear / (fadeDistance - distanceFadeNear);
        }
    }
}