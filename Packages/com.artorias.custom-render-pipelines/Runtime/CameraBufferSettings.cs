using System;
using UnityEngine;

namespace CustomRP
{
    [Serializable]
    public class CameraBufferSettings
    {
        public bool m_AllowHDR;
        public bool m_CopyColor, m_CopyColorReflection, m_CopyDepth, m_CopyDepthReflection;
        
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