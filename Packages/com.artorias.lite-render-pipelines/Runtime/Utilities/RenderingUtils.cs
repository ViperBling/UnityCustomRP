using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace LiteRP
{
    public static class RenderingUtils
    {
        private static Dictionary<RenderTextureFormat, bool> m_RenderTextureFormatSupport =
            new Dictionary<RenderTextureFormat, bool>();
        
        static Material s_ErrorMaterial;
        
        static Material errorMaterial
        {
            get
            {
                if (s_ErrorMaterial == null)
                {
                    try
                    {
                        s_ErrorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
                    }
                    catch {}
                }
                return s_ErrorMaterial;
            }
        }

        internal static void ClearSystemInfoCache()
        {
            m_RenderTextureFormatSupport.Clear();
        }

        /// <summary>
        /// 运行时RT格式检查
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static bool SupportsRenderTextureFormat(RenderTextureFormat format)
        {
            if (!m_RenderTextureFormatSupport.TryGetValue(format, out var support))
            {
                support = SystemInfo.SupportsRenderTextureFormat(format);
                m_RenderTextureFormatSupport.Add(format, support);
            }

            return support;
        }
        
        internal static bool RTHandleNeedReAllocate(RTHandle handle, in TextureDesc descriptor, bool scaled)
        {
            if (handle == null || handle.rt == null) return true;
            if (handle.useScaling != scaled) return true;
            if (!scaled && (handle.rt.width != descriptor.width || handle.rt.height != descriptor.height)) return true;
            
            return
                (DepthBits)handle.rt.descriptor.depthBufferBits != descriptor.depthBufferBits ||
                (handle.rt.descriptor.depthBufferBits == (int)DepthBits.None && handle.rt.descriptor.graphicsFormat != descriptor.colorFormat) ||
                handle.rt.descriptor.dimension != descriptor.dimension ||
                handle.rt.descriptor.enableRandomWrite != descriptor.enableRandomWrite ||
                handle.rt.descriptor.useMipMap != descriptor.useMipMap ||
                handle.rt.descriptor.autoGenerateMips != descriptor.autoGenerateMips ||
                (MSAASamples)handle.rt.descriptor.msaaSamples != descriptor.msaaSamples ||
                handle.rt.descriptor.bindMS != descriptor.bindTextureMS ||
                handle.rt.descriptor.useDynamicScale != descriptor.useDynamicScale ||
                handle.rt.descriptor.memoryless != descriptor.memoryless ||
                handle.rt.filterMode != descriptor.filterMode ||
                handle.rt.wrapMode != descriptor.wrapMode ||
                handle.rt.anisoLevel != descriptor.anisoLevel ||
                handle.rt.mipMapBias != descriptor.mipMapBias ||
                handle.name != descriptor.name;
        }
    }
}