using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP
{
    partial class LightingPass
    {
        [StructLayout(LayoutKind.Sequential)]
        struct DirectionalLightData
        {
            public const int stride = 4 * 4 * 3;
            public Vector4 m_Color, m_DirectionAndMask, m_ShadowData;
            
            public DirectionalLightData(
                ref VisibleLight visibleLight, Light light, Vector4 shadowData)
            {
                m_Color = visibleLight.finalColor;
                m_DirectionAndMask = -visibleLight.localToWorldMatrix.GetColumn(2);
                m_DirectionAndMask.w = light.renderingLayerMask.ReinterpretAsFloat();
                m_ShadowData = shadowData;
            }
        }
    }
}