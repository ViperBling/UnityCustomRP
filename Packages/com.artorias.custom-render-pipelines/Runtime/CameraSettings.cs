using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP
{
    [Serializable]
    public class CameraSettings
    {
        public bool m_CopyColor = true, m_CopyDepth = true;
        public int m_RenderingLayerMask = -1;
        public bool m_MaskLights = false;
        
        public enum RenderScaleMode { Inherit, Multiply, Override }
        public RenderScaleMode m_RenderScaleMode = RenderScaleMode.Inherit;
        
        [Range(CameraRenderer.m_RenderScaleMin, CameraRenderer.m_RenderScaleMax)]
        public float m_RenderScale = 1f;
        
        public bool m_KeepAlpha = false;
        
        [Serializable]
        public struct FinalBlendMode
        {
            public BlendMode m_Source, m_Destination;
        }
        public FinalBlendMode m_FinalBlendMode = new()
        {
            m_Source = BlendMode.One,
            m_Destination = BlendMode.Zero
        };
        
        public float GetRenderScale(float scale) =>
            m_RenderScaleMode == RenderScaleMode.Inherit ? scale :
            m_RenderScaleMode == RenderScaleMode.Override ? m_RenderScale :
            scale * m_RenderScale;
    }
}