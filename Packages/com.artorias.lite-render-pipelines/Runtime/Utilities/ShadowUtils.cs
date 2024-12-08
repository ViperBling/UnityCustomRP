using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace LiteRP
{
    public struct ShadowSliceData
    {
        /// <summary>
        /// The view matrix.
        /// </summary>
        public Matrix4x4 viewMatrix;

        /// <summary>
        /// The projection matrix.
        /// </summary>
        public Matrix4x4 projectionMatrix;

        /// <summary>
        /// The shadow transform matrix.
        /// </summary>
        public Matrix4x4 shadowTransform;

        /// <summary>
        /// The X offset to the shadow map.
        /// </summary>
        public int offsetX;

        /// <summary>
        /// The Y offset to the shadow map.
        /// </summary>
        public int offsetY;

        /// <summary>
        /// The maximum tile resolution in an Atlas.
        /// </summary>
        public int resolution;

        /// <summary>
        /// The shadow split data containing culling information.
        /// </summary>
        public ShadowSplitData splitData;

        /// <summary>
        /// Clears and resets the data.
        /// </summary>
        public void Clear()
        {
            viewMatrix = Matrix4x4.identity;
            projectionMatrix = Matrix4x4.identity;
            shadowTransform = Matrix4x4.identity;
            offsetX = offsetY = 0;
            resolution = 1024;
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
            // TextureDesc shadowDesc = RTHandleR
            
            // return 
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
    }
}