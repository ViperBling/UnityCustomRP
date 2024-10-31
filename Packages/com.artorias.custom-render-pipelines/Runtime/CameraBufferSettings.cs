using System;
using UnityEngine;

namespace CustomRP
{
    [Serializable]
    public struct CameraBufferSettings
    {
        public bool m_AllowHDR;
        public bool m_IsCopyColor, m_IsCopyColorReflection, m_IsCopyDepth, m_IsCopyDepthReflection;
        
        [Range(CameraRenderer.m_RenderScaleMin, CameraRenderer.m_RenderScaleMax)]
        public float m_RenderScale;
        
        public enum BicubicRescalingMode
        {
            Off,
            UpOnly,
            UpAndDown
        }
        public BicubicRescalingMode m_BicubicRescaling;
    }
}