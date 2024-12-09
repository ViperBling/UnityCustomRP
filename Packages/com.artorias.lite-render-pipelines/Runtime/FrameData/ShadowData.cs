using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace LiteRP.FrameData
{
    public class ShadowData : ContextItem
    {
        /// <summary>
        /// 是否开启主光源阴影
        /// </summary>
        public bool m_MainLightShadowEnable;

        /// <summary>
        /// 是否支持主光源阴影
        /// </summary>
        public bool m_SupportMainLightShadow;

        /// <summary>
        /// 主光源阴影贴图宽度
        /// </summary>
        public int m_MainLightShadowmapWidth;

        /// <summary>
        /// 主光源阴影贴图高度
        /// </summary>
        public int m_MainLightShadowmapHeight;

        /// <summary>
        /// 主光源阴影距离
        /// </summary>
        public float m_MainLightShadowDistance;

        /// <summary>
        /// 级联阴影级数
        /// </summary>
        public int m_MainLightShadowCascadesCount;

        /// <summary>
        /// 级联阴影划分
        /// </summary>
        public Vector3 m_MainLightShadowCascadesSplit;

        /// <summary>
        /// 级联阴影分级过渡
        /// </summary>
        public float m_MainLightShadowCascadeBorder;

        /// <summary>
        /// 软阴影
        /// </summary>
        public bool m_SupportSoftShadows;

        /// <summary>
        /// shadow map位数
        /// </summary>
        public int m_ShadowmapDepthBits;

        /// <summary>
        /// 阴影Bias
        /// </summary>
        public Vector4 m_MainLightShadowBias;

        /// <summary>
        /// shadowmap 分辨率
        /// </summary>
        public int m_MainLightShadowResolution;

        internal int m_MainLightTileShadowResolution;
        internal int m_MainLightRenderTargetWidth;
        internal int m_MainLightRenderTargetHeight;

        internal NativeArray<LightShadowCullingInfos> m_VisibleLightsShadowCullingInfos;

        public override void Reset()
        {
            m_MainLightShadowEnable = false;
            m_SupportMainLightShadow = false;
            m_MainLightShadowmapWidth = 0;
            m_MainLightShadowmapHeight = 0;
            m_MainLightShadowDistance = 0;
            m_MainLightShadowCascadesCount = 0;
            m_MainLightShadowCascadesSplit = Vector3.zero;
            m_MainLightShadowCascadeBorder = 0;
            m_SupportSoftShadows = false;
            m_ShadowmapDepthBits = 0;
            m_MainLightShadowBias = Vector4.zero;
            m_MainLightShadowResolution = 0;

            m_MainLightTileShadowResolution = 0;
            m_MainLightRenderTargetWidth = 0;
            m_MainLightRenderTargetHeight = 0;

            m_VisibleLightsShadowCullingInfos = default;
        }
    }
}