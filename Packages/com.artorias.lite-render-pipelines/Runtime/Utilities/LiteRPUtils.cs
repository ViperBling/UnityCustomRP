using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace LiteRP
{
    // HDR颜色缓冲区精度定义
    public enum HDRColorBufferPrecision
    {
        /// <summary> Typically R11G11B10f for faster rendering. Recommend for mobile.
        /// R11G11B10f can cause a subtle blue/yellow banding in some rare cases due to lower precision of the blue component.</summary>
        [Tooltip("Use 32-bits per pixel for HDR rendering.")]
        _32Bits,
        /// <summary>Typically R16G16B16A16f for better quality. Can reduce banding at the cost of memory and performance.</summary>
        [Tooltip("Use 64-bits per pixel for HDR rendering.")]
        _64Bits,
    }
    
    // 对NativeArrary的Unsafe扩展
    static class NativeArrayExtensions
    {
        /// <summary>
        /// IMPORTANT: Make sure you do not write to the value! There are no checks for this!
        /// </summary>
        public static unsafe ref T UnsafeElementAt<T>(this NativeArray<T> array, int index) where T : struct
        {
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafeReadOnlyPtr(), index);
        }

        public static unsafe ref T UnsafeElementAtMutable<T>(this NativeArray<T> array, int index) where T : struct
        {
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafePtr(), index);
        }
    }

    public static class LiteRPUtils
    {
        // 全局管线Asset
        internal static LiteRPAsset s_LiteRPAsset
        {
            get => GraphicsSettings.currentRenderPipeline as LiteRPAsset;
        }

        internal static RenderTextureDescriptor CreateRenderTextureDescriptor(Camera camera, int msaaSamples)
        {
            RenderTextureDescriptor desc;
            if (camera.targetTexture == null)
            {
                desc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
                desc.width = camera.pixelWidth;
                desc.height = camera.pixelHeight;
                desc.graphicsFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
                desc.depthBufferBits = 32;
                desc.msaaSamples = msaaSamples;
                desc.sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            }
            else
            {
                desc = camera.targetTexture.descriptor;
                desc.msaaSamples = msaaSamples;
                desc.width = camera.pixelWidth;
                desc.height = camera.pixelHeight;

                if (camera.cameraType == CameraType.SceneView)
                {
                    desc.graphicsFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
                }
            }
            desc.width = Mathf.Max(1, desc.width);
            desc.height = Mathf.Max(1, desc.height);

            desc.enableRandomWrite = false;
            desc.bindMS = false;
            desc.useDynamicScale = camera.allowDynamicResolution;
            desc.msaaSamples = SystemInfo.GetRenderTextureSupportedMSAASampleCount(desc);
            if (!SystemInfo.supportsStoreAndResolveAction)
            {
                desc.msaaSamples = 1;
            }

            return desc;
        }
        
        
    }
}