using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace LiteRP
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
        public int mainLightShadowmapWidth;
        
        /// <summary>
        /// 主光源阴影贴图高度
        /// </summary>
        public int mainLightShadowmapHeight;

        /// <summary>
        /// 主光源阴影距离
        /// </summary>
        public float mainLightShadowDistance;
        
        /// <summary>
        /// 级联阴影级数
        /// </summary>
        public int mainLightShadowCascadesCount;
        
        /// <summary>
        /// 级联阴影划分
        /// </summary>
        public Vector3 mainLightShadowCascadesSplit;

        /// <summary>
        /// 级联阴影分级过渡
        /// </summary>
        public float mainLightShadowCascadeBorder;

        /// <summary>
        /// 软阴影
        /// </summary>
        public bool supportSoftShadows;
        
        /// <summary>
        /// shadow map位数
        /// </summary>
        public int shadowmapDepthBits;

        /// <summary>
        /// 阴影Bias
        /// </summary>
        public Vector4 mainLightShadowBias;
        
        /// <summary>
        /// shadowmap 分辨率
        /// </summary>
        public int mainLightShadowResolution;

        internal int mainLightTileShadowResolution;
        internal int mainLightRenderTargetWidth;
        internal int mainLightRenderTargetHeight;
        
        // internal NativeArray<LightShadowCasterCullingInfo>
        
        public override void Reset()
        {
            throw new System.NotImplementedException();
        }
    }
}