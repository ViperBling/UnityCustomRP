using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace LiteRP.FrameData
{
    public class LightData : ContextItem
    {
        /// <summary>
        /// Holds the main light index from the <c>VisibleLight</c> list returned by culling. If there's no main light in the scene, <c>mainLightIndex</c> is set to -1.
        /// The main light is the directional light assigned as Sun source in light settings or the brightest directional light.
        /// <seealso cref="CullingResults"/>
        /// </summary>
        public int m_MainLightIndex;

        /// <summary>
        /// The number of additional lights visible by the camera.
        /// </summary>
        public int m_AdditionalLightsCount;

        /// <summary>
        /// Maximum amount of lights that can be shaded per-object. This value only affects forward rendering.
        /// </summary>
        public int m_MaxPerObjectAdditionalLightsCount;

        /// <summary>
        /// List of visible lights returned by culling.
        /// </summary>
        public NativeArray<VisibleLight> m_VisibleLights;
        
        /// <summary>
        /// True if mixed lighting is supported.
        /// </summary>
        public bool m_SupportsMixedLighting;

        /// <summary>
        /// True if box projection is enabled for reflection probes.
        /// </summary>
        public bool m_ReflectionProbeBoxProjection;

        /// <summary>
        /// True if blending is enabled for reflection probes.
        /// </summary>
        public bool m_ReflectionProbeBlending;

        public override void Reset()
        {
            m_MainLightIndex = -1;
            m_AdditionalLightsCount = 0;
            m_MaxPerObjectAdditionalLightsCount = 0;
            m_VisibleLights = default;
            m_SupportsMixedLighting = false;
            m_ReflectionProbeBoxProjection = false;
            m_ReflectionProbeBlending = false;
        }
    }
}