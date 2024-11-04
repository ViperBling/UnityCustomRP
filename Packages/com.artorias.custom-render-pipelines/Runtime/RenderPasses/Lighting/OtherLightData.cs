using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP
{
    partial class LightingPass
    {
        [StructLayout(LayoutKind.Sequential)]
        struct OtherLightData
        {
            public const int m_Stride = 4 * 4 * 5;
            public Vector4 m_Color, m_Position, m_DirectionAndMask, m_SpotAngle, m_ShadowData;

            public static OtherLightData CreatePointLight(ref VisibleLight visibleLight, Light light, Vector4 shadowData)
            {
                OtherLightData data;
                data.m_Color = visibleLight.finalColor;
                Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
                position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
                data.m_Position = position;
                data.m_SpotAngle = new Vector4(0f, 1f);
                data.m_DirectionAndMask = Vector4.zero;
                data.m_DirectionAndMask.w = light.renderingLayerMask.ReinterpretAsFloat();
                data.m_ShadowData = shadowData;
                return data;
            }
            
            public static OtherLightData CreateSpotLight(ref VisibleLight visibleLight, Light light, Vector4 shadowData)
            {
                OtherLightData data;
                data.m_Color = visibleLight.finalColor;
                Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
                position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
                data.m_Position = position;
                data.m_DirectionAndMask = -visibleLight.localToWorldMatrix.GetColumn(2);
                data.m_DirectionAndMask.w = light.renderingLayerMask.ReinterpretAsFloat();
                
                float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
                float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
                float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
                data.m_SpotAngle = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
                data.m_ShadowData = shadowData;
                
                return data;
            }
        }
    }
}