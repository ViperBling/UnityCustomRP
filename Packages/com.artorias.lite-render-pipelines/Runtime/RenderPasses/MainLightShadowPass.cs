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
            // ShadowUtils.Sha
        }
    }
}